using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle.Core;

public partial class Player : Node, IUnit
{
	[Export]
	public int MaxHealth { get; set; } = 100;

	public int CurrentHealth { get; set; }

	[Export]
	public int Attack { get; set; } = 10;

	[Export]
	public int Shield { get; set; } = 0;

	[Export]
	public int Defense { get; set; } = 0;

	[Export]
	public int MaxEnergy { get; set; } = 3;

	public int CurrentEnergy { get; set; }

	[Export]
	public int DrawCount { get; set; } = 5;

	[Export]
	public int MaxHandSize { get; set; } = 10;

	public List<Card.Card> Hand { get; set; } = new List<Card.Card>();

	public List<Card.Card> Deck { get; set; } = new List<Card.Card>();

	public List<Card.Card> DiscardPile { get; set; } = new List<Card.Card>();

	public string Class { get; set; } = "Default";

	private List<StatusEffect> _statusEffects = new List<StatusEffect>();
	public List<StatusEffect> StatusEffects => _statusEffects;

	public int Id { get; set; } = 0;

	public FactionType Faction => FactionType.Player;

	public string CharacterName { get; set; } = "Player";

	public CharacterDefinition CharacterDef { get; set; }

	public CharacterAttributes Attributes { get; set; }

	public override void _Ready()
	{
		if (CharacterDef != null)
		{
			Attributes = CharacterDef.GetAttributes();
			MaxHealth = Attributes.MaxHealth;
			Attack = Attributes.Attack;
			Defense = Attributes.Defense;
		}
		CurrentHealth = MaxHealth;
		CurrentEnergy = MaxEnergy;
	}

	public void Initialize(CharacterDefinition charDef)
	{
		CharacterDef = charDef;
		CharacterName = charDef.Name;
		Attributes = charDef.GetAttributes();
		MaxHealth = Attributes.MaxHealth;
		Attack = Attributes.Attack;
		Defense = Attributes.Defense;
		MaxEnergy = charDef.BaseEnergy;
		DrawCount = charDef.BaseDrawCount;
		CurrentHealth = MaxHealth;
		CurrentEnergy = MaxEnergy;
	}

	public void StartTurn()
	{
		CurrentEnergy = MaxEnergy;
		ProcessTurnStartEffects();
		DrawCards(DrawCount);
	}

	public void DrawCards(int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (Deck.Count == 0)
			{
				ShuffleDiscardIntoDeck();
			}

			if (Deck.Count > 0 && Hand.Count < MaxHandSize)
			{
				Card.Card card = Deck[0];
				Deck.RemoveAt(0);
				Hand.Add(card);
			}
		}
	}

	public void ShuffleDiscardIntoDeck()
	{
		foreach (Card.Card card in DiscardPile)
		{
			Deck.Add(card);
		}
		DiscardPile.Clear();
		ShuffleDeck();
	}

	public void ShuffleDeck()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int i = Deck.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			Card.Card temp = Deck[i];
			Deck[i] = Deck[j];
			Deck[j] = temp;
		}
	}

	public void DiscardHand()
	{
		List<Card.Card> cardsToDiscard = new List<Card.Card>();
		foreach (Card.Card card in Hand)
		{
			if (!card.IsRetain)
			{
				cardsToDiscard.Add(card);
			}
		}

		foreach (Card.Card card in cardsToDiscard)
		{
			Hand.Remove(card);
			DiscardPile.Add(card);
		}
	}

	public void PlayCard(Card.Card card)
	{
		if (CurrentEnergy >= card.Cost)
		{
			CurrentEnergy -= card.Cost;
			if (Attributes != null)
			{
				int silverKeyGain = Attributes.CalculateSilverKeyFromPower(card.Cost);
			}
			card.Effect(this);
			Hand.Remove(card);
			DiscardPile.Add(card);
		}
	}

	public void TakeDamage(int damage)
	{
		int remainingDamage = damage;

		if (Shield > 0)
		{
			if (Shield >= remainingDamage)
			{
				Shield -= remainingDamage;
				remainingDamage = 0;
			}
			else
			{
				remainingDamage -= Shield;
				Shield = 0;
			}
		}

		CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
	}

	public void TakeModifiedDamage(int damage, Enemy attacker)
	{
		int modifiedDamage = CalculateModifiedDamage(damage, attacker);
		TakeDamage(modifiedDamage);
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
		effect.OnApplyPlayer(this);
	}

	public void RemoveStatusEffect(StatusEffect effect)
	{
		if (_statusEffects.Remove(effect))
		{
			effect.OnRemovePlayer(this);
		}
	}

	public void ProcessTurnStartEffects()
	{
		foreach (StatusEffect effect in _statusEffects.ToArray())
		{
			effect.OnTurnStartPlayer(this);
		}
	}

	public void ProcessTurnEndEffects()
	{
		foreach (StatusEffect effect in _statusEffects.ToArray())
		{
			effect.OnTurnEndPlayer(this);
		}
		_statusEffects.RemoveAll(e => e.IsExpired());
	}

	public int CalculateModifiedDamage(int baseDamage, Enemy attacker)
	{
		int modifiedDamage = baseDamage;
		foreach (StatusEffect effect in _statusEffects)
		{
			modifiedDamage = effect.ModifyDamagePlayer(modifiedDamage, attacker, this);
		}
		return modifiedDamage;
	}

	public float GetHealthPercent()
	{
		return (float)CurrentHealth / MaxHealth;
	}

	public bool IsDead => CurrentHealth <= 0;

	public void ForceRevive(int health)
	{
		CurrentHealth = Mathf.Max(1, health);
	}

	public void UseRage(int amount)
	{
		if (Attributes != null && Attributes.CanUseRage(amount))
		{
			int returnAmount = Attributes.UseRage(amount);
			if (returnAmount > 0)
			{
				GD.Print($"[RageReturn] 怒气回冲：获得 {returnAmount} 点怒气");
			}
		}
	}

	public void AddRage(int amount)
	{
		if (Attributes != null)
		{
			Attributes.AddRage(amount);
		}
	}
}
