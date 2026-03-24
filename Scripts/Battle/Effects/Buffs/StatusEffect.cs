using Godot;

namespace FishEatFish.Battle.Effects.Buffs;

public enum StatusEffectType
{
    Buff,
    Debuff
}

public enum EffectTrigger
{
    OnApply,
    OnTurnStart,
    OnTurnEnd,
    OnDamageReceived,
    OnDamageDealt,
    OnKill
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
    public EffectTrigger Trigger { get; set; } = EffectTrigger.OnApply;

    [Export]
    public int IconPathHash { get; set; } = 0;

    protected int _remainingDuration;

    public int RemainingDuration => _remainingDuration;

    public virtual void OnApplyPlayer(Core.Player target) { }
    public virtual void OnRemovePlayer(Core.Player target) { }
    public virtual void OnTurnStartPlayer(Core.Player target) { }
    public virtual void OnTurnEndPlayer(Core.Player target)
    {
        _remainingDuration--;
    }

    public virtual void OnApplyEnemy(Core.Enemy target) { }
    public virtual void OnRemoveEnemy(Core.Enemy target) { }
    public virtual void OnTurnStartEnemy(Core.Enemy target) { }
    public virtual void OnTurnEndEnemy(Core.Enemy target)
    {
        _remainingDuration--;
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

    public bool IsExpired()
    {
        return _remainingDuration <= 0;
    }

    public void RefreshDuration()
    {
        _remainingDuration = Duration;
    }
}
