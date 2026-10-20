<#
.SYNOPSIS
    把参考截图（PNG）批量转成 JPG 并按宽度缩放，用于入库。

.DESCRIPTION
    本项目的 docs/screenshots/ 里存着《终末前夜 Morimens》界面参考图，
    原始 PNG 共约 31MB，直接入库会让 GitHub 仓库从 1.1MB 涨到 32MB，
    且推上去就永久留在 GitHub 历史里，事后删文件也减不掉体积。

    本脚本用系统自带的 GDI+（System.Drawing）把 PNG 转成 JPG：
      - 宽度限制 1920（保持原比例）
      - JPEG 质量 88
    转完把原 PNG 移到 _原始PNG/ 子目录（该目录不入库），本地仍保留最高清版本。

.PARAMETER SourceDir
    源目录，默认 docs/screenshots。

.PARAMETER MaxWidth
    最大宽度，默认 1920。

.PARAMETER Quality
    JPEG 质量，默认 88。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\convert-reference-images.ps1
#>
# ============================ 维护须知 ============================
# ⚠️ 本文件必须以「UTF-8 with BOM」保存！
#    PowerShell 5.1 会把无 BOM 的 .ps1 当 ANSI 读，中文全部乱码，
#    乱码会连累参数解析，报出 "A positional parameter cannot be found"
#    这类完全看不懂的错。改完本文件请确认开头三字节是 239,187,191。
#    （tools\check-task-stats.ps1 同理）
# ================================================================

[CmdletBinding()]
param(
    [string]$SourceDir = '',
    [int]$MaxWidth = 1920,
    [int]$Quality = 88
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $scriptDir
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $SourceDir = Join-Path $RepoRoot 'docs\screenshots'
}
if (-not (Test-Path $SourceDir)) {
    Write-Error "找不到目录: $SourceDir"
    exit 2
}

Add-Type -AssemblyName System.Drawing

# 找 JPEG 编码器并设质量
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object { $_.MimeType -eq 'image/jpeg' } | Select-Object -First 1
if ($null -eq $codec) { Write-Error '系统里找不到 JPEG 编码器'; exit 3 }

$encParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
$encParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
    [System.Drawing.Imaging.Encoder]::Quality, [long]$Quality)

$pngs = Get-ChildItem -Path $SourceDir -Filter '*.png' -File | Sort-Object Name
if ($pngs.Count -eq 0) {
    Write-Host '没有 PNG 需要转换（可能已转换过）'
    exit 0
}

$origDir = Join-Path $SourceDir '_原始PNG'
if (-not (Test-Path $origDir)) { New-Item -ItemType Directory -Path $origDir | Out-Null }

$before = 0; $after = 0
foreach ($f in $pngs) {
    $before += $f.Length

    $src = [System.Drawing.Image]::FromFile($f.FullName)
    try {
        $scale = [Math]::Min(1.0, $MaxWidth / [double]$src.Width)
        $w = [int][Math]::Round($src.Width * $scale)
        $h = [int][Math]::Round($src.Height * $scale)

        $bmp = New-Object System.Drawing.Bitmap($w, $h)
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                # 高质量双三次缩放
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $g.Clear([System.Drawing.Color]::Black)
                $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0, 0, $w, $h)))
            } finally { $g.Dispose() }

            $jpg = [System.IO.Path]::ChangeExtension($f.FullName, '.jpg')
            $bmp.Save($jpg, $codec, $encParams)
        } finally { $bmp.Dispose() }
    } finally { $src.Dispose() }

    $size = (Get-Item $jpg).Length
    $after += $size
    '{0,-42} {1,5}x{2,-5} {3,8:N1} MB -> {4,7:N1} KB' -f `
        $f.Name, $w, $h, ($f.Length / 1MB), ($size / 1KB)

    # 原 PNG 移到子目录保留
    Move-Item -LiteralPath $f.FullName -Destination (Join-Path $origDir $f.Name) -Force
}

$encParams.Dispose()
''
'原始 PNG 合计 : {0,8:N1} MB' -f ($before / 1MB)
'转成 JPG 合计 : {0,8:N1} MB' -f ($after / 1MB)
'原 PNG 已移到 : {0}' -f $origDir
