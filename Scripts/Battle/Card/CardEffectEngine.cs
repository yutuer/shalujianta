using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Card
{
	/// <summary>
	/// 效果执行的**上下文** —— 引擎要操作的对象与回调。
	/// 由 <c>BattleManager</c> 在出牌时组装。
	/// </summary>
	public class CardEffectContext
	{
		/// <summary>玩家</summary>
		public Player Player { get; set; }

		/// <summary>存活敌人</summary>
		public List<Enemy> Enemies { get; set; } = new List<Enemy>();

		/// <summary>正在打出的卡</summary>
		public Card Card { get; set; }

		/// <summary>出牌角色的属性（算百分比用）</summary>
		public CharacterAttributes Attributes { get; set; }

		/// <summary>怒气发给谁（这张卡属于哪个角色）</summary>
		public CharacterPosition RageTarget { get; set; }

		/// <summary>单体目标解析（按卡的 target 字段）</summary>
		public Func<Card, Enemy> ResolveEnemyTarget { get; set; }

		/// <summary>战斗日志</summary>
		public Action<string, LogType> Log { get; set; }

		/// <summary>这张卡打出后是否「消耗」（不进弃牌堆）—— 由引擎按词缀设置</summary>
		public bool CardConsumed { get; set; }
	}

	/// <summary>
	/// **卡牌效果引擎** —— 数据驱动的效果执行。
	///
	/// 以前是「一个字段一个分支」（<c>card.DamagePercent</c> / <c>card.ApplyBuffName</c> / …），
	/// 结算代码里各写一段；加一种新效果就得改结算流程。
	///
	/// 现在是「**效果列表 + 执行器分派**」：
	/// 新增一种效果 = 加一个执行器 + 数据表加一行，**结算流程一行不改**。
	///
	/// 触发时机由 <see cref="CardEffect.Timing"/> 决定：
	/// <c>onPlay</c>（打出，默认）/ <c>onDiscard</c>（弃牌阶段后）。
	/// 灵知觉醒那句「打出时**和弃牌阶段后**获得 3 层罪印」就靠它表达。
	/// </summary>
	public static class CardEffectEngine
	{
		/// <summary>执行一批效果</summary>
		/// <param name="card">正在打出的卡</param>
		/// <param name="effects">效果列表</param>
		/// <param name="ctx">上下文</param>
		/// <param name="timing">触发时机</param>
		public static void Execute(Card card, IReadOnlyList<CardEffect> effects, CardEffectContext ctx, string timing = CardEffectTiming.OnPlay)
		{
			if (card == null || effects == null || ctx == null) return;

			for (int i = 0; i < effects.Count; i++)
			{
				CardEffect effect = effects[i];
				if (effect == null) continue;
				if (!effect.MatchesTiming(timing)) continue;

				ExecuteOne(card, effect, ctx);
			}
		}

		private static void ExecuteOne(Card card, CardEffect effect, CardEffectContext ctx)
		{
			switch (effect.Type)
			{
				case CardEffectType.Damage:
					ExecuteDamage(card, effect, ctx);
					break;
				case CardEffectType.Shield:
					ExecuteShield(card, effect, ctx);
					break;
				case CardEffectType.Rage:
					ExecuteRage(card, effect, ctx);
					break;
				case CardEffectType.Heal:
					ExecuteHeal(card, effect, ctx);
					break;
				case CardEffectType.Energy:
					ExecuteEnergy(card, effect, ctx);
					break;
				case CardEffectType.Draw:
					ExecuteDraw(card, effect, ctx);
					break;
				case CardEffectType.Buff:
					ExecuteBuff(card, effect, ctx);
					break;
				case CardEffectType.Debuff:
					ExecuteDebuff(card, effect, ctx);
					break;
				case CardEffectType.Retain:
					// 保留：回合结束时这张卡留在手牌（由 Player.DiscardHand 按 IsRetain 跳过）
					card.IsRetain = true;
					break;
				case CardEffectType.Consume:
					// 消耗：打出后从牌库移除（不进弃牌堆）——由 BattleManager 收尾时判断
					ctx.CardConsumed = true;
					break;
				default:
					GD.PrintErr($"[CardEffectEngine] 未知效果类型：{effect.Type}（卡 {card.CardId}）");
					break;
			}
		}

		/// <summary>伤害：百分比（攻击力×N%）或固定值；可多次命中；可打全体</summary>
		private static void ExecuteDamage(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int damage = CardEffectCalculator.CalculateDamage(card, effect, ctx.Attributes);
			if (damage <= 0) return;

			int hits = Math.Max(1, effect.HitCount);
			string hitText = hits > 1 ? $"（{hits}次）" : "";

			if (effect.Target == CardEffectTarget.AllEnemies)
			{
				if (ctx.Enemies == null) return;
				foreach (Enemy enemy in ctx.Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					for (int i = 0; i < hits; i++)
					{
						enemy.TakeDamage(damage);
					}
					Log(ctx, $"你使用了{card.Name}，对{enemy.EnemyName}造成{damage}点伤害{hitText}", LogType.Damage);
				}
				return;
			}

			Enemy target = ctx.ResolveEnemyTarget?.Invoke(card);
			if (target == null)
			{
				if (card.Target == TargetType.Position)
				{
					Log(ctx, $"你使用了{card.Name}，但{card.TargetPosition}号位置没有存活的敌人", LogType.System);
				}
				return;
			}

			for (int i = 0; i < hits; i++)
			{
				target.TakeDamage(damage);
			}

			string targetDescription = card.Target switch
			{
				TargetType.Front => $"最前的敌人{target.EnemyName}",
				TargetType.Rear => $"最后的敌人{target.EnemyName}",
				_ => target.EnemyName
			};

			Log(ctx, $"你使用了{card.Name}，对{targetDescription}造成{damage}点伤害{hitText}", LogType.Damage);
		}

		/// <summary>护盾：百分比（防御力×N%）或固定值</summary>
		private static void ExecuteShield(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int shield = CardEffectCalculator.CalculateShield(card, effect, ctx.Attributes);
			if (shield <= 0 || ctx.Player == null) return;

			ctx.Player.AddShield(shield);
			Log(ctx, $"你使用了{card.Name}，获得{shield}点护盾", LogType.Shield);
		}

		/// <summary>怒气：给这张卡所属的角色（<c>value + perLevel × (等级 − 1)</c>，照参考游戏口径）</summary>
		private static void ExecuteRage(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int rage = CardLevelSystem.CalculateFixedValue(card.Level, effect.Value, effect.PerLevel);
			if (rage <= 0) return;

			if (ctx.RageTarget != null)
			{
				ctx.RageTarget.AddRage(rage);
			}
			else
			{
				GD.PrintErr($"[CardEffectEngine] 怒气没有目标角色：{card.CardId}");
			}

			Log(ctx, $"获得{rage}点怒气", LogType.System);
		}

		/// <summary>治疗：体质 × 百分比 × 等级系数（没配百分比就用固定值）</summary>
		private static void ExecuteHeal(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int heal = CardEffectCalculator.CalculateHeal(card, effect, ctx.Attributes);
			if (heal <= 0 || ctx.Player == null) return;

			ctx.Player.Heal(heal);
			Log(ctx, $"你使用了{card.Name}，回复{heal}点生命", LogType.Heal);
		}

		/// <summary>算力（能量）</summary>
		private static void ExecuteEnergy(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (effect.Value <= 0 || ctx.Player == null) return;

			ctx.Player.CurrentEnergy += effect.Value;
			Log(ctx, $"你使用了{card.Name}，获得{effect.Value}点能量", LogType.Energy);
		}

		/// <summary>抽牌：可带筛选（抽「算力消耗最低的指令卡」）</summary>
		private static void ExecuteDraw(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int count = effect.Count > 0 ? effect.Count : effect.Value;
			if (count <= 0 || ctx.Player == null) return;

			if (effect.Filter == CardDrawFilter.LowestCostInstruction)
			{
				int drawn = ctx.Player.DrawLowestCostInstructionCards(count);
				Log(ctx, $"你使用了{card.Name}，抽了{drawn}张算力消耗最低的指令卡", LogType.System);
			}
			else
			{
				ctx.Player.DrawCards(count);
				Log(ctx, $"你使用了{card.Name}，抽了{count}张牌", LogType.System);
			}
		}

		/// <summary>增益（落在目标身上）</summary>
		private static void ExecuteBuff(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(effect.Buff)) return;

			StatusEffect buff = EffectRegistry.CreateBuff(effect.Buff);
			if (buff == null)
			{
				GD.PrintErr($"[CardEffectEngine] 未知增益 id：{effect.Buff}（卡 {card.CardId}）");
				return;
			}

			if (effect.Duration > 0) buff.Duration = effect.Duration;
			Card.ApplyBuffValue(buff, effect.Value);
			buff.Initialize();

			Enemy target = ctx.ResolveEnemyTarget?.Invoke(card);
			if (target == null) return;

			target.AddStatusEffect(buff);
			Log(ctx, $"你使用了{card.Name}，给{target.EnemyName}附加了增益效果", LogType.System);
		}

		/// <summary>减益（落在目标身上）：层数 = 攻击力×百分比 或 固定值；可打全体</summary>
		private static void ExecuteDebuff(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(effect.Debuff)) return;

			int stacks = CardEffectCalculator.CalculateDebuffValue(card, effect, ctx.Attributes);
			if (stacks <= 0) return;

			// 全体敌人（参考游戏：「对所有敌人施加 41 层中毒」）
			if (effect.Target == CardEffectTarget.AllEnemies)
			{
				if (ctx.Enemies == null) return;
				foreach (Enemy enemy in ctx.Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					ApplyDebuffTo(enemy, card, effect, stacks, ctx);
				}
				return;
			}

			Enemy target = ctx.ResolveEnemyTarget?.Invoke(card);
			if (target == null) return;
			ApplyDebuffTo(target, card, effect, stacks, ctx);
		}

		/// <summary>把减益挂到指定敌人身上</summary>
		private static void ApplyDebuffTo(Enemy target, Card card, CardEffect effect, int stacks, CardEffectContext ctx)
		{
			StatusEffect debuff = EffectRegistry.CreateDebuff(effect.Debuff);
			if (debuff == null)
			{
				GD.PrintErr($"[CardEffectEngine] 未知减益 id：{effect.Debuff}（卡 {card.CardId}）");
				return;
			}

			if (effect.Duration > 0) debuff.Duration = effect.Duration;
			Card.ApplyDebuffValue(debuff, stacks);
			debuff.Initialize();

			target.AddStatusEffect(debuff);
			Log(ctx, $"你使用了{card.Name}，给{target.EnemyName}附加了减益效果{stacks}层", LogType.System);
		}

		private static void Log(CardEffectContext ctx, string message, LogType type)
		{
			ctx.Log?.Invoke(message, type);
		}
	}

	/// <summary>
	/// 效果的**数值计算**（伤害 / 护盾）—— 卡面显示与战斗结算**共用**，
	/// 所以「卡面写 146、实际打 146」永远成立。
	/// </summary>
	public static class CardEffectCalculator
	{
		/// <summary>伤害 = 攻击力 × 百分比 × 等级系数（没配百分比就用固定值）</summary>
		public static int CalculateDamage(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			if (effect.Percent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Attack, effect.Percent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}

		/// <summary>护盾 = 防御力 × 百分比 × 等级系数（没配百分比就用固定值）</summary>
		public static int CalculateShield(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			if (effect.Percent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Defense, effect.Percent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}

		/// <summary>
		/// **治疗 = 体质 × 百分比 × 等级系数**（没配百分比就用固定值）。
		///
		/// 照参考游戏《终末前夜 Morimens》：「回复 体质×N%」（珈伦「凸月之舞」回复 体质×9%）。
		/// </summary>
		public static int CalculateHeal(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			if (effect.Percent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Constitution, effect.Percent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}

		/// <summary>
		/// **减益层数 = 攻击力 × 百分比 × 等级系数**（没配百分比就用固定值）。
		///
		/// 照参考游戏：「施加 41 层中毒」= 中毒 攻击力×30%（珈伦「蟾蜍炖菜」）——
		/// 中毒的**层数**是按攻击力算的，不是写死的数字。
		/// </summary>
		public static int CalculateDebuffValue(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			if (effect.Percent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Attack, effect.Percent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}
	}
}
