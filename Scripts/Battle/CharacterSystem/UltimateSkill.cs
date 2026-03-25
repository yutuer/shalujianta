using Godot;
using System;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

public partial class UltimateSkill : Resource, ICardLevelable
{
    [Export]
    public string SkillId;

    [Export]
    public string Name;

    [Export]
    public string Description;

    [Export]
    public int RageCost = 100;

    [Export]
    public int Damage;

    [Export]
    public int Heal;

    [Export]
    public int Shield;

    [Export]
    public int DrawCount;

    [Export]
    public int BuffValue;

    [Export]
    public int BuffDuration;

    [Export]
    public int Level { get; set; } = 1;

    [Export]
    public int MaxLevel { get; set; } = 6;

    private CardUpgradeInfo _upgradeInfo = new CardUpgradeInfo();
    public CardUpgradeInfo UpgradeInfo => _upgradeInfo;

    public event System.Action<ICardLevelable> OnLevelChanged;
    public event System.Action<UltimateSkill> OnValuesChanged;

    [Export]
    public float DamageBaseCoefficient { get; set; } = 1.0f;

    [Export]
    public float HealBaseCoefficient { get; set; } = 1.0f;

    [Export]
    public float ShieldBaseCoefficient { get; set; } = 1.0f;

    [Export]
    public int DrawCountPerLevel { get; set; } = 0;

    [Export]
    public int BuffValuePerLevel { get; set; } = 0;

    private CharacterAttributes _linkedAttributes;

    public CharacterAttributes LinkedAttributes
    {
        get => _linkedAttributes;
        set
        {
            if (_linkedAttributes != value)
            {
                _linkedAttributes = value;
                OnLinkedAttributeChanged();
            }
        }
    }

    public int CalculatedDamage => CardLevelSystem.CalculateAttributeLinkedValue(Level, Damage, DamageBaseCoefficient);

    public int CalculatedHeal => CardLevelSystem.CalculateAttributeLinkedValue(Level, Heal, HealBaseCoefficient);

    public int CalculatedShield => CardLevelSystem.CalculateAttributeLinkedValue(Level, Shield, ShieldBaseCoefficient);

    public int CalculatedDrawCount => CardLevelSystem.CalculateFixedValue(Level, DrawCount, DrawCountPerLevel);

    public int CalculatedBuffValue => CardLevelSystem.CalculateFixedValue(Level, BuffValue, BuffValuePerLevel);

    private void OnLinkedAttributeChanged()
    {
        OnValuesChanged?.Invoke(this);
    }

    public void RefreshCalculatedValues()
    {
        OnValuesChanged?.Invoke(this);
    }

    public void LevelUp()
    {
        if (Level < MaxLevel)
        {
            Level++;
            _upgradeInfo.CurrentLevel = Level;
            OnLevelChanged?.Invoke(this);
            OnValuesChanged?.Invoke(this);
        }
    }

    public void SetLevel(int level)
    {
        int newLevel = Mathf.Clamp(level, 1, MaxLevel);
        if (Level != newLevel)
        {
            Level = newLevel;
            _upgradeInfo.CurrentLevel = Level;
            OnLevelChanged?.Invoke(this);
            OnValuesChanged?.Invoke(this);
        }
    }

    public void AddExp(int exp)
    {
        _upgradeInfo.AddExp(exp);
        if (_upgradeInfo.CurrentLevel > Level)
        {
            SetLevel(_upgradeInfo.CurrentLevel);
        }
    }

    public List<string> EffectIds { get; set; } = new List<string>();

    public List<Effect> CachedEffects { get; set; } = new List<Effect>();

    public delegate void SkillExecuteHandler(Player player, Enemy[] enemies);
    public SkillExecuteHandler OnExecute;

    public void Execute(Player player, Enemy[] enemies)
    {
        foreach (var effect in GetEffects())
        {
            var context = new EffectContext
            {
                Owner = player,
                Target = player,
                Value = effect.Value,
                CurrentTrigger = TriggerType.Immediate
            };
            EffectResolver.ApplyEffect(effect, context);
        }
        OnExecute?.Invoke(player, enemies);
    }

    public void LoadEffects()
    {
        CachedEffects.Clear();
        foreach (var effectId in EffectIds)
        {
            var effect = EffectRegistry.CreateEffect(effectId);
            if (effect != null)
            {
                CachedEffects.Add(effect);
            }
        }
    }

    public List<Effect> GetEffects()
    {
        if (CachedEffects.Count == 0 && EffectIds.Count > 0)
        {
            LoadEffects();
        }
        return CachedEffects;
    }

    public static UltimateSkill CreateRatUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("rat_ultimate") ?? new UltimateSkill
        {
            SkillId = "rat_ultimate",
            Name = "鼠群来袭",
            Description = "全体敌人受20伤害，抽3张",
            RageCost = 100,
            Damage = 20,
            DrawCount = 3
        };
    }

    public static UltimateSkill CreateOxUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("ox_ultimate") ?? new UltimateSkill
        {
            SkillId = "ox_ultimate",
            Name = "震天蛮牛冲",
            Description = "对最前方敌人造成50伤害",
            RageCost = 100,
            Damage = 50
        };
    }

    public static UltimateSkill CreateTigerUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("tiger_ultimate") ?? new UltimateSkill
        {
            SkillId = "tiger_ultimate",
            Name = "猛虎灭世斩",
            Description = "全体敌人受30伤害",
            RageCost = 100,
            Damage = 30
        };
    }

    public static UltimateSkill CreateRabbitUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("rabbit_ultimate") ?? new UltimateSkill
        {
            SkillId = "rabbit_ultimate",
            Name = "疾风连蹬",
            Description = "随机敌人攻击4次，各10伤害",
            RageCost = 100,
            Damage = 10,
            BuffValue = 4
        };
    }

    public static UltimateSkill CreateDragonUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("dragon_ultimate") ?? new UltimateSkill
        {
            SkillId = "dragon_ultimate",
            Name = "龙王咆哮",
            Description = "全体敌人受25伤害，附加3层中毒",
            RageCost = 100,
            Damage = 25,
            BuffValue = 3,
            BuffDuration = 3
        };
    }

    public static UltimateSkill CreateSnakeUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("snake_ultimate") ?? new UltimateSkill
        {
            SkillId = "snake_ultimate",
            Name = "万蛇噬咬",
            Description = "全体敌人受15伤害，附加5层中毒",
            RageCost = 100,
            Damage = 15,
            BuffValue = 5,
            BuffDuration = 3
        };
    }

    public static UltimateSkill CreateHorseUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("horse_ultimate") ?? new UltimateSkill
        {
            SkillId = "horse_ultimate",
            Name = "踏雪飞驹",
            Description = "对敌人造成20伤害，+2能量",
            RageCost = 100,
            Damage = 20,
            BuffValue = 2
        };
    }

    public static UltimateSkill CreateGoatUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("goat_ultimate") ?? new UltimateSkill
        {
            SkillId = "goat_ultimate",
            Name = "羊灵祝福",
            Description = "全体队友回复15生命，加10护盾",
            RageCost = 100,
            Heal = 15,
            Shield = 10
        };
    }

    public static UltimateSkill CreateMonkeyUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("monkey_ultimate") ?? new UltimateSkill
        {
            SkillId = "monkey_ultimate",
            Name = "齐天大圣",
            Description = "随机抽3张牌，本回合能量不限",
            RageCost = 100,
            DrawCount = 3,
            BuffValue = 999
        };
    }

    public static UltimateSkill CreateRoosterUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("rooster_ultimate") ?? new UltimateSkill
        {
            SkillId = "rooster_ultimate",
            Name = "雄鸡一唱天下白",
            Description = "敌人下回合无法行动",
            RageCost = 100,
            BuffDuration = 1
        };
    }

    public static UltimateSkill CreateDogUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("dog_ultimate") ?? new UltimateSkill
        {
            SkillId = "dog_ultimate",
            Name = "忠犬护主",
            Description = "全体队友加25护盾",
            RageCost = 100,
            Shield = 25
        };
    }

    public static UltimateSkill CreatePigUltimate()
    {
        return CharacterConfigLoader.GetUltimateSkill("pig_ultimate") ?? new UltimateSkill
        {
            SkillId = "pig_ultimate",
            Name = "猪刚鬣冲击",
            Description = "对最前方敌人造成35伤害，回10血",
            RageCost = 100,
            Damage = 35,
            Heal = 10
        };
    }

    public static UltimateSkill GetUltimateById(string skillId)
    {
        var fromConfig = CharacterConfigLoader.GetUltimateSkill(skillId);
        if (fromConfig != null)
        {
            return fromConfig;
        }

        return skillId switch
        {
            "rat_ultimate" => CreateRatUltimate(),
            "ox_ultimate" => CreateOxUltimate(),
            "tiger_ultimate" => CreateTigerUltimate(),
            "rabbit_ultimate" => CreateRabbitUltimate(),
            "dragon_ultimate" => CreateDragonUltimate(),
            "snake_ultimate" => CreateSnakeUltimate(),
            "horse_ultimate" => CreateHorseUltimate(),
            "goat_ultimate" => CreateGoatUltimate(),
            "monkey_ultimate" => CreateMonkeyUltimate(),
            "rooster_ultimate" => CreateRoosterUltimate(),
            "dog_ultimate" => CreateDogUltimate(),
            "pig_ultimate" => CreatePigUltimate(),
            _ => CreateRatUltimate()
        };
    }
}
