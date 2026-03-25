using Godot;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects.Effects;

public partial class DrawEffect : Effect
{
    [Export]
    public bool FromDiscardOnly { get; set; } = false;

    public DrawEffect()
    {
        Type = EffectType.Draw;
    }

    public override void Apply(EffectContext context)
    {
        if (context.Owner is Player player)
        {
            player.DrawCards(Value);
        }
    }
}
