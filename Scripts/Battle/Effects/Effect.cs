using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public enum TargetFaction
{
    Self,
    Enemy,
    Friendly,
    Any
}

public abstract partial class Effect : Resource
{
    [Export]
    public string EffectId { get; set; } = "";

    [Export]
    public EffectType Type { get; set; } = EffectType.Custom;

    [Export]
    public TriggerType Trigger { get; set; } = TriggerType.Immediate;

    [Export]
    public int Value { get; set; } = 0;

    [Export]
    public int Duration { get; set; } = 0;

    [Export]
    public TargetFaction TargetFaction { get; set; } = TargetFaction.Enemy;

    [Export]
    public bool Required { get; set; } = true;

    public abstract void Apply(EffectContext context);

    protected List<IUnit> ResolveTargets(EffectContext context)
    {
        var results = new List<IUnit>();
        var owner = context.Owner;
        var source = context.Source;
        var target = context.Target;

        switch (TargetFaction)
        {
            case TargetFaction.Self:
                if (owner != null)
                    results.Add(owner);
                break;

            case TargetFaction.Enemy:
                if (source != null && FactionHelper.IsEnemy(owner, source))
                    results.Add(source);
                else if (target != null && FactionHelper.IsEnemy(owner, target))
                    results.Add(target);
                break;

            case TargetFaction.Friendly:
                if (source != null && FactionHelper.IsFriendly(owner, source))
                    results.Add(source);
                else if (target != null && FactionHelper.IsFriendly(owner, target))
                    results.Add(target);
                break;

            case TargetFaction.Any:
                if (source != null) results.Add(source);
                if (target != null && target != source) results.Add(target);
                break;
        }

        return results;
    }
}

public static class FactionHelper
{
    public static bool IsEnemy(IUnit a, IUnit b)
    {
        if (a == null || b == null) return false;
        return a.Faction != b.Faction;
    }

    public static bool IsFriendly(IUnit a, IUnit b)
    {
        if (a == null || b == null) return false;
        return a.Faction == b.Faction;
    }
}
