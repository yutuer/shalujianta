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

    /// <summary>
    /// **伤害百分比口径**：> 0 时按「角色攻击力 × 百分比」算
    /// （如波吕克斯「无罪的圣裁」= 攻击力×300%，满级 182×3 = 546）。
    /// 0 = 走旧的绝对值口径（<see cref="Damage"/> × 系数 × 等级系数）。
    /// </summary>
    [Export]
    public int DamagePercent;

    [Export]
    public int Heal;

    /// <summary>
    /// **治疗按体质百分比**（照参考游戏《终末前夜 Morimens》：「回复 体质×N%」）。
    /// 珈伦「客官请慢用！」= 体质×25%（Lv4 → 112×25%×1.6 = 44.8 → 45）。
    /// </summary>
    [Export]
    public int HealPercent;

    /// <summary>
    /// **减益层数按攻击力百分比**（参考游戏：「施加 47 层中毒」= 中毒 攻击力×30%）。
    /// </summary>
    [Export]
    public int DebuffValuePercent;

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

    /// <summary>
    /// **队伍死亡抵抗加成**（%）—— 菲茵特「星之摇篮」。
    ///
    /// wiki 原文：「若{{词条|死亡抵抗}}不足 200% + x%，则 +100%」（1 级）
    ///            「若{{词条|死亡抵抗}}不足 300% + x%，则 +150%」（6 级）
    /// → 加成值 = 100 + 10×(等级-1)（1 级 100 / 6 级 150）。
    ///
    /// ⚠️ **x% = 菲茵特自己的死亡抵抗 × 2%**（wiki 天赋1「次生襁褓」：
    ///    「「星之摇篮」的死亡抵抗上限 +2%」）→ 上限在运行时算，见 <see cref="UltimateExecutor"/>。
    /// </summary>
    [Export]
    public int DeathResistanceGain;

    /// <summary>死亡抵抗加成的每级增量（10 → 1 级 100 / 6 级 150）</summary>
    [Export]
    public int DeathResistanceGainPerLevel;

    /// <summary>
    /// **队伍死亡抵抗上限**（%）—— 「若队伍死亡抵抗不足 上限 + x%，则 +加成」。
    ///
    /// wiki：1 级 200% / 6 级 300% → 基准 200 + 20×(等级-1)。
    /// </summary>
    [Export]
    public int DeathResistanceCap;

    /// <summary>死亡抵抗上限的每级增量（20 → 1 级 200 / 6 级 300）</summary>
    [Export]
    public int DeathResistanceCapPerLevel;

    /// <summary>
    /// **驱散自身脆弱**（菲茵特「星之摇篮」）。
    /// 照 wiki：「驱散自身{{词条|脆弱}}。」
    /// </summary>
    [Export]
    public bool DisperseSelfVulnerable;

    /// <summary>
    /// **移除所有手牌的「迟缓」状态**（珈伦「客官请慢用！」）。
    ///
    /// 照参考游戏《终末前夜 Morimens》：「移除所有手牌的 ⏱迟缓状态。」
    /// 「迟缓」词条：「使卡牌的算力消耗提高」—— 是**卡牌身上的状态**，不是敌人身上的减益。
    /// </summary>
    [Export]
    public bool RemoveSlowFromHand;

    /// <summary>
    /// **释放后置入手中的衍生卡 id**（珈伦「客官请慢用！」→ 1 张「奇妙料理」= jialun_derived）。
    /// </summary>
    [Export]
    public string AddCardToHandId = "";

    /// <summary>置入手中的张数</summary>
    [Export]
    public int AddCardToHandCount = 0;

    /// <summary>
    /// **置入手中的那张卡属于哪个角色**（卡牌不在卡组映射里，靠它归属）。
    /// 留空则由执行器按「释放狂气爆发的那个角色」推断。
    /// </summary>
    [Export]
    public string AddCardOwnerCharacterId = "";

    /// <summary>英文名（缺省由 skillId 推导）</summary>
    [Export]
    public string NameEn = "";

    /// <summary>
    /// **说明里提到的衍生卡 id**（如「无罪的圣裁」里的「圣心」= pollux_derived）——
    /// 点那个词会弹出这张衍生卡的说明面板。
    /// 数据表字段：<c>derivedCardId</c>
    /// </summary>
    [Export]
    public string DerivedCardId = "";

    /// <summary>
    /// **说明里那个衍生卡的名字**（如「圣心」）—— 用来把说明文本里的这个词做成可点击链接。
    /// 数据表字段：<c>derivedCardName</c>
    /// </summary>
    [Export]
    public string DerivedCardName = "";

    /// <summary>最近一次 Execute 的中文执行摘要</summary>
    public string LastExecuteSummary { get; private set; } = "";

    /// <summary>
    /// 最近一次 Execute 里**衍生卡没能置入手中**（手牌已满）。
    ///
    /// ⚠️ 这是一个**给 UI 用的明确状态**，不要靠去摘要字符串里找「手牌已满」来判断 ——
    /// 战斗日志窗口默认是折叠的，玩家看不到摘要，
    /// 所以「没拿到卡」这种**必须让玩家知道**的情况要把窗口弹出来（见 <c>BattleManager</c>）。
    /// </summary>
    public bool LastAddCardToHandFailed { get; set; } = false;

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

    public int CalculatedDamage
    {
        get
        {
            // 百分比口径：攻击力 × 百分比（**不额外乘等级系数** ——
            // 角色攻击力本身已随等级成长，且卡牌等级系统还没做）。
            if (DamagePercent > 0 && _linkedAttributes != null)
            {
                // ⚠️ 向上取整（照参考游戏《终末前夜 Morimens》口径，2026-10-25 主人拍板）
                return Mathf.CeilToInt(_linkedAttributes.Attack * DamagePercent / 100f);
            }
            return CardLevelSystem.CalculateAttributeLinkedValue(Level, Damage, DamageBaseCoefficient);
        }
    }

    public int CalculatedHeal => HealPercent > 0
        ? CardLevelSystem.CalculateAttributeLinkedValue(Level, _linkedAttributes != null ? _linkedAttributes.Constitution : 0, HealPercent / 100f)
        : CardLevelSystem.CalculateAttributeLinkedValue(Level, Heal, HealBaseCoefficient);

    /// <summary>减益层数（按攻击力百分比算，或固定值）</summary>
    public int CalculatedDebuffValue => DebuffValuePercent > 0
        ? CardLevelSystem.CalculateAttributeLinkedValue(Level, _linkedAttributes != null ? _linkedAttributes.Attack : 0, DebuffValuePercent / 100f)
        : CalculatedBuffValue;

    public int CalculatedShield => CardLevelSystem.CalculateAttributeLinkedValue(Level, Shield, ShieldBaseCoefficient);

    public int CalculatedDrawCount => CardLevelSystem.CalculateFixedValue(Level, DrawCount, DrawCountPerLevel);

    public int CalculatedBuffValue => CardLevelSystem.CalculateFixedValue(Level, BuffValue, BuffValuePerLevel);

    /// <summary>队伍死亡抵抗加成（%）—— 1 级 100 / 6 级 150</summary>
    public int CalculatedDeathResistanceGain => CardLevelSystem.CalculateFixedValue(Level, DeathResistanceGain, DeathResistanceGainPerLevel);

    /// <summary>队伍死亡抵抗上限（%）—— 1 级 200 / 6 级 300（不含菲茵特天赋的 x%）</summary>
    public int CalculatedDeathResistanceCap => CardLevelSystem.CalculateFixedValue(Level, DeathResistanceCap, DeathResistanceCapPerLevel);

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
    /// 执行狂气爆发
    /// 1) 旧路径：EffectIds → EffectResolver（保留兼容，若配置了 EffectIds 仍会执行）
    /// 2) 新路径：按 Damage/Heal/Shield/DrawCount/EnergyGain/IsAreaAttack/Target/ApplyDebuffName 结算真实效果
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人数组</param>
    /// <param name="owner">释放这个狂气爆发的角色（置入手中的衍生卡要靠它归属）</param>
    public void Execute(Player player, Enemy[] enemies, CharacterPosition owner = null)
    {
        GD.Print($"[UltimateSkill] Execute called: {SkillId}（{Name}）");

        // 每次执行前重置「衍生卡没拿到」的标记
        LastAddCardToHandFailed = false;

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
        LastExecuteSummary = UltimateExecutor.Execute(this, player, enemies, owner);

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

    /// <summary>
    /// 按 id 获取狂气爆发
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
            _ => null
        };

        if (fallback == null)
        {
            GD.PrintErr($"[UltimateSkill] 未找到狂气爆发：{skillId}");
        }

        return fallback;
    }
}
