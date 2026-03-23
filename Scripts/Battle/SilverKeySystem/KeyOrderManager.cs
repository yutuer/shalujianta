using Godot;
using System.Collections.Generic;

public partial class KeyOrderManager : Node
{
    private List<KeyOrder> _keyOrderLibrary = new List<KeyOrder>();
    private KeyOrder _equippedKeyOrder;
    private int _keyOrderUseCountThisTurn = 0;
    private const int MaxKeyOrderUsePerTurn = 2;

    public override void _Ready()
    {
        InitializeKeyOrderLibrary();
    }

    private void InitializeKeyOrderLibrary()
    {
        _keyOrderLibrary.Add(KeyOrder.CreateSilverSlash());
        _keyOrderLibrary.Add(KeyOrder.CreateKeyShield());
        _keyOrderLibrary.Add(KeyOrder.CreateEnergyInfusion());
        _keyOrderLibrary.Add(KeyOrder.CreateArmorBreak());
        _keyOrderLibrary.Add(KeyOrder.CreateLifeKey());
    }

    public void EquipKeyOrder(KeyOrder order)
    {
        _equippedKeyOrder = order;
    }

    public KeyOrder GetEquippedKeyOrder()
    {
        return _equippedKeyOrder;
    }

    public List<KeyOrder> GetRandomKeyOrders(int count)
    {
        List<KeyOrder> result = new List<KeyOrder>();
        List<KeyOrder> available = new List<KeyOrder>(_keyOrderLibrary);

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        int actualCount = Mathf.Min(count, available.Count);

        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = rng.RandiRange(0, available.Count - 1);
            result.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        return result;
    }

    public bool CanUseKeyOrder()
    {
        return _keyOrderUseCountThisTurn < MaxKeyOrderUsePerTurn;
    }

    public int GetRemainingKeyOrderUses()
    {
        return MaxKeyOrderUsePerTurn - _keyOrderUseCountThisTurn;
    }

    public void ResetTurnUsage()
    {
        _keyOrderUseCountThisTurn = 0;
    }

    public void RecordKeyOrderUse()
    {
        _keyOrderUseCountThisTurn++;
    }

    public void ApplyKeyOrderEffect(KeyOrder order, Player player, Enemy[] enemies)
    {
        switch (order.EffectType)
        {
            case KeyOrderEffectType.Damage:
                ApplyDamageEffect(order, enemies);
                break;
            case KeyOrderEffectType.Heal:
                ApplyHealEffect(order, player);
                break;
            case KeyOrderEffectType.Buff:
                ApplyBuffEffect(order, player);
                break;
            case KeyOrderEffectType.Debuff:
                ApplyDebuffEffect(order, enemies);
                break;
            case KeyOrderEffectType.Special:
                ApplySpecialEffect(order, player);
                break;
        }

        RecordKeyOrderUse();
    }

    private void ApplyDamageEffect(KeyOrder order, Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                enemy.TakeDamage(order.EffectValue);
            }
        }
    }

    private void ApplyHealEffect(KeyOrder order, Player player)
    {
        int healAmount = (int)(player.MaxHealth * order.EffectValue / 100.0f);
        player.Heal(healAmount);
    }

    private void ApplyBuffEffect(KeyOrder order, Player player)
    {
        player.AddShield(order.EffectValue);
    }

    private void ApplyDebuffEffect(KeyOrder order, Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                DefenseDownDebuff debuff = new DefenseDownDebuff
                {
                    DefenseReduction = order.EffectValue
                };
                debuff.Initialize();
                enemy.AddStatusEffect(debuff);
            }
        }
    }

    private void ApplySpecialEffect(KeyOrder order, Player player)
    {
        player.CurrentEnergy += order.EffectValue;
    }

    public List<KeyOrder> GetAllKeyOrders()
    {
        return new List<KeyOrder>(_keyOrderLibrary);
    }
}
