using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public class EffectContext
{
    public IUnit Owner { get; set; }
    public IUnit Source { get; set; }
    public IUnit Target { get; set; }
    public int Value { get; set; }
    public TriggerType CurrentTrigger { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

public class DamageContext : EffectContext
{
    public int Damage { get; set; }
    public int ShieldAbsorbed { get; set; }
    public bool IsBeforeDamage { get; set; }
}

public class HealContext : EffectContext
{
    public int HealAmount { get; set; }
}
