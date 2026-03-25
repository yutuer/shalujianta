using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Effects.Effects;

public partial class ApplyDebuffEffect : Effect
{
    [Export]
    public string DebuffId { get; set; } = "";

    [Export]
    public bool RefreshDuration { get; set; } = true;

    public ApplyDebuffEffect()
    {
        Type = EffectType.ApplyDebuff;
    }

    public override void Apply(EffectContext context)
    {
        var targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            StatusEffect debuff = EffectRegistry.CreateDebuff(DebuffId);
            if (debuff != null)
            {
                debuff.Initialize();
                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(debuff);
                }
            }
        }
    }
}
