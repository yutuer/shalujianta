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

    /// <summary>
    /// 尝试释放必杀技
    /// 怒气不足 → 返回 false，logMessage 写明原因
    /// 怒气足够 → 执行必杀技、怒气清零、返回 true，logMessage 为中文执行摘要
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人数组</param>
    /// <param name="logMessage">执行结果说明（失败原因或执行摘要）</param>
    /// <returns>是否成功释放必杀技</returns>
    public bool TryUseUltimate(Player player, Enemy[] enemies, out string logMessage)
    {
        GD.Print("[CharacterPosition] TryUseUltimate called");

        if (!CanUseUltimate())
        {
            logMessage = $"怒气不足（{CurrentRage}/{MaxRage}）";
            GD.Print($"[CharacterPosition] 释放必杀技失败：{logMessage}");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        string skillId = Character?.UltimateSkillId;
        if (string.IsNullOrEmpty(skillId))
        {
            logMessage = "该角色没有配置必杀技";
            GD.PrintErr($"[CharacterPosition] 角色 {Character?.Name} 缺少 ultimateSkillId，无法释放必杀技");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        UltimateSkill skill = UltimateSkill.GetUltimateById(skillId);
        if (skill == null)
        {
            logMessage = $"未找到必杀技数据（{skillId}）";
            GD.PrintErr($"[CharacterPosition] 未找到必杀技数据：{skillId}");
            GD.Print("[CharacterPosition] TryUseUltimate completed");
            return false;
        }

        // 把角色属性挂上去 —— 百分比口径的必杀技（攻击力 × 百分比）要靠它算数值
        if (Character != null)
        {
            skill.LinkedAttributes = Character.GetAttributes();
        }

        skill.Execute(player, enemies);

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
    /// 释放必杀技（保留旧接口，内部转发到 TryUseUltimate）
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
