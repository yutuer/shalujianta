using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects.Effects;

public partial class HealEffect : Effect
{
    [Export]
    public bool IsPercent { get; set; } = false;

    [Export]
    public bool CapAtMaxHealth { get; set; } = true;

    public HealEffect()
    {
        Type = EffectType.Heal;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            int healAmount = IsPercent
                ? (int)(target.MaxHealth * Value / 100.0f)
                : Value;

            target.Heal(healAmount);
        }
    }
}
