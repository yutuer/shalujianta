using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects.Effects;

public partial class DamageEffect : Effect
{
    [Export]
    public bool IgnoreDefense { get; set; } = false;

    [Export]
    public bool IgnoreShield { get; set; } = false;

    public DamageEffect()
    {
        Type = EffectType.Damage;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            int damage = Value;

            if (IgnoreDefense)
            {
                target.TakeDamage(damage);
            }
            else if (IgnoreShield)
            {
                target.TakeDamage(damage);
            }
            else
            {
                target.TakeDamage(damage);
            }
        }
    }
}
