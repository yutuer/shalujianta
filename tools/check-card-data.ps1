<#
.SYNOPSIS
    卡牌数据一致性校验：守住「每个角色各自的两张基本卡」这条硬要求。

.DESCRIPTION
    主人的要求（2026-10-24）：打击 / 防御 **不能被角色共用** ——
    每个角色都要单独有 2 张自己的卡（条目 / 词条 / 运行时实例各自独立），
    数值暂时可以先用同一套模板。

    这条要求一旦破掉，表现是「改一张卡影响了所有角色」（共享实例/共享条目）。
    本脚本把数据层的硬约束变成可重复执行的检查：

      1. 每个角色都声明了自己的两张基本卡（attackCardId / defenseCardId 非空）
      2. 没有任何一张基本卡被两个角色共用（基本卡 id 全局唯一）
      3. 角色引用的基本卡在 character_cards.json 里存在，且条目归属的 characterId 对得上
      4. 基本卡按百分比口径配数值（打击有 damagePercent / 防御有 shieldPercent），不靠绝对值
      5. character_cards.json 里没有「角色已不存在」的孤儿卡

    退出码 0 = 全部通过；非 0 = 有问题，逐条列出。

.PARAMETER RepoRoot
    仓库根目录，默认本脚本所在目录的上一级。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\check-card-data.ps1
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

$charPath = Join-Path $RepoRoot 'Data\characters.json'
$cardPath = Join-Path $RepoRoot 'Data\character_cards.json'
foreach ($p in @($charPath, $cardPath)) {
    if (-not (Test-Path $p)) { Write-Error "数据表不存在: $p"; exit 2 }
}

# ⚠️ 必须 -Encoding UTF8：不加就按 ANSI 解码，中文名会乱码（2026-10-21 踩过）
$charJson = Get-Content -LiteralPath $charPath -Raw -Encoding UTF8 | ConvertFrom-Json
$cardJson = Get-Content -LiteralPath $cardPath -Raw -Encoding UTF8 | ConvertFrom-Json

$characters = @($charJson.characters)
$cards = @($cardJson.cards)

$problems = @()

Write-Host "角色 $($characters.Count) 个 / 卡牌 $($cards.Count) 张" -ForegroundColor Cyan

# 卡牌索引：cardId -> 条目
$cardIndex = @{}
foreach ($c in $cards) {
    if ($cardIndex.ContainsKey($c.cardId)) {
        $problems += "[character_cards.json] 卡牌 id 重复：$($c.cardId)"
    } else {
        $cardIndex[$c.cardId] = $c
    }
}

$charIds = @{}
foreach ($ch in $characters) { $charIds[$ch.characterId] = $true }

# 基本卡 id 的归属表：卡 id -> 角色 id（用来查「两个角色共用同一张卡」）
$basicOwners = @{}

foreach ($ch in $characters) {
    $cid = $ch.characterId

    foreach ($slot in @(@('attackCardId', '打击'), @('defenseCardId', '防御'))) {
        $field = $slot[0]
        $slotName = $slot[1]
        $cardId = $ch.$field

        if ([string]::IsNullOrWhiteSpace($cardId)) {
            $problems += "[characters.json] 角色「$cid」缺 $field（$slotName 卡）—— 每个角色都要有自己的两张基本卡"
            continue
        }

        # 1) 同一张基本卡不得被两个角色共用
        if ($basicOwners.ContainsKey($cardId)) {
            $problems += "[characters.json] 基本卡「$cardId」被角色「$($basicOwners[$cardId])」和「$cid」共用 —— 每个角色的$slotName必须是各自独立的卡"
        } else {
            $basicOwners[$cardId] = $cid
        }

        # 2) 引用的卡必须在 character_cards.json 里存在
        if (-not $cardIndex.ContainsKey($cardId)) {
            $problems += "[characters.json] 角色「$cid」的$slotName 卡「$cardId」不在 character_cards.json 里"
            continue
        }

        $entry = $cardIndex[$cardId]

        # 3) 条目归属的角色要对得上
        if ($entry.characterId -ne $cid) {
            $problems += "[character_cards.json] 卡「$cardId」归属角色是「$($entry.characterId)」，但被角色「$cid」引用"
        }

        # 4) 基本卡按百分比口径配数值（绝对值在百分比化之后恒为 0）
        if ($field -eq 'attackCardId') {
            if ([int]$entry.damagePercent -le 0) {
                $problems += "[character_cards.json] 打击卡「$cardId」缺 damagePercent（必须按「角色攻击 × 百分比」配数值）"
            }
        } else {
            # 防御卡允许「不护盾但有别的词条」（如兔的防御抽 2 张），
            # 但那种情况必须显式带别的效果，不能是一张什么都不给的空牌。
            $hasShield = [int]$entry.shieldPercent -gt 0
            $hasOther = ([int]$entry.drawCountBase -gt 0) -or ([int]$entry.energyGainBase -gt 0) -or
                         ([int]$entry.healAmount -gt 0) -or (-not [string]::IsNullOrWhiteSpace($entry.applyBuffName)) -or
                         (-not [string]::IsNullOrWhiteSpace($entry.applyDebuffName))
            if (-not ($hasShield -or $hasOther)) {
                $problems += "[character_cards.json] 防御卡「$cardId」既没有 shieldPercent 也没有任何其他词条 —— 是一张空牌"
            }
        }
    }
}

# 5) 孤儿卡：条目里的 characterId 在 characters.json 里不存在
foreach ($c in $cards) {
    $owner = $c.characterId
    if ([string]::IsNullOrWhiteSpace($owner)) { continue }
    if (-not $charIds.ContainsKey($owner)) {
        $problems += "[character_cards.json] 卡「$($c.cardId)」归属角色「$owner」不存在 —— 孤儿卡"
    }
}

if ($problems.Count -eq 0) {
    Write-Host ''
    Write-Host ("OK  卡牌数据一致（" + $characters.Count + " 个角色各 2 张独立基本卡 / 共 " + $basicOwners.Count + " 张基本卡 / " + $cards.Count + " 张卡）") -ForegroundColor Green
    exit 0
}

Write-Host ''
Write-Host ('发现 ' + $problems.Count + ' 处问题：') -ForegroundColor Yellow
foreach ($p in $problems) { Write-Host ('  - ' + $p) -ForegroundColor Yellow }
exit 1
