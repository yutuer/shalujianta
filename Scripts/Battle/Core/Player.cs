using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Scenes;

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

	/// <summary>
	/// **本场战斗的队伍角色**（4 个唤醒体）—— D66 死亡抵抗要用。
	///
	/// 为什么需要它：本项目的玩家是**整队合并成的一个 Player**
	/// （MaxHealth = 队伍生命总和），Player 本身没有「队伍成员」的概念，
	/// 而死亡抵抗是**队伍里所有角色的总和值**（主人 2026-11-07 口径）→ 必须把队伍挂上来。
	/// 由 <c>BattleManager.InitializePlayer()</c> 赋值。
	/// </summary>
	public CharacterDefinition[] TeamCharacters { get; set; }

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

	/// <summary>
	/// 抽「**算力消耗最低的指令卡**」（参考游戏：明灯灼魂「抽 2 张算力消耗最低的指令卡」）。
	///
	/// 与 <see cref="DrawCards"/> 的区别：**不是随机抽** ——
	/// 把牌堆里的指令卡按费用升序排序，取最便宜的那几张。
	/// 牌堆里没有指令卡就抽不到（返回实际抽到的张数）。
	/// </summary>
	/// <param name="count">想抽几张</param>
	/// <returns>实际抽到的张数</returns>
	public int DrawLowestCostInstructionCards(int count)
	{
		int drawn = 0;

		for (int i = 0; i < count; i++)
		{
			if (Deck.Count == 0)
			{
				ShuffleDiscardIntoDeck();
			}

			if (Deck.Count == 0) break;
			if (Hand.Count >= MaxHandSize) break;

			// 找牌堆里费用最低的指令卡
			int bestIndex = -1;
			int bestCost = int.MaxValue;

			for (int j = 0; j < Deck.Count; j++)
			{
				Card.Card candidate = Deck[j];
				if (candidate == null) continue;
				if (candidate.CardType != Card.CardType.Instruction) continue;
				if (candidate.Cost < bestCost)
				{
					bestCost = candidate.Cost;
					bestIndex = j;
				}
			}

			if (bestIndex < 0) break;   // 牌堆里没有指令卡了

			Card.Card picked = Deck[bestIndex];
			Deck.RemoveAt(bestIndex);
			Hand.Add(picked);
			drawn++;
		}

		return drawn;
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

		// 记录扣血前是否还活着 —— 死亡抵抗只在「本来活着、被这一击打死」时才判定
		bool wasAlive = CurrentHealth > 0;

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

		// ⚠️ **死亡抵抗触发点**（D66）——
		// 官方措辞是「**受到致命伤害时**」→ 判定必须挂在扣血的那一刻，
		// 而不是等敌人回合全部打完再判（BattleManager.CheckGameOver 那时已经晚了一步）。
		if (wasAlive && CurrentHealth <= 0)
		{
			TryTriggerDeathResistance();
		}
	}

	/// <summary>
	/// **死亡抵抗判定与结算**（D66）。
	///
	/// 官方口径（词条主表 + 参考图 tooltip）：
	/// 「在战斗中受到致命伤害时有概率保留 1 点生命。
	///   触发后下回合获得 2 点算力和抽 2 张牌，队伍的死亡抵抗减半，
	///   后续获得的死亡抵抗减半，持续到调查结束。」
	///
	/// 概率 = **队伍里所有角色的死亡抵抗总和**（主人 2026-11-07 口径）；
	/// 超过 100% 视为必触发（概率不可能大于 100%）。
	/// </summary>
	private void TryTriggerDeathResistance()
	{
		DeathResistanceSystem system = GlobalData.DeathResistance;
		if (system == null) return;

		float total = system.GetTeamTotal(TeamCharacters);
		if (total <= 0f)
		{
			GD.Print($"[DeathResistance] 队伍死亡抵抗为 {total:F1}%，未触发");
			return;
		}

		if (!DeathResistanceSystem.RollChance(total))
		{
			GD.Print($"[DeathResistance] 队伍死亡抵抗 {total:F1}%，本次未触发");
			return;
		}

		// 触发 → 保留 1 点生命
		CurrentHealth = 1;
		system.OnTrigger();

		GD.Print($"[DeathResistance] 队伍死亡抵抗 {total:F1}% 触发！保留 1 点生命");

		// 「触发后下回合获得 2 点算力和抽 2 张牌」
		AddStatusEffect(new DeathResistanceReviveEffect());
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
			existing.MergeStacks(effect);   // 同名效果叠加层数（中毒还会把数值也累加）
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
