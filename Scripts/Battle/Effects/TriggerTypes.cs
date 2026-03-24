namespace FishEatFish.Battle.Effects;

public enum TriggerType
{
    Immediate,
    OnTurnStart,
    OnTurnEnd,
    OnDamageDealt,
    OnDamageReceived,
    OnHeal,
    OnKill,
    OnCardPlayed,
    OnConditionMet
}

public enum EventType
{
    OnTurnStart,
    OnTurnEnd,
    OnDamageBefore,
    OnDamageAfter,
    OnHeal,
    OnKill,
    OnShieldBroken,
    OnUnitDeath
}
