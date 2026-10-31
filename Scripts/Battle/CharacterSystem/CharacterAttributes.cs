using Godot;

namespace FishEatFish.Battle.CharacterSystem;

public class CharacterAttributes
{
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public int Constitution { get; set; } = 10;

    /// <summary>
    /// 暴击率（百分数，5 = 5%）。
    /// ⚠️ 用 float 而不是 int —— 参考图「详细属性」面板给的是 **14.6%** 这种小数
    /// （wiki 升格5 的原值也是 14.6%），int 存不下，只能四舍五入成 15，
    /// 就跟参考图对不上了。
    /// </summary>
    public float CritRate { get; set; } = 5f;

    public int CritDamage { get; set; } = 150;

    public int DeathResistance { get; set; } = 0;
    public int SilverKeyCharge { get; set; } = 0;

    /// <summary>
    /// 狂气回充等级（消耗狂气后按 <c>floor(消耗量 × 等级 / 10)</c> 回冲）。
    /// ⚠️ 用 float —— wiki 升格5（60 级 0 命）的原值是 **5.6**（珈伦），
    /// int 存不下会截断成 5，就跟 wiki 对不上了。
    /// </summary>
    public float RageReturn { get; set; } = 0f;
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
        // ⚠️ RageReturn 是 float（wiki 原值 5.6 这种小数），**5.6f 实际是 5.59999990463257** ——
        // 直接 floor(100 * 5.59999990463257 / 10) 会掉成 **55**（应为 56）。
        // 所以：① 先按 double 算（把 float 的误差摊薄）② 加一个极小的 epsilon 抵消二进制表示误差。
        //    epsilon 只在真实值恰好是整数时起作用（0.0001 远小于 1，不会把 x.9999 顶进位）。
        int returnAmount = Mathf.FloorToInt((float)(actualUsed * (double)RageReturn / 10.0 + 0.0001));
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
