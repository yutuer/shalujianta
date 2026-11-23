using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Scenes;

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

			// ⚠️ **D66 菲茵特「幼芽萌生」**：「若在本场战斗中触发过死亡抵抗，额外生效 1 次。」
			//   → 先扫一遍效果列表里有没有 `repeatOnDeathResistance` 标记，
			//     有的话再看「本场战斗是否触发过死亡抵抗」→ 决定整串效果跑 1 遍还是 2 遍。
			int repeat = GetRepeatCount(effects);

			for (int r = 0; r < repeat; r++)
			{
				for (int i = 0; i < effects.Count; i++)
				{
					CardEffect effect = effects[i];
					if (effect == null) continue;
					if (!effect.MatchesTiming(timing)) continue;

					// 重复标记本身不产生效果（它只是「跑几遍」的开关）
					if (effect.Type == CardEffectType.RepeatOnDeathResistance) continue;

					ExecuteOne(card, effect, ctx);
				}
			}
		}

		/// <summary>
		/// 算这串效果要执行几遍（菲茵特「幼芽萌生」的「额外生效 1 次」）。
		/// </summary>
		private static int GetRepeatCount(IReadOnlyList<CardEffect> effects)
		{
			foreach (CardEffect effect in effects)
			{
				if (effect == null) continue;
				if (effect.Type != CardEffectType.RepeatOnDeathResistance) continue;

				int wanted = effect.Count > 1 ? effect.Count : 2;

				DeathResistanceSystem dr = GlobalData.DeathResistance;
				if (dr == null || !dr.TriggeredThisBattle)
				{
					return 1;
				}

				GD.Print($"[CardEffectEngine] 本场战斗触发过死亡抵抗 → 该卡额外生效 {wanted - 1} 次（共 {wanted} 遍）");
				return wanted;
			}

			return 1;
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
				case CardEffectType.DeathResistanceDecay:
					ExecuteDeathResistanceDecay(card, effect, ctx);
					break;
				case CardEffectType.RepeatOnDeathResistance:
					// 重复标记由外层 GetRepeatCount 处理，这里不做事
					break;
				case CardEffectType.Counter:
					ExecuteCounter(card, effect, ctx);
					break;
				case CardEffectType.TriggerCounter:
					ExecuteTriggerCounter(card, effect, ctx);
					break;
				case CardEffectType.DisperseSelfDebuff:
					ExecuteDisperseSelfDebuff(card, effect, ctx);
					break;
			case CardEffectType.TriggerPoison:
					ExecuteTriggerPoison(card, effect, ctx);
					break;
				case CardEffectType.ShuffleIntoDeck:
					ExecuteShuffleIntoDeck(card, effect, ctx);
					break;
				case CardEffectType.AddCardsToHand:
					ExecuteAddCardsToHand(card, effect, ctx);
					break;
				case CardEffectType.ShuffleHandToDeck:
					ExecuteShuffleHandToDeck(card, effect, ctx);
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
						ApplyPoisonEqualDamage(enemy, damage, effect, ctx);
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
				// ⚠️ **随机目标**（D73：詹金「鼠群冲击」：「对随机敌人造成…伤害 X+4 次」）
				//   → **每一次命中都重新抽一个存活敌人**（不是同一个人挨 5 下）。
				Enemy hitTarget = card.Target == TargetType.Random
					? ctx.ResolveEnemyTarget?.Invoke(card)
					: target;
				if (hitTarget == null) continue;
				
				hitTarget.TakeDamage(damage);
				ctx.AddLastDamage(hitTarget, damage);
				ApplySinMarkBleed(hitTarget, damage, sinMarkPercent, ctx);
				ApplyPoisonEqualDamage(hitTarget, damage, effect, ctx);
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
				/// <summary>
		/// **伤害等量中毒**（D74：宁菲亚「夜潮汹涌」）。
		///
		/// wiki 原文：「造成 [攻击力*22.5%] 点伤害并**施加等量中毒**。」
		/// → 这次伤害打多少，敌人身上就叠多少层中毒（中毒的层数即伤害值）。
		/// </summary>
		private static void ApplyPoisonEqualDamage(Enemy target, int damage, CardEffect effect, CardEffectContext ctx)
		{
			if (!effect.PoisonEqualDamage) return;
			if (target == null || target.IsDead) return;
			if (damage <= 0) return;
			
			StatusEffect poison = EffectRegistry.CreateDebuff("poison");
			if (poison == null)
			{
				GD.PrintErr("[CardEffectEngine] 未知减益 id：poison（中毒）");
				return;
			}
			
			// 中毒的层数存 DamagePerTurn（层数即伤害值），走 Card.ApplyDebuffValue
			Card.ApplyDebuffValue(poison, damage);
			poison.Initialize();
			target.AddStatusEffect(poison);
			
			GD.Print($"[PoisonEqual] {target.EnemyName} 本次伤害 {damage} → 叠加 {damage} 层中毒");
			Log(ctx, $"给{target.EnemyName}附加了{damage}层中毒", LogType.System);
		}
		
		/// <summary>
		/// **置入多张指定卡到手牌**（D74：庞托斯「驭魇哨音」：「生成 3 种不同的「囚魇」置入手中」）。
		///
		/// 注意：置入的是独立实例（从模板克隆），不共享同一个 Card 对象。
		/// 注意：**继承来源卡的等级**（D74）：6 级的「驭魇哨音」生成的囚魇应该按 6 级算数值。
		/// </summary>
		private static void ExecuteAddCardsToHand(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(effect.AddCardsToHandIds) || ctx.Player == null) return;
			
			string[] ids = effect.AddCardsToHandIds.Split(',');
			int added = 0;
			foreach (string rawId in ids)
			{
				string cardId = rawId.Trim();
				if (string.IsNullOrEmpty(cardId)) continue;
			
				if (!CharacterCardLoader.TryGetCharacterCard(cardId, out Card template))
				{
					GD.PrintErr($"[CardEffectEngine] 卡 {card.CardId}：要置入手牌的卡「{cardId}」不在 character_cards.json 里");
					continue;
				}
			
				// 手牌上限（与 Player.DrawCards 一致）
				if (ctx.Player.Hand.Count >= ctx.Player.MaxHandSize)
				{
					GD.Print($"[CardEffectEngine] 卡 {card.CardId}：手牌已满（{ctx.Player.Hand.Count}/{ctx.Player.MaxHandSize}），未能置入「{template.Name}」");
					break;
				}
			
				Card instance = template.Clone();
					instance.Level = Mathf.Clamp(card.Level, 1, instance.MaxLevel);
					if (ctx.RageTarget?.Character != null)
					{
					instance.OwnerCharacterId = ctx.RageTarget.Character.CharacterId;
						instance.LinkedAttributes = ctx.RageTarget.Character.GetAttributes();
					}
				ctx.Player.Hand.Add(instance);
				added++;
			}
			
			if (added > 0)
			{
				Log(ctx, $"你使用了{card.Name}，将{added}张衍生卡置入手中", LogType.System);
			}
		}
		
		/// <summary>
		/// **把手牌洗回抽牌堆**（D74：卡茜亚「统统消失！」：「将其他手牌洗入抽牌堆」）。
		///
		/// 注意：wiki 那句还有「弃掉手牌中的症状和状态卡」—— 本项目没有「症状卡 / 状态卡」分类，
		/// 这一部分登记在数据表的 pendingMechanics 里。
		/// </summary>
		private static void ExecuteShuffleHandToDeck(Card card, CardEffect effect, CardEffectContext ctx)
		{
			// ⚠️ 这里**不要再判 effect.ShuffleHandToDeck** —— 能走到这个 case 就说明
			//    effect.Type 已经是 "shuffleHandToDeck"（引擎按 type 分派），再判一次是自相矛盾。
			if (ctx.Player == null) return;
			
			int shuffled = ctx.Player.ShuffleHandToDeck();
			Log(ctx, $"你使用了{card.Name}，将手牌 {shuffled} 张洗入抽牌堆", LogType.System);
			
			if (effect.DrawAfterShuffle && shuffled > 0)
			{
				ctx.Player.DrawCards(shuffled);
					Log(ctx, $"你使用了{card.Name}，抽了{shuffled}张牌", LogType.System);
			}
		}
		
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
		/// **本场战斗死亡抵抗触发衰减率**（菲茵特「无边星彩」灵知觉醒）。
		///
		/// wiki 原文：「本场战斗{{词条|死亡抵抗}}触发衰减率从 50% 降低为 45%。」
		/// 数据表：<c>{ "type": "deathResistanceDecay", "value": 45 }</c>（value = 百分比 45）。
		/// </summary>
		private static void ExecuteDeathResistanceDecay(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (effect.Value <= 0) return;

			DeathResistanceSystem dr = GlobalData.DeathResistance;
			if (dr == null)
			{
				GD.PrintErr($"[CardEffectEngine] 死亡抵抗系统不可用，{card.Name} 的衰减率调整已跳过");
				return;
			}

			float rate = effect.Value / 100f;
			if (rate > 1f) rate = 1f;

			dr.SetDecayRate(rate);
		}

		/// <summary>
		/// **获得反击层数**（D68，诺缔拉「打击」/「高热区禁入」/ 菲茵特「无边星彩」）。
		///
		/// wiki 原文：「获得 <font color=#b75a64>[攻击力*30%]</font> 层{{词条|反击}}。」
		/// → 层数 = **攻击力 × 百分比 × 等级系数**（跟伤害/护盾百分比同一套缩放）。
		///
		/// ⚠️ 存在**角色身上**（不是卡上）→ 洗回抽牌堆 / 被弃掉都不影响。
		/// </summary>
		private static void ExecuteCounter(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (ctx.RageTarget == null)
			{
				GD.PrintErr($"[CardEffectEngine] 反击效果没有归属角色：{card.CardId}");
				return;
			}

			if (effect.Percent <= 0) return;

			// ⚠️ 公式**只写一处**（<c>CardEffectCalculator.CalculateCounter</c>）：
			//    卡面显示与战斗结算共用同一套 → 不会再现「卡面写 X、战斗给 Y」（踩坑 §124）。
			float stacks = CardEffectCalculator.CalculateCounter(card, effect, ctx.Attributes);
			if (stacks <= 0f) return;

			// ⚠ 用**实际获得**的层数写日志（已含「施加的反击提高 50%」倍率），别用未乘倍率的原始值
			float gained = ctx.RageTarget.AddCounter(stacks, effect.Temporary);
			Log(ctx, $"获得{gained:F1}层反击", LogType.System);
		}

		/// <summary>
		/// **触发反击**（D68，诺缔拉「整装待发」：「对所有敌人触发 100% {{词条|反击}}」）。
		///
		/// → 对每个目标造成「队伍当前总反击 × 百分比」的**纯粹伤害**。
		/// </summary>
		private static void ExecuteTriggerCounter(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (ctx.Player == null) return;
			if (effect.Percent <= 0) return;

			float teamCounter = ctx.Player.GetTeamCounter();
			if (teamCounter <= 0f)
			{
				GD.Print($"[CardEffectEngine] {card.Name} 触发反击，但队伍当前没有反击层数");
				return;
			}

			float damage = teamCounter * effect.Percent / 100f;
			int counterDamage = Mathf.CeilToInt(damage);
			if (counterDamage <= 0) return;

			List<Enemy> targets = new List<Enemy>();
			if (effect.Target == CardEffectTarget.AllEnemies)
			{
				foreach (Enemy enemy in ctx.Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					targets.Add(enemy);
				}
			}
			else
			{
				Enemy single = ctx.ResolveEnemyTarget?.Invoke(card);
				if (single != null && !single.IsDead) targets.Add(single);
			}

			if (targets.Count == 0) return;

			GD.Print($"[CardEffectEngine] {card.Name} 触发反击：队伍总反击 {teamCounter:F1} 层 × {effect.Percent}% → {counterDamage} 点纯粹伤害（{targets.Count} 个目标）");

			foreach (Enemy target in targets)
			{
				target.TakePureDamage(counterDamage);
			}

			Log(ctx, $"对所有敌人触发{counterDamage}点反击伤害", LogType.Damage);
		}

		/// <summary>
		/// **驱散自身减益**（D71，图鲁「螺湮重临」：「驱散自身{{词条|虚弱}}状态。」）。
		///
		/// → 移除**玩家自己**身上同名（<c>EffectName</c>）的减益。
		/// ⚠️ 数据表里写的是**减益 id**（weak / poison / …），
		///    而状态对象里存的是**状态名**（Weak / Poison）→ 这里做不区分大小写的比对。
		/// </summary>
		private static void ExecuteDisperseSelfDebuff(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(effect.Debuff)) return;
			if (ctx.Player == null) return;

			string wanted = effect.Debuff.Trim();
			int removed = 0;

			foreach (StatusEffect status in ctx.Player.StatusEffects.ToArray())
			{
				if (status == null) continue;
				if (!string.Equals(status.EffectName, wanted, System.StringComparison.OrdinalIgnoreCase)) continue;
				ctx.Player.RemoveStatusEffect(status);
				removed++;
			}

			if (removed <= 0)
			{
				GD.Print($"[CardEffectEngine] {card.Name} 驱散自身 {wanted}：自身没有该状态");
				return;
			}

			GD.Print($"[CardEffectEngine] {card.Name} 驱散自身 {wanted} {removed} 层");
			Log(ctx, $"驱散自身{wanted}状态 {removed} 层", LogType.System);
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
			// ⚠️ 「施加的护盾提高 50%」（诺缔拉灵知觉醒）→ 倍率从**出牌角色**那里取
			float gainMultiplier = ctx.RageTarget != null ? ctx.RageTarget.ShieldGainMultiplier : 1.0f;
			int shield = CardEffectCalculator.CalculateShield(card, effect, ctx.Attributes, gainMultiplier);
			if (shield <= 0 || ctx.Player == null) return;
		
			// ⚠️ **弃牌换护盾**（D73：温柯尔「精神重建」）：
			//   「弃掉所有手牌，每弃掉 1 张牌获得的护盾提高 50%」
			//   → 先弃牌（保留牌不弃），再按弃掉的张数放大护盾。
			if (effect.DiscardShieldBonusPercent > 0)
			{
				int discarded = ctx.Player.DiscardHandForShield();
				if (discarded > 0)
				{
					// ⚠️ 先转 double 再算，避免 float 精度少一格（踩坑 §107 / §114）
					double boosted = (double)shield * (1.0 + discarded * effect.DiscardShieldBonusPercent / 100.0);
					shield = Mathf.CeilToInt((float)boosted);
					Log(ctx, $"弃掉{discarded}张手牌，护盾提高{discarded * effect.DiscardShieldBonusPercent}%", LogType.System);
				}
			}

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
		/// **触发目标身上的中毒**（徐「入骨相思」：「造成 攻击力×300% 点必定暴击的伤害并触发 80% 中毒」）。
		///
		/// 灰机 wiki 词条「中毒」：「回合结束时受到等量层数的纯粹伤害。」
		/// → 主人口径（2026-11-13）：「对方身上有多少层的毒，就给对方造成 80%×毒层数的伤害。」
		///
		/// → 对每个目标造成「其身上中毒层数 × 百分比」的**纯粹伤害**。
		/// ⚠️ **不消耗毒层**（主人 2026-11-13 拍板）：毒层保留，回合末照常再结算一次中毒。
		/// ⚠️ 百分比**不乘等级系数**（wiki 效果1~6 都是 80%）。
		/// </summary>
		private static void ExecuteTriggerPoison(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (effect.Percent <= 0) return;

			List<Enemy> targets = new List<Enemy>();
			if (effect.Target == CardEffectTarget.AllEnemies)
			{
				if (ctx.Enemies == null) return;
				foreach (Enemy enemy in ctx.Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					targets.Add(enemy);
				}
			}
			else
			{
				Enemy target = ctx.ResolveEnemyTarget?.Invoke(card);
				if (target == null) return;
				targets.Add(target);
			}

			foreach (Enemy enemy in targets)
			{
				int stacks = enemy.GetPoisonStacks();
				if (stacks <= 0)
				{
					Log(ctx, $"你使用了{card.Name}，{enemy.EnemyName} 身上没有中毒层数，未触发中毒", LogType.System);
					continue;
				}

				int damage = CardEffectCalculator.CalculateTriggerPoisonDamage(stacks, effect);
				if (damage <= 0) continue;

				// ⚠️ **纯粹伤害**（与中毒回合末那条同一通道）：不走防御、不吃易伤/破甲、不挂来源
				enemy.TakePureDamage(damage);
				Log(ctx, $"你使用了{card.Name}，触发 {enemy.EnemyName} 的 {stacks} 层中毒，造成 {damage} 点纯粹伤害", LogType.System);
			}
		}
		
		/// <summary>
		/// **洗牌入抽牌堆**（D73）——
		/// 把一张**衍生卡**洗进抽牌堆（不是置入手牌）。
		///
		/// 灰机 wiki：
		///   - 詹金「集结鼠群」：「将 1 张造成 攻击力×30% 点伤害 X+4 次伤害的「鼠群冲击」洗入抽牌堆顶部。」
		///   - 卡斯托尔「蔽日之羽」：「将 1 张「黑羽」洗入抽牌堆。」
		///
		/// ⚠️ 洗进去的是**独立实例**（从模板克隆），不共享同一个 Card 对象。
		/// </summary>
		private static void ExecuteShuffleIntoDeck(Card card, CardEffect effect, CardEffectContext ctx)
		{
			if (string.IsNullOrEmpty(effect.ShuffleCardId) || ctx.Player == null) return;
		
			int count = effect.ShuffleCount > 0 ? effect.ShuffleCount : 1;
			int inserted = ctx.Player.InsertCardsToDeckTop(
				effect.ShuffleCardId, count, effect.ShuffleCost, effect.ShuffleConsume, card.Level);
		
			Log(ctx, $"你使用了{card.Name}，将 {inserted} 张「{effect.ShuffleCardId}」洗入抽牌堆", LogType.System);
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

			// 「每出牌获得的狂气」按觉醒卡的等级算一次（D74：宁菲亚「沉逝苦海」5 / 6 / 7 / 8 / 9 / 10 点）
			int awakeningRageGain = CardLevelSystem.CalculateFixedValue(
				card.Level, effect.AwakeningRageGain, effect.AwakeningRageGainPerLevel);
			
			// 「每出牌获得的反击层数」（D74：凯刻斯「蜕变之躯」攻击力 10%）
			float awakeningCounterGain = 0f;
			if (effect.AwakeningCounterGainPercent > 0 && attributes != null)
			{
				// 先转 double 再算，避免 float 精度少一格（踩坑 §107 / §114）
				awakeningCounterGain = (float)((double)attributes.Attack * effect.AwakeningCounterGainPercent / 100.0
					* CardLevelSystem.CalculateCoefficient(card.Level));
			}
			
			bool activated = ctx.RageTarget.ActivateAwakening(
				heal,
				awakeningRageGain,
				effect.AwakeningDebuff,
				debuffStacks,
				effect.AwakeningTriggerCardId,
				card.CardId,
				effect.AwakeningBleedCardId,
				effect.AwakeningBleedPercent,
				effect.AwakeningCounterPercent,
				effect.AwakeningShieldPercent,
				awakeningCounterGain);

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
			float percent = effect.EffectivePercent;
			if (percent > 0 && attributes != null)
			{
				if (effect.IgnoreLevelScale)
				{
					// 恒定系数：攻击力 × N%（向上取整，照参考游戏口径）
					return Mathf.CeilToInt((float)((double)attributes.Attack * percent / 100.0));
				}
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Attack, percent / 100f);
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
		public static int CalculateShield(Card card, CardEffect effect, CharacterAttributes attributes, float gainMultiplier = 1.0f)
		{
			if (card == null || effect == null) return 0;

			int level = card.Level;
			int value = 0;
			float percent = effect.EffectivePercent;
			if (percent > 0 && attributes != null)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Defense, percent / 100f);
			}
			else if (effect.Value > 0)
			{
				value = CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
		
			// ⚠️ **按「最大生命」算的那一部分**（D73：墨菲「圣女作成」：
			//    「获得总计（最大生命 5% + [防御力*10%]）点护盾和 1 点算力。」）
			//   → 本项目的「最大生命」= 体质（<c>MaxHealth =&gt; Constitution</c>）→ 基数取体质。
			//   → 与 <c>Percent</c>（防御力百分比）**相加**，不是二选一。
			if (effect.HealthPercent > 0 && attributes != null)
			{
				value += CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Constitution, effect.HealthPercent / 100f);
			}
		
			if (value <= 0) return 0;

			// ⚠️ **「施加的护盾提高 50%」**（诺缔拉灵知觉醒「维修大师」）
			//   → 倍率作用在**这一次施加**上（觉醒之前的不追溯）。
			if (gainMultiplier > 0f && gainMultiplier != 1f)
			{
				value = Mathf.CeilToInt(value * gainMultiplier);
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
			// ⚠️ **按防御力算的百分比**（D72）：「降低 防御力×N% 点力量」这类减益。
			//    基数是**施法者自己**的防御力（不是目标的）—— 否则卡面没法显示确切值。
			if (effect.DefensePercent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Defense, effect.DefensePercent / 100f);
			}
			if (effect.EffectivePercent > 0 && attributes != null)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, attributes.Attack, effect.EffectivePercent / 100f);
			}
			if (effect.Value > 0)
			{
				return CardLevelSystem.CalculateAttributeLinkedValue(level, effect.Value, 1f);
			}
			return 0;
		}

		/// <summary>
		/// **触发中毒的伤害** = 目标身上的中毒层数 × 百分比（徐「入骨相思」，D72）。
		///
		/// ⚠️ **不乘等级系数**（wiki 效果1~6 都是 80% → 这个百分比是固定的）。
		/// ⚠️ 向上取整（与伤害 / 护盾同一口径）。
		/// </summary>
		public static int CalculateTriggerPoisonDamage(int poisonStacks, CardEffect effect)
		{
			if (poisonStacks <= 0 || effect == null || effect.Percent <= 0) return 0;
			// ⚠️ 先转 double 再算，避免 float 精度少一格（踩坑 §107 / §114）
			double raw = (double)poisonStacks * effect.Percent / 100.0;
			return Mathf.CeilToInt((float)raw);
		}
		/// <summary>
		/// **反击层数 = 攻击力 × 百分比 × 等级系数**（D68，诺缔拉「高热区禁入」/ 菲茵特「无边星彩」）。
		///
		/// ⚠️ 公式**只写这一处** —— 卡面显示与战斗结算共用它，
		///    所以不会再现「卡面写 221 层、战斗涨 110 层」（踩坑 §124）。
		///
		/// ⚠️ 保持 **float**：反击层数是 float（踩坑 §118「字段的数值类型 = 精度上限」）；
		///    卡面显示时再向上取整（与状态图标的层数口径一致）。
		/// </summary>
		public static float CalculateCounter(Card card, CardEffect effect, CharacterAttributes attributes)
		{
			if (card == null || effect == null) return 0f;
			float percent = effect.EffectivePercent;
			if (percent <= 0 || attributes == null) return 0f;

			int attack = attributes.Attack;
			if (attack <= 0) return 0f;

			// ⚠️ 跟伤害/护盾同一套：先转 double 再乘，避免 float 精度少一格（踩坑 §107 / §114）
			return (float)((double)attack * percent / 100.0
				* CardLevelSystem.CalculateCoefficient(card.Level));
		}
	}
}
