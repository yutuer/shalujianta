using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.CharacterSystem;

/// <summary>
/// **觉醒登记的「弃牌阶段后」层数效果**（罪印）。
///
/// 照参考游戏《终末前夜 Morimens》—— 波吕克斯「净世之路」：
/// 「⚠灵知觉醒：打出时和**弃牌阶段后**获得 3 层 ✕罪印，每有 1 张手牌额外获得 1 层 ✕罪印。」
/// → 「打出时」那一次由效果列表自己结算（timing 含 onPlay）；
/// 「弃牌阶段后」每回合一次，由觉醒状态登记、回合末执行。
/// </summary>
public class AwakeningStackSpec
{
    /// <summary>层数 id（罪印 = <see cref="CardStackIds.SinMark"/>）</summary>
    public string StackId { get; set; } = "";

    /// <summary>基础层数（3）</summary>
    public int Count { get; set; }

    /// <summary>每张手牌额外层数（1）</summary>
    public int CountPerHand { get; set; }
}

public partial class CharacterPosition : Node2D
{
    [Export]
    public int PositionIndex;

    [Export]
    public CharacterDefinition Character;

    [Export]
    public int CurrentRage { get; set; } = 0;

    public const int MaxRage = 100;

    private bool isAnimating = false;

    public override void _Ready()
    {
    }

    public void AddRage(int amount)
    {
        CurrentRage = Mathf.Min(CurrentRage + amount, MaxRage);
    }

    public void ResetRage()
    {
        CurrentRage = 0;
    }

    public bool CanUseUltimate()
    {
        return CurrentRage >= MaxRage;
    }

    // ==================== 灵知觉醒（打出后在本场战斗持续生效） ====================

    /// <summary>灵知觉醒是否已激活（打出灵知觉醒卡后生效，整场战斗持续）</summary>
    public bool AwakeningActive { get; private set; } = false;

    /// <summary>灵知觉醒：每次打出卡牌后回复多少生命（按觉醒卡等级算好后存下来）</summary>
    public int AwakeningHealAmount { get; private set; } = 0;

    /// <summary>灵知觉醒：每次打出卡牌后获得多少怒气</summary>
    public int AwakeningRageGain { get; private set; } = 0;

    /// <summary>灵知觉醒：打出指定那张卡时，对全体敌人施加的减益 id（空 = 没有这一步）</summary>
    public string AwakeningDebuffId { get; private set; } = "";

    /// <summary>灵知觉醒：那个减益的层数（按攻击力百分比算好）</summary>
    public int AwakeningDebuffStacks { get; private set; } = 0;

    /// <summary>灵知觉醒：打出**哪张卡**时触发那个减益（衍生卡 id）</summary>
    public string AwakeningTriggerCardId { get; private set; } = "";

    /// <summary>灵知觉醒是由**哪张卡**激活的（打出它本身不再触发觉醒的持续效果）</summary>
    public string AwakeningSourceCardId { get; private set; } = "";

    /// <summary>
    /// **灵知觉醒授予的出血：哪张卡**的伤害附带出血
    /// （波吕克斯「净世之路」→ 授予「圣心」100% 伤害的出血）。
    /// </summary>
    public string AwakeningBleedCardId { get; private set; } = "";

    /// <summary>
    /// **灵知觉醒授予的出血：多少百分比**（圣心 = 100）。
    /// ⚠️ 这个百分比是**唯一的真相源** —— 卡上的出血效果只声明「我要觉醒授予的出血」，
    /// 具体多少由觉醒决定（照参考游戏：数字写在净世之路的效果里）。
    /// </summary>
    public int AwakeningBleedPercent { get; private set; } = 0;

    /// <summary>
    /// **觉醒登记的「弃牌阶段后」层数效果**（罪印）——
    /// 打出灵知觉醒卡时登记，之后每回合的弃牌阶段后结算一次。
    /// </summary>
    private readonly List<AwakeningStackSpec> _awakeningDiscardStacks = new List<AwakeningStackSpec>();

    /// <summary>觉醒登记的「弃牌阶段后」层数效果（只读）</summary>
    public IReadOnlyList<AwakeningStackSpec> AwakeningDiscardStacks => _awakeningDiscardStacks;

    // ==================== 层数（罪印等）：挂在角色身上的 buff ====================

    /// <summary>
    /// **层数表**（罪印等）。
    ///
    /// ⚠️ 罪印挂在**角色身上**、**不是卡上** ——
    /// 主人 2026-10-25 23:38 拍板：「罪印打出后, 就是记在玩家身上了, 以buff的形式存在。」
    /// → 卡洗回抽牌堆 / 被弃掉都不影响罪印。
    /// </summary>
    private readonly Dictionary<string, int> _stacks = new Dictionary<string, int>();

    /// <summary>罪印层数（0 = 没有）</summary>
    public int SinMarkStacks => GetStack(CardStackIds.SinMark);

    /// <summary>取某个层数（没有就是 0）</summary>
    public int GetStack(string stackId)
    {
        if (string.IsNullOrEmpty(stackId)) return 0;
        return _stacks.TryGetValue(stackId, out int value) ? value : 0;
    }

    /// <summary>
    /// **加层数**（罪印不设上限 —— 主人 2026-10-25 23:59 拍板「确认不设上限，线性叠加」）。
    /// </summary>
    public void AddStack(string stackId, int amount)
    {
        if (string.IsNullOrEmpty(stackId) || amount <= 0) return;
        _stacks[stackId] = GetStack(stackId) + amount;
        GD.Print($"[CharacterPosition] {Character?.Name} 获得 {amount} 层{CardStackIds.DisplayName(stackId)}（当前 {GetStack(stackId)} 层）");
    }

    /// <summary>罪印的**显示用**状态（没有罪印时返回 null）</summary>
    private StatusEffect _sinMarkStatus;

    /// <summary>
    /// 取罪印的**显示用状态**（纯展示：层数的真值是 <see cref="SinMarkStacks"/>）。
    /// 用来在状态图标行里画出「✕罪印 N」—— 罪印挂在**角色**身上，不是玩家身上。
    /// </summary>
    public StatusEffect GetSinMarkStatus()
    {
        int stacks = SinMarkStacks;
        if (stacks <= 0) return null;

        if (_sinMarkStatus == null)
        {
            _sinMarkStatus = EffectRegistry.CreateBuff(CardStackIds.SinMark);
            if (_sinMarkStatus == null)
            {
                GD.PrintErr($"[CharacterPosition] 罪印显示状态创建失败：{CardStackIds.SinMark}");
                return null;
            }
        }

        _sinMarkStatus.StackCount = stacks;
        return _sinMarkStatus;
    }

    /// <summary>
    /// **登记「弃牌阶段后」触发的层数效果**（打出灵知觉醒卡时调用）。
    /// 之后每回合的弃牌阶段后按「基础层数 + 手牌数 × 每手牌层数」结算一次。
    /// </summary>
    public void RegisterAwakeningDiscardStack(string stackId, int count, int countPerHand)
    {
        if (string.IsNullOrEmpty(stackId) || count <= 0) return;
        // ⚠️ **同 id 只登记一条**（2026-10-26 跑测发现）：
        // 参考词条「重复打出灵知觉醒无法叠加生效」→ 净世之路是**保留卡**，打出后会回到手里，
        // 可以重复打出。若每次都追加一条，弃牌阶段后就会**结算多次**（罪印翻倍）。
        // 所以：已登记过同 id → **只更新数值，不新增条目**。
        foreach (AwakeningStackSpec existing in _awakeningDiscardStacks)
        {
            if (existing == null || existing.StackId != stackId) continue;

            existing.Count = count;
            existing.CountPerHand = countPerHand;
            GD.Print($"[CharacterPosition] {Character?.Name} 的「弃牌阶段后」层数效果已登记过 {CardStackIds.DisplayName(stackId)}，重复打出不叠加（仍只有一条）");
            return;
        }

        _awakeningDiscardStacks.Add(new AwakeningStackSpec
        {
            StackId = stackId,
            Count = count,
            CountPerHand = countPerHand
        });
        GD.Print($"[CharacterPosition] {Character?.Name} 的灵知觉醒登记了「弃牌阶段后」层数效果：" +
            $"{count} + 手牌数×{countPerHand} 层{CardStackIds.DisplayName(stackId)}");
    }

    /// <summary>
    /// **激活灵知觉醒**（打出灵知觉醒卡时调用）。
    ///
    /// 照参考游戏《终末前夜 Morimens》「⚠灵知觉醒」词条：
    /// 「打出后使唤醒体在本场战斗中获得特殊的能力提升。**重复打出灵知觉醒无法叠加生效**。」
    /// → 重复打出不叠加，但也不清空已有状态。
    /// </summary>
    public bool ActivateAwakening(int healAmount, int rageGain, string debuffId, int debuffStacks, string triggerCardId, string sourceCardId = "",
        string bleedCardId = "", int bleedPercent = 0)
    {
        if (AwakeningActive)
        {
            GD.Print($"[CharacterPosition] {Character?.Name} 的灵知觉醒已激活，重复打出不叠加（保留原效果）");
            return false;
        }

        AwakeningActive = true;
        AwakeningHealAmount = healAmount;
        AwakeningRageGain = rageGain;
        AwakeningDebuffId = debuffId ?? "";
        AwakeningDebuffStacks = debuffStacks;
        AwakeningTriggerCardId = triggerCardId ?? "";
        AwakeningSourceCardId = sourceCardId ?? "";
        AwakeningBleedCardId = bleedCardId ?? "";
        AwakeningBleedPercent = bleedPercent;

        // ⚠️ 「每打出一张卡牌回复 X 生命 + Y 狂气」只有配了数值的觉醒卡才有
        // （波吕克斯「净世之路」没有这条 → 别写成「回复0生命 + 0狂气」，那是误导）
        string perCardText = "";
        if (healAmount > 0 || rageGain > 0)
        {
            perCardText = $"每打出一张卡牌回复{healAmount}生命 + {rageGain}狂气";
        }

        GD.Print($"[CharacterPosition] {Character?.Name} 灵知觉醒激活" +
            (string.IsNullOrEmpty(perCardText) ? "" : $"：{perCardText}") +
            (string.IsNullOrEmpty(AwakeningDebuffId) ? "" : $"，打出「{AwakeningTriggerCardId}」时全体敌人获得{AwakeningDebuffStacks}层{AwakeningDebuffId}") +
            (string.IsNullOrEmpty(AwakeningBleedCardId) ? "" : $"，「{AwakeningBleedCardId}」的伤害附带{AwakeningBleedPercent}%出血"));
        return true;
    }

    /// <summary>
    /// 尝试释放狂气爆发
    /// 怒气不足 → 返回 false，logMessage 写明原因
    /// 怒气足够 → 执行狂气爆发、怒气清零、返回 true，logMessage 为中文执行摘要
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人数组</param>
    /// <param name="logMessage">执行结果说明（失败原因或执行摘要）</param>
    /// <returns>是否成功释放狂气爆发</returns>
    public bool TryUseUltimate(Player player, Enemy[] enemies, out string logMessage)
    {
        GD.Print("[CharacterPosition] TryUseUltimate called");

        if (!CanUseUltimate())
        {
            logMessage = $"怒气不足（{CurrentRage}/{MaxRage}）";
            GD.Print($"[CharacterPosition] 释放狂气爆发失败：{logMessage}");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        string skillId = Character?.UltimateSkillId;
        if (string.IsNullOrEmpty(skillId))
        {
            logMessage = "该角色没有配置狂气爆发";
            GD.PrintErr($"[CharacterPosition] 角色 {Character?.Name} 缺少 ultimateSkillId，无法释放狂气爆发");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        UltimateSkill skill = UltimateSkill.GetUltimateById(skillId);
        if (skill == null)
        {
            logMessage = $"未找到狂气爆发数据（{skillId}）";
            GD.PrintErr($"[CharacterPosition] 未找到狂气爆发数据：{skillId}");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        // 把角色属性挂上去 —— 百分比口径的狂气爆发（攻击力 × 百分比）要靠它算数值
        if (Character != null)
        {
            skill.LinkedAttributes = Character.GetAttributes();
        }

        skill.Execute(player, enemies, this);

        // 怒气清零
        CurrentRage = 0;

        string characterName = string.IsNullOrEmpty(Character?.Name) ? "角色" : Character.Name;
        // 用**实际执行摘要**（不是说明文本）—— 说明里写着的机制没实现时，日志不能说假话
        string detail = string.IsNullOrEmpty(skill.LastExecuteSummary) ? skill.Description : skill.LastExecuteSummary;
        logMessage = $"{characterName} 释放了【{skill.Name}】：{detail}";

        GD.Print($"[CharacterPosition] {logMessage}");
        GD.Print("[CharacterPosition] TryUseUltimate completed");
        return true;
    }

    /// <summary>
    /// 释放狂气爆发（保留旧接口，内部转发到 TryUseUltimate）
    /// </summary>
    public void UseUltimate(Player player, Enemy[] enemies)
    {
        GD.Print("[CharacterPosition] UseUltimate called");

        TryUseUltimate(player, enemies, out string logMessage);
        GD.Print($"[CharacterPosition] {logMessage}");

        GD.Print("[CharacterPosition] UseUltimate completed");
    }

    public void PlayAttackAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        Vector2 originalPos = Position;
        tween.TweenProperty(this, "position", new Vector2(originalPos.X + 20, originalPos.Y), 0.1f);
        tween.TweenProperty(this, "position", originalPos, 0.1f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    public void PlayDamagedAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        var originalModulate = Modulate;

        tween.TweenProperty(this, "modulate", new Color(1f, 0.3f, 0.3f), 0.1f);
        tween.TweenProperty(this, "modulate", originalModulate, 0.1f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    public void PlayIdleAnimation()
    {
    }

    public void PlayUltimateAnimation(Player player, Enemy[] enemies)
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 0.3f, 1f), 0.2f);
        tween.TweenProperty(this, "modulate", Modulate, 0.3f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    private void OnAnimationComplete()
    {
        isAnimating = false;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}
