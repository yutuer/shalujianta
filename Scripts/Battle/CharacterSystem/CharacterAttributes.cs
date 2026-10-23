using Godot;

namespace FishEatFish.Battle.CharacterSystem;

public class CharacterAttributes
{
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public int Constitution { get; set; } = 10;
    public int CritRate { get; set; } = 5;
    public int CritDamage { get; set; } = 150;

    public int DeathResistance { get; set; } = 0;
    public int SilverKeyCharge { get; set; } = 0;
    public int RageReturn { get; set; } = 0;
    public int MaxRage { get; set; } = 100;

    public int CurrentRage { get; set; } = 0;

    /// <summary>
    /// 生命上限 = 体质。
    /// ⚠️ 体质已改用「参考游戏量级」（几十~一百多），所以这里不再乘 10 ——
    /// 否则生命会变成几百上千（改之前的数值是 体质×10，量级重标定后等价）。
    /// </summary>
    public int MaxHealth => Constitution;

    public CharacterRace Race { get; set; } = CharacterRace.Chaos;

    public CharacterAttributes() { }

    public CharacterAttributes(CharacterAttributes other)
    {
        Attack = other.Attack;
        Defense = other.Defense;
        Constitution = other.Constitution;
        CritRate = other.CritRate;
        CritDamage = other.CritDamage;
        DeathResistance = other.DeathResistance;
        SilverKeyCharge = other.SilverKeyCharge;
        RageReturn = other.RageReturn;
        MaxRage = other.MaxRage;
        CurrentRage = other.CurrentRage;
        Race = other.Race;
    }

    public static int CalculateCritRateFromFloat(float rate)
    {
        return Mathf.CeilToInt(rate * 100);
    }

    public static int CalculateCritDamageFromFloat(float damage)
    {
        return Mathf.CeilToInt(damage * 100);
    }

    public static float CalculateCritRateToFloat(int critRate)
    {
        return critRate / 100f;
    }

    public static float CalculateCritDamageToFloat(int critDamage)
    {
        return critDamage / 100f;
    }

    public void AddRage(int amount)
    {
        CurrentRage = Mathf.Min(CurrentRage + amount, MaxRage);
    }

    public int UseRage(int amount)
    {
        int actualUsed = Mathf.Min(amount, CurrentRage);
        int returnAmount = Mathf.FloorToInt(actualUsed * RageReturn / 10f);
        CurrentRage -= actualUsed;
        return returnAmount;
    }

    public bool CanUseRage(int amount)
    {
        return CurrentRage >= amount;
    }

    public int CalculateSilverKeyFromPower(int powerUsed)
    {
        return powerUsed * SilverKeyCharge;
    }

    public void ApplyRaceDefaults(CharacterRace race)
    {
        Race = race;
    }
}

public static class RaceAttributeConfig
{
    public static RaceConfig GetConfig(CharacterRace race)
    {
        return race switch
        {
            CharacterRace.Chaos => new RaceConfig
            {
                Race = CharacterRace.Chaos
            },
            CharacterRace.Abyss => new RaceConfig
            {
                Race = CharacterRace.Abyss
            },
            CharacterRace.Flesh => new RaceConfig
            {
                Race = CharacterRace.Flesh
            },
            CharacterRace.Hyperdimension => new RaceConfig
            {
                Race = CharacterRace.Hyperdimension
            },
            _ => new RaceConfig()
        };
    }
}

public class RaceConfig
{
    public CharacterRace Race { get; set; } = CharacterRace.Chaos;
}
