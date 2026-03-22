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
}
