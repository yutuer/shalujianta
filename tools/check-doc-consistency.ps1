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

$problems = @()
$mdFiles = Get-ChildItem -Path $RepoRoot -Filter *.md -Recurse -File |
    Where-Object { $_.FullName -notmatch $skipDirs }

Write-Host "扫描 $($mdFiles.Count) 个 markdown 文件..." -ForegroundColor Cyan

# ---------- 1~3. 逐文件检查 ----------
foreach ($f in $mdFiles) {
    $raw = [System.IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
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

    # 4. 术语残留（README 的「变更记录」段落豁免，那里是历史叙述）
    $scan = $raw
    if ($rel -eq 'README.md') {
        $cut = $scan.IndexOf('## 8. 变更记录')
        if ($cut -gt 0) { $scan = $scan.Substring(0, $cut) }
    }
    foreach ($w in $Blacklist) {
        if ($scan.Contains($w)) {
            $problems += "[$rel] 术语残留「$w」—— 当前模型应统一用「簇图 / 割点」"
        }
    }

    # 1. 内部链接
    $dir = Split-Path -Parent $f.FullName
    foreach ($m in [regex]::Matches($raw, '\]\(([^)#][^)]*)\)')) {
        $link = $m.Groups[1].Value
        if ($link -match '^(https?|mailto)') { continue }
        $target = Join-Path $dir ($link -replace '/', '\')
        if (-not (Test-Path $target)) {
            $problems += "[$rel] 坏链：$link"
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

# ---------- 可选：远端回读比对 ----------
if ($CheckRemote) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) {
        Write-Host '  跳过远端比对：未找到 gh' -ForegroundColor Yellow
    } else {
        $remote = 'yutuer/shalujianta'
        $tracked = @('README.md', 'tasks.md', 'docs/开发计划.md', 'docs/踩坑记录.md', 'docs/调试场景准备器.md', '.gitignore')
        foreach ($r in $tracked) {
            $api = $r -replace ' ', '%20'
            $j = (& gh api "repos/$remote/contents/$api" 2>$null) | ConvertFrom-Json
            if ($null -eq $j -or -not $j.content) { $problems += "[远端] 取不到 $r"; continue }
            $remoteBytes = [Convert]::FromBase64String(($j.content -replace '\s', ''))
            $sha = [System.Security.Cryptography.SHA256]::Create()
            $rh = [BitConverter]::ToString($sha.ComputeHash($remoteBytes)).Replace('-', '')
            $lh = (Get-FileHash -LiteralPath (Join-Path $RepoRoot ($r -replace '/', '\')) -Algorithm SHA256).Hash
            if ($rh -ne $lh) { $problems += "[远端] $r 与本地不一致（远端可能没推上去）" }
        }
        Write-Host '  远端比对完成' -ForegroundColor Green
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
