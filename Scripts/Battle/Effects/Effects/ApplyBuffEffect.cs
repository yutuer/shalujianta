using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Effects.Effects;

public partial class ApplyBuffEffect : Effect
{
    [Export]
    public string BuffId { get; set; } = "";

    [Export]
    public bool RefreshDuration { get; set; } = true;

    [Export]
    public int StackCount { get; set; } = 1;

    public ApplyBuffEffect()
    {
        Type = EffectType.ApplyBuff;
    }

    public override void Apply(EffectContext context)
    {
        var targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            StatusEffect buff = EffectRegistry.CreateBuff(BuffId);
            if (buff != null)
            {
                buff.Initialize();
                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(buff);
                }
            }
        }
    }
}
