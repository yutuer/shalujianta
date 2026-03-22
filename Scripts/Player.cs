using Godot;
using System.Collections.Generic;

public partial class Player : Node
{
	[Export]
	public int MaxHealth { get; set; } = 100;

	public int CurrentHealth { get; set; }

	[Export]
	public int Attack { get; set; } = 10;

	[Export]
	public int Shield { get; set; } = 0;

	[Export]
	public int MaxEnergy { get; set; } = 3;

	public int CurrentEnergy { get; set; }

	[Export]
	public int DrawCount { get; set; } = 5;

	[Export]
	public int MaxHandSize { get; set; } = 10;

	public List<Card> Hand { get; set; } = new List<Card>();

	public List<Card> Deck { get; set; } = new List<Card>();

	public List<Card> DiscardPile { get; set; } = new List<Card>();

	public string Class { get; set; } = "Default";

	private List<StatusEffect> _statusEffects = new List<StatusEffect>();
	public List<StatusEffect> StatusEffects => _statusEffects;

	public override void _Ready()
	{
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
				Card card = Deck[0];
				Deck.RemoveAt(0);
				Hand.Add(card);
			}
		}
	}

	public void ShuffleDiscardIntoDeck()
	{
		foreach (Card card in DiscardPile)
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
			Card temp = Deck[i];
			Deck[i] = Deck[j];
			Deck[j] = temp;
		}
	}

	public void DiscardHand()
	{
		List<Card> cardsToDiscard = new List<Card>();
		foreach (Card card in Hand)
		{
			if (!card.IsRetain)
			{
				cardsToDiscard.Add(card);
			}
		}

		foreach (Card card in cardsToDiscard)
		{
			Hand.Remove(card);
			DiscardPile.Add(card);
		}
	}

	public void PlayCard(Card card)
	{
		if (CurrentEnergy >= card.Cost)
		{
			CurrentEnergy -= card.Cost;
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

	public bool IsDead()
	{
		return CurrentHealth <= 0;
	}

	public float GetHealthPercent()
	{
		return (float)CurrentHealth / MaxHealth;
	}
}
