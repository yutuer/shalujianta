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

    [Export]
    public bool IsEngraved { get; set; } = false;

    [Export]
    public EngravingSystem.EngravingType EngravingType { get; set; } = EngravingSystem.EngravingType.Shield;

    public EngravingSystem.EngravingEffect EngravingEffect { get; set; }

    [Export]
    public bool IsDefense { get; set; } = false;

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
			Target = Target,
			TargetPosition = TargetPosition,
			Damage = Damage,
			ShieldGain = ShieldGain,
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
