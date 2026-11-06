using Godot;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.CharacterSystem;

/// <summary>
/// **队伍级死亡抵抗系统** —— D66（2026-11-07）按参考游戏《终末前夜 Morimens》重做。
///
/// ## 官方口径（三处出处）
/// - 词条主表「死亡抵抗」：「在战斗中受到致命伤害时有概率保留 1 点生命。
///   触发后在本次调查中概率减半，获得的死亡抵抗减半。」
/// - 参考图 `docs/screenshots/战斗4_死亡抵抗.jpg` 的官方 tooltip：
///   「在战斗中受到致命伤害时有概率保留 1 点生命。触发后**下回合获得 2 点算力和抽 2 张牌**，
///   **队伍的死亡抵抗减半**，**后续获得的死亡抵抗减半**，持续到调查结束。
///   若有至少 1 个**混沌界域**唤醒体，**死亡抵抗初始 +100%**。」
/// - 主人 2026-11-07 口径：**死亡抵抗 = 队伍里所有角色的总和值**。
///
/// ## 数值模型（就这一条公式，别再分叉）
/// <code>
/// 队伍死亡抵抗 = 队伍基础总和 × 触发乘区 + 获得的加成
/// </code>
/// - **队伍基础总和** = Σ 各角色基础死亡抵抗（wiki 原值，带小数，不取整）
///   ＋ 混沌界域初始 +100%（队伍里至少有 1 个混沌界域唤醒体时）。
/// - **触发乘区**：初始 1.0；每次触发 ×(1-衰减率)，默认衰减率 50%
///   （菲茵特「无边星彩」觉醒后**本场战斗**衰减率降为 45%）。
/// - **获得的加成**：每次「获得死亡抵抗」时**按获得乘区折算后**累加；
///   获得乘区初始 1.0，每次触发 ×(1-衰减率)—— 这就是「后续获得的死亡抵抗减半」。
///
/// ⚠️ **与旧实现的差别（这是 D66 的核心）**：
/// - 旧版 `DeathResistanceSystem` 挂在 `TeamReviveSystem` 的**私有字段**里
///   → 每 `new` 一次就重置，「持续到调查结束」跨战斗不成立；
///   而且 `TeamReviveSystem` 整个类**零调用点**（2026-11-05 实测），
///   语义也是反的（旧版是「队友死亡→队友复活」，官方是「自己受致命伤害→自己保留 1 血」）。
/// - 现在：状态挂在静态的 <see cref="FishEatFish.Scenes.GlobalData.DeathResistance"/>
///   → 跨场景、跨战斗存活；触发点接在 <see cref="Player.TakeDamage"/>（唯一扣血入口）。
/// </summary>
public class DeathResistanceSystem
{
    // ==================== 调查级状态（跨战斗存活） ====================

    /// <summary>
    /// **触发乘区** —— 「队伍的死亡抵抗减半」。
    /// 初始 1.0；每次触发 ×(1-衰减率)。
    /// </summary>
    public float CurrentMultiplier { get; private set; } = 1f;

    /// <summary>
    /// **获得乘区** —— 「后续获得的死亡抵抗减半」。
    /// 初始 1.0；每次触发 ×(1-衰减率)。
    /// 作用在**之后新获得**的死亡抵抗上（获得时折算，见 <see cref="AddBonus"/>）。
    /// </summary>
    public float GainMultiplier { get; private set; } = 1f;

    /// <summary>
    /// **累计获得的死亡抵抗加成**（已按获得乘区折算过）。
    /// </summary>
    public float BonusSum { get; private set; } = 0f;

    /// <summary>本调查内触发了几次（诊断用）</summary>
    public int TriggerCount { get; private set; } = 0;

    // ==================== 战斗级状态（每场战斗重置） ====================

    /// <summary>
    /// **本场战斗中是否触发过死亡抵抗**。
    /// 菲茵特「幼芽萌生」：「若在本场战斗中触发过死亡抵抗，额外生效 1 次。」
    /// </summary>
    public bool TriggeredThisBattle { get; private set; } = false;

    /// <summary>
    /// **本场战斗的触发衰减率**（默认 0.5 = 减半）。
    /// 菲茵特「无边星彩」觉醒：「本场战斗死亡抵抗触发衰减率从 50% 降低为 45%。」
    /// </summary>
    public float DecayRate { get; private set; } = 0.5f;

    // ==================== 查询 ====================

    /// <summary>
    /// 队伍**基础**死亡抵抗总和 = Σ 各角色基础值 ＋ 混沌界域初始 +100%。
    ///
    /// ⚠️ **不取整** —— wiki 原值带小数（珊 61.6 / 菲茵特 33.6 / 珈伦 28），
    /// 取整会跟 wiki 和参考图对不上。
    /// </summary>
    /// <param name="team">队伍（可以是 null / 空数组）</param>
    public static float GetTeamBase(CharacterDefinition[] team)
    {
        float sum = 0f;
        bool hasChaos = false;

        if (team != null)
        {
            foreach (CharacterDefinition character in team)
            {
                if (character == null) continue;
                sum += character.BaseDeathResistance;
                if (character.Race == CharacterRace.Chaos)
                {
                    hasChaos = true;
                }
            }
        }

        // 「若有至少 1 个混沌界域唤醒体，死亡抵抗初始 +100%。」
        if (hasChaos)
        {
            sum += 100f;
        }

        return sum;
    }

    /// <summary>
    /// 队伍**当前有效**死亡抵抗（面板 / HUD / 概率判定都用这个）。
    /// </summary>
    public float GetTeamTotal(CharacterDefinition[] team)
    {
        return GetTeamBase(team) * CurrentMultiplier + BonusSum;
    }

    /// <summary>
    /// 死亡抵抗**概率判定**。
    ///
    /// ⚠️ **超过 100% 视为必触发** —— 队伍总和本来就能超过 100%
    /// （例：珊 61.6 + 珈伦 28 + 菲茵特 33.6 = 123.2），
    /// 概率不可能大于 100%，所以按 <c>min(总和, 100)</c> 判定（≥100% 必中）。
    ///
    /// ⚠️ **不取整** —— 61.6 就按 61.6% 判定（roll 是 1~100 的整数，
    /// 所以实际命中 61/100）。取整成 62 反而比 wiki 原值更"虚高"。
    /// </summary>
    /// <param name="total">队伍当前有效死亡抵抗</param>
    /// <returns>是否触发</returns>
    public static bool RollChance(float total)
    {
        if (total <= 0f)
        {
            return false;
        }

        float chance = Mathf.Min(total, 100f);
        int roll = (int)(GD.Randi() % 100) + 1;

        return roll <= chance;
    }

    // ==================== 变更 ====================

    /// <summary>
    /// **触发结算** —— 两个乘区各按衰减率折一次。
    /// 「触发后…队伍的死亡抵抗减半，后续获得的死亡抵抗减半。」
    /// </summary>
    public void OnTrigger()
    {
        float factor = 1f - DecayRate;
        if (factor < 0f)
        {
            factor = 0f;
        }

        CurrentMultiplier *= factor;
        GainMultiplier *= factor;
        TriggerCount++;
        TriggeredThisBattle = true;

        GD.Print($"[DeathResistance] 触发！触发乘区 ×{factor:F2} → {CurrentMultiplier:F4}，获得乘区 ×{factor:F2} → {GainMultiplier:F4}（本调查第 {TriggerCount} 次）");
    }

    /// <summary>
    /// **获得死亡抵抗加成**（菲茵特「星之摇篮」用）。
    ///
    /// ⚠️ **获得时按「获得乘区」折算**（不是读取时再乘）——
    /// 这样「后续获得的死亡抵抗减半」只影响**触发之后**拿到的加成，
    /// 不会把触发之前已经拿到的加成也一起砍半。
    /// </summary>
    /// <param name="amount">加成的原始值（%），可带小数</param>
    public void AddBonus(float amount)
    {
        if (amount <= 0f) return;

        float scaled = amount * GainMultiplier;
        BonusSum += scaled;

        GD.Print($"[DeathResistance] 获得死亡抵抗 +{amount:F1}%（按获得乘区 ×{GainMultiplier:F4} 折算为 +{scaled:F1}%，累计加成 {BonusSum:F1}%）");
    }

    /// <summary>
    /// **战斗开始** —— 重置战斗级状态（跨战斗的乘区与加成**保留**）。
    /// </summary>
    public void OnBattleStart()
    {
        TriggeredThisBattle = false;
        DecayRate = 0.5f;
        GD.Print("[DeathResistance] 战斗开始：战斗级状态已重置（触发乘区 ×" +
                 $"{CurrentMultiplier:F4}，获得乘区 ×{GainMultiplier:F4}，累计加成 {BonusSum:F1}%）");
    }

    /// <summary>
    /// **调查结束 / 换新地图** —— 全部重置（「持续到调查结束」的边界）。
    /// </summary>
    public void ResetForNewInvestigation()
    {
        CurrentMultiplier = 1f;
        GainMultiplier = 1f;
        BonusSum = 0f;
        TriggerCount = 0;
        TriggeredThisBattle = false;
        DecayRate = 0.5f;
        GD.Print("[DeathResistance] 调查重置：死亡抵抗状态全部清零");
    }

    /// <summary>
    /// **设置本场战斗的触发衰减率**（菲茵特「无边星彩」觉醒：50% → 45%）。
    /// </summary>
    /// <param name="rate">衰减率（0.45 = 减到 55%）</param>
    public void SetDecayRate(float rate)
    {
        DecayRate = rate;
        GD.Print($"[DeathResistance] 本场战斗死亡抵抗触发衰减率设为 {rate * 100f:F0}%");
    }
}
