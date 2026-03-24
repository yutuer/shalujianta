using Godot;

namespace FishEatFish.Battle.Effects;

public partial class EffectDefinition : Resource
{
    [Export]
    public string EffectId { get; set; } = "";

    [Export]
    public EffectType Type { get; set; } = EffectType.Damage;

    [Export]
    public TriggerType Trigger { get; set; } = TriggerType.Immediate;

    [Export]
    public int Value { get; set; } = 0;

    [Export]
    public int Duration { get; set; } = 0;

    [Export]
    public TargetFaction TargetFaction { get; set; } = TargetFaction.Enemy;

    [Export]
    public string BuffId { get; set; } = "";

    [Export]
    public bool IsDebuff { get; set; } = false;

    [Export]
    public bool IgnoreDefense { get; set; } = false;

    [Export]
    public bool IgnoreShield { get; set; } = false;

    [Export]
    public bool IsPercent { get; set; } = false;

    [Export]
    public bool CapAtMaxHealth { get; set; } = true;
}
