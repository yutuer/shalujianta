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
