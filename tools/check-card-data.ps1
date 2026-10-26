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

# 6) 效果列表与旧字段**必须一致**（唯一真相源是 effects[]，旧字段是派生值）
#    两处一旦漂移，就会出现「卡面写一套、战斗打另一套」。
$RAGE_ATTACK = 15    # 攻击牌怒气（RageManager.RageOnAttack）
$RAGE_DEFENSE = 10   # 防御牌怒气（RageManager.RageOnDefense）

function Get-IntField($obj, [string]$name, [int]$fallback = 0) {
    if ($null -ne $obj -and $obj.PSObject.Properties.Name -contains $name -and $null -ne $obj.$name) {
        return [int]$obj.$name
    }
    return $fallback
}

foreach ($c in $cards) {
    $cid = $c.cardId
    $effects = @()
    if ($null -ne $c.PSObject.Properties.Name -contains 'effects' -and $null -ne $c.effects) {
        $effects = @($c.effects)
    }
    if ($effects.Count -eq 0) {
        $problems += "[character_cards.json] 卡「$cid」没有 effects[] —— 结算走效果列表引擎，没效果就不会触发任何东西"
        continue
    }

    $byType = @{}
    foreach ($e in $effects) {
        $t = $e.type
        if (-not $byType.ContainsKey($t)) { $byType[$t] = @() }
        $byType[$t] += $e
    }

    # 伤害
    $dmgPct = Get-IntField $c 'damagePercent' 0
    if ($dmgPct -gt 0) {
        if (-not $byType.ContainsKey('damage')) {
            $problems += "[character_cards.json] 卡「$cid」有 damagePercent=$dmgPct 但 effects 里没有 damage"
        } else {
            $e = $byType['damage'][0]
            if ([int]$e.percent -ne $dmgPct) {
                $problems += "[character_cards.json] 卡「$cid」伤害百分比不一致：旧字段 $dmgPct% vs effects $($e.percent)%"
            }
            $wantTarget = if ($c.isAreaAttack) { 'allEnemies' } else { 'target' }
            if ($e.target -ne $wantTarget) {
                $problems += "[character_cards.json] 卡「$cid」伤害目标不一致：期望 $wantTarget vs effects $($e.target)"
            }
        }
    }

    # 护盾
    $shieldPct = Get-IntField $c 'shieldPercent' 0
    if ($shieldPct -gt 0) {
        if (-not $byType.ContainsKey('shield')) {
            $problems += "[character_cards.json] 卡「$cid」有 shieldPercent=$shieldPct 但 effects 里没有 shield"
        } elseif ([int]$byType['shield'][0].percent -ne $shieldPct) {
            $problems += "[character_cards.json] 卡「$cid」护盾百分比不一致：旧字段 $shieldPct% vs effects $($byType['shield'][0].percent)%"
        }
    }

    # 怒气（新口径 2026-10-25，照参考游戏《终末前夜 Morimens》）：
    #   唯一真相源 = effects 里的 rage 条目 → 怒气 = value + perLevel × (等级 − 1)
    #   基本卡（打击/防御）= 5 + 1×(级−1)；灵知觉醒卡 = 25 + 5×(级−1)；其他专属指令卡不给怒气
    $ownRage = Get-IntField $c 'rageGain' 0
    if ($ownRage -gt 0) {
        $problems += "[character_cards.json] 卡「$cid」还留着旧的 rageGain=$ownRage 字段（新口径已弃用，请删掉，怒气只认 effects 里的 rage 条目）"
    }
    $suffix = ($cid -split '_')[-1]
    $isAwakening = ($c.cardType -eq 'awakening')
    $isBasic = ($suffix -eq 'attack' -or $suffix -eq 'defense')
    if ($byType.ContainsKey('rage')) {
        $e = $byType['rage'][0]
        $v = [int]$e.value
        $pl = [int]$e.perLevel
        if ($isAwakening) {
            if ($v -ne 25 -or $pl -ne 5) {
                $problems += "[character_cards.json] 卡「$cid」是灵知觉醒卡，怒气应为 25 + 5×(级−1)，实际是 $v + $pl×(级−1)"
            }
        }
        elseif ($isBasic) {
            if ($v -ne 5 -or $pl -ne 1) {
                $problems += "[character_cards.json] 卡「$cid」是基本卡，怒气应为 5 + 1×(级−1)，实际是 $v + $pl×(级−1)"
            }
        }
        else {
            $problems += "[character_cards.json] 卡「$cid」是专属指令卡，按参考口径**不给怒气**（参考图属性面板没有「狂气」行），请删掉 effects 里的 rage 条目"
        }
    }
    elseif ($isAwakening) {
        $problems += "[character_cards.json] 卡「$cid」是灵知觉醒卡，effects 里缺 rage 条目（怒气应为 25 + 5×(级−1)）"
    }
    elseif ($isBasic) {
        $problems += "[character_cards.json] 卡「$cid」是基本卡，effects 里缺 rage 条目（怒气应为 5 + 1×(级−1)）"
    }

    # ⚠️ **灵知觉醒卡必须「打出即销毁」**（主人 2026-10-27 拍板）
    # 「所有角色的卡牌3 都不能重复使用 —— 某个角色用过后，该次战斗该角色就处于灵知觉醒状态，
    #   所以这张卡牌可以看成打出就销毁。」
    # → 缺 consume 条目 = 打出后还会留在手里（净世之路还有 retain）→ 就能重复打出 → 违反口径。
    if ($isAwakening -and -not $byType.ContainsKey('consume')) {
        $problems += "[character_cards.json] 卡「$cid」是灵知觉醒卡，effects 里缺 consume 条目（灵知觉醒卡只能打出一次 —— 打出即从牌库移除；缺了就会变成可重复使用）"
    }

    # 抽牌
    $drawCount = Get-IntField $c 'drawCountBase' 0
    if ($drawCount -gt 0) {
        if (-not $byType.ContainsKey('draw')) {
            $problems += "[character_cards.json] 卡「$cid」有 drawCountBase=$drawCount 但 effects 里没有 draw"
        } else {
            $e = $byType['draw'][0]
            if ([int]$e.count -ne $drawCount) {
                $problems += "[character_cards.json] 卡「$cid」抽牌数不一致：旧字段 $drawCount vs effects $($e.count)"
            }
            $wantFilter = if ($null -ne $c.PSObject.Properties.Name -contains 'drawFilter') { [string]$c.drawFilter } else { '' }
            if ([string]$e.filter -ne $wantFilter) {
                $problems += "[character_cards.json] 卡「$cid」抽牌筛选不一致：旧字段 '$wantFilter' vs effects '$($e.filter)'"
            }
        }
    }

    # 增益 / 减益
    if ($null -ne $c.PSObject.Properties.Name -contains 'applyBuffName' -and -not [string]::IsNullOrWhiteSpace($c.applyBuffName)) {
        if (-not $byType.ContainsKey('buff')) {
            $problems += "[character_cards.json] 卡「$cid」有 applyBuffName=$($c.applyBuffName) 但 effects 里没有 buff"
        } elseif ([string]$byType['buff'][0].buff -ne [string]$c.applyBuffName) {
            $problems += "[character_cards.json] 卡「$cid」增益 id 不一致：旧字段 $($c.applyBuffName) vs effects $($byType['buff'][0].buff)"
        }
    }
    if ($null -ne $c.PSObject.Properties.Name -contains 'applyDebuffName' -and -not [string]::IsNullOrWhiteSpace($c.applyDebuffName)) {
        if (-not $byType.ContainsKey('debuff')) {
            $problems += "[character_cards.json] 卡「$cid」有 applyDebuffName=$($c.applyDebuffName) 但 effects 里没有 debuff"
        } elseif ([string]$byType['debuff'][0].debuff -ne [string]$c.applyDebuffName) {
            $problems += "[character_cards.json] 卡「$cid」减益 id 不一致：旧字段 $($c.applyDebuffName) vs effects $($byType['debuff'][0].debuff)"
        }
    }

    # 保留词缀
    $hasRetain = $byType.ContainsKey('retain')
    $isRetain = ($null -ne $c.PSObject.Properties.Name -contains 'isRetain' -and [bool]$c.isRetain)
    if ($hasRetain -ne $isRetain) {
        $problems += "[character_cards.json] 卡「$cid」保留词缀不一致：isRetain=$isRetain vs effects 有 retain=$hasRetain"
    }
}

if ($problems.Count -eq 0) {
    Write-Host ''
    Write-Host ("OK  卡牌数据一致（" + $characters.Count + " 个角色各 2 张独立基本卡 / 共 " + $basicOwners.Count + " 张基本卡 / " + $cards.Count + " 张卡）") -ForegroundColor Green
    Write-Host ("    效果列表与旧字段逐项一致（唯一真相源 = effects[]）") -ForegroundColor Green
    exit 0
}

Write-Host ''
Write-Host ('发现 ' + $problems.Count + ' 处问题：') -ForegroundColor Yellow
foreach ($p in $problems) { Write-Host ('  - ' + $p) -ForegroundColor Yellow }
exit 1
