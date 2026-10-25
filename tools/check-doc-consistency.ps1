<#
.SYNOPSIS
    文档一致性回读测试：把「回读一遍、找出前后矛盾」变成可重复执行的检查。

.DESCRIPTION
    本项目的文档密度很高（README 主干 + 多份专题文档 + 校验脚本），
    改动一多就会出现「同一件事在不同文件里说法不一致」的问题。
    2026-10-20 的一次手工回读就抓到了：
      - 术语错误：把「洞（顶点）」写成「桥（cut edge）」，实际该判断的是**割点**
      - 旧称残留：「桥树」与后来引入传送门/单向门后的「簇图」混用
      - 结论过期：早期条目「簇只由洞产生」已被「簇有两个来源」推翻

    本脚本检查 5 类问题：

      1. 内部链接有效性：所有 .md 里的相对链接是否指向真实存在的文件
      2. 换行符一致性：文件内是否混用 CRLF / LF（混用会让 diff 变脏）
      3. 乱码扫描：是否出现 U+FFFD 替换字符（编码损坏的信号）
      4. 术语残留：黑名单里的"误用形式"是否还留在正文里
      5. 统计一致性：转调 tools\check-task-stats.ps1（tasks / README / 开发计划 四处互校）

    退出码 0 = 全部通过；非 0 = 有问题，逐条列出。

.PARAMETER RepoRoot
    仓库根目录，默认本脚本所在目录的上一级。

.PARAMETER CheckRemote
    开关。加上后会把远端（GitHub）上的文件拉回来与本地做 SHA256 逐字节比对，
    用来验证「推送是否真的完整」。需要本机装了 gh 且已认证。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\check-doc-consistency.ps1
    powershell -ExecutionPolicy Bypass -File tools\check-doc-consistency.ps1 -CheckRemote
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = '',
    [switch]$CheckRemote
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepoRoot = Split-Path -Parent $scriptDir
}
if (-not (Test-Path $RepoRoot)) { Write-Error "仓库根目录不存在: $RepoRoot"; exit 2 }

# 排除目录（构建产物 / 工具缓存 / 第三方内容）
$skipDirs = 'obj|bin|\.godot|\.mcp|\.git\\'

# 已知的历史文件，单独豁免换行符检查（本次未改动，不顺手改）
$crlfExempt = @('docs\战斗场景布局.md')

# 术语黑名单：这些是「误用形式」。注意不含单独的「桥树」与「cut edge」——
# 因为文档里在**术语纠正段**会合法地引用它们来解释"为什么不用这两个词"。
$Blacklist = @(
    'BridgeTreeAnalyzer',
    '桥树预检',
    '桥树分析',
    '桥树判定',
    '桥树用例',
    '桥树结构'
)

# ⚠️ 「必杀技」是**旧称**（D54 起本项目统一叫「狂气爆发」）。
# 它**只允许出现在历史记录段**里 —— 那些段落记的是「当时发生的事」，改了就不准了。
# 下面的表给出「从哪一行开始算历史」；键是相对路径（用反斜杠）。
$TermCutMarkers = @{
    'README.md'          = '## 8. 变更记录'
    'docs\开发计划.md'    = '## 4. 逐项展开'
}

# 整份都是历史记录的（逐条踩坑档案），不参与术语检查
$TermSkipFiles = @(
    'docs\踩坑记录.md'
)

$problems = @()
$mdFiles = Get-ChildItem -Path $RepoRoot -Filter *.md -Recurse -File |
    Where-Object { $_.FullName -notmatch $skipDirs }

Write-Host "扫描 $($mdFiles.Count) 个 markdown 文件..." -ForegroundColor Cyan

# 生成 GitHub 风格的标题锚点集合（小写、去标点、空格转连字符）
function Get-Anchors([string]$md) {
    $set = @{}
    foreach ($line in ($md -split "`n")) {
        if ($line -match '^#{1,6}\s+(.+?)\s*$') {
            $t = $Matches[1]
            $t = $t -replace '\[(.*?)\]\(.*?\)', '$1'   # 去掉行内链接语法
            $t = $t -replace '[`*_~]', ''                    # 去掉强调/代码标记
            $a = $t.ToLower()
            $a = $a -replace '[^\p{L}\p{N}\s\-_]', ''      # 去掉标点
            $a = $a.Trim() -replace '\s', '-'               # 每个空格各转一个连字符（与 GitHub 一致）
            if ($a) { $set[$a] = $true }
        }
    }
    return $set
}

# 预读所有 md 的内容与锚点，供链接检查用
$fileText = @{}
$fileAnchors = @{}
foreach ($f in $mdFiles) {
    $rel = $f.FullName.Substring($RepoRoot.Length).TrimStart('\')
    $fileText[$rel] = [System.IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    $fileAnchors[$rel] = Get-Anchors $fileText[$rel]
}

# ---------- 1~3. 逐文件检查 ----------
foreach ($f in $mdFiles) {
    $raw = $fileText[$f.FullName.Substring($RepoRoot.Length).TrimStart('\')]
    $rel = $f.FullName.Substring($RepoRoot.Length).TrimStart('\')

    # 2. 换行符一致性
    $crlf = ([regex]::Matches($raw, "`r`n")).Count
    $lfOnly = ([regex]::Matches($raw, "(?<!`r)`n")).Count
    if ($crlf -gt 0 -and $lfOnly -gt 0) {
        $problems += "[$rel] 混用换行符：CRLF=$crlf, LF=$lfOnly（建议统一为 LF）"
    } elseif ($crlf -gt 0 -and $rel -notin $crlfExempt) {
        $problems += "[$rel] 全文使用 CRLF（建议统一为 LF；如为历史文件请加进脚本的 `$crlfExempt）"
    }

    # 3. 乱码
    if ($raw -match "\uFFFD") {
        $lines = $raw -split "`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "\uFFFD") {
                $problems += "[${rel}:$($i + 1)] 出现替换字符 U+FFFD（编码损坏）：$($lines[$i].Trim())"
            }
        }
    }

    # 4. 术语残留（历史记录段豁免 —— 那里记的是「当时发生的事」）
    $scan = $raw
    if ($rel -in $TermSkipFiles) {
        $scan = ''
    } elseif ($TermCutMarkers.ContainsKey($rel)) {
        $cut = $scan.IndexOf($TermCutMarkers[$rel])
        if ($cut -gt 0) { $scan = $scan.Substring(0, $cut) }
    }
    foreach ($w in $Blacklist) {
        if ($scan.Contains($w)) {
            $problems += "[$rel] 术语残留「$w」—— 当前模型应统一用「簇图 / 割点」"
        }
    }
    # 「必杀技」是旧称（D54 起统一叫「狂气爆发」）—— 只允许出现在历史记录段
    # ⚠️ 逐行扫：写「术语口径 / 旧称」的那几行是**合法引用**（要告诉读者旧称是什么）→ 放行。
    if ($scan.Contains('必杀技')) {
        $lines = $scan -split "`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if (-not $line.Contains('必杀技')) { continue }
            if ($line -match '旧称|术语对照|术语口径|叫法') { continue }   # 合法引用旧称
            $problems += "[${rel}:$($i + 1)] 术语残留「必杀技」—— 本项目统一叫「狂气爆发」（旧称只允许出现在变更记录 / 逐项展开 / 踩坑记录等历史段）"
            break
        }
    }

    # 1. 内部链接（含 #锚点 的锚点有效性）
    $dir = Split-Path -Parent $f.FullName
    foreach ($m in [regex]::Matches($raw, '\]\(([^)]+)\)')) {
        $link = $m.Groups[1].Value
        if ($link -match '^(https?|mailto)') { continue }

        $pathPart = $link
        $anchor = ''
        $hashPos = $link.IndexOf('#')
        if ($hashPos -ge 0) {
            $pathPart = $link.Substring(0, $hashPos)
            $anchor = $link.Substring($hashPos + 1)
        }

        if ($pathPart -eq '') {
            $target = $f.FullName          # 同文件内锚点
        } else {
            $target = Join-Path $dir ($pathPart -replace '/', '\')
        }

        if (-not (Test-Path $target)) {
            $problems += "[$rel] 坏链：$link"
            continue
        }

        if ($anchor) {
            $tRel = $target.Substring($RepoRoot.Length).TrimStart('\')
            if ($fileAnchors.ContainsKey($tRel) -and -not $fileAnchors[$tRel].ContainsKey($anchor)) {
                $problems += "[$rel] 锚点失效：$link（目标文件里没有能生成该锚点的标题）"
            }
        }
    }
}

# ---------- 5. 统计一致性（转调已有校验器）----------
$statsScript = Join-Path $RepoRoot 'tools\check-task-stats.ps1'
if (Test-Path $statsScript) {
    $out = & powershell -ExecutionPolicy Bypass -File $statsScript 2>&1
    if ($LASTEXITCODE -ne 0) {
        $problems += "统计表四处互校未通过（tools\check-task-stats.ps1）："
        foreach ($l in $out) { if ($_ -ne $null -and "$l".Trim()) { $problems += "    $l" } }
    } else {
        Write-Host '  统计表四处互校：OK' -ForegroundColor Green
    }
}

# ---------- 6. 声明数字回读（规模 / 条目数 / 踩坑条数）----------
# 背景：文档里到处写「N 个 .cs / N 行 / N 个条目 / N 条踩坑」，
# 这些数字过期了没人会发现 —— 本块把它们与实算值逐一对一遍。
function Get-NonEmptyLineCount([string]$dir) {
    $total = 0
    foreach ($f in (Get-ChildItem $dir -Recurse -Filter '*.cs' -File)) {
        # ⚠️ 必须 -Encoding UTF8：不加就按 ANSI 解码，中文注释会被解码坏并**截断后面的行**，
        #    行数少算几百行（2026-10-21 实测 29,969 vs 30,489）。
        foreach ($l in (Get-Content -LiteralPath $f.FullName -Encoding UTF8)) {
            if ($l.Trim() -ne '') { $total++ }
        }
    }
    return $total
}

$readmePath = Join-Path $RepoRoot 'README.md'
$planPath = Join-Path $RepoRoot 'docs\开发计划.md'
$pitPath = Join-Path $RepoRoot 'docs\踩坑记录.md'

if ((Test-Path $readmePath) -and (Test-Path $planPath) -and (Test-Path $pitPath)) {
    $readmeLines = Get-Content -LiteralPath $readmePath -Encoding UTF8
    $planLines = Get-Content -LiteralPath $planPath -Encoding UTF8
    $pitLines = Get-Content -LiteralPath $pitPath -Encoding UTF8

    # --- 6.1 README 规模数字 ---
    $csCount = (Get-ChildItem (Join-Path $RepoRoot 'Scripts') -Recurse -Filter '*.cs' -File).Count
    $csLines = Get-NonEmptyLineCount (Join-Path $RepoRoot 'Scripts')
    $tscnCount = (Get-ChildItem (Join-Path $RepoRoot 'Scenes') -Recurse -Filter '*.tscn' -File).Count
    $jsonCount = (Get-ChildItem (Join-Path $RepoRoot 'Data') -Filter '*.json' -File).Count

    $scaleLine = @($readmeLines | Where-Object { $_ -match '^\|\s*规模\s*\|' })[0]
    if ($null -ne $scaleLine) {
        $doc = @{}
        if ($scaleLine -match '(\d+)\s*个 C# 脚本') { $doc['cs'] = [int]$matches[1] }
        if ($scaleLine -match '([\d,]+)\s*行') { $doc['lines'] = [int]($matches[1] -replace ',', '') }
        if ($scaleLine -match '(\d+)\s*个\s*`?\.tscn') { $doc['tscn'] = [int]$matches[1] }
        if ($scaleLine -match '(\d+)\s*张 JSON') { $doc['json'] = [int]$matches[1] }

        $real = @{ 'cs' = $csCount; 'lines' = $csLines; 'tscn' = $tscnCount; 'json' = $jsonCount }
        $label = @{ 'cs' = 'C# 脚本数'; 'lines' = 'C# 非空行数'; 'tscn' = '.tscn 数'; 'json' = 'Data JSON 表数' }
        foreach ($k in @('cs', 'lines', 'tscn', 'json')) {
            if ($doc.ContainsKey($k) -and $doc[$k] -ne $real[$k]) {
                $problems += "[README.md] 规模数字漂移：$($label[$k]) 文档写 $($doc[$k])，实算 $($real[$k])"
            }
        }
    }

    # --- 6.2 踩坑记录条数（实际标题数 vs 各处声明）---
    # ⚠️ 变更记录里的行（| 2026-10-20 | ...）是**历史快照**，不该参与核对 —— 跳过。
    $pitActual = @($pitLines | Where-Object { $_ -match '^## \d+\. ' }).Count
    $claims = @()
    foreach ($l in $readmeLines) {
        if ($l -match '^\|\s*\d{4}-\d{2}-\d{2}\s*\|') { continue }
        if ($l -match '历史上踩过的\s*(\d+)\s*个坑') { $claims += , @('README.md', [int]$matches[1]) }
        if ($l -match '\*\*(\d+)\s*条真实踩坑与规矩\*\*') { $claims += , @('README.md', [int]$matches[1]) }
    }
    foreach ($l in $planLines) {
        if ($l -match '^\|\s*\d{4}-\d{2}-\d{2}\s*\|') { continue }
        if ($l -match '\|\s*`docs/踩坑记录\.md`\s*\|\s*(\d+)\s*条真实踩坑与规矩\s*\|') { $claims += , @('docs/开发计划.md', [int]$matches[1]) }
    }
    foreach ($c in $claims) {
        if ($c[1] -ne $pitActual) {
            $problems += "[$($c[0])] 踩坑记录条数漂移：写 $($c[1])，实算 $pitActual"
        }
    }

    # --- 6.3 踩坑记录目录表必须覆盖全部条目 ---
    $tocNums = @($pitLines | Where-Object { $_ -match '^\|\s*(\d+)\s*\|' } | ForEach-Object { [int]($_ -replace '^\|\s*(\d+)\s*\|.*$', '$1') })
    $headNums = @($pitLines | Where-Object { $_ -match '^## (\d+)\. ' } | ForEach-Object { [int]$matches[1] })
    $missing = @($headNums | Where-Object { $tocNums -notcontains $_ })
    if ($missing.Count -gt 0) {
        $problems += "[docs/踩坑记录.md] 目录表漏了条目：$(($missing | Sort-Object) -join ', ')"
    }

    # --- 6.4 开发计划条目数（逐项展开标题 vs 统计表 vs README 声明）---
    $dActual = @($planLines | Where-Object { $_ -match '^### D\d+ ' }).Count
    $dStat = $null
    foreach ($l in $planLines) {
        if ($l -match '^\|\s*本轮迭代\s*\|\s*(\d+)\s*\|') { $dStat = [int]$matches[1] }
    }
    if ($null -ne $dStat -and $dStat -ne $dActual) {
        $problems += "[docs/开发计划.md] 本轮迭代条目数漂移：统计表 $dStat，逐项展开 $dActual"
    }
    foreach ($l in $readmeLines) {
        if ($l -match '^\|\s*\d{4}-\d{2}-\d{2}\s*\|') { continue }   # 变更记录是历史快照
        if ($l -match 'docs/开发计划\.md`\]\s*·\s*(\d+)\s*个条目') {
            if ([int]$matches[1] -ne $dActual) {
                $problems += "[README.md] 迭代条目数漂移：写 $($matches[1])，实算 $dActual"
            }
        }
        if ($l -match '三步流程\s*\+\s*(\d+)\s*个条目') {
            if ([int]$matches[1] -ne $dActual) {
                $problems += "[README.md] 文档索引里的迭代条目数漂移：写 $($matches[1])，实算 $dActual"
            }
        }
    }

    Write-Host '  声明数字回读：已核对（规模 / 条目数 / 踩坑条数 / 目录表覆盖）' -ForegroundColor Green
}

# ---------- 可选：远端回读比对 ----------
if ($CheckRemote) {
    # gh 可能只装在用户级 PATH（某些 shell 会话不继承），找不到时兜底常见便携版路径
    $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
    $ghExe = $null
    if ($null -ne $ghCmd) {
        $ghExe = $ghCmd.Source
    } else {
        $fallback = Join-Path $env:USERPROFILE 'Tools\gh\bin\gh.exe'
        if (Test-Path $fallback) { $ghExe = $fallback }
    }

    if ($null -eq $ghExe) {
        Write-Host '  跳过远端比对：未找到 gh（可装到 %USERPROFILE%\Tools\gh\bin）' -ForegroundColor Yellow
    } else {
        $remote = 'yutuer/shalujianta'
        $tracked = @('README.md', 'tasks.md', 'docs/开发计划.md', 'docs/踩坑记录.md', 'docs/调试场景准备器.md', '.gitignore', 'tools/check-doc-consistency.ps1')
        foreach ($r in $tracked) {
            $api = $r -replace ' ', '%20'
            $j = (& $ghExe api "repos/$remote/contents/$api" 2>$null) | ConvertFrom-Json
            if ($null -eq $j -or -not $j.content) { $problems += "[远端] 取不到 $r"; continue }
            $remoteBytes = [Convert]::FromBase64String(($j.content -replace '\s', ''))
            $sha = [System.Security.Cryptography.SHA256]::Create()
            $rh = [BitConverter]::ToString($sha.ComputeHash($remoteBytes)).Replace('-', '')
            $lh = (Get-FileHash -LiteralPath (Join-Path $RepoRoot ($r -replace '/', '\')) -Algorithm SHA256).Hash
            if ($rh -ne $lh) { $problems += "[远端] $r 与本地不一致（远端可能没推上去）" }
        }
        Write-Host "  远端比对完成（$($tracked.Count) 个文件）" -ForegroundColor Green
    }
}

# ---------- 汇总 ----------
if ($problems.Count -eq 0) {
    Write-Host ''
    Write-Host ('OK  文档一致性通过（' + $mdFiles.Count + ' 个文件：链接 / 换行符 / 编码 / 术语 / 统计）') -ForegroundColor Green
    exit 0
}

Write-Host ''
Write-Host ('发现 ' + $problems.Count + ' 处问题：') -ForegroundColor Yellow
foreach ($p in $problems) { Write-Host ('  - ' + $p) -ForegroundColor Yellow }
exit 1
