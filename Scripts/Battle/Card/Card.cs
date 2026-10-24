using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.EngravingSystem;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle.Card;

public enum TargetType
{
    None = 0,
    Front = 1,
    Rear = 2,
    Position = 3
}

/// <summary>
/// **卡牌类型**（对照参考游戏《终末前夜 Morimens》）：
/// 指令 = 普通指令卡；衍生 = 由大招 / 卡牌派生出来的卡；灵知觉醒 = 打出后给角色加怒气并产生新能力。
/// </summary>
public enum CardType
{
    /// <summary>指令（普通卡牌）</summary>
    Instruction = 0,
    /// <summary>衍生卡（不属于角色原生卡组，由别处生成）</summary>
    Derived = 1,
    /// <summary>灵知觉醒（打出后加怒气 + 产生新能力）</summary>
    Awakening = 2
}

/// <summary>卡牌词缀（数组，可被 buff 增减）</summary>
public static class CardAffix
{
    /// <summary>保留：回合结束留在手牌，不进弃牌堆</summary>
    public const string Retain = "retain";
    /// <summary>消耗：打出后从牌库移除</summary>
    public const string Consume = "consume";
    /// <summary>回合唯一：每回合只能打出一次</summary>
    public const string UniquePerTurn = "uniquePerTurn";
}

public partial class Card : Resource, ICardLevelable
{
	[Export]
	public string CardId { get; set; } = "";

	[Export]
	public string Name { get; set; } = "Card";

	[Export]
	public string Description { get; set; } = "";

	[Export]
	public int Level { get; set; } = 1;

	[Export]
	public int MaxLevel { get; set; } = 6;

	private CardUpgradeInfo _upgradeInfo = new CardUpgradeInfo();
	public CardUpgradeInfo UpgradeInfo => _upgradeInfo;

	public event System.Action<ICardLevelable> OnLevelChanged;
	public event System.Action<Card> OnValuesChanged;

	[Export]
	public float DamageBaseCoefficient { get; set; } = 0.6f;

	[Export]
	public float ShieldBaseCoefficient { get; set; } = 0.6f;

	[Export]
	public float HealBaseCoefficient { get; set; } = 0.6f;

	[Export]
	public float CounterBaseCoefficient { get; set; } = 0.5f;

	[Export]
	public int RageGainPerLevel { get; set; } = 0;

	[Export]
	public int BuffValueBase { get; set; } = 0;

	[Export]
	public int BuffValuePerLevel { get; set; } = 0;

	[Export]
	public int DebuffValueBase { get; set; } = 0;

	[Export]
	public int DebuffValuePerLevel { get; set; } = 0;

	[Export]
	public int EnergyGainBase { get; set; } = 0;

	[Export]
	public int EnergyGainPerLevel { get; set; } = 0;

	[Export]
	public int DrawCountBase { get; set; } = 0;

	[Export]
	public int DrawCountPerLevel { get; set; } = 0;

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

	public void RefreshCalculatedValues()
	{
		OnValuesChanged?.Invoke(this);
	}

	private void OnLinkedAttributeChanged()
	{
		OnValuesChanged?.Invoke(this);
	}

	public int CalculatedDamage => CardLevelSystem.CalculateAttributeLinkedValue(Level, Damage, DamageBaseCoefficient);

	public int CalculatedShield => CardLevelSystem.CalculateAttributeLinkedValue(Level, ShieldGain, ShieldBaseCoefficient);

	public int CalculatedHeal => CardLevelSystem.CalculateAttributeLinkedValue(Level, HealAmount, HealBaseCoefficient);

	public int CalculatedRageGain => CardLevelSystem.CalculateRageGain(Level, RageGainPerLevel);

	public int CalculatedBuffValue => CardLevelSystem.CalculateFixedValue(Level, BuffValueBase, BuffValuePerLevel);

	public int CalculatedDebuffValue => CardLevelSystem.CalculateFixedValue(Level, DebuffValueBase, DebuffValuePerLevel);

	public int CalculatedEnergyGain => CardLevelSystem.CalculateFixedValue(Level, EnergyGainBase, EnergyGainPerLevel);

	public int CalculatedDrawCount => CardLevelSystem.CalculateFixedValue(Level, DrawCountBase, DrawCountPerLevel);

	/// <summary>
	/// **最终伤害**：优先走百分比口径（角色攻击 × 百分比 × 等级系数）；
	/// 没有百分比时回退到卡牌写死的绝对值（兼容旧数据）。
	/// 卡面文案与战斗结算**必须都调这个函数**（否则两边数值会漂）。
	/// </summary>
	/// <param name="attributes">角色属性（为空时按绝对值算）</param>
	/// <param name="level">按哪一级算（0 = 卡牌当前等级；**升级预览传 等级+1**）</param>
	public int GetFinalDamage(CharacterAttributes attributes, int level = 0)
	{
		int lv = level > 0 ? level : Level;
		if (DamagePercent > 0 && attributes != null)
		{
			return CardLevelSystem.CalculateAttributeLinkedValue(lv, attributes.Attack, DamagePercent / 100f);
		}
		return CardLevelSystem.CalculateAttributeLinkedValue(lv, Damage, DamageBaseCoefficient);
	}

	/// <summary>
	/// **最终护盾**：优先走百分比口径（角色防御 × 百分比 × 等级系数）；没有则回退绝对值。
	/// </summary>
	/// <param name="attributes">角色属性（为空时按绝对值算）</param>
	/// <param name="level">按哪一级算（0 = 卡牌当前等级；**升级预览传 等级+1**）</param>
	public int GetFinalShield(CharacterAttributes attributes, int level = 0)
	{
		int lv = level > 0 ? level : Level;
		if (ShieldPercent > 0 && attributes != null)
		{
			return CardLevelSystem.CalculateAttributeLinkedValue(lv, attributes.Defense, ShieldPercent / 100f);
		}
		return CardLevelSystem.CalculateAttributeLinkedValue(lv, ShieldGain, ShieldBaseCoefficient);
	}

	/// <summary>
	/// 这张卡是不是**防御卡**（配色 / 图标 / 列表判定统一走这里）。
	///
	/// ⚠️ 不能再直接判 <c>ShieldGain > 0</c>：百分比化之后绝对值字段恒为 0，
	/// 防御卡会被当成普通卡画成紫边。三个信号任意一个成立即是防御卡：
	/// 卡位标记 <see cref="IsDefense"/> / 绝对值护盾 / 护盾百分比。
	/// </summary>
	public bool IsDefenseCard()
	{
		return IsDefense || ShieldGain > 0 || ShieldPercent > 0;
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

	[Export]
	public int Cost { get; set; } = 1;

	[Export]
	public int BaseCost { get; set; } = 1;

	public bool IsRetain { get; set; } = false;

    [Export]
    public bool IsAttack { get; set; } = false;

    [Export]
    public bool IsAreaAttack { get; set; } = false;

    [Export]
    public TargetType Target { get; set; } = TargetType.None;

    [Export]
    public int TargetPosition { get; set; } = -1;

    [Export]
    public int Damage { get; set; } = 0;

	[Export]
	public int ShieldGain { get; set; } = 0;

	[Export]
    public int EnergyGain { get; set; } = 0;

    [Export]
    public int HealAmount { get; set; } = 0;

	[Export]
	public int DrawCount { get; set; } = 0;

	[Export]
	public int BlockDuration { get; set; } = 0;

	[Export]
	public string ApplyBuffName { get; set; } = "";

	[Export]
	public int ApplyBuffDuration { get; set; } = 0;

	[Export]
	public string ApplyDebuffName { get; set; } = "";

	[Export]
	public int ApplyDebuffDuration { get; set; } = 0;

	/// <summary>
	/// 伤害百分比：**最终伤害 = 角色攻击 × 本百分比 × 等级系数**。
	/// 卡牌不写死伤害绝对值（对照参考游戏：升级界面写的是「护盾：防御力*18%」）。
	/// </summary>
	[Export]
	public int DamagePercent { get; set; } = 0;

	/// <summary>
	/// 护盾百分比：**最终护盾 = 角色防御 × 本百分比 × 等级系数**。
	/// </summary>
	[Export]
	public int ShieldPercent { get; set; } = 0;

    [Export]
    public bool IsEngraved { get; set; } = false;

    [Export]
    public EngravingSystem.EngravingType EngravingType { get; set; } = EngravingSystem.EngravingType.Shield;

    public EngravingSystem.EngravingEffect EngravingEffect { get; set; }

    [Export]
    public bool IsDefense { get; set; } = false;

    /// <summary>
    /// **卡牌类型**（指令 / 衍生 / 灵知觉醒）—— 决定卡面文案与效果的呈现方式。
    /// 数据表字段：<c>cardType</c>
    /// </summary>
    [Export]
    public CardType CardType { get; set; } = CardType.Instruction;

    /// <summary>
    /// **词缀数组**（保留 / 消耗 / 回合唯一…）—— 后续 buff 可以给卡牌**增减**词缀。
    /// 数据表字段：<c>affixes</c>
    /// ⚠️ 不用 [Export]：Godot 的 C# 绑定不支持导出 <c>List&lt;string&gt;</c>（同 <c>EffectIds</c>）。
    /// </summary>
    public List<string> Affixes { get; set; } = new List<string>();

    /// <summary>
    /// **卡牌自带怒气值**（绝对值，0 = 不指定）。
    ///
    /// 为什么需要：灵知觉醒这类卡的怒气是**它自己的**（参考图：净世之路 50 点），
    /// 不属于「攻击牌 15 / 防御牌 10」那套按卡位给的默认。
    /// 数据表字段：<c>rageGain</c>
    /// </summary>
    [Export]
    public int RageGain { get; set; } = 0;

    /// <summary>
    /// **所属角色名**（显示用）—— 文案里要写「波吕克斯获得 10 点怒气」这种带角色名的话。
    /// 战斗里由 <c>Attacker</c> 建卡时挂上，界面里由角色一览挂上。
    /// </summary>
    public string OwnerName { get; set; } = "";

    /// <summary>
    /// **策划写的专属说明**（灵知觉醒卡那一大段）。
    /// 数据表字段：<c>description</c>。普通指令卡留空，文案由数值自动生成。
    /// </summary>
    [Export]
    public string TemplateDescription { get; set; } = "";

    [Export]
    public string Style { get; set; } = "striker";

    public List<string> EffectIds { get; set; } = new List<string>();

    public List<Effect> CachedEffects { get; set; } = new List<Effect>();

    public System.Action<Player> Effect { get; set; }

	public Card()
	{
		Effect = (player) => { };
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

    public void AddEffectId(string effectId)
    {
        if (!EffectIds.Contains(effectId))
        {
            EffectIds.Add(effectId);
        }
    }

	public void ResetCost()
	{
		Cost = BaseCost;
	}

	public void ReduceCost(int amount)
	{
		Cost = Mathf.Max(0, Cost - amount);
	}

	/// <summary>
	/// 造一份**独立副本**。
	///
	/// 为什么需要：<c>CharacterCardLoader</c> 返回的是**共享的模板实例**，
	/// 而战斗里会改卡牌状态（费用 -1、刻印回填 <c>IsEngraved</c>、升级 <c>Level</c>…）——
	/// 直接用模板会把这些改动漏到下一场战斗，甚至漏进刻印列表。
	/// </summary>
	public Card Clone()
	{
		return new Card
		{
			CardId = CardId,
			Name = Name,
			Description = Description,
			Style = Style,
			Level = Level,
			MaxLevel = MaxLevel,
			Cost = Cost,
			BaseCost = BaseCost,
			IsRetain = IsRetain,
			IsAttack = IsAttack,
			IsAreaAttack = IsAreaAttack,
			IsDefense = IsDefense,
			CardType = CardType,
			Affixes = new List<string>(Affixes),
			TemplateDescription = TemplateDescription,
			RageGain = RageGain,
			Target = Target,
			TargetPosition = TargetPosition,
			Damage = Damage,
			ShieldGain = ShieldGain,
			DamagePercent = DamagePercent,
			ShieldPercent = ShieldPercent,
			EnergyGain = EnergyGain,
			HealAmount = HealAmount,
			DrawCount = DrawCount,
			BlockDuration = BlockDuration,
			DamageBaseCoefficient = DamageBaseCoefficient,
			ShieldBaseCoefficient = ShieldBaseCoefficient,
			HealBaseCoefficient = HealBaseCoefficient,
			CounterBaseCoefficient = CounterBaseCoefficient,
			EnergyGainBase = EnergyGainBase,
			EnergyGainPerLevel = EnergyGainPerLevel,
			DrawCountBase = DrawCountBase,
			DrawCountPerLevel = DrawCountPerLevel,
			BuffValueBase = BuffValueBase,
			BuffValuePerLevel = BuffValuePerLevel,
			DebuffValueBase = DebuffValueBase,
			DebuffValuePerLevel = DebuffValuePerLevel,
			ApplyBuffName = ApplyBuffName,
			ApplyBuffDuration = ApplyBuffDuration,
			ApplyDebuffName = ApplyDebuffName,
			ApplyDebuffDuration = ApplyDebuffDuration,
			RageGainPerLevel = RageGainPerLevel,
			EffectIds = new List<string>(EffectIds)
		};
	}

	/// <summary>
	/// 按卡牌数据造出要挂到目标身上的**增益**。
	///
	/// ⚠️ 这里曾经是 <c>switch</c> 直接 <c>new StrengthBuff()</c> 之类**裸 new**：
	///   - 裸 buff 没有 <c>IconPath</c> → 状态生效了但**状态图标画不出来**
	///   - 没有名称 / 描述 → 图标下方的数值与提示也是空的
	///   - 时长直接写 <c>ApplyBuffDuration</c>，数据缺字段时是 0 → **挂上就过期**
	/// 现在统一走 <see cref="EffectRegistry"/>：图标 / 名称 / 描述 / 默认时长都来自配置表
	/// （<c>Data/buffs.json</c>），卡牌只负责「挂哪个 + 挂多久 + 数值多少」。
	/// </summary>
	public StatusEffect CreateBuffFromCard()
	{
		if (string.IsNullOrEmpty(ApplyBuffName)) return null;

		StatusEffect buff = EffectRegistry.CreateBuff(ApplyBuffName);
		if (buff == null)
		{
			GD.PrintErr($"[Card] CreateBuffFromCard 未知增益 id={ApplyBuffName}（卡牌 {CardId}）");
			return null;
		}

		// 时长：卡牌指定的优先；缺省回落配置表（0 会被当成「立刻过期」）
		if (ApplyBuffDuration > 0)
		{
			buff.Duration = ApplyBuffDuration;
		}

		// 数值：取卡牌数据里的 buffValue —— 卡面文案也是从同一个字段生成的，
		// 这样「卡面写 +N」与「实际生效 +N」永远是同一个数。
		ApplyBuffValue(buff, CalculatedBuffValue);

		buff.Initialize();
		return buff;
	}

	/// <summary>
	/// 按卡牌数据造出要挂到目标身上的**减益**（与 <see cref="CreateBuffFromCard"/> 同构）。
	/// </summary>
	public StatusEffect CreateDebuffFromCard()
	{
		if (string.IsNullOrEmpty(ApplyDebuffName)) return null;

		StatusEffect debuff = EffectRegistry.CreateDebuff(ApplyDebuffName);
		if (debuff == null)
		{
			GD.PrintErr($"[Card] CreateDebuffFromCard 未知减益 id={ApplyDebuffName}（卡牌 {CardId}）");
			return null;
		}

		if (ApplyDebuffDuration > 0)
		{
			debuff.Duration = ApplyDebuffDuration;
		}

		ApplyDebuffValue(debuff, CalculatedDebuffValue);

		debuff.Initialize();
		return debuff;
	}

	/// <summary>
	/// 把卡牌的增益数值写进效果对象。
	/// 不同增益的数值字段名字不一样（AttackBonus / DefenseBonus / HealPerTurn…），
	/// 只能按类型分派 —— 没有对应字段的增益会打警告而不是静默丢掉数值。
	/// </summary>
	private static void ApplyBuffValue(StatusEffect buff, int value)
	{
		if (buff == null || value <= 0) return;

		switch (buff)
		{
			case StrengthBuff sb: sb.AttackBonus = value; break;
			case DefenseBuff db: db.DefenseBonus = value; break;
			case RegenerationBuff rb: rb.HealPerTurn = value; break;
			case ThornsBuff tb: tb.DamageReflect = value; break;
			case FuryBuff fb: fb.AttackMultiplier = value; break;
			default:
				GD.PrintErr($"[Card] 增益「{buff.EffectName}」没有对应的数值字段，卡牌数值 {value} 未生效");
				break;
		}
	}

	/// <summary>
	/// 把卡牌的减益数值写进效果对象。
	/// ⚠️ 易伤 / 破甲是**百分比**口径（受到伤害 +N%），其余是固定值 ——
	/// 数据表里的 debuffValue 目前没有任何卡在用，口径等主人定。
	/// </summary>
	private static void ApplyDebuffValue(StatusEffect debuff, int value)
	{
		if (debuff == null || value <= 0) return;

		switch (debuff)
		{
			case WeakDebuff wd: wd.AttackReduction = value; break;
			case PoisonDebuff pd: pd.DamagePerTurn = value; break;
			case SlowDebuff sd: sd.EnergyReduction = value; break;
			case VulnerableDebuff vd: vd.DamageIncrease = value; break;
			case DefenseDownDebuff dd: dd.DamageIncreasePercent = value; break;
			default:
				GD.PrintErr($"[Card] 减益「{debuff.EffectName}」没有对应的数值字段，卡牌数值 {value} 未生效");
				break;
		}
	}
}
