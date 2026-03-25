using Godot;

namespace FishEatFish.Battle.Card;

public enum ValueSourceType
{
    Attack,
    Defense,
    Constitution,
    Fixed
}

public enum ValueEffectType
{
    Damage,
    Shield,
    Heal,
    Counter,
    EnergyGain,
    DrawCount,
    BuffValue,
    DebuffValue
}

public class CardValueConfig
{
    public ValueSourceType SourceType { get; set; } = ValueSourceType.Fixed;
    public ValueEffectType EffectType { get; set; } = ValueEffectType.Damage;
    public float BaseCoefficient { get; set; } = 1.0f;
    public int BaseFixedValue { get; set; } = 0;
    public int PerLevelValue { get; set; } = 0;
    public int MinValue { get; set; } = int.MinValue;
    public int MaxValue { get; set; } = int.MaxValue;
}

public static class CardLevelSystem
{
    private const float LEVEL_BONUS_PERCENT = 0.20f;
    private const int BASE_RAGE_GAIN = 5;

    public static float CalculateCoefficient(int level, float baseCoefficient = 1.0f)
    {
        if (level <= 0) level = 1;
        return baseCoefficient * (1.0f + LEVEL_BONUS_PERCENT * (level - 1));
    }

    public static int CalculateAttributeLinkedValue(
        int level,
        int attributeValue,
        float baseCoefficient,
        int minValue = int.MinValue,
        int maxValue = int.MaxValue)
    {
        float coefficient = CalculateCoefficient(level, baseCoefficient);
        int value = Mathf.RoundToInt(attributeValue * coefficient);
        return Mathf.Clamp(value, minValue, maxValue);
    }

    public static int CalculateFixedValue(int level, int baseValue, int perLevelValue)
    {
        if (level <= 0) level = 1;
        return baseValue + perLevelValue * (level - 1);
    }

    public static int CalculateRageGain(int level, int perLevelIncrease = 0)
    {
        return CalculateFixedValue(level, BASE_RAGE_GAIN, perLevelIncrease);
    }

    public static int GetValueFromConfig(
        CardValueConfig config,
        int level,
        CharacterSystem.CharacterAttributes attributes)
    {
        return config.SourceType switch
        {
            ValueSourceType.Attack => CalculateAttributeLinkedValue(
                level,
                attributes.Attack,
                config.BaseCoefficient,
                config.MinValue,
                config.MaxValue),
            ValueSourceType.Defense => CalculateAttributeLinkedValue(
                level,
                attributes.Defense,
                config.BaseCoefficient,
                config.MinValue,
                config.MaxValue),
            ValueSourceType.Constitution => CalculateAttributeLinkedValue(
                level,
                attributes.Constitution,
                config.BaseCoefficient,
                config.MinValue,
                config.MaxValue),
            ValueSourceType.Fixed => CalculateFixedValue(level, config.BaseFixedValue, config.PerLevelValue),
            _ => config.BaseFixedValue
        };
    }

    public static CardValueConfig CreateDamageConfig(float baseCoefficient = 1.0f)
    {
        return new CardValueConfig
        {
            SourceType = ValueSourceType.Attack,
            EffectType = ValueEffectType.Damage,
            BaseCoefficient = baseCoefficient
        };
    }

    public static CardValueConfig CreateShieldConfig(float baseCoefficient = 1.0f)
    {
        return new CardValueConfig
        {
            SourceType = ValueSourceType.Defense,
            EffectType = ValueEffectType.Shield,
            BaseCoefficient = baseCoefficient
        };
    }

    public static CardValueConfig CreateHealConfig(float baseCoefficient = 1.0f)
    {
        return new CardValueConfig
        {
            SourceType = ValueSourceType.Constitution,
            EffectType = ValueEffectType.Heal,
            BaseCoefficient = baseCoefficient
        };
    }

    public static CardValueConfig CreateCounterConfig(float baseCoefficient = 1.0f)
    {
        return new CardValueConfig
        {
            SourceType = ValueSourceType.Attack,
            EffectType = ValueEffectType.Counter,
            BaseCoefficient = baseCoefficient
        };
    }

    public static CardValueConfig CreateFixedConfig(int baseValue, int perLevelValue = 0)
    {
        return new CardValueConfig
        {
            SourceType = ValueSourceType.Fixed,
            EffectType = ValueEffectType.Damage,
            BaseFixedValue = baseValue,
            PerLevelValue = perLevelValue
        };
    }
}

public class CardUpgradeInfo
{
    public int CurrentLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 6;
    public int UpgradeCost { get; set; } = 100;
    public int CurrentExp { get; set; } = 0;
    public int ExpToNextLevel { get; set; } = 100;

    public bool CanUpgrade => CurrentLevel < MaxLevel;
    public float LevelProgress => MaxLevel > 1 ? (float)CurrentExp / ExpToNextLevel : 1.0f;

    public void AddExp(int exp)
    {
        CurrentExp += exp;
        while (CurrentExp >= ExpToNextLevel && CurrentLevel < MaxLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel++;
            ExpToNextLevel = CalculateExpForLevel(CurrentLevel + 1);
            UpgradeCost = CalculateUpgradeCost(CurrentLevel);
        }
    }

    public static int CalculateExpForLevel(int level)
    {
        return 100 * level * level;
    }

    public static int CalculateUpgradeCost(int level)
    {
        return 100 * level;
    }
}

public interface ICardLevelable
{
    int Level { get; set; }
    int MaxLevel { get; }
    CardUpgradeInfo UpgradeInfo { get; }
    void LevelUp();
    void SetLevel(int level);
    event System.Action<ICardLevelable> OnLevelChanged;
}
