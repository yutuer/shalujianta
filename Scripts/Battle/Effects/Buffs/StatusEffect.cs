using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Effects;

namespace FishEatFish.Battle.Effects.Buffs;

public enum StatusEffectType
{
    Buff,
    Debuff
}

public abstract partial class StatusEffect : Resource
{
    [Export]
    public string EffectName { get; set; } = "";

    [Export]
    public string Description { get; set; } = "";

    [Export]
    public int Duration { get; set; } = 1;

    [Export]
    public StatusEffectType EffectType { get; set; } = StatusEffectType.Buff;

    [Export]
    public int IconPathHash { get; set; } = 0;

        /// <summary>图标资源路径（由 EffectRegistry 按 buffId 配置，UI 直接用这个字段取图标）</summary>
        [Export]
        public string IconPath { get; set; } = "";

    [Export]
    public int StackCount { get; set; } = 1;

    protected int _remainingDuration;

    public int RemainingDuration => _remainingDuration;

    public List<Effect> Effects { get; set; } = new List<Effect>();

    public virtual void OnApplyPlayer(Core.Player target)
    {
        ApplyEffectsToTarget(target, target);
    }

    public virtual void OnRemovePlayer(Core.Player target)
    {
    }

    public virtual void OnTurnStartPlayer(Core.Player target)
    {
        ApplyEffectsToTarget(target, target);
    }

    public virtual void OnTurnEndPlayer(Core.Player target)
    {
        TickDuration();
    }

    public virtual void OnApplyEnemy(Core.Enemy target)
    {
        ApplyEffectsToTarget(target, target);
    }

    public virtual void OnRemoveEnemy(Core.Enemy target)
    {
    }

    public virtual void OnTurnStartEnemy(Core.Enemy target)
    {
        ApplyEffectsToTarget(target, target);
    }

    public virtual void OnTurnEndEnemy(Core.Enemy target)
    {
        TickDuration();
    }

    private void ApplyEffectsToTarget(Core.IUnit owner, Core.IUnit target)
    {
        foreach (var effect in Effects)
        {
            var context = new EffectContext
            {
                Owner = owner,
                Target = target,
                Value = effect.Value,
                CurrentTrigger = GetTriggerType(effect)
            };

            EffectResolver.ApplyEffect(effect, context);
        }
    }

    private TriggerType GetTriggerType(Effect effect)
    {
        return effect.Trigger;
    }

    public virtual int ModifyDamagePlayer(int baseDamage, Core.Enemy attacker, Core.Player defender)
    {
        return baseDamage;
    }

    public virtual int ModifyDamageEnemy(int baseDamage, Core.Enemy attacker, Core.Enemy defender)
    {
        return baseDamage;
    }

    public virtual int ModifyHealing(int baseHealing, Core.Player target)
    {
        return baseHealing;
    }

    public virtual int ModifyShield(int baseShield, Core.Player target)
    {
        return baseShield;
    }

    public void Initialize()
    {
        _remainingDuration = Duration;
    }

    public void TickDuration()
    {
        _remainingDuration--;
    }

    /// <summary>
    /// **同名减益再次施加时的合并**（层数累加）。
    ///
    /// 默认行为：层数 +1（与以前的 <c>StackCount++</c> 完全一样）。
    /// 需要「层数即数值」的减益（如中毒：41 层 → 掉 41 点）要覆写它，
    /// 把**数值**也一起累加 —— 否则中毒叠了两次还是按第一次的量掉血。
    /// </summary>
    public virtual void MergeStacks(StatusEffect incoming)
    {
        if (incoming == null) return;
        StackCount += incoming.StackCount;
    }

    public bool IsExpired()
    {
        return _remainingDuration <= 0;
    }

    public void RefreshDuration()
    {
        _remainingDuration = Duration;
    }

    public void AddEffect(Effect effect)
    {
        if (!Effects.Contains(effect))
        {
            Effects.Add(effect);
        }
    }

    public void RemoveEffect(Effect effect)
    {
        Effects.Remove(effect);
    }

    public void ClearEffects()
    {
        Effects.Clear();
    }
}
