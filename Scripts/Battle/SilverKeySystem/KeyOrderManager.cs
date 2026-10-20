using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

public partial class KeyOrderManager : Node
{
    private List<KeyOrder> _keyOrderLibrary = new List<KeyOrder>();
    private KeyOrder _equippedKeyOrder;
    private int _keyOrderUseCountThisTurn = 0;

    /// <summary>每回合可使用的钥令次数上限（单一真相源）</summary>
    public const int MaxKeyOrderUsePerTurn = 2;

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
        // 钳制到 0：次数可能因外部误调用而超过上限，不能返回负数
        return Mathf.Max(0, MaxKeyOrderUsePerTurn - _keyOrderUseCountThisTurn);
    }

    /// <summary>本回合已使用的钥令次数（0 ~ MaxKeyOrderUsePerTurn）</summary>
    public int GetUsedCountThisTurn()
    {
        return _keyOrderUseCountThisTurn;
    }

    /// <summary>本回合是否还没用过钥令（第一次用）</summary>
    public bool IsFirstUseThisTurn()
    {
        return _keyOrderUseCountThisTurn == 0;
    }

    public void ResetTurnUsage()
    {
        _keyOrderUseCountThisTurn = 0;
    }

    public void RecordKeyOrderUse()
    {
        _keyOrderUseCountThisTurn++;
        GD.Print($"[KeyOrderManager] RecordKeyOrderUse called: 本回合已用 {_keyOrderUseCountThisTurn}/{MaxKeyOrderUsePerTurn} 次");
    }

    /// <summary>
    /// 释放钥令效果（并记录本次使用）。
    ///
    /// ⚠️ 使用次数**只在这里记一次**。历史上调用方在调完本方法后又调了一次
    /// <see cref="RecordKeyOrderUse"/>，导致每回合计数 +2 —— 「每回合 2 次」的限额
    /// 实际只能用 1 次。调用方不要再重复记录。
    ///
    /// ⚠️ 这里同时是**限额的守门人**：超过每回合上限时**直接拒绝、不执行效果**。
    /// 光靠调用方检查是不够的 —— 历史上就有调用方漏检导致上限被突破。
    /// </summary>
    /// <returns>是否真的释放了（false = 超限或参数非法，效果未执行）</returns>
    public bool ApplyKeyOrderEffect(KeyOrder order, Player player, Enemy[] enemies)
    {
        if (order == null)
        {
            GD.PrintErr("[KeyOrderManager] ApplyKeyOrderEffect 收到空钥令，已跳过");
            return false;
        }

        if (!CanUseKeyOrder())
        {
            GD.PrintErr($"[KeyOrderManager] ApplyKeyOrderEffect 拒绝：本回合钥令已用满（{_keyOrderUseCountThisTurn}/{MaxKeyOrderUsePerTurn}）");
            return false;
        }

        GD.Print($"[KeyOrderManager] ApplyKeyOrderEffect called: {order.Name}");

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

        // 效果走通用路径（EffectIds），让 JSON 配置的钥令也能生效
        order.Execute(player, enemies);

        RecordKeyOrderUse();
        GD.Print("[KeyOrderManager] ApplyKeyOrderEffect completed");
        return true;
    }

    private void ApplyDamageEffect(KeyOrder order, Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(order.EffectValue);
                GD.Print($"[KeyOrderManager] 钥令伤害: {enemy.EnemyName} 受到 {order.EffectValue} 点伤害");
            }
        }
    }

    private void ApplyHealEffect(KeyOrder order, Player player)
    {
        if (player == null) return;

        int healAmount = (int)(player.MaxHealth * order.EffectValue / 100.0f);
        player.Heal(healAmount);
        GD.Print($"[KeyOrderManager] 钥令治疗: 回复 {healAmount} 点生命");
    }

    private void ApplyBuffEffect(KeyOrder order, Player player)
    {
        if (player == null) return;

        player.AddShield(order.EffectValue);
        GD.Print($"[KeyOrderManager] 钥令护盾: 获得 {order.EffectValue} 点护盾");
    }

    private void ApplyDebuffEffect(KeyOrder order, Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                DefenseDownDebuff debuff = new DefenseDownDebuff
                {
                    DefenseReduction = order.EffectValue
                };
                debuff.Initialize();
                enemy.AddStatusEffect(debuff);
                GD.Print($"[KeyOrderManager] 钥令破甲: {enemy.EnemyName} 防御 -{order.EffectValue}");
            }
        }
    }

    private void ApplySpecialEffect(KeyOrder order, Player player)
    {
        if (player == null) return;

        player.CurrentEnergy += order.EffectValue;
        GD.Print($"[KeyOrderManager] 钥令能量: 获得 {order.EffectValue} 点能量");
    }

    public List<KeyOrder> GetAllKeyOrders()
    {
        return new List<KeyOrder>(_keyOrderLibrary);
    }
}
