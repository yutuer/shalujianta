using Godot;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.CharacterSystem;

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
    /// **激活灵知觉醒**（打出灵知觉醒卡时调用）。
    ///
    /// 照参考游戏《终末前夜 Morimens》「⚠灵知觉醒」词条：
    /// 「打出后使唤醒体在本场战斗中获得特殊的能力提升。**重复打出灵知觉醒无法叠加生效**。」
    /// → 重复打出不叠加，但也不清空已有状态。
    /// </summary>
    public void ActivateAwakening(int healAmount, int rageGain, string debuffId, int debuffStacks, string triggerCardId, string sourceCardId = "")
    {
        if (AwakeningActive)
        {
            GD.Print($"[CharacterPosition] {Character?.Name} 的灵知觉醒已激活，重复打出不叠加（保留原效果）");
            return;
        }

        AwakeningActive = true;
        AwakeningHealAmount = healAmount;
        AwakeningRageGain = rageGain;
        AwakeningDebuffId = debuffId ?? "";
        AwakeningDebuffStacks = debuffStacks;
        AwakeningTriggerCardId = triggerCardId ?? "";
        AwakeningSourceCardId = sourceCardId ?? "";

        GD.Print($"[CharacterPosition] {Character?.Name} 灵知觉醒激活：每打出一张卡牌回复{healAmount}生命 + {rageGain}狂气" +
            (string.IsNullOrEmpty(AwakeningDebuffId) ? "" : $"，打出「{AwakeningTriggerCardId}」时全体敌人获得{AwakeningDebuffStacks}层{AwakeningDebuffId}"));
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
