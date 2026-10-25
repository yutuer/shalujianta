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

		/// <summary>
		/// **本次结算累计造成的伤害** —— 出血按它算层数（出血 = 本次伤害的 N%）。
		/// 每次结算伤害前会先 <see cref="ResetLastDamage"/>。
		/// </summary>
		public int LastDamageTotal { get; private set; } = 0;

		/// <summary>本次结算打到的敌人（出血挂到他们身上）</summary>
		public List<Enemy> LastDamageTargets { get; private set; } = new List<Enemy>();

		/// <summary>清空「本次伤害」记录（每次结算伤害前调用）</summary>
		public void ResetLastDamage()
		{
			LastDamageTotal = 0;
			LastDamageTargets.Clear();
		}

		/// <summary>记一笔伤害（同时登记被打的敌人）</summary>
		public void AddLastDamage(Enemy target, int damage)
		{
			if (damage <= 0) return;
			LastDamageTotal += damage;
			if (target != null && !LastDamageTargets.Contains(target))
			{
				LastDamageTargets.Add(target);
			}
		}
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
				case CardEffectType.Awakening:
					ExecuteAwakening(card, effect, ctx);
					break;
				case CardEffectType.RetainStack:
					// 保留叠加：回合末结算（见 BattleManager.EndTurn）——
					// 打出时不用做什么，上限直接从效果列表里读。
					break;
				case CardEffectType.Stack:
					ExecuteStack(card, effect, ctx);
					break;
				case CardEffectType.Bleed:
					ExecuteBleed(card, effect, ctx);
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

			// ⚠️ **罪印**：波吕克斯身上的罪印层数 → 这次伤害附带 S% 出血
			//   （参考游戏「✕罪印」：「每层使波吕克斯造成的伤害附加 1% 出血。」）
			// ⚠️ **用的是「出牌前」的层数** —— 主人 2026-10-25 23:38 拍板：
			//   「先有罪印的 buff，才会有出血」→ 效果顺序必须是 **伤害 → 出血 → 罪印**。
			//   罪印由后面的 `stack` 效果加，所以这里读到的必然是本发之前的层数。
			int sinMarkPercent = ctx.RageTarget != null ? ctx.RageTarget.SinMarkStacks : 0;

			ctx.ResetLastDamage();

			if (effect.Target == CardEffectTarget.AllEnemies)
			{
				if (ctx.Enemies == null) return;
				foreach (Enemy enemy in ctx.Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					for (int i = 0; i < hits; i++)
					{
						enemy.TakeDamage(damage);
						ctx.AddLastDamage(enemy, damage);
						ApplySinMarkBleed(enemy, damage, sinMarkPercent, ctx);
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
				ctx.AddLastDamage(target, damage);
				ApplySinMarkBleed(target, damage, sinMarkPercent, ctx);
			}

			string targetDescription = card.Target switch
			{
				TargetType.Front => $"最前的敌人{target.EnemyName}",
				TargetType.Rear => $"最后的敌人{target.EnemyName}",
				_ => target.EnemyName
			};

			Log(ctx, $"你使用了{card.Name}，对{targetDescription}造成{damage}点伤害{hitText}", LogType.Damage);
		}

		/// <summary>
		/// **罪印带来的出血**：这次伤害附加「罪印层数 %」的出血。
		/// 层数 = <c>ceil(本次伤害 × 罪印层数 / 100)</c>（取整口径照 D51 全局 = ceil）。
		/// </summary>
		private static void ApplySinMarkBleed(Enemy target, int damage, int sinMarkPercent, CardEffectContext ctx)
		{
			if (target == null || sinMarkPercent <= 0 || damage <= 0) return;
			int stacks = Mathf.CeilToInt(damage * sinMarkPercent / 100f);
			if (stacks <= 0) return;
			GD.Print($"[SinMarkBleed] {target.EnemyName} 本次伤害 {damage} × 罪印 {sinMarkPercent}% → {stacks} 层出血");
			ApplyBleed(target, stacks, ctx);
		}

		/// <summary>
		/// **层数**（罪印）—— 加到**角色身上**（不是卡上）。
		///
		/// 照参考游戏《终末前夜 Morimens》「✕罪印」词条：「每层使波吕克斯造成的伤害附加 1% 出血。」
		/// ⚠️ 主人 2026-10-25 23:38 拍板：罪印是**角色身上的 buff**（「记在玩家身上了」），
		/// 所以卡洗回抽牌堆 / 被弃掉都不影响它。
		///
		/// 层数 = <c>count + 手牌数 × countPerHand</c>
		/// （净世之路：「获得 3 层罪印，每有 1 张手牌额外获得 1 层罪印」——
		/// 主人 2026-10-25 23:58 拍板：**按当时手牌数结算，不设上限**）。
		/// </summary>
		private static void ExecuteStack(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (ctx.RageTarget == null)
			{
				GD.PrintErr($"[CardEffectEngine] 层数效果没有归属角色：{card.CardId}");
				return;
			}

			int count = effect.Count;
			if (effect.CountPerHand > 0 && ctx.Player != null)
			{
				// ⚠️ 「当时手牌数」：打出时这张卡**还在手里**（效果结算完才移出手牌）
				count += ctx.Player.Hand.Count * effect.CountPerHand;
			}
			if (count <= 0) return;

			ctx.RageTarget.AddStack(effect.StackId, count);
			Log(ctx, $"获得{count}层{CardStackIds.DisplayName(effect.StackId)}（当前 {ctx.RageTarget.GetStack(effect.StackId)} 层）", LogType.System);
		}

		/// <summary>
		/// **出血**：这张卡造成的伤害附带 N% 的出血（挂到被打的敌人身上）。
		///
		/// 照参考游戏《终末前夜 Morimens》「出血」词条：
		/// 「回合结束时受到等量层数的**纯粹伤害**并移除该状态。」
		///
		/// ⚠️ **百分比来自灵知觉醒**（主人拍板）：
		/// 「100% 出血」是灵知觉醒「净世之路」授予的，**不是圣心自己的词缀** →
		/// 未觉醒的波吕克斯打出圣心**只有罪印的 1%，没有 100% 出血**。
		/// 所以 <c>requireAwakening</c> 为真时，百分比取觉醒授予的值（取不到就不施加）。
		/// </summary>
		private static void ExecuteBleed(Card card, CardEffect effect, CardEffectContext ctx)
		{
			int percent = ResolveBleedPercent(card, effect, ctx);
			if (percent <= 0) return;
			if (ctx.LastDamageTotal <= 0 || ctx.LastDamageTargets.Count == 0) return;

			int stacks = Mathf.CeilToInt(ctx.LastDamageTotal * percent / 100f);
			if (stacks <= 0) return;

			GD.Print($"[BleedEffect] {card.Name} 出血 {percent}%（本次伤害 {ctx.LastDamageTotal}）→ {stacks} 层");

			foreach (Enemy enemy in ctx.LastDamageTargets)
			{
				if (enemy == null || enemy.IsDead) continue;
				ApplyBleed(enemy, stacks, ctx);
			}
		}

		/// <summary>
		/// **出血百分比**：
		/// 需要觉醒的 → 取觉醒授予的百分比（觉醒没授予这张卡就是 0 = 不施加）；
		/// 不需要觉醒的 → 取效果自身的 <c>percentOfDamage</c>。
		/// </summary>
		private static int ResolveBleedPercent(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (!effect.RequireAwakening)
			{
				return effect.PercentOfDamage;
			}

			CharacterPosition owner = ctx.RageTarget;
			if (owner == null || !owner.AwakeningActive) return 0;
			if (string.IsNullOrEmpty(owner.AwakeningBleedCardId)) return 0;
			if (owner.AwakeningBleedCardId != card.CardId) return 0;
			return owner.AwakeningBleedPercent;
		}

		/// <summary>
		/// **把出血挂到敌人身上**（同名出血层数累加 —— 罪印的 1% 与圣心的 100% 会**相加**）。
		/// </summary>
		private static void ApplyBleed(Enemy target, int stacks, CardEffectContext ctx)
		{
			if (target == null || stacks <= 0) return;

			StatusEffect bleed = EffectRegistry.CreateDebuff("bleed");
			if (bleed == null)
			{
				GD.PrintErr("[CardEffectEngine] 未知减益 id：bleed（出血）");
				return;
			}

			// 出血：**层数即伤害**（回合末造成等量层数的纯粹伤害）
			Card.ApplyDebuffValue(bleed, stacks);
			bleed.Initialize();

			target.AddStatusEffect(bleed);

			int totalBleed = 0;
			foreach (StatusEffect s in target.StatusEffects)
			{
				if (s != null && s.EffectName == bleed.EffectName) totalBleed += s.StackCount;
			}
			GD.Print($"[BleedApply] {target.EnemyName} 身上出血合计 {totalBleed} 层（本次 +{stacks}）");

			Log(ctx, $"给{target.EnemyName}附加了{stacks}层出血", LogType.System);
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

		/// <summary>
		/// **灵知觉醒**：打出后使角色在本场战斗中获得特殊能力。
		///
		/// 照参考游戏《终末前夜 Morimens》—— 珈伦「凸月之舞」：
		/// 「珈伦获得 25 点狂气。⚠灵知觉醒：**珈伦打出卡牌后**，回复 体质×9% 点生命，获得 5 点狂气。
		/// 打出「奇妙料理」后，对所有敌人施加 攻击力×15% 层中毒。」
		///
		/// ⚠️ 关键：**是「每次打出卡牌后」触发，不是「打出这张卡时」**。
		/// 所以这里只把**数值算好存起来**（按这张觉醒卡的等级），
		/// 真正的触发挂在 BattleManager.OnCardPlayed 上。
		/// </summary>
		private static void ExecuteAwakening(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (ctx.RageTarget == null)
			{
				GD.PrintErr($"[CardEffectEngine] 灵知觉醒没有归属角色：{card.CardId}");
				return;
			}

			CharacterAttributes attributes = ctx.Attributes;

			// 回复量取「时机 = onAwakening」的那个治疗效果
			// （珈伦「凸月之舞」：回复 体质×9% 点生命 —— **每次打出卡牌后**才回，不是打出这张卡时）
			int heal = 0;
			foreach (CardEffect e in card.Effects)
			{
				if (e.Type == CardEffectType.Heal && e.MatchesTiming(CardEffectTiming.OnAwakening))
				{
					heal = CardEffectCalculator.CalculateHeal(card, e, attributes);
					break;
				}
			}

			int debuffStacks = effect.AwakeningDebuffPercent > 0 && attributes != null
				? CardLevelSystem.CalculateAttributeLinkedValue(card.Level, attributes.Attack, effect.AwakeningDebuffPercent / 100f)
				: 0;

			// ⚠️ **「弃牌阶段后」那一次要登记到觉醒状态里**（净世之路）：
			// 「⚠灵知觉醒：打出时和**弃牌阶段后**获得 3 层 ✕罪印，每有 1 张手牌额外获得 1 层 ✕罪印。」
			// → 「打出时」那一次由效果列表自己结算（这条 stack 效果的 timing 含 onPlay）；
			//   「弃牌阶段后」每回合一次，由觉醒状态登记、回合末执行。
			string discardStackId = "";
			int discardStackCount = 0;
			int discardStackPerHand = 0;
			foreach (CardEffect e in card.Effects)
			{
				if (e == null || e.Type != CardEffectType.Stack) continue;
				if (!e.MatchesTiming(CardEffectTiming.OnDiscard)) continue;
				discardStackId = e.StackId;
				discardStackCount = e.Count;
				discardStackPerHand = e.CountPerHand;
				break;
			}

			bool activated = ctx.RageTarget.ActivateAwakening(
				heal,
				effect.AwakeningRageGain,
				effect.AwakeningDebuff,
				debuffStacks,
				effect.AwakeningTriggerCardId,
				card.CardId,
				effect.AwakeningBleedCardId,
				effect.AwakeningBleedPercent);

			// 登记「弃牌阶段后」的层数效果（罪印）
			// ⚠️ 重复打出会走到「已登记过」的分支 → 只更新数值，不新增条目（不会翻倍结算）
			ctx.RageTarget.RegisterAwakeningDiscardStack(discardStackId, discardStackCount, discardStackPerHand);

			if (activated)
			{
				Log(ctx, heal > 0 || effect.AwakeningRageGain > 0
					? $"灵知觉醒生效：之后每打出一张卡牌，回复{heal}点生命并获得{effect.AwakeningRageGain}点狂气"
					: "灵知觉醒生效", LogType.System);
			}
			else
			{
				Log(ctx, "灵知觉醒已生效，重复打出不叠加（保留原效果）", LogType.System);
			}
		}

		/// <summary>
		/// **对全体敌人施加减益**（供灵知觉醒这类「角色级」触发复用）。
		///
	/// 珈伦「凸月之舞」：「打出「奇妙料理」后，对所有敌人施加 攻击力×15% 层中毒。」
		/// 减益层数在激活觉醒时就按攻击力算好了，这里只负责挂上去。
		/// </summary>
		public static void ApplyDebuffToAllEnemies(Card sourceCard, string debuffId, int stacks, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(debuffId) || stacks <= 0 || ctx == null) return;
			if (ctx.Enemies == null) return;

			CardEffect effect = new CardEffect
			{
				Type = CardEffectType.Debuff,
				Debuff = debuffId,
				Target = CardEffectTarget.AllEnemies
			};

			foreach (Enemy enemy in ctx.Enemies)
			{
				if (enemy == null || enemy.IsDead) continue;
				ApplyDebuffTo(enemy, sourceCard, effect, stacks, ctx);
			}
		}

		private static void Log(CardEffectContext ctx, string message, LogType type)
		{
			// ⚠️ 同时输出到 stdout —— 战斗日志在 UI 里，脚本 / 跑测取证看不到
			GD.Print($"[BattleLog] {message}");
			ctx.Log?.Invoke(message, type);
		}
	}

	/// <summary>
	/// 效果的**数值计算**（伤害 / 护盾）—— 卡面显示与战斗结算**共用**，
	/// 所以「卡面写 146、实际打 146」永远成立。
	/// </summary>
	public static class CardEffectCalculator
	{
		/// <summary>
		/// 伤害 = 攻击力 × 百分比 × 等级系数（没配百分比就用固定值）。
		///
		/// ⚠️ 效果上打开 <c>ignoreLevelScale</c> 时**不乘等级系数**——
		/// 波吕克斯「圣心」= 攻击力×70%，参考游戏六个等级写的都是 70%（恒定）。
		/// </summary>
		public static int CalculateDamage(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			if (effect.Percent > 0 && attributes != null)
			{
				if (effect.IgnoreLevelScale)
				{
					// 恒定系数：攻击力 × N%（向上取整，照参考游戏口径）
					return Mathf.CeilToInt(attributes.Attack * effect.Percent / 100f);
				}
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Attack, effect.Percent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}

		/// <summary>
		/// 护盾 = 防御力 × 百分比 × 等级系数（没配百分比就用固定值）。
		///
		/// ⚠️ 卡上有**保留叠加**时（珈伦「静默守望」）再乘 2^层数 ——
		/// 参考游戏：「回合结束时若在手中，算力消耗提高 1，回复生命和护盾翻倍。最多叠加 2 次。」
		/// </summary>
		public static int CalculateShield(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			int value;
			if (effect.Percent > 0 && attributes != null)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Defense, effect.Percent / 100f);
			}
			else if (effect.Value > 0)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			else
			{
				return 0;
			}

			return value * GetRetainStackMultiplier(card);
		}

		/// <summary>
		/// **保留叠加的翻倍倍率** = 2^层数。
		/// 叠 1 层 → ×2、叠 2 层 → ×4（参考游戏「回复生命和护盾翻倍。最多叠加 2 次」）。
		/// </summary>
		public static int GetRetainStackMultiplier(Card card)
		{
			if (card == null || card.RetainStackCount <= 0) return 1;
			int multiplier = 1;
			for (int i = 0; i < card.RetainStackCount; i++) multiplier *= 2;
			return multiplier;
		}

		/// <summary>
		/// **治疗 = 体质 × 百分比 × 等级系数**（没配百分比就用固定值）。
		///
		/// 照参考游戏《终末前夜 Morimens》：「回复 体质×N%」（珈伦「凸月之舞」回复 体质×9%）。
		///
		/// ⚠️ 卡上有**保留叠加**时（珈伦「静默守望」）再乘 2^层数。
		/// </summary>
		public static int CalculateHeal(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			int value;
			if (effect.Percent > 0 && attributes != null)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Constitution, effect.Percent / 100f);
			}
			else if (effect.Value > 0)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			else
			{
				return 0;
			}

			return value * GetRetainStackMultiplier(card);
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
