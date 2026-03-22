using Godot;
using System.Collections.Generic;

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

	[Export]
	public bool IsElite { get; set; } = false;

	[Export]
	public bool IsBoss { get; set; } = false;

	private IEnemyAI _ai;
	private AIAction _currentAction;

	public AIAction CurrentAction => _currentAction;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		InitializeAI();
	}

	private void InitializeAI()
	{
		_ai = Behavior.ToLower() switch
		{
			"aggressive" => new AggressiveAI(),
			"defensive" => new DefensiveAI(),
			"balanced" => new BalancedAI(),
			_ => new AggressiveAI()
		};
	}

	public int TakeDamage(int damage)
	{
		int actualDamage = CombatCalculator.CalculateDamage(damage, Defense);
		CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
		return actualDamage;
	}

	public int TakeDamageWithVariance(int baseDamage, float variancePercent = 0.1f)
	{
		int damage = CombatCalculator.CalculateDamageWithVariance(baseDamage, variancePercent);
		return TakeDamage(damage);
	}

	public int AttackPlayer(Player player)
	{
		int damage = CombatCalculator.CalculateDamageWithVariance(Attack);
		return player.TakeDamage(damage);
	}

	public void PerformAction(Player player, List<Enemy> allEnemies)
	{
		if (_ai == null)
		{
			InitializeAI();
		}

		_currentAction = _ai.ChooseAction(this, player, allEnemies);

		switch (_currentAction.Type)
		{
			case AIActionType.Attack:
				AttackPlayer(player);
				break;
			case AIActionType.Defend:
				AddDefense(_currentAction.Value);
				break;
			case AIActionType.Buff:
				AddAttack(_currentAction.Value);
				break;
			case AIActionType.Heal:
				Heal(_currentAction.Value);
				break;
		}
	}

	public void AddDefense(int amount)
	{
		Defense += amount;
	}

	public void AddAttack(int amount)
	{
		Attack += amount;
	}

	public void Heal(int amount)
	{
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
	}

	public float GetHealthPercent()
	{
		return (float)CurrentHealth / MaxHealth;
	}

	public bool IsLowHealth()
	{
		return GetHealthPercent() < 0.3f;
	}

	public bool IsCriticalHealth()
	{
		return GetHealthPercent() < 0.15f;
	}

	public AIAction GetQueuedAction(Player player, List<Enemy> allEnemies)
	{
		if (_ai == null)
		{
			InitializeAI();
		}
		return _ai.ChooseAction(this, player, allEnemies);
	}
}
