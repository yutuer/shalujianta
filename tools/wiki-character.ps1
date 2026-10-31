#Requires -Version 5.1
<#
.SYNOPSIS
    从灰机 wiki（morimens.huijiwiki.com）抓角色页，解析出属性 / 升格 / 卡牌数据。

.DESCRIPTION
    为什么需要这个脚本：
    本项目的角色属性以前只能靠主人逐个截图「详细属性_<名>.jpg」手抄，
    卡片效果也只能抄参考图，既慢又容易抄错（波吕克斯暴击率就曾被抄成 15，实际是 14.6%）。
    灰机 wiki 的角色页把这些**全部**写成了结构化 wikitext：
      - 本体数值：1/10/20/…/90 级 九档的 体质 / 攻击 / 防御
      - 升格 1~5：每条给 2 项属性的「旧值 → 新值」（升格 N 生效到 10N 级，**升格5 = 60 级**）
      - 打击 / 防御 / 技能卡1 / 技能卡2 / 灵知觉醒 / 狂气爆发 / 特殊卡：6 级效果全文
      - 启灵 1~15、灵塑 1~10、内在灵格、天赋、语音

    属性合成规则（主人 2026-11-01 口径，已用参考图「详细属性」逐项验证）：
      **属性 = 通用值(9 项) + wiki 升格5(=60 级) 那 2 条覆盖对应项**
      通用值 = 暴击率5% / 暴击伤害50% / 界域精通0 / 伤害强效0 /
                基础狂气100 / 狂气回充等级0 / 银钥充能等级15 / 黑印掉落0 / 死亡抵抗0

    抓取原理（踩过坑，别改）：
    灰机 wiki 前面挂了 Cloudflare，**curl / Invoke-WebRequest / node fetch 全部 403**，
    因为 Cloudflare 的 JS 挑战要跑浏览器。
    但 **Chrome 的 headless 模式 + 换成正常 UA** 可以直接过（挑战页标题是「请稍候…」）。
    关键两点：
      1. `--user-agent=` 必须是**正常 Chrome UA**（headless 默认 UA 带 `HeadlessChrome`，会被识别）
      2. 用 `--timeout=30000` 等真实时间，别用 `--virtual-time-budget`（虚拟时钟会让挑战页永远转圈）

.PARAMETER Names
    角色名（wiki 页面标题），逗号分隔。例：`-Names 珈伦,波吕克斯`

.PARAMETER OutDir
    输出目录（raw wikitext + 解析出的 JSON）。默认写到系统临时目录，**不污染仓库**。

.PARAMETER ChromePath
    指定 Chrome / Edge 可执行文件。默认自动探测。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\wiki-character.ps1 -Names 珈伦,波吕克斯

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\wiki-character.ps1 -Names 墨菲 -OutDir D:\wiki

.NOTES
    退出码 0 = 全部角色解析成功；非 0 = 有角色失败（清单打印在输出末尾）。
#>
[CmdletBinding()]
param(
    [string[]]$Names = @(),
    [string]$OutDir = '',
    [string]$ChromePath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Web

$WikiBase = 'https://morimens.huijiwiki.com'
$NormalUA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36'

# ---------------- 通用值（主人 2026-11-01 口径） ----------------
$GenericAttrs = [ordered]@{
    '暴击率'       = @{ field = 'BaseCritRate';        value = 5 }
    '暴击伤害'     = @{ field = 'BaseCritDamage';      value = 50 }
    '界域精通'     = @{ field = 'BaseDomainMastery';   value = 0 }
    '伤害强效'     = @{ field = 'BaseDamageBonus';     value = 0 }
    '基础狂气'     = @{ field = 'BaseMaxRage';         value = 100 }
    '狂气回充等级' = @{ field = 'BaseRageReturn';      value = 0 }
    '银钥充能等级' = @{ field = 'BaseSilverKeyCharge'; value = 15 }
    '黑印掉落'     = @{ field = 'BaseBlackMarkDrop';   value = 0 }
    '死亡抵抗'     = @{ field = 'BaseDeathResistance'; value = 0 }
}

# ---------------- 工具函数 ----------------

function Resolve-Chrome([string]$Explicit) {
    if ($Explicit -and (Test-Path $Explicit)) { return $Explicit }
    $cands = @(
        'C:\Program Files\Google\Chrome\Application\chrome.exe',
        'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe',
        'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
        'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
    )
    foreach ($c in $cands) { if (Test-Path $c) { return $c } }
    throw '找不到 Chrome / Edge，请用 -ChromePath 指定'
}

function Get-WikiRaw([string]$Title, [string]$Chrome) {
    $url  = $WikiBase + '/index.php?title=' + [uri]::EscapeDataString($Title) + '&action=raw'
    $lastErr = ''

    # Cloudflare 偶尔会直接返回 403（连续请求触发风控），重试 3 次并换新 profile
    for ($try = 1; $try -le 3; $try++) {
        $prof = Join-Path $env:TEMP ('crwiki_' + [guid]::NewGuid().ToString('N'))
        $dump = Join-Path $env:TEMP ('crwiki_' + [guid]::NewGuid().ToString('N') + '.html')

        & $Chrome '--headless=new' '--disable-gpu' '--no-sandbox' '--disable-dev-shm-usage' `
            "--user-data-dir=$prof" "--user-agent=$NormalUA" '--timeout=30000' '--dump-dom' $url 2>$null |
            Out-File -FilePath $dump -Encoding UTF8

        if (-not (Test-Path $dump)) { $lastErr = 'Chrome 抓取失败'; continue }
        $html = [System.IO.File]::ReadAllText($dump, [Text.Encoding]::UTF8)
        Remove-Item $dump -Force -ErrorAction SilentlyContinue

        if ($html -match '请稍候' -and $html -notmatch '<pre') {
            $lastErr = '被 Cloudflare 挑战页拦截（UA 或等待时间不对）'
            Start-Sleep -Seconds (3 * $try)
            continue
        }

        $m = [regex]::Match($html, '(?s)<pre[^>]*>(.*?)</pre>')
        if (-not $m.Success) {
            $lastErr = '页面里没有 <pre>（该角色在 wiki 上可能没有页）'
            break
        }
        return [System.Web.HttpUtility]::HtmlDecode($m.Groups[1].Value)
    }

    throw $lastErr
}

function Get-Field([string]$Text, [string]$Field) {
    # 允许字段名与 '=' 之间有空格（wiki 里 `|特殊卡牌1名字 =奇妙料理` 这种写法很常见）
    $m = [regex]::Match($Text, "(?m)^\|\s*" + [regex]::Escape($Field) + "\s*=(.*)$")
    if ($m.Success) { return (Clean-Value $m.Groups[1].Value.Trim()) }
    return $null
}

# 清掉 wikitext 里的 HTML 注释（wiki 模板常在字段里写选项说明）
function Clean-Value([string]$V) {
    if ($null -eq $V) { return $V }
    return ([regex]::Replace($V, '<!--.*?-->', '')).Trim()
}

function Get-FieldOrEmpty([string]$Text, [string]$Field) {
    $v = Get-Field $Text $Field
    if ($null -eq $v) { return '' }
    return $v
}

# 升格效果：'{{属性|暴击率}} 13.0% → 14.6%' -> Attr=暴击率, Value=14.6
function Get-Ascension([string]$Text) {
    $tiers = @()
    for ($n = 1; $n -le 5; $n++) {
        $raw = Get-Field $Text "升格${n}效果"
        if ([string]::IsNullOrWhiteSpace($raw)) { continue }
        $pairs = @()
        foreach ($m in [regex]::Matches($raw, '\{\{属性\|([^}]+)\}\}\s*([^<]*)')) {
            $attr = $m.Groups[1].Value.Trim()
            $val  = $m.Groups[2].Value.Trim()
            if ($val -match '→\s*(.+)$') { $val = $Matches[1].Trim() }
            $num = $val -replace '[^\d.]', ''
            if ([string]::IsNullOrWhiteSpace($num)) { continue }
            $pairs += [PSCustomObject]@{ attr = $attr; value = [double]$num; raw = $val }
        }
        # 升格 N 生效到 10N 级：升格1→10 级 … 升格5→60 级
        $tiers += [PSCustomObject]@{ tier = $n; upToLevel = $n * 10; text = $raw; pairs = $pairs }
    }
    return $tiers
}

function Get-BodyStats([string]$Text) {
    $rows = @()
    foreach ($lv in @(1, 10, 20, 30, 40, 50, 60, 70, 80, 90)) {
        $con = Get-Field $Text "${lv}级体质"
        $atk = Get-Field $Text "${lv}级攻击"
        $def = Get-Field $Text "${lv}级防御"
        if ($null -eq $con -and $null -eq $atk -and $null -eq $def) { continue }
        $rows += [PSCustomObject]@{
            level     = $lv
            constitution = if ($null -ne $con) { [double]$con } else { $null }
            attack       = if ($null -ne $atk) { [double]$atk } else { $null }
            defense      = if ($null -ne $def) { [double]$def } else { $null }
        }
    }
    return $rows
}

# 卡牌 / 技能：名字 + 算力 + 描述 + 6 级效果
# 打击 / 防御 没有「名字」字段（只有描述 + 效果），所以单独给默认名。
function Get-Card([string]$Text, [string]$Prefix, [string]$Kind, [string]$DefaultName = '') {
    $name = Get-FieldOrEmpty $Text "${Prefix}名字"
    if ([string]::IsNullOrWhiteSpace($name)) {
        # 打击 / 防御：只要有描述或任一级效果就算有这张卡
        $hasEffect = $false
        for ($i = 1; $i -le 6; $i++) {
            if ($null -ne (Get-Field $Text "${Prefix}效果${i}")) { $hasEffect = $true; break }
        }
        if (-not $hasEffect) { return $null }
        $name = if ($DefaultName) { $DefaultName } else { $Prefix }
    }
    $cost = Get-FieldOrEmpty $Text "${Prefix}算力"
    $desc = Get-FieldOrEmpty $Text "${Prefix}描述"
    $effPrefix = if ($Kind -eq 'ultimate') { '狂气效果' } else { "${Prefix}效果" }
    $effects = @()
    for ($i = 1; $i -le 6; $i++) {
        $e = Get-Field $Text "${effPrefix}${i}"
        if ($null -eq $e) { continue }
        $effects += [PSCustomObject]@{ level = $i; text = $e }
    }
    return [PSCustomObject]@{
        key       = $Prefix
        kind      = $Kind
        name      = $name
        cost      = if ($cost -match '^\d+$') { [int]$cost } else { $null }
        costRaw   = $cost
        desc      = $desc
        effects   = $effects
    }
}

function Get-CharacterData([string]$Name, [string]$Text) {
    $body   = Get-BodyStats $Text
    $tiers  = Get-Ascension $Text
    $last   = if ($tiers.Count -gt 0) { $tiers[$tiers.Count - 1] } else { $null }

    # 合成属性：通用值 + 升格5(=60 级) 那 2 条覆盖
    $merged = [ordered]@{}
    foreach ($k in $GenericAttrs.Keys) {
        $merged[$k] = [PSCustomObject]@{
            attr = $k; field = $GenericAttrs[$k].field
            value = $GenericAttrs[$k].value
            source = '通用值'
        }
    }
    if ($last) {
        foreach ($p in $last.pairs) {
            if ($merged.Contains($p.attr)) {
                $merged[$p.attr].value  = $p.value
                $merged[$p.attr].source = ('升格' + $last.tier + '（60 级时）')
            }
        }
    }

    $row60 = $body | Where-Object { $_.level -eq 60 } | Select-Object -First 1

    $cards = @()
    foreach ($c in @(
        @{ p = '打击';     k = 'strike';        n = '打击' },
        @{ p = '防御';     k = 'defense';       n = '防御' },
        @{ p = '技能1（右）'; k = 'skill1';     n = '' },
        @{ p = '技能2（左）'; k = 'skill2';     n = '' },
        @{ p = '灵知觉醒'; k = 'awakening';     n = '' },
        @{ p = '狂气爆发'; k = 'ultimate';      n = '' },
        @{ p = '特殊卡牌1'; k = 'specialCard1'; n = '' },
        @{ p = '特殊卡牌2'; k = 'specialCard2'; n = '' },
        @{ p = '特殊技能1'; k = 'specialSkill1'; n = '' }
    )) {
        $card = Get-Card $Text $c.p $c.k $c.n
        if ($card) { $cards += $card }
    }

    $enlighten = @()
    for ($i = 1; $i -le 15; $i++) {
        $e = Get-Field $Text "启灵${i}效果"
        if ($null -eq $e) { continue }
        $enlighten += [PSCustomObject]@{
            level = $i
            name  = Get-FieldOrEmpty $Text "启灵${i}名字"
            text  = $e
        }
    }

    $talents = @()
    for ($i = 1; $i -le 10; $i++) {
        $t = Get-Field $Text "灵塑${i}"
        if ($null -eq $t) { continue }
        $talents += [PSCustomObject]@{ level = $i; text = $t }
    }

    return [PSCustomObject]@{
        name        = $Name
        domain      = Get-FieldOrEmpty $Text '界域'
        type        = Get-FieldOrEmpty $Text '唤醒体类型'
        rarity      = Get-FieldOrEmpty $Text '稀有度'
        gender      = Get-FieldOrEmpty $Text '性别'
        englishName = Get-FieldOrEmpty $Text '英文名'
        bodyStats   = $body
        bodyAt60    = $row60
        ascension   = $tiers
        attributes  = @($merged.Values)
        cards       = $cards
        enlighten   = $enlighten
        talents     = $talents
        innerCell   = [PSCustomObject]@{
            constitution = Get-FieldOrEmpty $Text '内在灵格限定体质'
            attack       = Get-FieldOrEmpty $Text '内在灵格限定攻击'
            defense      = Get-FieldOrEmpty $Text '内在灵格限定防御'
        }
        innateTalent = [PSCustomObject]@{
            name = Get-FieldOrEmpty $Text '固有天赋1'
            desc = Get-FieldOrEmpty $Text '天赋1描述'
        }
    }
}

# ---------------- 主流程 ----------------

# -Names 支持逗号分隔（powershell -File 传参不会自动拆数组）
$NameList = @()
foreach ($n in $Names) { $NameList += ($n -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
$Names = $NameList

if ($Names.Count -eq 0) {
    Write-Error '请用 -Names 指定角色名，例：-Names 珈伦,波吕克斯'
    exit 2
}

if (-not $OutDir) {
    $OutDir = Join-Path $env:TEMP ('fishwiki_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
}
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

$chrome = Resolve-Chrome $ChromePath
Write-Output "wiki  : $WikiBase"
Write-Output "浏览器: $chrome"
Write-Output "输出  : $OutDir"
Write-Output ''

$ok = @(); $fail = @()

foreach ($name in $Names) {
    Write-Output ('==================== ' + $name + ' ====================')
    try {
        $raw = Get-WikiRaw $name $chrome
        $rawFile = Join-Path $OutDir ($name + '.wiki')
        [System.IO.File]::WriteAllText($rawFile, $raw, (New-Object Text.UTF8Encoding($false)))

        $data = Get-CharacterData $name $raw

        Write-Output ("  原文 {0} 字符 -> {1}" -f $raw.Length, $rawFile)
        Write-Output ("  界域={0}  类型={1}  稀有度={2}  性别={3}" -f $data.domain, $data.type, $data.rarity, $data.gender)

        Write-Output '  -- 本体属性（wiki 九档）--'
        foreach ($r in $data.bodyStats) {
            Write-Output ("     {0,3} 级 : 体质 {1} / 攻击 {2} / 防御 {3}" -f $r.level, $r.constitution, $r.attack, $r.defense)
        }

        Write-Output '  -- 升格（升格 N 在 10N 级生效；60 级时吃满最后一条升格）--'
        foreach ($t in $data.ascension) {
            Write-Output ("     升格{0}（{1} 级生效）: {2}" -f $t.tier, $t.upToLevel, $t.text)
        }

        Write-Output '  -- 合成属性（通用值 + 升格5 覆盖）--'
        foreach ($a in $data.attributes) {
            Write-Output ("     {0,-12} = {1,-6} <- {2,-14} [{3}]" -f $a.attr, $a.value, $a.source, $a.field)
        }

        Write-Output '  -- 卡牌 --'
        foreach ($c in $data.cards) {
            Write-Output ("     [{0}] {1}（算力 {2}） 效果 {3} 条" -f $c.kind, $c.name, $c.costRaw, $c.effects.Count)
        }

        $jsonFile = Join-Path $OutDir ($name + '.json')
        $data | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonFile -Encoding UTF8
        Write-Output ("  JSON -> {0}" -f $jsonFile)
        $ok += $name
    }
    catch {
        Write-Output ('  !! 失败: ' + $_.Exception.Message)
        $fail += $name
    }
    Write-Output ''
}

Write-Output ('成功 {0} 个: {1}' -f $ok.Count, ($ok -join ', '))
if ($fail.Count -gt 0) {
    Write-Output ('失败 {0} 个: {1}' -f $fail.Count, ($fail -join ', '))
    exit 1
}
exit 0
