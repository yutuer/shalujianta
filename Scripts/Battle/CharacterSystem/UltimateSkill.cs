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

    /// <summary>是否全体攻击（true=对所有存活敌人生效）</summary>
    [Export]
    public bool IsAreaAttack;

    /// <summary>单体目标：front / rear / random / enemy，空或其它值=第一个存活敌人</summary>
    [Export]
    public string Target = "";

    /// <summary>附加减益名：poison / weak / slow / silence，空=不附加</summary>
    [Export]
    public string ApplyDebuffName = "";

    /// <summary>释放后获得的能量</summary>
    [Export]
    public int EnergyGain;

    /// <summary>英文名（缺省由 skillId 推导）</summary>
    [Export]
    public string NameEn = "";

    /// <summary>最近一次 Execute 的中文执行摘要</summary>
    public string LastExecuteSummary { get; private set; } = "";

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

    /// <summary>
    /// 执行大招
    /// 1) 旧路径：EffectIds → EffectResolver（保留兼容，若配置了 EffectIds 仍会执行）
    /// 2) 新路径：按 Damage/Heal/Shield/DrawCount/EnergyGain/IsAreaAttack/Target/ApplyDebuffName 结算真实效果
    /// </summary>
    public void Execute(Player player, Enemy[] enemies)
    {
        GD.Print($"[UltimateSkill] Execute called: {SkillId}（{Name}）");

        // 旧路径：EffectIds → EffectResolver（与新路径互不冲突）
        if (player != null)
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
        }
        else
        {
            GD.PrintErr($"[UltimateSkill] 【{Name}】玩家为空，跳过 EffectIds 效果");
        }

        // 新路径：按字段结算真实效果
        LastExecuteSummary = UltimateExecutor.Execute(this, player, enemies);

        OnExecute?.Invoke(player, enemies);
        GD.Print($"[UltimateSkill] Execute completed: {SkillId} -> {LastExecuteSummary}");
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
        return UltimateLoader.GetUltimate("rat_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("rat_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "rat_ultimate",
            Name = "鼠群来袭",
            Description = "全体敌人受20伤害，抽3张",
            RageCost = 100,
            Damage = 20,
            DrawCount = 3,
            IsAreaAttack = true
        };
    }

    public static UltimateSkill CreateOxUltimate()
    {
        return UltimateLoader.GetUltimate("ox_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("ox_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "ox_ultimate",
            Name = "震天蛮牛冲",
            Description = "对最前方敌人造成50伤害",
            RageCost = 100,
            Damage = 50,
            Target = "front"
        };
    }

    public static UltimateSkill CreateTigerUltimate()
    {
        return UltimateLoader.GetUltimate("tiger_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("tiger_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "tiger_ultimate",
            Name = "猛虎灭世斩",
            Description = "全体敌人受30伤害",
            RageCost = 100,
            Damage = 30,
            IsAreaAttack = true
        };
    }

    public static UltimateSkill CreateRabbitUltimate()
    {
        return UltimateLoader.GetUltimate("rabbit_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("rabbit_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "rabbit_ultimate",
            Name = "疾风连蹬",
            Description = "对随机敌人造成10伤害",
            RageCost = 100,
            Damage = 10,
            BuffValue = 4,
            Target = "random"
        };
    }

    public static UltimateSkill CreateDragonUltimate()
    {
        return UltimateLoader.GetUltimate("dragon_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("dragon_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "dragon_ultimate",
            Name = "龙王咆哮",
            Description = "全体敌人受25伤害并附加中毒（每回合3点伤害，持续3回合）",
            RageCost = 100,
            Damage = 25,
            BuffValue = 3,
            BuffDuration = 3,
            ApplyDebuffName = "poison",
            IsAreaAttack = true
        };
    }

    public static UltimateSkill CreateSnakeUltimate()
    {
        return UltimateLoader.GetUltimate("snake_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("snake_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "snake_ultimate",
            Name = "万蛇噬咬",
            Description = "全体敌人受15伤害并附加中毒（每回合5点伤害，持续3回合）",
            RageCost = 100,
            Damage = 15,
            BuffValue = 5,
            BuffDuration = 3,
            ApplyDebuffName = "poison",
            IsAreaAttack = true
        };
    }

    public static UltimateSkill CreateHorseUltimate()
    {
        return UltimateLoader.GetUltimate("horse_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("horse_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "horse_ultimate",
            Name = "踏雪飞驹",
            Description = "对1名敌人造成20伤害，获得2点能量",
            RageCost = 100,
            Damage = 20,
            BuffValue = 2,
            EnergyGain = 2
        };
    }

    public static UltimateSkill CreateGoatUltimate()
    {
        return UltimateLoader.GetUltimate("goat_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("goat_ultimate")
            ?? new UltimateSkill
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
        return UltimateLoader.GetUltimate("monkey_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("monkey_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "monkey_ultimate",
            Name = "齐天大圣",
            Description = "随机抽3张牌",
            RageCost = 100,
            DrawCount = 3,
            BuffValue = 999
        };
    }

    public static UltimateSkill CreateRoosterUltimate()
    {
        return UltimateLoader.GetUltimate("rooster_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("rooster_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "rooster_ultimate",
            Name = "雄鸡一唱天下白",
            Description = "敌人下回合无法行动",
            RageCost = 100,
            BuffDuration = 1,
            ApplyDebuffName = "silence",
            Target = "enemy"
        };
    }

    public static UltimateSkill CreateDogUltimate()
    {
        return UltimateLoader.GetUltimate("dog_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("dog_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "dog_ultimate",
            Name = "忠犬护主",
            Description = "获得25护盾",
            RageCost = 100,
            Shield = 25
        };
    }

    public static UltimateSkill CreatePigUltimate()
    {
        return UltimateLoader.GetUltimate("pig_ultimate")
            ?? CharacterConfigLoader.GetUltimateSkill("pig_ultimate")
            ?? new UltimateSkill
        {
            SkillId = "pig_ultimate",
            Name = "猪刚鬣冲击",
            Description = "对最前方敌人造成35伤害，回复10生命",
            RageCost = 100,
            Damage = 35,
            Heal = 10,
            Target = "front"
        };
    }

    /// <summary>
    /// 按 id 获取大招
    /// 顺序：ultimates.json（字段最全，唯一权威源）→ characters.json 内嵌 ultimateSkill → 硬编码兜底
    /// </summary>
    public static UltimateSkill GetUltimateById(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            GD.PrintErr("[UltimateSkill] GetUltimateById called: skillId 为空");
            return null;
        }

        // 1) 优先使用 ultimates.json
        UltimateSkill fromUltimateConfig = UltimateLoader.GetUltimate(skillId);
        if (fromUltimateConfig != null)
        {
            return fromUltimateConfig;
        }

        // 2) 回退到 characters.json 内嵌的 ultimateSkill
        UltimateSkill fromCharacterConfig = CharacterConfigLoader.GetUltimateSkill(skillId);
        if (fromCharacterConfig != null)
        {
            GD.Print($"[UltimateSkill] ultimates.json 未找到 {skillId}，回退 characters.json");
            return fromCharacterConfig;
        }

        // 3) 硬编码兜底
        UltimateSkill fallback = skillId switch
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
            _ => null
        };

        if (fallback == null)
        {
            GD.PrintErr($"[UltimateSkill] 未找到大招：{skillId}");
        }

        return fallback;
    }
}
