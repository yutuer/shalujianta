namespace FishEatFish.Battle.Core;

public enum FactionType
{
    Player,
    Enemy,
    Neutral
}

public interface IUnit
{
    int Id { get; }
    int CurrentHealth { get; set; }
    int MaxHealth { get; }
    int Attack { get; set; }
    int Defense { get; set; }
    int Shield { get; set; }
    FactionType Faction { get; }
    bool IsDead { get; }
    float GetHealthPercent();

    void TakeDamage(int damage);
    void Heal(int amount);
    void AddShield(int amount);
}
