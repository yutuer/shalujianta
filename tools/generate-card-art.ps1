#Requires -Version 5.1
<#
.SYNOPSIS
    生成「战斗专属美术」资源：角色专属卡插画 / 圆形头像 / 钥令图标。

.DESCRIPTION
    本脚本只写 .svg 文件，不碰任何 .cs / .tscn / .json / project.godot。

    产出三类：
      1) Assets\Cards\card_<cardId>.svg      220x140  每张角色专属卡一张插画
         （专属卡不在 cards.json 里，代码按 cardId 推导路径：res://Assets/Cards/card_<cardId>.svg）
      2) Assets\Portraits\avatar_<charId>.svg 128x128 圆形头像
         （代码优先读它，没有才退回立绘裁头部）
      3) Assets\Icons\keyorder.svg            64x64   钥令图标

    卡牌清单来自 Data\character_cards.json（当前 96 张 = 12 可玩角色 x5 + 12 BOSS x3），
    所以新增卡只要改 JSON 再跑一次本脚本即可，不用改代码。

    ⚠️ **注意**：本脚本生成的是「程序化占位」，但其中 12 个可玩角色的卡面插画与头像
    已经被主人**手工精修**过（2026-10-23）。再跑一次本脚本会**覆盖掉那些手改**。
    要重新生成请先备份，或只把它当"初始版本生成器"用。

    生成的 SVG 必须满足 Godot 4.6 的 ThorVG 导入器限制（每条都写进自检里）：
      - 根元素同时带 xmlns / width / height / viewBox，且 viewBox 必须从 "0 0 W H" 开始
      - 不用 <text>（ThorVG 不支持，文字会直接消失）
      - 不用 <filter> / <mask> / <clipPath> / <image>
      - 颜色一律 hex，半透明用 fill-opacity / stroke-opacity，不用 rgba()/rgb()
      - 渐变写 x1/y1/x2/y2 为 0~1 的小数，id 在同一文件内不重名
      - 文件为 UTF-8 无 BOM + LF 换行，末尾留一个换行

.PARAMETER RepoRoot
    仓库根目录。默认为本脚本所在目录的上一级。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\generate-card-art.ps1
    退出码 0 = 全部文件生成且自检通过；非 0 = 有文件没过自检。
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepoRoot = Split-Path -Parent $scriptDir
}

$CardDir = Join-Path $RepoRoot 'Assets\Cards'
$PortraitDir = Join-Path $RepoRoot 'Assets\Portraits'
$IconDir = Join-Path $RepoRoot 'Assets\Icons'
$CardDataPath = Join-Path $RepoRoot 'Data\character_cards.json'

foreach ($d in @($CardDir, $PortraitDir, $IconDir)) {
    if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

# ============================ 颜色与格式化 ============================

function ConvertTo-Rgb {
    param([string]$Hex)
    $h = $Hex.TrimStart('#')
    if ($h.Length -eq 3) { $h = "$($h[0])$($h[0])$($h[1])$($h[1])$($h[2])$($h[2])" }
    ,@(
        [Convert]::ToInt32($h.Substring(0, 2), 16)
        [Convert]::ToInt32($h.Substring(2, 2), 16)
        [Convert]::ToInt32($h.Substring(4, 2), 16)
    )
}

function Get-Lighter {
    param([string]$Hex, [double]$Pct)
    $c = ConvertTo-Rgb $Hex
    $r = [int][Math]::Round($c[0] + (255 - $c[0]) * $Pct)
    $g = [int][Math]::Round($c[1] + (255 - $c[1]) * $Pct)
    $b = [int][Math]::Round($c[2] + (255 - $c[2]) * $Pct)
    return ('#{0:X2}{1:X2}{2:X2}' -f $r, $g, $b)
}

function Get-Darker {
    param([string]$Hex, [double]$Pct)
    $c = ConvertTo-Rgb $Hex
    $r = [int][Math]::Round($c[0] * (1 - $Pct))
    $g = [int][Math]::Round($c[1] * (1 - $Pct))
    $b = [int][Math]::Round($c[2] * (1 - $Pct))
    return ('#{0:X2}{1:X2}{2:X2}' -f $r, $g, $b)
}

function Format-Num {
    param([double]$Value)
    $s = ([Math]::Round($Value, 1)).ToString('0.0', [Globalization.CultureInfo]::InvariantCulture)
    if ($s.EndsWith('.0')) { $s = $s.Substring(0, $s.Length - 2) }
    return $s
}

function New-SvgDocument {
    param(
        [int]$Width,
        [int]$Height,
        [string[]]$Defs = @(),
        [string[]]$Body = @()
    )
    $lines = @()
    $lines += ('<svg xmlns="http://www.w3.org/2000/svg" width="{0}" height="{1}" viewBox="0 0 {0} {1}">' -f $Width, $Height)
    if ($Defs.Count -gt 0) {
        $lines += '  <defs>'
        foreach ($d in $Defs) { $lines += ('    ' + $d) }
        $lines += '  </defs>'
    }
    foreach ($b in $Body) { $lines += ('  ' + $b) }
    $lines += '</svg>'
    return (($lines -join "`n") + "`n")
}

# ============================ 调色板 ============================
# Main = 主色（身体/背景），Accent = 辅色（描边高亮/点缀）
# 主色与 generate-placeholder-svg.ps1 里的立绘保持一致，插画和立绘看起来是一套的。

$PlayablePalette = [ordered]@{
    'rat'     = @{ Main = '#7C6E96'; Accent = '#D8C8E8'; Feature = 'rat' }
    'ox'      = @{ Main = '#9C7048'; Accent = '#E8C89A'; Feature = 'ox' }
    'tiger'   = @{ Main = '#D8912F'; Accent = '#FFE0A0'; Feature = 'tiger' }
    'rabbit'  = @{ Main = '#D9A8BE'; Accent = '#FFF0F6'; Feature = 'rabbit' }
    'dragon'  = @{ Main = '#2F7E93'; Accent = '#8FE6F2'; Feature = 'dragon' }
    'snake'   = @{ Main = '#3E9455'; Accent = '#B6F0C6'; Feature = 'snake' }
    'horse'   = @{ Main = '#A8523F'; Accent = '#F0C0A0'; Feature = 'horse' }
    'goat'    = @{ Main = '#CDBB84'; Accent = '#FFF6DA'; Feature = 'goat' }
    'monkey'  = @{ Main = '#B5814A'; Accent = '#F0CE96'; Feature = 'monkey' }
    'rooster' = @{ Main = '#C9502F'; Accent = '#FFD98A'; Feature = 'rooster' }
    'dog'     = @{ Main = '#4B6BB0'; Accent = '#B6CCF5'; Feature = 'dog' }
    'pig'     = @{ Main = '#C58AB0'; Accent = '#FFE0F2'; Feature = 'pig' }
}

# 12 星座 BOSS：角色是「人」，头像用头盔 + 冠饰，不套动物特征。
$BossPalette = [ordered]@{
    'mu'        = @{ Main = '#A83A32'; Accent = '#F0A08C'; Feature = 'crest0' }
    'aldebaran' = @{ Main = '#7E5B34'; Accent = '#E0C080'; Feature = 'crest1' }
    'saga'      = @{ Main = '#5A63A8'; Accent = '#B4BCF0'; Feature = 'crest2' }
    'deathmask' = @{ Main = '#2F7A72'; Accent = '#9EE0D4'; Feature = 'crest3' }
    'aiolia'   = @{ Main = '#B08420'; Accent = '#FFE08A'; Feature = 'crest4' }
    'shaka'     = @{ Main = '#5E8E58'; Accent = '#C0E4AE'; Feature = 'crest5' }
    'dochiki'   = @{ Main = '#6E52A0'; Accent = '#C4AEF0'; Feature = 'crest6' }
    'milo'      = @{ Main = '#3B3055'; Accent = '#9A82C8'; Feature = 'crest7' }
    'aiolos'    = @{ Main = '#B06030'; Accent = '#F0B478'; Feature = 'crest8' }
    'shura'     = @{ Main = '#4F6B4A'; Accent = '#A8C89A'; Feature = 'crest9' }
    'camus'     = @{ Main = '#3E86A8'; Accent = '#A8DCF0'; Feature = 'crest10' }
    'aphrodite' = @{ Main = '#A85F82'; Accent = '#F0B4CE'; Feature = 'crest11' }
}

$AllPalette = @{}
foreach ($k in $PlayablePalette.Keys) { $AllPalette[$k] = $PlayablePalette[$k] }
foreach ($k in $BossPalette.Keys) { $AllPalette[$k] = $BossPalette[$k] }

# ============================ 卡面插画构件 ============================

# 5 个字形族：攻击=刀锋 / 防御=盾 / 特殊=爆星 / 特殊2=法球 / 特殊3=波纹
function Get-FamilyGlyph {
    param(
        [string]$Family,
        [string]$Accent,
        [string]$Light
    )
    switch ($Family) {
        'blade' {
            @(
                ('<path d="M110 22 L127 60 L110 104 L93 60 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />' -f $Accent),
                ('<path d="M72 38 L83 64 L72 90 L61 64 Z" fill="{0}" fill-opacity="0.5" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent),
                ('<path d="M148 38 L159 64 L148 90 L137 64 Z" fill="{0}" fill-opacity="0.5" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent)
            )
        }
        'shield' {
            @(
                ('<path d="M110 20 L148 36 L148 70 C148 94 128 108 110 114 C92 108 72 94 72 70 L72 36 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />' -f $Accent),
                ('<path d="M110 38 L133 47 L133 69 C133 85 120 95 110 99 C100 95 87 85 87 69 L87 47 Z" fill="{0}" fill-opacity="0.45" />' -f $Light)
            )
        }
        'burst' {
            $pts = @()
            for ($i = 0; $i -lt 16; $i++) {
                $rad = if ($i % 2 -eq 0) { 44 } else { 19 }
                $ang = -90 + $i * 22.5
                $px = 110 + $rad * [Math]::Cos($ang * [Math]::PI / 180)
                $py = 66 + $rad * [Math]::Sin($ang * [Math]::PI / 180)
                $pts += ('{0},{1}' -f (Format-Num $px), (Format-Num $py))
            }
            @(
                ('<polygon points="{0}" fill="{1}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />' -f ($pts -join ' '), $Accent),
                ('<circle cx="110" cy="66" r="13" fill="{0}" fill-opacity="0.55" />' -f $Light)
            )
        }
        'orb' {
            @(
                ('<circle cx="110" cy="66" r="30" fill="{0}" stroke="#0B0E14" stroke-width="4" />' -f $Accent),
                ('<ellipse cx="110" cy="66" rx="47" ry="16" fill="none" stroke="{0}" stroke-width="5" transform="rotate(-22 110 66)" />' -f $Light),
                ('<circle cx="110" cy="66" r="12" fill="{0}" fill-opacity="0.6" />' -f $Light)
            )
        }
        'wave' {
            @(
                ('<path d="M52 44 C74 24 92 62 112 44 C132 26 148 62 168 44" fill="none" stroke="{0}" stroke-width="8" stroke-linecap="round" />' -f $Accent),
                ('<path d="M52 68 C74 48 92 86 112 68 C132 50 148 86 168 68" fill="none" stroke="{0}" stroke-width="8" stroke-linecap="round" fill-opacity="0.72" />' -f $Accent),
                ('<path d="M52 92 C74 72 92 110 112 92 C132 74 148 110 168 92" fill="none" stroke="{0}" stroke-width="8" stroke-linecap="round" fill-opacity="0.46" />' -f $Accent)
            )
        }
        default { throw "未知的字形族: $Family" }
    }
}

# 12 个角落徽记：用来在同一字形族里区分不同角色
function Get-OverlayEmblem {
    param(
        [int]$Index,
        [string]$Accent
    )
    $f = 'fill="{0}" fill-opacity="0.85"' -f $Accent
    switch ($Index % 12) {
        0 { @('<path d="M15 15 L35 15 L15 35 Z" ' + $f + ' />') }
        1 { @('<path d="M205 15 L185 15 L205 35 Z" ' + $f + ' />') }
        2 { @('<path d="M15 125 L35 125 L15 105 Z" ' + $f + ' />') }
        3 { @('<path d="M205 125 L185 125 L205 105 Z" ' + $f + ' />') }
        4 { @('<circle cx="22" cy="22" r="8" ' + $f + ' />') }
        5 { @('<circle cx="198" cy="22" r="8" ' + $f + ' />') }
        6 { @('<circle cx="22" cy="118" r="8" ' + $f + ' />') }
        7 { @('<circle cx="198" cy="118" r="8" ' + $f + ' />') }
        8 { @('<rect x="14" y="52" width="8" height="36" rx="4" ' + $f + ' />') }
        9 { @('<rect x="198" y="52" width="8" height="36" rx="4" ' + $f + ' />') }
        10 { @('<rect x="86" y="13" width="48" height="8" rx="4" ' + $f + ' />') }
        default { @('<rect x="86" y="119" width="48" height="8" rx="4" ' + $f + ' />') }
    }
}

# 4 种边框装饰变体
function Get-VariantDecoration {
    param(
        [int]$Index,
        [string]$Light
    )
    switch ($Index % 4) {
        0 { @('<g fill="{0}" fill-opacity="0.35"><circle cx="60" cy="126" r="3" /><circle cx="74" cy="126" r="3" /></g>' -f $Light) }
        1 { @('<path d="M58 126 H100" fill="none" stroke="{0}" stroke-width="3" stroke-opacity="0.35" stroke-linecap="round" />' -f $Light) }
        2 { @('<path d="M118 16 A42 42 0 0 1 162 34" fill="none" stroke="{0}" stroke-width="4" stroke-opacity="0.3" stroke-linecap="round" />' -f $Light) }
        default { @('<g fill="{0}" fill-opacity="0.3"><rect x="178" y="114" width="11" height="11" rx="2" /><rect x="193" y="114" width="11" height="11" rx="2" /></g>' -f $Light) }
    }
}

# 角色类型决定背景构图
function Get-RoleFrame {
    param(
        [string]$Family,
        [string]$Light
    )
    switch ($Family) {
        'blade' {
            @(
                ('<g stroke="{0}" stroke-width="4" stroke-opacity="0.3" stroke-linecap="round"><path d="M168 36 L198 22" /><path d="M172 58 L206 44" /><path d="M166 80 L198 66" /></g>' -f $Light)
            )
        }
        'shield' {
            @(
                ('<g fill="none" stroke="{0}" stroke-width="4" stroke-opacity="0.28"><path d="M58 96 A58 58 0 0 1 58 34" /><path d="M42 102 A78 78 0 0 1 42 28" /></g>' -f $Light)
            )
        }
        'orb' {
            @(
                ('<circle cx="110" cy="66" r="56" fill="none" stroke="{0}" stroke-width="4" stroke-opacity="0.28" />' -f $Light),
                ('<circle cx="110" cy="66" r="64" fill="none" stroke="{0}" stroke-width="3" stroke-opacity="0.16" />' -f $Light)
            )
        }
        'wave' {
            @(
                ('<g fill="none" stroke="{0}" stroke-width="4" stroke-opacity="0.26"><path d="M40 26 H180" /><path d="M40 116 H180" /></g>' -f $Light)
            )
        }
        default {
            @(
                ('<polygon points="110,8 162,36 162,90 110,118 58,90 58,36" fill="none" stroke="{0}" stroke-width="4" stroke-opacity="0.26" stroke-linejoin="round" />' -f $Light)
            )
        }
    }
}

function New-CardArtSvg {
    param(
        [string]$CardId,
        [string]$Main,
        [string]$Accent,
        [string]$Family,
        [int]$Variant,
        [int]$Overlay
    )
    $light = Get-Lighter $Main 0.30
    $dark = Get-Darker $Main 0.42
    $deep = Get-Darker $Main 0.70

    $bgId = "bg_$CardId"
    $glowId = "glow_$CardId"
    $hillId = "hill_$CardId"

    $defs = @(
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $bgId),
        ('  <stop offset="0" stop-color="{0}"/>' -f $dark),
        ('  <stop offset="1" stop-color="{0}"/>' -f $deep),
        ('</linearGradient>'),
        ('<radialGradient id="{0}" cx="0.5" cy="0.5" r="0.5">' -f $glowId),
        ('  <stop offset="0" stop-color="{0}" stop-opacity="0.52"/>' -f $Accent),
        ('  <stop offset="1" stop-color="{0}" stop-opacity="0"/>' -f $Accent),
        ('</radialGradient>'),
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $hillId),
        ('  <stop offset="0" stop-color="{0}"/>' -f $Main),
        ('  <stop offset="1" stop-color="{0}"/>' -f $dark),
        ('</linearGradient>')
    )

    $b = @()
    # 1. 底色
    $b += ('<rect x="0" y="0" width="220" height="140" fill="url(#{0})" />' -f $bgId)

    # 2. 斜向速度光线（背景纹理）
    $b += ('<g stroke="{0}" stroke-width="10" stroke-opacity="0.06" stroke-linecap="round">' -f $Accent)
    $b += '  <path d="M-10 118 L96 12" />'
    $b += '  <path d="M40 132 L150 22" />'
    $b += '  <path d="M96 140 L210 26" />'
    $b += '</g>'

    # 3. 中央辉光
    $b += ('<circle cx="110" cy="66" r="72" fill="url(#{0})" />' -f $glowId)

    # 4. 角色类型框架（压在辉光上、字形下）
    foreach ($l in (Get-RoleFrame -Family $Family -Light $light)) { $b += $l }

    # 5. 主字形（放大 1.15 倍，缩到卡面 130x80 后才看得清）
    $b += '<g transform="translate(110 66) scale(1.15) translate(-110 -66)">'
    foreach ($l in (Get-FamilyGlyph -Family $Family -Accent $Accent -Light $light)) { $b += $l }
    $b += '</g>'

    # 6. 地面剪影（给画面一点纵深）
    $b += ('<path d="M0 120 C40 110 72 126 110 118 C150 109 182 126 220 114 L220 140 L0 140 Z" fill="url(#{0})" fill-opacity="0.55" />' -f $hillId)
    $b += ('<path d="M0 132 C46 124 78 136 118 130 C156 124 190 136 220 128 L220 140 L0 140 Z" fill="{0}" fill-opacity="0.5" />' -f $deep)

    # 7. 变体装饰 + 角落徽记
    foreach ($l in (Get-VariantDecoration -Index $Variant -Light $light)) { $b += $l }
    foreach ($l in (Get-OverlayEmblem -Index $Overlay -Accent $Accent)) { $b += $l }

    # 8. 内描边
    $b += '<rect x="1.5" y="1.5" width="217" height="137" fill="none" stroke="#0B0E14" stroke-width="3" />'

    return New-SvgDocument -Width 220 -Height 140 -Defs $defs -Body $b
}

# ============================ 圆形头像构件 ============================

# 动物角色头部特征（画在头部之上）
function Expand-FeatureTokens {
    param(
        [string[]]$Lines,
        [string]$Accent,
        [string]$Light,
        [string]$Dark
    )
    $out = @()
    foreach ($line in $Lines) {
        $t = $line.Replace('{0}', $Accent).Replace('{1}', $Light).Replace('{2}', $Dark)
        $out += $t
    }
    return $out
}

function Get-AnimalFeature {
    param(
        [string]$Feature,
        [string]$Accent,
        [string]$Light,
        [string]$Dark
    )
    switch ($Feature) {
        'rat' {
            @(
                '<circle cx="45" cy="38" r="15" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<circle cx="83" cy="38" r="15" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<circle cx="45" cy="38" r="7" fill="{1}" />',
                '<circle cx="83" cy="38" r="7" fill="{1}" />'
            )
        }
        'ox' {
            @(
                '<path d="M48 50 C38 38 33 26 34 12" fill="none" stroke="{0}" stroke-width="9" stroke-linecap="round" />',
                '<path d="M80 50 C90 38 95 26 94 12" fill="none" stroke="{0}" stroke-width="9" stroke-linecap="round" />',
                '<ellipse cx="64" cy="86" rx="14" ry="9" fill="{1}" stroke="#0B0E14" stroke-width="3" />'
            )
        }
        'tiger' {
            @(
                '<circle cx="44" cy="40" r="12" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<circle cx="84" cy="40" r="12" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<path d="M54 52 H74" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />',
                '<path d="M56 62 H72" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />',
                '<path d="M64 52 V70" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />'
            )
        }
        'rabbit' {
            @(
                '<ellipse cx="50" cy="26" rx="9" ry="24" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<ellipse cx="78" cy="26" rx="9" ry="24" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<ellipse cx="50" cy="27" rx="4" ry="15" fill="{1}" />',
                '<ellipse cx="78" cy="27" rx="4" ry="15" fill="{1}" />'
            )
        }
        'dragon' {
            @(
                '<path d="M46 56 C32 46 24 46 16 54" fill="none" stroke="{0}" stroke-width="7" stroke-linecap="round" />',
                '<path d="M82 56 C96 46 104 46 112 54" fill="none" stroke="{0}" stroke-width="7" stroke-linecap="round" />',
                '<path d="M60 40 L64 22 L70 40 Z" fill="{1}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
                '<circle cx="64" cy="86" r="5" fill="{1}" stroke="#0B0E14" stroke-width="2.5" />'
            )
        }
        'snake' {
            @(
                '<path d="M28 96 C18 74 22 46 64 46 C106 46 110 74 100 96 Z" fill="{0}" fill-opacity="0.45" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
                '<path d="M64 88 L64 104" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />',
                '<path d="M64 104 L58 114" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />',
                '<path d="M64 104 L70 114" fill="none" stroke="{1}" stroke-width="4" stroke-linecap="round" />'
            )
        }
        'horse' {
            @(
                '<path d="M46 48 L40 16 L58 40 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
                '<path d="M82 48 L88 16 L70 40 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
                '<path d="M50 42 C56 26 72 26 78 42" fill="none" stroke="{1}" stroke-width="8" stroke-linecap="round" />',
                '<ellipse cx="64" cy="88" rx="16" ry="10" fill="{1}" stroke="#0B0E14" stroke-width="3" />'
            )
        }
        'goat' {
            @(
                '<path d="M50 44 C34 36 30 18 44 14 C56 11 58 26 50 36" fill="none" stroke="{0}" stroke-width="8" stroke-linecap="round" />',
                '<path d="M78 44 C94 36 98 18 84 14 C72 11 70 26 78 36" fill="none" stroke="{0}" stroke-width="8" stroke-linecap="round" />',
                '<path d="M58 92 L64 112 L70 92 Z" fill="{1}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />'
            )
        }
        'monkey' {
            @(
                '<circle cx="34" cy="72" r="12" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<circle cx="94" cy="72" r="12" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<path d="M56 34 C60 22 68 22 72 34" fill="none" stroke="{1}" stroke-width="8" stroke-linecap="round" />',
                '<ellipse cx="64" cy="90" rx="15" ry="10" fill="{1}" stroke="#0B0E14" stroke-width="3" />'
            )
        }
        'rooster' {
            @(
                '<path d="M52 42 C52 28 60 26 64 34 C68 24 76 26 76 40 Z" fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
                '<path d="M76 68 L92 74 L76 80 Z" fill="{1}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
                '<path d="M48 54 C36 62 36 78 48 84" fill="none" stroke="{0}" stroke-width="6" stroke-linecap="round" />'
            )
        }
        'dog' {
            @(
                '<ellipse cx="30" cy="70" rx="11" ry="20" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<ellipse cx="98" cy="70" rx="11" ry="20" fill="{0}" stroke="#0B0E14" stroke-width="4" />',
                '<ellipse cx="64" cy="88" rx="15" ry="11" fill="{1}" stroke="#0B0E14" stroke-width="3" />',
                '<circle cx="64" cy="83" r="4" fill="#0B0E14" />'
            )
        }
        'pig' {
            @(
                '<path d="M40 48 L26 30 L44 40 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
                '<path d="M88 48 L102 30 L84 40 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
                '<ellipse cx="64" cy="88" rx="16" ry="12" fill="{1}" stroke="#0B0E14" stroke-width="3" />',
                '<circle cx="59" cy="88" r="2.6" fill="#0B0E14" />',
                '<circle cx="69" cy="88" r="2.6" fill="#0B0E14" />'
            )
        }
        default { @() }
    }
}

# BOSS 头盔冠饰（12 种）
function Get-CrestFeature {
    param(
        [string]$Feature,
        [string]$Accent,
        [string]$Light
    )
    switch ($Feature) {
        'crest0' { @('<path d="M64 8 L72 32 L56 32 Z" fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent) }
        'crest1' { @('<path d="M42 28 C34 14 52 8 64 20 C76 8 94 14 86 28 Z" fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent) }
        'crest2' { @('<path d="M50 10 L64 32 L78 10 M64 32 L64 44" fill="none" stroke="{0}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" />' -f $Accent) }
        'crest3' { @('<path d="M64 6 A16 16 0 1 1 63 6 Z" fill="none" stroke="{0}" stroke-width="5" />' -f $Accent) }
        'crest4' { @('<path d="M40 30 L50 12 L60 30 L68 12 L78 30 L88 12" fill="none" stroke="{0}" stroke-width="6" stroke-linejoin="round" stroke-linecap="round" />' -f $Accent) }
        'crest5' { @('<path d="M64 6 L76 34 L52 34 Z" fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent) }
        'crest6' { @('<path d="M42 26 L64 8 L86 26" fill="none" stroke="{0}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" />' -f $Accent) }
        'crest7' { @('<circle cx="64" cy="22" r="14" fill="none" stroke="{0}" stroke-width="5" />' -f $Accent) }
        'crest8' { @('<path d="M34 34 C34 14 52 8 64 22 C76 8 94 14 94 34" fill="none" stroke="{0}" stroke-width="6" stroke-linecap="round" />' -f $Accent) }
        'crest9' { @('<path d="M46 30 L64 10 L82 30 M46 30 H82" fill="none" stroke="{0}" stroke-width="5" stroke-linejoin="round" stroke-linecap="round" />' -f $Accent) }
        'crest10' { @('<circle cx="64" cy="20" r="11" fill="{0}" stroke="#0B0E14" stroke-width="3" />' -f $Accent) }
        default { @('<path d="M44 32 C44 12 60 6 64 20 C68 6 84 12 84 32" fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />' -f $Accent) }
    }
}

function New-AvatarSvg {
    param(
        [string]$CharId,
        [string]$Main,
        [string]$Accent,
        [string]$Feature
    )
    # 三层配色：圆盘 = 主色提亮 / 头 = 主色 / 特征 = 辅色（亮）
    # 目的：头像只有 64x64，必须靠「三层明度差 + 黑色描边」拉开层次，
    # 否则整个头像糊成一团深色，在暗底 UI 上等于看不见。
    $disc = Get-Lighter $Main 0.45
    $headLight = Get-Lighter $Main 0.20
    $featureLight = Get-Lighter $Accent 0.25
    $dark = Get-Darker $Main 0.35

    $bgId = "bg_$CharId"
    $headId = "head_$CharId"

    $defs = @(
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $bgId),
        ('  <stop offset="0" stop-color="{0}"/>' -f $disc),
        ('  <stop offset="1" stop-color="{0}"/>' -f $Main),
        ('</linearGradient>'),
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $headId),
        ('  <stop offset="0" stop-color="{0}"/>' -f $headLight),
        ('  <stop offset="1" stop-color="{0}"/>' -f $Main),
        ('</linearGradient>')
    )

    $b = @()
    # 圆形底 + 渐变
    $b += '<circle cx="64" cy="64" r="64" fill="#0B0E14" />'
    $b += ('<circle cx="64" cy="64" r="60" fill="url(#{0})" />' -f $bgId)

    # BOSS：头盔（先画，压在头下）
    if ($Feature.StartsWith('crest')) {
        $b += ('<path d="M26 84 C26 48 42 34 64 34 C86 34 102 48 102 84 Z" fill="{0}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />' -f $dark)
        $b += ('<path d="M34 66 H94" fill="none" stroke="{0}" stroke-width="6" stroke-linecap="round" />' -f $Accent)
    }

    # 头
    $b += ('<circle cx="64" cy="70" r="30" fill="url(#{0})" stroke="#0B0E14" stroke-width="4" />' -f $headId)

    # 眼
    $b += '<ellipse cx="54" cy="68" rx="4" ry="5" fill="#0B0E14" />'
    $b += '<ellipse cx="74" cy="68" rx="4" ry="5" fill="#0B0E14" />'

    # 特征
    if ($Feature.StartsWith('crest')) {
        foreach ($l in (Get-CrestFeature -Feature $Feature -Accent $Accent -Light $light)) { $b += $l }
    } else {
        $featLines = Get-AnimalFeature -Feature $Feature -Accent $Accent -Light $featureLight -Dark $dark
        foreach ($l in (Expand-FeatureTokens -Lines $featLines -Accent $Accent -Light $featureLight -Dark $dark)) { $b += $l }
    }

    # 外圈
    $b += ('<circle cx="64" cy="64" r="60" fill="none" stroke="{0}" stroke-width="4" />' -f $Accent)

    return New-SvgDocument -Width 128 -Height 128 -Defs $defs -Body $b
}

# ============================ 钥令图标 ============================

function New-KeyOrderSvg {
    $defs = @(
        '<linearGradient id="kgold" x1="0" y1="0" x2="0" y2="1">',
        '  <stop offset="0" stop-color="#FFE9A8"/>',
        '  <stop offset="1" stop-color="#C08A32"/>',
        '</linearGradient>'
    )
    $b = @()
    # 柄
    $b += '<circle cx="32" cy="17" r="12" fill="url(#kgold)" stroke="#0B0E14" stroke-width="3" />'
    $b += '<circle cx="32" cy="17" r="5" fill="#0B0E14" fill-opacity="0.55" />'
    # 杆
    $b += '<rect x="27.5" y="26" width="9" height="30" rx="2" fill="url(#kgold)" stroke="#0B0E14" stroke-width="3" />'
    # 齿
    $b += '<rect x="35" y="38" width="10" height="6" rx="2" fill="url(#kgold)" stroke="#0B0E14" stroke-width="3" />'
    $b += '<rect x="35" y="49" width="10" height="6" rx="2" fill="url(#kgold)" stroke="#0B0E14" stroke-width="3" />'
    return New-SvgDocument -Width 64 -Height 64 -Defs $defs -Body $b
}

# ============================ 写文件 ============================

function Write-SvgFile {
    param(
        [string]$Path,
        [string]$Content
    )
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

# ============================ 主流程 ============================

if (-not (Test-Path -LiteralPath $CardDataPath)) {
    throw "找不到卡牌数据文件：$CardDataPath"
}

$cardJson = Get-Content -LiteralPath $CardDataPath -Encoding UTF8 -Raw | ConvertFrom-Json
$cards = @($cardJson.cards)
if ($cards.Count -eq 0) { throw "character_cards.json 里没有读到任何卡牌" }

# 角色索引：决定变体与徽记偏移
$charOrder = @()
foreach ($k in $PlayablePalette.Keys) { $charOrder += $k }
foreach ($k in $BossPalette.Keys) { $charOrder += $k }
$charIndexMap = @{}
for ($i = 0; $i -lt $charOrder.Count; $i++) { $charIndexMap[$charOrder[$i]] = $i }

# 卡号后缀 -> 字形族
$familyMap = @{
    'attack'   = 'blade'
    'defense'  = 'shield'
    'special'  = 'burst'
    'special2' = 'orb'
    'special3' = 'wave'
}

# 字形族 -> 角落徽记偏移（同一角色的 5 张卡徽记各不相同）
$familyOffset = @{
    'blade'  = 0
    'shield' = 4
    'burst'  = 8
    'orb'    = 2
    'wave'   = 6
}

$written = @()
$skipped = @()

foreach ($card in $cards) {
    $cardId = [string]$card.cardId
    $charId = [string]$card.characterId

    if ([string]::IsNullOrWhiteSpace($cardId)) { $skipped += '(空 cardId)'; continue }
    if (-not $AllPalette.ContainsKey($charId)) { $skipped += "$cardId (未知角色 $charId)"; continue }

    $parts = $cardId -split '_'
    $suffix = $parts[-1]
    if (-not $familyMap.ContainsKey($suffix)) { $skipped += "$cardId (未知后缀 $suffix)"; continue }

    $pal = $AllPalette[$charId]
    $idx = $charIndexMap[$charId]
    $family = $familyMap[$suffix]
    $variant = $idx % 4
    $overlay = ($idx + $familyOffset[$family]) % 12

    $svg = New-CardArtSvg -CardId $cardId -Main $pal.Main -Accent $pal.Accent -Family $family -Variant $variant -Overlay $overlay
    Write-SvgFile -Path (Join-Path $CardDir "card_$cardId.svg") -Content $svg
    $written += "Assets/Cards/card_$cardId.svg"
}

# 圆形头像：12 可玩角色 + 12 BOSS
foreach ($charId in $charOrder) {
    $pal = $AllPalette[$charId]
    $svg = New-AvatarSvg -CharId $charId -Main $pal.Main -Accent $pal.Accent -Feature $pal.Feature
    Write-SvgFile -Path (Join-Path $PortraitDir "avatar_$charId.svg") -Content $svg
    $written += "Assets/Portraits/avatar_$charId.svg"
}

# 钥令图标
Write-SvgFile -Path (Join-Path $IconDir 'keyorder.svg') -Content (New-KeyOrderSvg)
$written += 'Assets/Icons/keyorder.svg'

# ============================ 自检 ============================

$errors = @()
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)

foreach ($rel in $written) {
    $path = Join-Path $RepoRoot ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path)) { $errors += "$rel : 文件不存在"; continue }

    $bytes = [System.IO.File]::ReadAllBytes($path)
    # BOM 检查
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $errors += "$rel : 含 UTF-8 BOM"
    }
    # CRLF 检查
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($text.Contains("`r`n")) { $errors += "$rel : 含 CRLF 换行" }
    if (-not $text.EndsWith("`n")) { $errors += "$rel : 末尾缺少换行" }
    # 禁用元素
    foreach ($tag in @('<text', '<filter', '<mask', '<clipPath', '<image', 'rgba(', 'rgb(')) {
        if ($text.Contains($tag)) { $errors += "$rel : 含禁用内容 $tag" }
    }
    # 根元素
    if (-not $text.StartsWith('<svg xmlns="http://www.w3.org/2000/svg" width="')) {
        $errors += "$rel : 根元素格式不对"
    }
    if (-not $text.Contains('viewBox="0 0 ')) { $errors += "$rel : viewBox 必须以 0 0 开头" }
    # 渐变 id 唯一
    $ids = [regex]::Matches($text, 'id="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
    if (($ids | Select-Object -Unique).Count -ne $ids.Count) { $errors += "$rel : 存在重复的 id" }
}

Write-Host ''
Write-Host "已生成：$($written.Count) 个文件"
Write-Host "  卡面插画：$(($written | Where-Object { $_ -like 'Assets/Cards/*' }).Count) 张"
Write-Host "  圆形头像：$(($written | Where-Object { $_ -like 'Assets/Portraits/*' }).Count) 个"
Write-Host "  钥令图标：1 个"
if ($skipped.Count -gt 0) {
    Write-Host ''
    Write-Host "跳过：$($skipped.Count) 条"
    foreach ($s in $skipped) { Write-Host "  - $s" }
}

if ($errors.Count -gt 0) {
    Write-Host ''
    Write-Host "自检未通过：$($errors.Count) 项" -ForegroundColor Red
    foreach ($e in $errors) { Write-Host "  - $e" -ForegroundColor Red }
    exit 1
}

Write-Host ''
Write-Host '自检通过。' -ForegroundColor Green
exit 0
