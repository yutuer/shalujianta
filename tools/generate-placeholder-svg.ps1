#Requires -Version 5.1
<#
.SYNOPSIS
    生成「程序化占位」阶段的 SVG 美术资源（12 角色立绘 / 6 敌人立绘 / 16 状态图标）。

.DESCRIPTION
    本脚本只写 .svg 文件，不碰任何 .cs / .tscn / .json / project.godot。

    生成的 SVG 必须满足 Godot 4.6 的 ThorVG 导入器限制（每条都写进校验里）：
      - 根元素同时带 xmlns / width / height / viewBox，且 viewBox 必须从 "0 0 W H" 开始
        （反面教材：Assets/Icons/skull.svg 用了 viewBox="-32 -32 64 64"，导入尺寸会错乱）
      - 不用 <text>（ThorVG 不支持，文字会直接消失）
      - 不用 <filter> / <mask> / <clipPath> / <image>
      - 颜色一律 hex，半透明用 fill-opacity / stroke-opacity，不用 rgba()/rgb()
      - 渐变写 x1/y1/x2/y2 为 0~1 的小数，id 在同一文件内不重名
      - 文件为 UTF-8 无 BOM + LF 换行，末尾留一个换行

    风格基调：深色冷调、扁平剪影、暗底 + 描边 + 低饱和主色 + 一点亮色点缀。

.PARAMETER RepoRoot
    仓库根目录。默认为本脚本所在目录的上一级。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\generate-placeholder-svg.ps1
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

$PortraitDir = Join-Path $RepoRoot 'Assets\Portraits'
$IconDir = Join-Path $RepoRoot 'Assets\Icons'

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

# ============================ 胸章几何形 ============================
# 12 个角色 12 种、6 个敌人 6 种，全部互不相同。

function Get-Emblem {
    param(
        [string]$Kind,
        [string]$Color,
        [double]$CX = 128,
        [double]$CY = 205
    )
    $cx = Format-Num $CX
    $cy = Format-Num $CY
    $up = Format-Num ($CY - 14)
    $dn = Format-Num ($CY + 14)
    $lf = Format-Num ($CX - 13)
    $rt = Format-Num ($CX + 13)
    $lf7 = Format-Num ($CX - 7)
    $rt7 = Format-Num ($CX + 7)
    $c7 = Format-Num ($CY - 7)
    $c5 = Format-Num ($CY - 5)
    $c5b = Format-Num ($CY + 5)
    $fill = ('fill="{0}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round"' -f $Color)

    switch ($Kind) {
        'diamond' {
            '<path d="M{0} {1} L{2} {3} L{0} {4} L{5} {3} Z" {6} />' -f $cx, $up, $rt, $cy, $dn, $lf, $fill
        }
        'triangleUp' {
            '<path d="M{0} {1} L{2} {3} L{4} {3} Z" {5} />' -f $cx, $up, $rt, $dn, $lf, $fill
        }
        'triangleDown' {
            '<path d="M{0} {1} L{2} {1} L{3} {4} Z" {5} />' -f $lf, $up, $rt, $cx, $dn, $fill
        }
        'circle' {
            '<circle cx="{0}" cy="{1}" r="13" {2} />' -f $cx, $cy, $fill
        }
        'ellipse' {
            '<ellipse cx="{0}" cy="{1}" rx="15" ry="9" {2} />' -f $cx, $cy, $fill
        }
        'ring' {
            '<ellipse cx="{0}" cy="{1}" rx="12" ry="12" fill="none" stroke="{2}" stroke-width="5" />' -f $cx, $cy, $Color
        }
        'hexagon' {
            '<polygon points="{0},{1} {2},{3} {2},{4} {0},{5} {6},{4} {6},{3}" {7} />' -f $cx, $up, $rt, $c7, $c5b, $dn, $lf, $fill
        }
        'star5' {
            $pts = @()
            for ($i = 0; $i -lt 10; $i++) {
                $rad = if ($i % 2 -eq 0) { 15 } else { 6.5 }
                $ang = -90 + $i * 36
                $px = $CX + $rad * [Math]::Cos($ang * [Math]::PI / 180)
                $py = $CY + $rad * [Math]::Sin($ang * [Math]::PI / 180)
                $pts += ('{0},{1}' -f (Format-Num $px), (Format-Num $py))
            }
            '<polygon points="{0}" {1} />' -f ($pts -join ' '), $fill
        }
        'cross' {
            '<path d="M{0} {1} H{2} V{3} H{4} V{5} H{2} V{6} H{0} V{5} H{7} V{3} H{0} Z" {8} />' -f `
                (Format-Num ($CX - 5)), $up, (Format-Num ($CX + 5)), $c5, (Format-Num ($CX + 14)), $c5b, $dn, (Format-Num ($CX - 14)), $fill
        }
        'wave' {
            '<path d="M{0} {1} C{2} {3} {4} {5} {6} {7}" fill="none" stroke="{8}" stroke-width="6" stroke-linecap="round" />' -f `
                (Format-Num ($CX - 15)), (Format-Num ($CY + 5)), (Format-Num ($CX - 7)), (Format-Num ($CY - 13)), `
                (Format-Num ($CX + 1)), (Format-Num ($CY + 15)), (Format-Num ($CX + 15)), (Format-Num ($CY - 4)), $Color
        }
        'chevron' {
            $a = '<path d="M{0} {1} L{2} {3} L{4} {1}" fill="none" stroke="{5}" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />' -f `
                (Format-Num ($CX - 13)), (Format-Num ($CY - 2)), $cx, (Format-Num ($CY + 10)), (Format-Num ($CX + 13)), $Color
            $b = '<path d="M{0} {1} L{2} {3} L{4} {1}" fill="none" stroke="{5}" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />' -f `
                (Format-Num ($CX - 13)), (Format-Num ($CY - 12)), $cx, (Format-Num ($CY)), (Format-Num ($CX + 13)), $Color
            "$a`n    $b"
        }
        'square' {
            '<rect x="{0}" y="{1}" width="22" height="22" rx="3" {2} />' -f (Format-Num ($CX - 11)), (Format-Num ($CY - 11)), $fill
        }
        'saltire' {
            '<path d="M{0} {1} L{2} {3} M{2} {1} L{0} {3}" fill="none" stroke="{4}" stroke-width="6" stroke-linecap="round" />' -f `
                $lf, $up, $rt, $dn, $Color
        }
        'doubleCircle' {
            $a = '<circle cx="{0}" cy="{1}" r="13" fill="none" stroke="{2}" stroke-width="4" />' -f $cx, $cy, $Color
            $b = '<circle cx="{0}" cy="{1}" r="5" fill="{2}" stroke="#0B0E14" stroke-width="2" />' -f $cx, $cy, $Color
            "$a`n    $b"
        }
        'teardrop' {
            '<path d="M{0} {1} C{2} {3} {4} {5} {4} {6} A12 12 0 1 1 {7} {6} C{7} {5} {8} {3} {0} {1} Z" {9} />' -f `
                $cx, $up, (Format-Num ($CX + 6)), (Format-Num ($CY - 4)), (Format-Num ($CX + 12)), `
                (Format-Num ($CY + 1)), (Format-Num ($CY + 5)), (Format-Num ($CX - 12)), (Format-Num ($CX - 6)), $fill
        }
        'bars' {
            $a = '<rect x="{0}" y="{1}" width="24" height="4" rx="2" {2} />' -f (Format-Num ($CX - 12)), (Format-Num ($CY - 10)), $fill
            $b = '<rect x="{0}" y="{1}" width="24" height="4" rx="2" {2} />' -f (Format-Num ($CX - 12)), (Format-Num ($CY - 2)), $fill
            $c = '<rect x="{0}" y="{1}" width="24" height="4" rx="2" {2} />' -f (Format-Num ($CX - 12)), (Format-Num ($CY + 6)), $fill
            "$a`n    $b`n    $c"
        }
        'scale' {
            '<path d="M{0} {1} C{2} {3} {4} {5} {6} {7}" fill="none" stroke="{8}" stroke-width="6" stroke-linecap="round" />' -f `
                (Format-Num ($CX + 9)), $up, (Format-Num ($CX - 11)), (Format-Num ($CY - 7)), `
                (Format-Num ($CX + 12)), (Format-Num ($CY + 3)), (Format-Num ($CX - 8)), (Format-Num ($CY + 8)), $Color
        }
        'sparkle' {
            '<path d="M{0} {1} C{2} {3} {4} {5} {6} {7} C{4} {8} {2} {9} {0} {10} C{11} {9} {12} {8} {13} {7} C{12} {5} {11} {3} {0} {1} Z" {14} />' -f `
                $cx, (Format-Num ($CY - 15)), (Format-Num ($CX + 3)), (Format-Num ($CY - 4)), (Format-Num ($CX + 4)), `
                (Format-Num ($CY - 3)), (Format-Num ($CX + 15)), $cy, (Format-Num ($CY + 3)), (Format-Num ($CY + 4)), `
                (Format-Num ($CY + 15)), (Format-Num ($CX - 3)), (Format-Num ($CX - 4)), (Format-Num ($CX - 15)), $fill
        }
        'crown' {
            '<path d="M{0} {1} L{0} {2} L{3} {4} L{5} {6} L{7} {4} L{8} {2} L{8} {1} Z" {9} />' -f `
                (Format-Num ($CX - 14)), (Format-Num ($CY - 6)), (Format-Num ($CY + 10)), (Format-Num ($CX - 5)), `
                (Format-Num ($CY + 2)), $cx, (Format-Num ($CY - 10)), (Format-Num ($CX + 5)), (Format-Num ($CX + 14)), $fill
        }
        default { throw "未知的胸章类型: $Kind" }
    }
}

# ============================ 立绘构造 ============================

function Get-BodyPath {
    param(
        [double]$Sh, [double]$Wa, [double]$Hp,
        [double]$NeckY = 132, [double]$ShY = 158, [double]$WaY = 214, [double]$HpY = 250, [double]$BaseY = 296
    )
    $shL = 128 - $Sh; $shR = 128 + $Sh
    $waL = 128 - $Wa; $waR = 128 + $Wa
    $hpL = 128 - $Hp; $hpR = 128 + $Hp
    $neckL = 110; $neckR = 146

    $d = @()
    $d += ('M{0} {1}' -f $neckL, (Format-Num $NeckY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($neckL + ($shL - $neckL) * 0.45)), (Format-Num ($NeckY + 3)),
        (Format-Num ($neckL + ($shL - $neckL) * 0.85)), (Format-Num ($ShY - 14)),
        (Format-Num $shL), (Format-Num $ShY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($shL + 4)), (Format-Num ($WaY - 26)),
        (Format-Num ($waL - 4)), (Format-Num ($WaY - 12)),
        (Format-Num $waL), (Format-Num $WaY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($waL + 2)), (Format-Num ($HpY - 20)),
        (Format-Num ($hpL + 6)), (Format-Num ($HpY - 9)),
        (Format-Num $hpL), (Format-Num $HpY))
    $d += ('L{0} {1}' -f (Format-Num ($hpL - 3)), (Format-Num $BaseY))
    $d += ('L{0} {1}' -f (Format-Num ($hpR + 3)), (Format-Num $BaseY))
    $d += ('L{0} {1}' -f (Format-Num $hpR), (Format-Num $HpY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($hpR - 6)), (Format-Num ($HpY - 9)),
        (Format-Num ($waR - 2)), (Format-Num ($HpY - 20)),
        (Format-Num $waR), (Format-Num $WaY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($waR + 4)), (Format-Num ($WaY - 12)),
        (Format-Num ($shR - 4)), (Format-Num ($WaY - 26)),
        (Format-Num $shR), (Format-Num $ShY))
    $d += ('C{0} {1} {2} {3} {4} {5}' -f `
        (Format-Num ($shR + ($neckR - $shR) * 0.85)), (Format-Num ($ShY - 14)),
        (Format-Num ($shR + ($neckR - $shR) * 0.45)), (Format-Num ($NeckY + 3)),
        $neckR, (Format-Num $NeckY))
    $d += 'Z'
    return ($d -join ' ')
}

function New-SvgDocument {
    param(
        [int]$Width,
        [int]$Height,
        [string]$ViewBox,
        [string[]]$Defs = @(),
        [string[]]$Body = @()
    )
    $lines = @()
    $lines += ('<svg xmlns="http://www.w3.org/2000/svg" width="{0}" height="{1}" viewBox="{2}">' -f $Width, $Height, $ViewBox)
    if ($Defs.Count -gt 0) {
        $lines += '  <defs>'
        foreach ($d in $Defs) { $lines += ('    ' + $d) }
        $lines += '  </defs>'
    }
    foreach ($b in $Body) { $lines += ('  ' + $b) }
    $lines += '</svg>'
    return (($lines -join "`n") + "`n")
}

function Expand-Tokens {
    param([string[]]$Lines, [hashtable]$Map)
    $out = @()
    foreach ($line in $Lines) {
        $t = $line
        foreach ($k in $Map.Keys) { $t = $t.Replace($k, $Map[$k]) }
        $out += $t
    }
    return $out
}

function New-PortraitSvg {
    param(
        [string]$SvgId = 'x',
        [string]$Main,
        [string]$Accent,
        [double]$Sh, [double]$Wa, [double]$Hp,
        [double]$NeckY = 132, [double]$ShY = 158, [double]$WaY = 214, [double]$HpY = 250, [double]$BaseY = 296,
        [double]$HeadCX = 128, [double]$HeadCY = 104, [double]$HeadR = 36,
        [string[]]$Behind = @(),
        [string[]]$After = @(),
        [string]$Eyes = '',
        [string]$Emblem = 'circle',
        [string]$BodyPath = '',
        [string]$Collar = 'auto'
    )
    $light = Get-Lighter $Main 0.20
    $dark = Get-Darker $Main 0.32
    $aLight = Get-Lighter $Accent 0.30
    $aDark = Get-Darker $Accent 0.28

    # 渐变 id 按文件唯一：Godot 逐文件导入时同名也没问题，但同名 id 一旦被内联到同一份
    # HTML / 文档里，url(#...) 会全部解析到第一个同名 id，所有角色会串成同一个颜色。
    $bodyGradId = "bodyGrad_$SvgId"
    $headGradId = "headGrad_$SvgId"

    $map = @{
        '{MAIN}'   = $Main
        '{LIGHT}'  = $light
        '{DARK}'   = $dark
        '{ACC}'    = $Accent
        '{ALIGHT}' = $aLight
        '{ADARK}'  = $aDark
        '{HEADGRAD}' = $headGradId
    }

    $defs = @(
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $bodyGradId)
        ('  <stop offset="0" stop-color="{0}"/>' -f $Main)
        ('  <stop offset="1" stop-color="{0}"/>' -f $dark)
        '</linearGradient>'
        ('<linearGradient id="{0}" x1="0" y1="0" x2="0" y2="1">' -f $headGradId)
        ('  <stop offset="0" stop-color="{0}"/>' -f $light)
        ('  <stop offset="1" stop-color="{0}"/>' -f $Main)
        '</linearGradient>'
    )

    $b = @()
    # 1. 底部投影
    $b += '<ellipse cx="128" cy="302" rx="76" ry="13" fill="#000000" fill-opacity="0.42" />'

    # 2. 身体之前的装饰（尾巴 / 鬃毛 / 盘尾）
    foreach ($l in (Expand-Tokens -Lines $Behind -Map $map)) { $b += $l }

    # 3. 身体剪影
    $bp = if ($BodyPath) { $BodyPath } else { Get-BodyPath -Sh $Sh -Wa $Wa -Hp $Hp -NeckY $NeckY -ShY $ShY -WaY $WaY -HpY $HpY -BaseY $BaseY }
    $b += ('<path d="{0}" fill="url(#{1})" stroke="#0B0E14" stroke-width="5" stroke-linejoin="round" />' -f $bp, $bodyGradId)

    # 4. 领口
    $collar = if ($Collar -eq 'auto') {
        '<path d="M106 142 L128 166 L150 142 L158 154 L128 184 L98 154 Z" fill="{ADARK}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />'
    } else { $Collar }
    if ($collar) {
        foreach ($l in (Expand-Tokens -Lines @($collar) -Map $map)) { $b += $l }
    }

    # 5. 头
    $b += ('<circle cx="{0}" cy="{1}" r="{2}" fill="url(#{3})" stroke="#0B0E14" stroke-width="5" />' -f `
        (Format-Num $HeadCX), (Format-Num $HeadCY), (Format-Num $HeadR), $headGradId)

    # 6. 眼睛
    if ($Eyes) {
        foreach ($l in (Expand-Tokens -Lines ($Eyes -split "`n") -Map $map)) { $b += $l }
    } else {
        $ex = Format-Num ($HeadCX - 15)
        $ex2 = Format-Num ($HeadCX + 15)
        $ey = Format-Num ($HeadCY + 2)
        $b += ('<ellipse cx="{0}" cy="{1}" rx="4.5" ry="5.5" fill="#0B0E14" />' -f $ex, $ey)
        $b += ('<ellipse cx="{0}" cy="{1}" rx="4.5" ry="5.5" fill="#0B0E14" />' -f $ex2, $ey)
    }

    # 7. 头部特征等（画在头之上）
    foreach ($l in (Expand-Tokens -Lines $After -Map $map)) { $b += $l }

    # 8. 胸口徽记
    $emb = Get-Emblem -Kind $Emblem -Color $Accent -CX 128 -CY 205
    foreach ($l in ($emb -split "`n")) { $b += ($l -replace '^ +', '') }

    return New-SvgDocument -Width 256 -Height 320 -ViewBox '0 0 256 320' -Defs $defs -Body $b
}

# ============================ 12 个角色 ============================
# {MAIN}=主色 {LIGHT}=主色提亮 {DARK}=主色压暗
# {ACC}=辅色 {ALIGHT}=辅色提亮 {ADARK}=辅色压暗

$Characters = [ordered]@{}

$Characters['rat'] = @{
    Main = '#7C6E96'; Accent = '#D8C8E8'
    Sh = 60; Wa = 44; Hp = 52
    Behind = @(
        '<path d="M180 252 C204 250 214 226 199 218 C188 212 181 226 190 233" fill="none" stroke="{ACC}" stroke-width="7" stroke-linecap="round" />'
    )
    After = @(
        '<circle cx="96" cy="58" r="18" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<circle cx="160" cy="58" r="18" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<circle cx="96" cy="58" r="9" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="160" cy="58" r="9" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />'
    )
    Emblem = 'diamond'
}

$Characters['pollux'] = @{
    Main = '#5A63A8'; Accent = '#B4BCF0'
    Sh = 66; Wa = 48; Hp = 56
    Behind = @(
        '<path d="M64 86 L128 40 L192 86 L128 132 Z" fill="none" stroke="{ACC}" stroke-width="5" stroke-linejoin="round" />'
    )
    After = @(
        '<circle cx="96" cy="58" r="18" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<circle cx="160" cy="58" r="18" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<circle cx="96" cy="58" r="9" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="160" cy="58" r="9" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />'
    )
    Emblem = 'diamond'
}

$Characters['ox'] = @{
    Main = '#9C7048'; Accent = '#E8C89A'
    Sh = 74; Wa = 56; Hp = 64
    After = @(
        '<path d="M102 80 C88 66 80 52 78 34" fill="none" stroke="{ACC}" stroke-width="13" stroke-linecap="round" />',
        '<path d="M154 80 C168 66 176 52 178 34" fill="none" stroke="{ACC}" stroke-width="13" stroke-linecap="round" />',
        '<ellipse cx="128" cy="126" rx="17" ry="11" fill="{LIGHT}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="121" cy="124" r="2.6" fill="#0B0E14" />',
        '<circle cx="135" cy="124" r="2.6" fill="#0B0E14" />',
        '<circle cx="128" cy="141" r="8" fill="none" stroke="{ACC}" stroke-width="4" />'
    )
    Emblem = 'triangleUp'
}

$Characters['tiger'] = @{
    Main = '#D8912F'; Accent = '#FFE0A0'
    Sh = 72; Wa = 54; Hp = 62
    After = @(
        '<circle cx="98" cy="64" r="13" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="158" cy="64" r="13" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<path d="M112 78 H144" fill="none" stroke="{ACC}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M114 88 H142" fill="none" stroke="{ACC}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M112 98 H144" fill="none" stroke="{ACC}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M128 78 V98" fill="none" stroke="{ACC}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M118 126 Q128 134 138 126" fill="none" stroke="#0B0E14" stroke-width="4" stroke-linecap="round" />',
        '<path d="M100 118 L86 114" fill="none" stroke="{ACC}" stroke-width="3" stroke-linecap="round" />',
        '<path d="M156 118 L170 114" fill="none" stroke="{ACC}" stroke-width="3" stroke-linecap="round" />'
    )
    Eyes = @(
        '<ellipse cx="112" cy="112" rx="4.5" ry="5.5" fill="#0B0E14" />',
        '<ellipse cx="144" cy="112" rx="4.5" ry="5.5" fill="#0B0E14" />'
    ) -join "`n"
    Emblem = 'circle'
}

$Characters['rabbit'] = @{
    Main = '#D9A8BE'; Accent = '#FFF0F6'
    Sh = 58; Wa = 42; Hp = 50
    Behind = @(
        '<circle cx="192" cy="238" r="12" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />'
    )
    After = @(
        '<ellipse cx="110" cy="46" rx="12" ry="34" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<ellipse cx="146" cy="46" rx="12" ry="34" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="5" />',
        '<ellipse cx="110" cy="48" rx="5" ry="22" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<ellipse cx="146" cy="48" rx="5" ry="22" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<path d="M124 118 L132 118 L128 126 Z" fill="{DARK}" stroke="#0B0E14" stroke-width="2" stroke-linejoin="round" />'
    )
    Emblem = 'hexagon'
}

$Characters['dragon'] = @{
    Main = '#2F7E93'; Accent = '#8FE6F2'
    Sh = 66; Wa = 48; Hp = 56
    After = @(
        '<path d="M106 74 C90 58 76 60 62 74" fill="none" stroke="{ACC}" stroke-width="10" stroke-linecap="round" />',
        '<path d="M150 74 C166 58 180 60 194 74" fill="none" stroke="{ACC}" stroke-width="10" stroke-linecap="round" />',
        '<polygon points="190,158 220,150 198,180" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<polygon points="198,188 224,184 202,212" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<polygon points="196,218 220,220 198,244" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<circle cx="128" cy="128" r="5" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2.5" />'
    )
    Emblem = 'star5'
}

$Characters['snake'] = @{
    Main = '#3E9455'; Accent = '#B6F0C6'
    Sh = 56; Wa = 40; Hp = 48
    Behind = @(
        '<ellipse cx="66" cy="268" rx="30" ry="14" fill="none" stroke="{ACC}" stroke-width="12" />',
        '<path d="M96 270 C108 254 96 238 82 244" fill="none" stroke="{MAIN}" stroke-width="12" stroke-linecap="round" />'
    )
    After = @(
        '<path d="M106 70 L128 38 L150 70 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<rect x="104" y="66" width="48" height="10" rx="4" fill="{MAIN}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="128" cy="62" r="5" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2.5" />'
    )
    Eyes = @(
        '<ellipse cx="114" cy="106" rx="4.5" ry="8" fill="#0B0E14" />',
        '<ellipse cx="142" cy="106" rx="4.5" ry="8" fill="#0B0E14" />'
    ) -join "`n"
    Emblem = 'triangleDown'
}

$Characters['horse'] = @{
    Main = '#A8523F'; Accent = '#F0C0A0'
    Sh = 70; Wa = 50; Hp = 58
    Behind = @(
        '<path d="M92 80 L98 40 L112 66 L128 28 L144 66 L158 40 L164 80 L128 94 Z" fill="{DARK}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<path d="M186 250 C212 246 222 220 210 200" fill="none" stroke="{MAIN}" stroke-width="12" stroke-linecap="round" />'
    )
    After = @(
        '<ellipse cx="128" cy="128" rx="18" ry="12" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="121" cy="126" r="2.6" fill="#0B0E14" />',
        '<circle cx="135" cy="126" r="2.6" fill="#0B0E14" />'
    )
    Emblem = 'ring'
}

$Characters['goat'] = @{
    Main = '#CDBB84'; Accent = '#FFF6DA'
    Sh = 66; Wa = 48; Hp = 56
    After = @(
        '<path d="M104 76 C88 72 78 56 88 46 C98 36 114 44 108 56 C104 63 96 60 99 54" fill="none" stroke="{ACC}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />',
        '<path d="M152 76 C168 72 178 56 168 46 C158 36 142 44 148 56 C152 63 160 60 157 54" fill="none" stroke="{ACC}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />',
        '<path d="M118 136 L128 176 L138 136 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />'
    )
    Emblem = 'cross'
}

$Characters['monkey'] = @{
    Main = '#B5814A'; Accent = '#F0CE96'
    Sh = 64; Wa = 48; Hp = 56
    Behind = @(
        '<circle cx="200" cy="244" r="22" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<path d="M180 250 C192 260 202 258 206 248" fill="none" stroke="{MAIN}" stroke-width="10" stroke-linecap="round" />'
    )
    After = @(
        '<circle cx="92" cy="100" r="15" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="164" cy="100" r="15" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="92" cy="100" r="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="164" cy="100" r="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<ellipse cx="128" cy="122" rx="20" ry="14" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="120" cy="119" r="2.6" fill="#0B0E14" />',
        '<circle cx="136" cy="119" r="2.6" fill="#0B0E14" />'
    )
    Emblem = 'wave'
}

$Characters['rooster'] = @{
    Main = '#C9502F'; Accent = '#FFD98A'
    Sh = 58; Wa = 42; Hp = 50
    After = @(
        '<circle cx="114" cy="54" r="10" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<circle cx="128" cy="44" r="12" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<circle cx="142" cy="54" r="10" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<rect x="106" y="60" width="44" height="11" rx="5" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<path d="M118 124 L138 124 L128 148 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<circle cx="128" cy="147" r="7" fill="{MAIN}" stroke="#0B0E14" stroke-width="3" />'
    )
    Eyes = @(
        '<ellipse cx="112" cy="102" rx="4.5" ry="5.5" fill="#0B0E14" />',
        '<ellipse cx="144" cy="102" rx="4.5" ry="5.5" fill="#0B0E14" />'
    ) -join "`n"
    Emblem = 'chevron'
}

$Characters['dog'] = @{
    Main = '#4B6BB0'; Accent = '#B6CCF5'
    Sh = 68; Wa = 50; Hp = 58
    After = @(
        '<ellipse cx="90" cy="108" rx="14" ry="27" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<ellipse cx="166" cy="108" rx="14" ry="27" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<ellipse cx="128" cy="122" rx="19" ry="14" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<ellipse cx="128" cy="116" rx="8" ry="6" fill="#0B0E14" />'
    )
    Eyes = @(
        '<ellipse cx="114" cy="100" rx="4.5" ry="5.5" fill="#0B0E14" />',
        '<ellipse cx="142" cy="100" rx="4.5" ry="5.5" fill="#0B0E14" />'
    ) -join "`n"
    Emblem = 'square'
}

$Characters['pig'] = @{
    Main = '#C58AB0'; Accent = '#FFE0F2'
    Sh = 76; Wa = 60; Hp = 70
    After = @(
        '<circle cx="100" cy="66" r="14" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="156" cy="66" r="14" fill="url(#{HEADGRAD})" stroke="#0B0E14" stroke-width="4.5" />',
        '<ellipse cx="128" cy="118" rx="17" ry="12" fill="{ACC}" stroke="#0B0E14" stroke-width="3.5" />',
        '<circle cx="121" cy="118" r="3" fill="#0B0E14" />',
        '<circle cx="135" cy="118" r="3" fill="#0B0E14" />'
    )
    Eyes = @(
        '<ellipse cx="112" cy="96" rx="4.5" ry="5.5" fill="#0B0E14" />',
        '<ellipse cx="144" cy="96" rx="4.5" ry="5.5" fill="#0B0E14" />'
    ) -join "`n"
    Emblem = 'ellipse'
}

# ============================ 6 个敌人 ============================

$Enemies = [ordered]@{}

$Enemies['guard'] = @{
    Main = '#5A6470'; Accent = '#A8B4C0'
    Sh = 80; Wa = 66; Hp = 76
    ShY = 158; WaY = 218; HpY = 252
    HeadCY = 92; HeadR = 30
    Eyes = @(
        '<rect x="106" y="100" width="16" height="7" rx="3" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2" />',
        '<rect x="134" y="100" width="16" height="7" rx="3" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2" />'
    ) -join "`n"
    After = @(
        '<path d="M94 94 C94 62 108 46 128 46 C148 46 162 62 162 94 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="5" stroke-linejoin="round" />',
        '<path d="M128 20 L138 48 L118 48 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />',
        '<rect x="46" y="150" width="62" height="136" rx="18" fill="{DARK}" stroke="#0B0E14" stroke-width="5" />',
        '<rect x="56" y="160" width="42" height="116" rx="12" fill="none" stroke="{ACC}" stroke-width="3" stroke-opacity="0.7" />',
        '<circle cx="77" cy="218" r="10" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />'
    )
    Emblem = 'saltire'
}

$Enemies['raider'] = @{
    Main = '#7A4A3A'; Accent = '#D2A08A'
    Sh = 44; Wa = 32; Hp = 38
    ShY = 160; WaY = 220; HpY = 254
    HeadCY = 100; HeadR = 29
    Eyes = @(
        '<ellipse cx="128" cy="102" rx="5" ry="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />'
    ) -join "`n"
    After = @(
        '<path d="M94 106 C92 70 104 44 128 44 C152 44 164 70 162 106 C154 92 142 84 128 84 C114 84 102 92 94 106 Z" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" stroke-linejoin="round" />',
        '<line x1="206" y1="62" x2="214" y2="292" stroke="#3A2A22" stroke-width="7" stroke-linecap="round" />',
        '<path d="M206 6 L219 38 L210 64 L199 38 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="3.5" stroke-linejoin="round" />',
        '<path d="M102 134 L154 134 L148 150 L108 150 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />'
    )
    Emblem = 'doubleCircle'
}

$Enemies['scout'] = @{
    Main = '#3E5A4A'; Accent = '#9ED2B4'
    Sh = 40; Wa = 36; Hp = 48
    ShY = 178; WaY = 224; HpY = 252; BaseY = 292
    HeadCY = 118; HeadR = 31
    Eyes = @(
        '<ellipse cx="128" cy="118" rx="9" ry="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="128" cy="118" r="3" fill="#0B0E14" />'
    ) -join "`n"
    After = @(
        '<path d="M96 122 C94 88 106 72 128 72 C150 72 162 88 160 122 C152 108 142 102 128 102 C114 102 104 108 96 122 Z" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" stroke-linejoin="round" />',
        '<ellipse cx="72" cy="276" rx="20" ry="14" fill="{MAIN}" stroke="#0B0E14" stroke-width="4" />',
        '<ellipse cx="184" cy="276" rx="20" ry="14" fill="{MAIN}" stroke="#0B0E14" stroke-width="4" />',
        '<path d="M58 214 L80 192 L86 198 L64 220 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />'
    )
    Emblem = 'teardrop'
}

$Enemies['elite_tiger'] = @{
    Main = '#C97A28'; Accent = '#FFE2A8'
    Sh = 82; Wa = 72; Hp = 80
    ShY = 172; WaY = 234; HpY = 262
    HeadCY = 88; HeadR = 44
    Eyes = @(
        '<path d="M100 84 L120 90" fill="none" stroke="{ACC}" stroke-width="6" stroke-linecap="round" />',
        '<path d="M156 84 L136 90" fill="none" stroke="{ACC}" stroke-width="6" stroke-linecap="round" />'
    ) -join "`n"
    After = @(
        '<circle cx="92" cy="48" r="15" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="164" cy="48" r="15" fill="{DARK}" stroke="#0B0E14" stroke-width="4.5" />',
        '<circle cx="92" cy="48" r="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="164" cy="48" r="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<path d="M110 60 L107 78" fill="none" stroke="{DARK}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M128 54 L128 74" fill="none" stroke="{DARK}" stroke-width="5" stroke-linecap="round" />',
        '<path d="M146 60 L149 78" fill="none" stroke="{DARK}" stroke-width="5" stroke-linecap="round" />',
        '<ellipse cx="128" cy="116" rx="30" ry="18" fill="{ACC}" stroke="#0B0E14" stroke-width="4" />',
        '<path d="M120 104 L136 104 L128 116 Z" fill="#0B0E14" />',
        '<path d="M110 126 L119 126 L114 148 Z" fill="#FFF6E0" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<path d="M137 126 L146 126 L142 148 Z" fill="#FFF6E0" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />'
    )
    Emblem = 'bars'
}

$Enemies['elite_snake'] = @{
    Main = '#4E9A62'; Accent = '#C0F0CC'
    Sh = 0; Wa = 0; Hp = 0
    BodyPath = 'M128 150 C104 154 88 172 88 196 C88 220 104 234 128 234 C152 234 168 220 168 196 C168 172 152 154 128 150 Z'
    Collar = ''
    HeadCY = 112; HeadR = 34
    Eyes = @(
        '<ellipse cx="128" cy="108" rx="13" ry="10" fill="#F0F8E0" stroke="#0B0E14" stroke-width="3" />',
        '<ellipse cx="128" cy="108" rx="4" ry="9" fill="#0B0E14" />'
    ) -join "`n"
    After = @(
        '<ellipse cx="128" cy="268" rx="76" ry="22" fill="none" stroke="{DARK}" stroke-width="20" />',
        '<ellipse cx="128" cy="222" rx="62" ry="18" fill="none" stroke="{MAIN}" stroke-width="18" />',
        '<ellipse cx="128" cy="182" rx="48" ry="15" fill="none" stroke="{LIGHT}" stroke-width="16" />',
        '<path d="M128 146 L128 176" fill="none" stroke="{MAIN}" stroke-width="22" stroke-linecap="round" />',
        '<circle cx="96" cy="180" r="4" fill="{ACC}" fill-opacity="0.55" />',
        '<circle cx="160" cy="180" r="4" fill="{ACC}" fill-opacity="0.55" />',
        '<circle cx="80" cy="222" r="4" fill="{ACC}" fill-opacity="0.55" />',
        '<circle cx="176" cy="222" r="4" fill="{ACC}" fill-opacity="0.55" />',
        '<path d="M128 142 L128 156" fill="none" stroke="#D0405A" stroke-width="4" stroke-linecap="round" />',
        '<path d="M128 156 L119 166" fill="none" stroke="#D0405A" stroke-width="4" stroke-linecap="round" />',
        '<path d="M128 156 L137 166" fill="none" stroke="#D0405A" stroke-width="4" stroke-linecap="round" />'
    )
    Emblem = 'scale'
}

$Enemies['boss'] = @{
    Main = '#3A2A4A'; Accent = '#D08A3C'
    Sh = 0; Wa = 0; Hp = 0
    BodyPath = 'M128 126 C104 128 84 140 74 158 C64 178 58 216 56 258 L54 296 L202 296 L200 258 C198 216 192 178 182 158 C172 140 152 128 128 126 Z'
    Collar = '<path d="M100 134 L128 168 L156 134 L164 146 L128 186 L92 146 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4" stroke-linejoin="round" />'
    HeadCY = 92; HeadR = 32
    Eyes = @(
        '<ellipse cx="128" cy="92" rx="10" ry="12" fill="{ACC}" stroke="#0B0E14" stroke-width="3" />',
        '<ellipse cx="128" cy="92" rx="4" ry="7" fill="#0B0E14" />',
        '<ellipse cx="104" cy="86" rx="6" ry="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />',
        '<ellipse cx="152" cy="86" rx="6" ry="7" fill="{ACC}" stroke="#0B0E14" stroke-width="2.5" />'
    ) -join "`n"
    After = @(
        '<path d="M94 64 L90 34 L106 50 L128 26 L150 50 L166 34 L162 64 Z" fill="{ACC}" stroke="#0B0E14" stroke-width="4.5" stroke-linejoin="round" />',
        '<circle cx="128" cy="52" r="4" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2" />',
        '<circle cx="106" cy="58" r="3" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2" />',
        '<circle cx="150" cy="58" r="3" fill="{ALIGHT}" stroke="#0B0E14" stroke-width="2" />',
        '<path d="M104 150 C96 200 92 250 90 292" fill="none" stroke="#0B0E14" stroke-opacity="0.5" stroke-width="4" stroke-linecap="round" />',
        '<path d="M152 150 C160 200 164 250 166 292" fill="none" stroke="#0B0E14" stroke-opacity="0.5" stroke-width="4" stroke-linecap="round" />'
    )
    Emblem = 'sparkle'
}

# ============================ 16 个状态图标 ============================

$Icons = [ordered]@{}

$Icons['strength'] = @{
    Color = '#5AA8F0'
    Art = @(
        '<polygon points="32,6 43,19 37,19 37,25 27,25 27,19 21,19" fill="#5AA8F0" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<rect x="19" y="28" width="26" height="24" rx="8" fill="#7FC4F7" stroke="#0B0E14" stroke-width="3" />',
        '<path d="M27 35 V42" fill="none" stroke="#0B0E14" stroke-width="2.5" stroke-linecap="round" />',
        '<path d="M32 35 V42" fill="none" stroke="#0B0E14" stroke-width="2.5" stroke-linecap="round" />',
        '<path d="M37 35 V42" fill="none" stroke="#0B0E14" stroke-width="2.5" stroke-linecap="round" />'
    )
}

$Icons['attack_up'] = @{
    Color = '#5AA8F0'
    Art = @(
        '<path d="M16 46 L44 18" fill="none" stroke="#CFE6FF" stroke-width="6" stroke-linecap="round" />',
        '<path d="M48 46 L20 18" fill="none" stroke="#CFE6FF" stroke-width="6" stroke-linecap="round" />',
        '<path d="M12 42 L22 52" fill="none" stroke="#5AA8F0" stroke-width="6" stroke-linecap="round" />',
        '<path d="M52 42 L42 52" fill="none" stroke="#5AA8F0" stroke-width="6" stroke-linecap="round" />',
        '<circle cx="46" cy="14" r="4" fill="#5AA8F0" stroke="#0B0E14" stroke-width="2" />',
        '<circle cx="18" cy="14" r="4" fill="#5AA8F0" stroke="#0B0E14" stroke-width="2" />'
    )
}

$Icons['vampirism'] = @{
    Color = '#E05A6A'
    Art = @(
        '<path d="M32 10 C40 23 47 30 47 38 A15 15 0 1 1 17 38 C17 30 24 23 32 10 Z" fill="#E05A6A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M24 44 L29 44 L26 54 Z" fill="#FFF0F0" stroke="#0B0E14" stroke-width="1.5" stroke-linejoin="round" />',
        '<path d="M35 44 L40 44 L38 54 Z" fill="#FFF0F0" stroke="#0B0E14" stroke-width="1.5" stroke-linejoin="round" />'
    )
}

$Icons['defense'] = @{
    Color = '#5AA8F0'
    Art = @(
        '<path d="M32 9 L50 17 V32 C50 44 41 51 32 55 C23 51 14 44 14 32 V17 Z" fill="#5AA8F0" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M32 15 L44 20 V32 C44 41 38 46 32 49 C26 46 20 41 20 32 V20 Z" fill="none" stroke="#B6DCFF" stroke-width="2.5" stroke-linejoin="round" />'
    )
}

$Icons['defense_up'] = @{
    Color = '#5AA8F0'
    Art = @(
        '<polygon points="32,4 40,14 36,14 36,18 28,18 28,14 24,14" fill="#B6DCFF" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<path d="M32 20 L46 26 V36 C46 45 39 50 32 53 C25 50 18 45 18 36 V26 Z" fill="#5AA8F0" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />'
    )
}

$Icons['regeneration'] = @{
    Color = '#5AD08A'
    Art = @(
        '<path d="M32 53 C18 44 10 36 10 27 A11 11 0 0 1 32 21 A11 11 0 0 1 54 27 C54 36 46 44 32 53 Z" fill="#5AD08A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<rect x="29" y="25" width="6" height="16" fill="#EFFFF4" />',
        '<rect x="24" y="30" width="16" height="6" fill="#EFFFF4" />'
    )
}

$Icons['thorns'] = @{
    Color = '#E0B24A'
    Art = @(
        '<path d="M14 46 L22 18 L30 46 Z" fill="#E0B24A" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<path d="M26 46 L32 10 L38 46 Z" fill="#E0B24A" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<path d="M36 46 L44 20 L50 46 Z" fill="#E0B24A" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />',
        '<rect x="11" y="46" width="42" height="7" rx="3" fill="#8A6B22" stroke="#0B0E14" stroke-width="2.5" />'
    )
}

$Icons['fury'] = @{
    Color = '#F08A3A'
    Art = @(
        '<path d="M36 5 L17 34 L28 34 L24 59 L47 25 L35 25 Z" fill="#F08A3A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M33 24 L28 36 L34 36 L31 47" fill="none" stroke="#FFD08A" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />'
    )
}

$Icons['weak'] = @{
    Color = '#A87AD8'
    Art = @(
        '<path d="M32 58 L13 35 L26 35 L26 9 L38 9 L38 35 L51 35 Z" fill="#A87AD8" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M20 49 H44" fill="none" stroke="#D6BEF2" stroke-width="3" stroke-linecap="round" />'
    )
}

$Icons['vulnerable'] = @{
    Color = '#E05A5A'
    Art = @(
        '<path d="M32 9 L50 17 V32 C50 44 41 51 32 55 C23 51 14 44 14 32 V17 Z" fill="#E05A5A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M31 15 L24 29 L34 35 L27 47" fill="none" stroke="#12151D" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />',
        '<circle cx="32" cy="32" r="4" fill="#12151D" />'
    )
}

$Icons['poison'] = @{
    Color = '#7AD04A'
    Art = @(
        '<circle cx="24" cy="27" r="14" fill="#D8DCC8" stroke="#0B0E14" stroke-width="3" />',
        '<rect x="16" y="35" width="16" height="9" rx="3" fill="#D8DCC8" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="19" cy="25" r="3.5" fill="#0B0E14" />',
        '<circle cx="29" cy="25" r="3.5" fill="#0B0E14" />',
        '<path d="M46 30 C50 37 53 40 53 44 A7 7 0 1 1 39 44 C39 40 42 37 46 30 Z" fill="#7AD04A" stroke="#0B0E14" stroke-width="2.5" stroke-linejoin="round" />'
    )
}

$Icons['burn'] = @{
    Color = '#F07A3A'
    Art = @(
        '<path d="M32 5 C44 21 51 30 51 40 A19 19 0 0 1 13 40 C13 30 20 21 32 5 Z" fill="#F07A3A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M32 26 C38 34 40 38 40 42 A8 8 0 0 1 24 42 C24 38 26 34 32 26 Z" fill="#FFD98A" />'
    )
}

$Icons['bleed'] = @{
    Color = '#D83A3A'
    Art = @(
        '<path d="M32 7 C41 22 49 30 49 38 A17 17 0 1 1 15 38 C15 30 23 22 32 7 Z" fill="#D83A3A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<ellipse cx="25" cy="40" rx="4" ry="6" fill="#F08A8A" fill-opacity="0.7" />'
    )
}

$Icons['slow'] = @{
    Color = '#7A9AC0'
    Art = @(
        '<ellipse cx="32" cy="42" rx="13" ry="15" fill="#7A9AC0" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="18" cy="24" r="4" fill="#7A9AC0" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="26" cy="15" r="4" fill="#7A9AC0" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="38" cy="15" r="4" fill="#7A9AC0" stroke="#0B0E14" stroke-width="2.5" />',
        '<circle cx="46" cy="24" r="4" fill="#7A9AC0" stroke="#0B0E14" stroke-width="2.5" />'
    )
}

$Icons['silence'] = @{
    Color = '#9080B0'
    Art = @(
        '<path d="M24 30 V24 A8 8 0 0 1 40 24 V30" fill="none" stroke="#C0B0DC" stroke-width="5" stroke-linecap="round" />',
        '<rect x="20" y="28" width="24" height="22" rx="5" fill="#9080B0" stroke="#0B0E14" stroke-width="3" />',
        '<circle cx="32" cy="38" r="3" fill="#12151D" />',
        '<path d="M14 52 L50 12" fill="none" stroke="#12151D" stroke-width="6" stroke-linecap="round" />'
    )
}

$Icons['defense_down'] = @{
    Color = '#C04A4A'
    Art = @(
        '<path d="M30 10 L14 18 V32 C14 42 21 49 30 53 Z" fill="#C04A4A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M34 10 L50 18 V32 C50 42 43 49 34 53 Z" fill="#C04A4A" stroke="#0B0E14" stroke-width="3" stroke-linejoin="round" />',
        '<path d="M30 26 L26 33 L30 38" fill="none" stroke="#12151D" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />',
        '<path d="M34 26 L38 33 L34 38" fill="none" stroke="#12151D" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />'
    )
}

# ============================ 写文件 + 自检 ============================

function Write-SvgFile {
    param([string]$Path, [string]$Content)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $text = ($Content -replace "`r`n", "`n")
    if (-not $text.EndsWith("`n")) { $text += "`n" }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $text, $utf8NoBom)
}

function Test-SvgFile {
    param([string]$Path, [int]$Width, [int]$Height)
    $errors = @()
    $name = Split-Path -Leaf $Path
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $errors += '文件带 UTF-8 BOM'
    }
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($text.Contains("`r")) { $errors += '换行含 CR（非纯 LF）' }
    if (-not $text.EndsWith("`n")) { $errors += '文件末尾没有换行' }

    $m = [regex]::Match($text, '<svg\b[^>]*>')
    if (-not $m.Success) {
        $errors += '找不到 <svg> 根元素'
    } else {
        $root = $m.Value
        if (-not $root.Contains('xmlns="http://www.w3.org/2000/svg"')) { $errors += '根元素缺 xmlns' }
        if (-not $root.Contains('width=')) { $errors += '根元素缺 width' }
        if (-not $root.Contains('height=')) { $errors += '根元素缺 height' }
        if (-not $root.Contains('viewBox=')) { $errors += '根元素缺 viewBox' }
        $vm = [regex]::Match($root, 'viewBox="([^"]+)"')
        if ($vm.Success) {
            $vb = ($vm.Groups[1].Value -split '\s+') -join ' '
            if ($vb -ne "0 0 $Width $Height") { $errors += "viewBox 不是 0 0 $Width $Height（实际: $vb）" }
        }
        $wm = [regex]::Match($root, 'width="([^"]+)"')
        $hm = [regex]::Match($root, 'height="([^"]+)"')
        if ($wm.Success -and $wm.Groups[1].Value -ne "$Width") { $errors += "width 不是 $Width" }
        if ($hm.Success -and $hm.Groups[1].Value -ne "$Height") { $errors += "height 不是 $Height" }
    }

    foreach ($bad in @('<text', '<filter', '<mask', '<clipPath', '<image', 'rgba(', 'rgb(', 'font-')) {
        if ($text.Contains($bad)) { $errors += "含禁用内容: $bad" }
    }

    $ids = @()
    foreach ($im in [regex]::Matches($text, 'id="([^"]+)"')) { $ids += $im.Groups[1].Value }
    foreach ($rm in [regex]::Matches($text, 'url\(#([^)]+)\)')) {
        $rid = $rm.Groups[1].Value
        if ($ids -notcontains $rid) { $errors += "引用了不存在的 id: $rid" }
    }
    $dupIds = $ids | Group-Object | Where-Object { $_.Count -gt 1 }
    foreach ($d in $dupIds) { $errors += "id 重复: $($d.Name)" }

    try {
        $doc = New-Object System.Xml.XmlDocument
        $doc.LoadXml($text)
    } catch {
        $errors += "XML 解析失败: $($_.Exception.Message)"
    }

    # ThorVG 子集 lint：标签 / 属性 / 颜色值都必须在白名单内，
    # 这样「Godot 能不能导入」就是可自动断言的，而不是只靠浏览器能画。
    if ($doc) {
        $allowedTags = @('svg', 'defs', 'linearGradient', 'radialGradient', 'stop',
            'rect', 'circle', 'ellipse', 'line', 'polyline', 'polygon', 'path', 'g')
        $allowedAttrs = @('xmlns', 'width', 'height', 'viewBox', 'id',
            'x', 'y', 'x1', 'y1', 'x2', 'y2', 'cx', 'cy', 'r', 'rx', 'ry',
            'd', 'points', 'fill', 'stroke', 'stroke-width', 'stroke-linecap',
            'stroke-linejoin', 'fill-opacity', 'stroke-opacity', 'offset', 'stop-color',
            'opacity', 'transform', 'gradientUnits', 'spreadMethod')
        $colorAttrs = @('fill', 'stroke', 'stop-color')
        $hexPattern = '^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$'
        $urlPattern = '^url\(#[A-Za-z0-9_\-]+\)$'

        foreach ($el in $doc.GetElementsByTagName('*')) {
            if ($allowedTags -notcontains $el.Name) {
                $errors += "ThorVG 不支持的标签: <$($el.Name)>"
            }
            foreach ($at in $el.Attributes) {
                if ($at.Prefix -eq 'xmlns' -or $at.Name -eq 'xmlns') { continue }
                if ($allowedAttrs -notcontains $at.Name) {
                    $errors += "<$($el.Name)> 上出现未列入白名单的属性: $($at.Name)"
                }
                if ($colorAttrs -contains $at.Name) {
                    $v = $at.Value.Trim()
                    if ($v -ne 'none' -and $v -notmatch $hexPattern -and $v -notmatch $urlPattern) {
                        $errors += "<$($el.Name)> 的 $($at.Name) 不是 hex/none/url: $v"
                    }
                }
                if ($at.Name -eq 'stroke-width' -or $at.Name -eq 'fill-opacity' -or $at.Name -eq 'stroke-opacity') {
                    $v = $at.Value.Trim()
                    if ($v -notmatch '^[0-9]+(\.[0-9]+)?$') {
                        $errors += "<$($el.Name)> 的 $($at.Name) 不是数值: $v"
                    }
                }
            }
        }
    }

    if ($errors.Count -eq 0) { return $null }
    return ,$errors
}

$written = @()

foreach ($id in $Characters.Keys) {
    $c = $Characters[$id]
    $svg = New-PortraitSvg -SvgId $id @c
    $path = Join-Path $PortraitDir ("char_$id.svg")
    Write-SvgFile -Path $path -Content $svg
    $written += $path
}

foreach ($id in $Enemies.Keys) {
    $c = $Enemies[$id]
    $svg = New-PortraitSvg -SvgId $id @c
    $path = Join-Path $PortraitDir ("enemy_$id.svg")
    Write-SvgFile -Path $path -Content $svg
    $written += $path
}

foreach ($id in $Icons.Keys) {
    $ic = $Icons[$id]
    $plate = '<rect x="2" y="2" width="60" height="60" rx="14" fill="#12151D" stroke="{0}" stroke-width="2" stroke-opacity="0.85" />' -f (Get-Darker $ic.Color 0.55)
    $iconBody = @($plate)
    $iconBody += $ic.Art
    $svg = New-SvgDocument -Width 64 -Height 64 -ViewBox '0 0 64 64' -Body $iconBody
    $path = Join-Path $IconDir ("status_$id.svg")
    Write-SvgFile -Path $path -Content $svg
    $written += $path
}

# ============================ 自检 ============================

$failed = 0
$rows = @()
foreach ($path in $written) {
    $isPortrait = $path.Contains('Portraits')
    $errs = Test-SvgFile -Path $path -Width $(if ($isPortrait) { 256 } else { 64 }) -Height $(if ($isPortrait) { 320 } else { 64 })
    $len = (Get-Item $path).Length
    if ($errs) {
        $failed++
        $rows += [pscustomobject]@{ File = (Split-Path -Leaf $path); Bytes = $len; Result = ($errs -join '; ') }
    } else {
        $rows += [pscustomobject]@{ File = (Split-Path -Leaf $path); Bytes = $len; Result = 'OK' }
    }
}

$rows | Format-Table -AutoSize | Out-String -Width 200 | Write-Output
Write-Output ("生成 {0} 个文件，通过 {1}，失败 {2}" -f $written.Count, ($written.Count - $failed), $failed)

if ($failed -gt 0) { exit 1 }
exit 0
