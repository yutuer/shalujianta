using Godot;

public partial class Enemy : Node
{
	[Export]
	public string EnemyName { get; set; } = "Enemy";

	[Export]
	public int MaxHealth { get; set; } = 50;

	public int CurrentHealth { get; set; }

	[Export]
	public int Attack { get; set; } = 10;

	[Export]
	public int Defense { get; set; } = 0;

	[Export]
	public string Behavior { get; set; } = "Default";

	[Export]
	public int Position { get; set; } = 0;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}
	
	public void TakeDamage(int damage)
	{
		int actualDamage = Mathf.Max(0, damage - Defense);
		CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
	}
	
	public void AttackPlayer(Player player)
	{
		player.TakeDamage(Attack);
	}
	
	public void AddDefense(int amount)
	{
		Defense += amount;
	}
	
	public void Heal(int amount)
	{
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
	}
}
