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

	public StatusEffect CreateBuffFromCard()
	{
		if (string.IsNullOrEmpty(ApplyBuffName)) return null;

		StatusEffect buff = ApplyBuffName.ToLower() switch
		{
			"strength" => new StrengthBuff(),
			"defense" => new DefenseBuff(),
			"regeneration" => new RegenerationBuff(),
			"thorns" => new ThornsBuff(),
			"fury" => new FuryBuff(),
			_ => null
		};

		if (buff != null)
		{
			buff.Duration = ApplyBuffDuration;
			buff.Initialize();
		}

		return buff;
	}

	public StatusEffect CreateDebuffFromCard()
	{
		if (string.IsNullOrEmpty(ApplyDebuffName)) return null;

		StatusEffect debuff = ApplyDebuffName.ToLower() switch
		{
			"weak" => new WeakDebuff(),
			"vulnerable" => new VulnerableDebuff(),
			"poison" => new PoisonDebuff(),
			"slow" => new SlowDebuff(),
			"silence" => new SilenceDebuff(),
			_ => null
		};

		if (debuff != null)
		{
			debuff.Duration = ApplyDebuffDuration;
			debuff.Initialize();
		}

		return debuff;
	}
}
