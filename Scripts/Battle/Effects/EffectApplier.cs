using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public class EffectSourceContext
{
    public IUnit Caster { get; set; }
    public IUnit Target { get; set; }
    public EffectSourceType SourceType { get; set; }
    public string SourceId { get; set; }
}

public class EffectApplier
{
    public static readonly EffectApplier Instance = new EffectApplier();

    public EffectInstance Apply(EffectDefinition definition, EffectSourceContext context)
    {
        var effectInstance = new EffectInstance
        {
            Template = definition,
            SourceId = context.Caster?.Id,
            SourceType = context.SourceType,
            CurrentValue = definition.Value,
            RemainingDuration = definition.Duration
        };

        switch (context.SourceType)
        {
            case EffectSourceType.Buff:
                ConfigureForBuff(definition, context, effectInstance);
                break;
            case EffectSourceType.Artifact:
                ConfigureForArtifact(definition, context, effectInstance);
                break;
            case EffectSourceType.Skill:
                ConfigureForSkill(definition, context, effectInstance);
                break;
            case EffectSourceType.Passive:
                ConfigureForPassive(definition, context, effectInstance);
                break;
        }

        if (definition.Trigger != TriggerType.Immediate)
        {
            EventTriggerSystem.Instance.RegisterEffect(effectInstance);
        }

        return effectInstance;
    }

    private void ConfigureForBuff(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Target;
        inst.TargetFaction = TargetFaction.Self;
    }

    private void ConfigureForArtifact(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Caster;
        inst.TargetFaction = TargetFaction.Enemy;
    }

    private void ConfigureForSkill(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        if (def.Type == EffectType.ApplyDebuff)
        {
            inst.Owner = ctx.Target;
            inst.TargetFaction = TargetFaction.Self;
        }
        else
        {
            inst.Owner = ctx.Caster;
            inst.TargetFaction = TargetFaction.Enemy;
        }
    }

    private void ConfigureForPassive(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Caster;
        inst.TargetFaction = def.TargetFaction;
    }
}
