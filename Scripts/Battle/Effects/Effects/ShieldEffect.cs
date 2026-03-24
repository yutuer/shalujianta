using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects.Effects;

public partial class ShieldEffect : Effect
{
    [Export]
    public bool IsRetain { get; set; } = false;

    public ShieldEffect()
    {
        Type = EffectType.Shield;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            target.AddShield(Value);
        }
    }
}
