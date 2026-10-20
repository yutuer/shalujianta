#Requires -Version 5.1
<#
.SYNOPSIS
    校验 tasks.md 与 README.md 里的任务统计表，是否与实际任务行一致。

.DESCRIPTION
    本项目的历史教训：tasks.md 的「六、任务统计」表曾长期失真
    （自称 61 任务 / 23 完成，实际 87 行 / 33 完成），因为改了任务行却没人回头改表。
    本脚本把「统计」变成可自动校验的东西，任何一处漂移都会当场暴露。

    校验四处：
      1. tasks.md  一~五节 任务行实算  vs  tasks.md  「六、任务统计」表
      2. tasks.md  一~五节 任务行实算  vs  README.md「5. 当前进度基线」表
      3. docs/开发计划.md 需求池总表状态  vs  逐项展开标题状态  vs  「6. 统计」表
      4. 任务行里是否存在一行含多个状态标记（可能是笔误）

.PARAMETER RepoRoot
    仓库根目录。默认为本脚本所在目录的上一级。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\check-task-stats.ps1
    退出码 0 = 一致；非 0 = 有漂移，需要更新表格。
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

# 解析仓库根目录：优先用参数，否则按脚本自身位置向上退一级
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepoRoot = Split-Path -Parent $scriptDir
}

# ============================ 维护须知 ============================
# ⚠️ 本文件必须以「UTF-8 with BOM」保存！
#    PowerShell 5.1 会把无 BOM 的 .ps1 当 ANSI 读，中文与 emoji 全部乱码，
#    表现为 "Unexpected token" 之类的解析错误。用 VS Code 保存时请选
#    "UTF-8 with BOM"，或用记事本「另存为 → UTF-8」。
#
#    emoji 一律用码点构造，不要在源码里直接写，避免再次依赖 BOM：
#      U+2705 ✅   U+1F504 🔄（超出 BMP，必须 ConvertFromUtf32 转代理对）
#      U+2B1C ⬜   U+274C ❌
# ================================================================

# 用码点构造 emoji，避免脚本文件必须带 BOM 才能被 PowerShell 5.1 正确解析
# 注意：U+1F504 超出 BMP，必须用 ConvertFromUtf32 转成代理对，不能直接 [char]0x1F504
$Done  = [char]::ConvertFromUtf32(0x2705)   # ✅
$Doing = [char]::ConvertFromUtf32(0x1F504)  # 🔄
$Todo  = [char]::ConvertFromUtf32(0x2B1C)   # ⬜
$Block = [char]::ConvertFromUtf32(0x274C)   # ❌
$Markers = @($Done, $Doing, $Todo, $Block)

$tasksPath = Join-Path $RepoRoot 'tasks.md'
$readmePath = Join-Path $RepoRoot 'README.md'

foreach ($p in @($tasksPath, $readmePath)) {
    if (-not (Test-Path $p)) {
        Write-Error "找不到文件: $p"
        exit 2
    }
}

function Get-SectionRows {
    <#
      取出指定小节内的表格数据行，返回 PSCustomObject 列表。
      每项: Section(小节名) / Row(原始行) / Status(状态列文本)
    #>
    param([string[]]$Lines, [string]$StartPattern, [string]$EndPattern, [int]$StatusIndex = 2)

    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $StartPattern) { $start = $i; break }
    }
    if ($start -lt 0) { return @() }

    $end = $Lines.Count
    for ($i = $start + 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $EndPattern) { $end = $i; break }
    }

    $rows = @()
    for ($i = $start + 1; $i -lt $end; $i++) {
        $line = $Lines[$i]
        if (-not $line.StartsWith('|')) { continue }
        $cells = $line.Split('|')
        if ($cells.Count -le $StatusIndex) { continue }
        $status = $cells[$StatusIndex]   # 状态列位置由调用方指定（tasks.md=2，开发计划总表=6）
        $hasMarker = $false
        foreach ($m in $Markers) { if ($status.Contains($m)) { $hasMarker = $true; break } }
        if (-not $hasMarker) { continue }
        $rows += [PSCustomObject]@{ Section = $Lines[$start]; Row = $line; Status = $status }
    }
    return $rows
}

function Get-Key {
    # 归一化分类名：去掉空白与括号等非字类字符，取前 3 字做匹配键
    param([string]$Name)
    $clean = ($Name -replace '[^0-9A-Za-z\u4e00-\u9fff]', '')
    if ($clean.Length -gt 3) { $clean = $clean.Substring(0, 3) }
    return $clean
}

function Get-ExpectedTable {
    # 从 markdown 里读出 "分类 | 总数 | 完成 | 进行 | 待开始" 形态的统计表
    param([string[]]$Lines, [string]$StartPattern, [string]$EndPattern)

    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $StartPattern) { $start = $i; break }
    }
    if ($start -lt 0) { return @{} }

    $end = $Lines.Count
    for ($i = $start + 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $EndPattern) { $end = $i; break }
    }

    $map = @{}
    for ($i = $start + 1; $i -lt $end; $i++) {
        $line = $Lines[$i]
        if (-not $line.StartsWith('|')) { continue }
        $cells = $line.Split('|')
        if ($cells.Count -lt 6) { continue }
        $name = $cells[1].Trim()
        if ($name -eq '' ) { continue }
        $nums = @()
        foreach ($idx in 2,3,4,5) {
            $v = ($cells[$idx] -replace '[^0-9]', '')
            if ($v -eq '') { $nums += $null } else { $nums += [int]$v }
        }
        # 表头与分隔行没有数字，靠这一点识别（不要按名字过滤，否则会误伤 Bug修复 这类分类名）
        if ($null -eq $nums[0]) { continue }
        $key = Get-Key $name
        $map[$key] = [PSCustomObject]@{ Name = $name; Total = $nums[0]; Done = $nums[1]; Doing = $nums[2]; Todo = $nums[3] }
    }
    return $map
}

function Compare-Table {
    param($Expected, $Actual, [string]$Label)

    $problems = @()
    foreach ($key in $Expected.Keys) {
        if (-not $Actual.ContainsKey($key)) {
            $problems += "$Label 缺少分类 [$($Expected[$key].Name)]"
            continue
        }
        $e = $Expected[$key]; $a = $Actual[$key]
        if ($e.Total -ne $a.Total) { $problems += "$Label [$($e.Name)] 总数 表内=$($a.Total) 实算=$($e.Total)" }
        if ($e.Done  -ne $a.Done)  { $problems += "$Label [$($e.Name)] 完成 表内=$($a.Done) 实算=$($e.Done)" }
        if ($e.Doing -ne $a.Doing) { $problems += "$Label [$($e.Name)] 进行 表内=$($a.Doing) 实算=$($e.Doing)" }
        if ($e.Todo  -ne $a.Todo)  { $problems += "$Label [$($e.Name)] 待开 表内=$($a.Todo) 实算=$($e.Todo)" }
    }
    return $problems
}

# ---------- 1. 实算 tasks.md 一~五节 ----------
$taskLines = Get-Content $tasksPath -Encoding UTF8
$sections = @(
    @{ Name = '高优先级'; Start = '^##\s*一、'; End = '^##\s*二、' },
    @{ Name = '中优先级'; Start = '^##\s*二、'; End = '^##\s*三、' },
    @{ Name = '低优先级'; Start = '^##\s*三、'; End = '^##\s*四、' },
    @{ Name = '商店系统'; Start = '^##\s*四、'; End = '^##\s*五、' },
    @{ Name = 'Bug修复'; Start = '^##\s*五、'; End = '^##\s*六、' }
)

$actual = @{}
$grandTotal = 0; $grandDone = 0; $grandDoing = 0; $grandTodo = 0; $grandBlock = 0
$multiMarker = @()

foreach ($s in $sections) {
    $rows = Get-SectionRows -Lines $taskLines -StartPattern $s.Start -EndPattern $s.End
    $d = 0; $p = 0; $t = 0; $b = 0
    foreach ($r in $rows) {
        $found = @()
        foreach ($m in $Markers) { if ($r.Status.Contains($m)) { $found += $m } }
        if ($found.Count -gt 1) { $multiMarker += $r.Row.Trim() }
        switch ($found[0]) {
            $Done  { $d++ }
            $Doing { $p++ }
            $Todo  { $t++ }
            $Block { $b++ }
        }
    }
    $actual[(Get-Key $s.Name)] = [PSCustomObject]@{
        Name = $s.Name; Total = $d + $p + $t + $b; Done = $d; Doing = $p; Todo = $t; Block = $b
    }
    $grandTotal += $d + $p + $t + $b; $grandDone += $d; $grandDoing += $p; $grandTodo += $t; $grandBlock += $b
    '{0,-10} 总数={1,3}  完成={2,3}  进行={3,3}  待开={4,3}' -f $s.Name, ($d+$p+$t+$b), $d, $p, $t
}
'-' * 52
'{0,-10} 总数={1,3}  完成={2,3}  进行={3,3}  待开={4,3}  阻塞={5,3}' -f '合计', $grandTotal, $grandDone, $grandDoing, $grandTodo, $grandBlock
''

$problems = @()

# ---------- 2. 对比 tasks.md 自己的统计表 ----------
$tasksTable = Get-ExpectedTable -Lines $taskLines -StartPattern '^##\s*六、任务统计' -EndPattern '^##\s*七、'
if ($tasksTable.Count -eq 0) {
    $problems += 'tasks.md 里找不到「六、任务统计」表'
} else {
    $problems += Compare-Table -Expected $actual -Actual $tasksTable -Label 'tasks.md 统计表'
    # 合计行单独核对
    $totalRow = $null
    foreach ($k in $tasksTable.Keys) { if ($tasksTable[$k].Name -match '合计|总计') { $totalRow = $tasksTable[$k] } }
    if ($null -eq $totalRow) {
        $problems += 'tasks.md 统计表里找不到合计/总计行'
    } else {
        if ($totalRow.Total -ne $grandTotal) { $problems += "tasks.md 合计行 总数 表内=$($totalRow.Total) 实算=$grandTotal" }
        if ($totalRow.Done  -ne $grandDone)  { $problems += "tasks.md 合计行 完成 表内=$($totalRow.Done) 实算=$grandDone" }
        if ($totalRow.Doing -ne $grandDoing) { $problems += "tasks.md 合计行 进行 表内=$($totalRow.Doing) 实算=$grandDoing" }
        if ($totalRow.Todo  -ne $grandTodo)  { $problems += "tasks.md 合计行 待开 表内=$($totalRow.Todo) 实算=$grandTodo" }
    }
}

# ---------- 3. 对比 README 的进度基线表 ----------
if (Test-Path $readmePath) {
    $readmeLines = Get-Content $readmePath -Encoding UTF8
    $readmeTable = Get-ExpectedTable -Lines $readmeLines -StartPattern '^##\s*5\.' -EndPattern '^##\s*6\.'
    if ($readmeTable.Count -eq 0) {
        $problems += 'README.md 里找不到「5. 当前进度基线」表'
    } else {
        $problems += Compare-Table -Expected $actual -Actual $readmeTable -Label 'README 进度基线'
        $totalRow = $null
        foreach ($k in $readmeTable.Keys) { if ($readmeTable[$k].Name -match '合计|总计') { $totalRow = $readmeTable[$k] } }
        if ($null -ne $totalRow) {
            if ($totalRow.Total -ne $grandTotal) { $problems += "README 合计行 总数 表内=$($totalRow.Total) 实算=$grandTotal" }
            if ($totalRow.Done  -ne $grandDone)  { $problems += "README 合计行 完成 表内=$($totalRow.Done) 实算=$grandDone" }
            if ($totalRow.Doing -ne $grandDoing) { $problems += "README 合计行 进行 表内=$($totalRow.Doing) 实算=$grandDoing" }
            if ($totalRow.Todo  -ne $grandTodo)  { $problems += "README 合计行 待开 表内=$($totalRow.Todo) 实算=$grandTodo" }
        }
    }
}

# ---------- 4. 开发计划（docs/开发计划.md）三处互校 ----------
#    1) 需求池总表的状态列
#    2) 逐项展开标题里的状态
#    3) 第 6 节统计表的数字
#    改一处状态必须三处同步，改漏任何一处都会当场报出来。
$planPath = Join-Path $RepoRoot 'docs\开发计划.md'
if (Test-Path $planPath) {
    $planLines = Get-Content $planPath -Encoding UTF8

    # 4a. 需求池总表实算（状态在第 6 列：ID|名称|类型|关联|参考图|状态）
    $planRows = Get-SectionRows -Lines $planLines `
        -StartPattern '^##\s*3\.\s*需求池总表' -EndPattern '^##\s*4\.' -StatusIndex 6

    if ($planRows.Count -eq 0) {
        $problems += 'docs/开发计划.md 里找不到「3. 需求池总表」的条目行'
    } else {
        $pDone = 0; $pDoing = 0; $pTodo = 0; $pBlock = 0
        $planStatus = @{}
        foreach ($r in $planRows) {
            $found = @()
            foreach ($m in $Markers) { if ($r.Status.Contains($m)) { $found += $m } }
            if ($found.Count -gt 1) { $problems += "开发计划条目含多个状态标记: $($r.Row.Trim())" }
            if ($found.Count -eq 0) { continue }
            switch ($found[0]) {
                $Done  { $pDone++ }
                $Doing { $pDoing++ }
                $Todo  { $pTodo++ }
                $Block { $pBlock++ }
            }
            $cells = $r.Row.Split('|')
            $planStatus[$cells[1].Trim()] = $found[0]
        }
        $pTotal = $pDone + $pDoing + $pTodo + $pBlock

        # 4b. 与逐项展开标题里的状态对比
        $esc = ($Markers | ForEach-Object { [regex]::Escape($_) }) -join '|'
        $titlePattern = '^(###\s*(D\d+)\s*·.*?)(' + $esc + ')\s*$'
        $titleStatus = @{}
        foreach ($line in $planLines) {
            if ($line -match $titlePattern) {
                $titleStatus[$Matches[2]] = $Matches[3]
            }
        }
        foreach ($id in $planStatus.Keys) {
            if (-not $titleStatus.ContainsKey($id)) {
                $problems += "开发计划条目 [$id] 在需求池总表有，但逐项展开缺对应标题"
            } elseif ($titleStatus[$id] -ne $planStatus[$id]) {
                $problems += "开发计划 [$id] 状态不一致：总表=$($planStatus[$id]) 展开标题=$($titleStatus[$id])"
            }
        }
        foreach ($id in $titleStatus.Keys) {
            if (-not $planStatus.ContainsKey($id)) {
                $problems += "开发计划条目 [$id] 在逐项展开有，但需求池总表缺对应行"
            }
        }

        # 4c. 与第 6 节统计表对比
        $planTable = Get-ExpectedTable -Lines $planLines -StartPattern '^##\s*6\.' -EndPattern '^##\s*7\.'
        if ($planTable.Count -eq 0) {
            $problems += 'docs/开发计划.md 里找不到「6. 统计」表'
        } else {
            $prow = $null
            foreach ($k in $planTable.Keys) { $prow = $planTable[$k]; break }
            if ($prow.Total -ne $pTotal) { $problems += "开发计划统计表 总数 表内=$($prow.Total) 实算=$pTotal" }
            if ($prow.Done  -ne $pDone)  { $problems += "开发计划统计表 完成 表内=$($prow.Done) 实算=$pDone" }
            if ($prow.Doing -ne $pDoing) { $problems += "开发计划统计表 进行 表内=$($prow.Doing) 实算=$pDoing" }
            if ($prow.Todo  -ne $pTodo)  { $problems += "开发计划统计表 待开 表内=$($prow.Todo) 实算=$pTodo" }
        }
    }
} else {
    Write-Host '提示: 未找到 docs\开发计划.md，跳过开发计划校验' -ForegroundColor DarkGray
}

# ---------- 5. 一行多个状态标记（可能是笔误）----------
foreach ($row in $multiMarker) {
    $problems += "任务行含多个状态标记（可能笔误）: $row"
}

# ---------- 汇总 ----------
if ($problems.Count -eq 0) {
    Write-Host ''
    Write-Host ('OK  统计一致: tasks.md 实算 / tasks.md 统计表 / README 进度基线 三处完全相同 (共 ' + $grandTotal + ' 行)') -ForegroundColor Green
    if (Test-Path (Join-Path $RepoRoot 'docs\开发计划.md')) {
        Write-Host ('OK  开发计划一致: 需求池总表 / 逐项展开标题 / 6.统计表 三处完全相同 (共 ' + $pTotal + ' 条)') -ForegroundColor Green
    }
    exit 0
}

Write-Host ''
Write-Host '发现 ' $problems.Count ' 处不一致：' -ForegroundColor Yellow
foreach ($p in $problems) { Write-Host ('  - ' + $p) -ForegroundColor Yellow }
Write-Host ''
Write-Host '修复方式：更新 tasks.md「六、任务统计」与 README.md「5. 当前进度基线」两张表，使其与实算一致。' -ForegroundColor Yellow
Write-Host '          若报的是开发计划：需求池总表的状态列、逐项展开标题的状态、「6. 统计」表三处必须同步改。' -ForegroundColor Yellow
exit 1
