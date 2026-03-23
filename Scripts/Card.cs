using Godot;

public enum TargetType
{
    None = 0,
    Front = 1,
    Rear = 2,
    Position = 3
}

public partial class Card : Resource
{
	[Export]
	public string CardId { get; set; } = "";

	[Export]
	public string Name { get; set; } = "Card";

	[Export]
	public string Description { get; set; } = "";

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
    public EngravingType EngravingType { get; set; } = EngravingType.Shield;

    public EngravingEffect EngravingEffect { get; set; }

    [Export]
    public bool IsDefense { get; set; } = false;

    public System.Action<Player> Effect { get; set; }

	public Card()
	{
		Effect = (player) => { };
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
