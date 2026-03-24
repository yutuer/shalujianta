using System;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Effects;

namespace FishEatFish.Battle.Effects;

public enum EffectSourceType
{
    Skill,
    Buff,
    Passive,
    Artifact,
    SilverKey
}

public class EffectInstance
{
    public EffectDefinition Template { get; set; }
    public IUnit Owner { get; set; }
    public TargetFaction TargetFaction { get; set; }
    public int? SourceId { get; set; }
    public EffectSourceType SourceType { get; set; }
    public int CurrentValue { get; set; }
    public int RemainingDuration { get; set; }
    public bool IsActive { get; set; } = true;

    public bool CanTrigger(EffectContext context)
    {
        if (Owner != context.Target) return false;

        if (SourceId.HasValue && RequiresSourceValidation())
        {
            if (!EntityManager.Instance.Exists(SourceId.Value))
            {
                return false;
            }
        }

        return true;
    }

    public void Apply(EffectContext context)
    {
        if (Template == null) return;

        var effectContext = new EffectContext
        {
            Source = Owner,
            Target = context.Target,
            Value = CurrentValue
        };

        switch (Template.Type)
        {
            case EffectType.Damage:
                var damageEffect = new DamageEffect { Value = CurrentValue };
                damageEffect.Apply(effectContext);
                break;
            case EffectType.Heal:
                var healEffect = new HealEffect { Value = CurrentValue };
                healEffect.Apply(effectContext);
                break;
            case EffectType.Shield:
                var shieldEffect = new ShieldEffect { Value = CurrentValue };
                shieldEffect.Apply(effectContext);
                break;
        }
    }

    private bool RequiresSourceValidation()
    {
        return Template.Type == EffectType.Damage && TargetFaction == TargetFaction.Enemy;
    }
}
