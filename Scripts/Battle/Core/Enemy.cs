using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Core;

public partial class Enemy : Node, IUnit
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
	private List<StatusEffect> _statusEffects = new List<StatusEffect>();

	public AIAction CurrentAction => _currentAction;
	public List<StatusEffect> StatusEffects => _statusEffects;

	public int Id { get; set; } = 0;

	public FactionType Faction => FactionType.Enemy;

	public int Shield { get; set; } = 0;

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

	public void TakeDamage(int damage)
	{
		int actualDamage = CombatCalculator.CalculateDamage(damage, Defense);
		CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
	}

	public void TakeDamageWithVariance(int baseDamage, float variancePercent = 0.1f)
	{
		int damage = CombatCalculator.CalculateDamageWithVariance(baseDamage, variancePercent);
		TakeDamage(damage);
	}

	public void AttackPlayer(Player player)
	{
		int damage = CombatCalculator.CalculateDamageWithVariance(Attack);
		player.TakeDamage(damage);
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

	public void AddShield(int amount)
	{
		Shield += amount;
	}

	public void AddStatusEffect(StatusEffect effect)
	{
		StatusEffect existing = _statusEffects.Find(e => e.EffectName == effect.EffectName);
		if (existing != null)
		{
			existing.RefreshDuration();
			return;
		}

		effect.Initialize();
		_statusEffects.Add(effect);
		effect.OnApplyEnemy(this);
	}

	public void RemoveStatusEffect(StatusEffect effect)
	{
		if (_statusEffects.Remove(effect))
		{
			effect.OnRemoveEnemy(this);
		}
	}

	public void ProcessTurnStartEffects()
	{
		foreach (StatusEffect effect in _statusEffects.ToArray())
		{
			effect.OnTurnStartEnemy(this);
		}
	}

	public void ProcessTurnEndEffects()
	{
		foreach (StatusEffect effect in _statusEffects.ToArray())
		{
			effect.OnTurnEndEnemy(this);
		}
		_statusEffects.RemoveAll(e => e.IsExpired());
	}

	public int CalculateModifiedDamage(int baseDamage, Enemy attacker)
	{
		int modifiedDamage = baseDamage;
		foreach (StatusEffect effect in _statusEffects)
		{
			modifiedDamage = effect.ModifyDamageEnemy(modifiedDamage, attacker, this);
		}
		return modifiedDamage;
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

	public bool IsDead => CurrentHealth <= 0;
}
