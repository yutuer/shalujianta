using Godot;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects.Effects;

public partial class EnergyEffect : Effect
{
    [Export]
    public bool ThisTurnOnly { get; set; } = false;

    public EnergyEffect()
    {
        Type = EffectType.Energy;
    }

    public override void Apply(EffectContext context)
    {
        if (context.Owner is Player player)
        {
            player.CurrentEnergy += Value;
        }
    }
}
