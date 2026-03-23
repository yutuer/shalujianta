using Godot;
using System;

public partial class KeyOrder : Resource
{
    [Export]
    public string KeyOrderId;

    [Export]
    public string Name;

    [Export]
    public string Description;

    [Export]
    public int SilverKeyCost = 1000;

    [Export]
    public KeyOrderEffectType EffectType = KeyOrderEffectType.Damage;

    [Export]
    public int EffectValue = 50;

    [Export]
    public int Duration = 0;

    public delegate void KeyOrderExecuteHandler(Player player, Enemy[] enemies);
    public KeyOrderExecuteHandler OnExecute;

    public void Execute(Player player, Enemy[] enemies)
    {
        OnExecute?.Invoke(player, enemies);
    }

    public static KeyOrder CreateSilverSlash()
    {
        var order = new KeyOrder
        {
            KeyOrderId = "silver_slash",
            Name = "银光斩",
            Description = "对所有敌人造成50伤害",
            SilverKeyCost = 1000,
            EffectType = KeyOrderEffectType.Damage,
            EffectValue = 50
        };
        return order;
    }

    public static KeyOrder CreateKeyShield()
    {
        var order = new KeyOrder
        {
            KeyOrderId = "key_shield",
            Name = "钥之护盾",
            Description = "全体队友获得30护盾",
            SilverKeyCost = 1000,
            EffectType = KeyOrderEffectType.Buff,
            EffectValue = 30
        };
        return order;
    }

    public static KeyOrder CreateEnergyInfusion()
    {
        var order = new KeyOrder
        {
            KeyOrderId = "energy_infusion",
            Name = "能量灌注",
            Description = "本回合能量+5",
            SilverKeyCost = 1000,
            EffectType = KeyOrderEffectType.Special,
            EffectValue = 5
        };
        return order;
    }

    public static KeyOrder CreateArmorBreak()
    {
        var order = new KeyOrder
        {
            KeyOrderId = "armor_break",
            Name = "破甲之钥",
            Description = "敌人防御-10持续3回合",
            SilverKeyCost = 1000,
            EffectType = KeyOrderEffectType.Debuff,
            EffectValue = 10,
            Duration = 3
        };
        return order;
    }

    public static KeyOrder CreateLifeKey()
    {
        var order = new KeyOrder
        {
            KeyOrderId = "life_key",
            Name = "生命之钥",
            Description = "恢复攻击者30%最大生命",
            SilverKeyCost = 1000,
            EffectType = KeyOrderEffectType.Heal,
            EffectValue = 30
        };
        return order;
    }
}
