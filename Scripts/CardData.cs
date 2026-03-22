using Godot;

public partial class CardData : Resource
{
	[Export]
	public string CardId { get; set; } = "";

	[Export]
	public string Name { get; set; } = "";

	[Export]
	public string Description { get; set; } = "";

	[Export]
	public int Cost { get; set; } = 1;

	[Export]
	public int BaseCost { get; set; } = 1;

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
    public string SpritePath { get; set; } = "";

	[Export]
	public bool IsRetain { get; set; } = false;

	public Card CreateCard()
	{
		Card card = new Card
		{
			Name = Name,
			Description = Description,
			Cost = Cost,
			BaseCost = BaseCost,
			IsAttack = IsAttack,
			IsAreaAttack = IsAreaAttack,
			Target = Target,
			TargetPosition = TargetPosition,
			Damage = Damage,
			ShieldGain = ShieldGain,
			EnergyGain = EnergyGain,
			HealAmount = HealAmount,
			IsRetain = IsRetain
		};
		return card;
	}
}
