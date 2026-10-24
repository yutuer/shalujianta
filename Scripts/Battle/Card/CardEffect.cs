using System.Collections.Generic;

namespace FishEatFish.Battle.Card
{
	/// <summary>
	/// **卡牌效果**（一条 = 一个效果）。
	///
	/// 为什么要它：以前卡牌的效果是「一个字段一个分支」
	/// （<c>damagePercent</c> / <c>applyBuffName</c> / <c>drawCountBase</c> …），
	/// 结算代码里各写一个分支 —— 加一种新效果就要改结算流程。
	///
	/// 改成效果列表后：**新增一种效果 = 加一个执行器 + 数据表加一行**，结算流程一行不改。
	///
	/// 数据表写法（<c>Data/character_cards.json</c>）：
	/// <code>
	/// "effects": [
	///   { "type": "damage", "percent": 80, "target": "allEnemies", "hitCount": 2 },
	///   { "type": "rage",   "value": 15 },
	///   { "type": "buff",   "buff": "strength", "value": 3, "duration": 3, "target": "enemy" }
	/// ]
	/// </code>
	/// </summary>
	public class CardEffect
	{
		/// <summary>效果类型：damage / shield / rage / heal / energy / draw / buff / debuff / retain / consume</summary>
		public string Type { get; set; } = "";

		/// <summary>百分比（对角色属性）：伤害 = 攻击力×N%，护盾 = 防御力×N%</summary>
		public int Percent { get; set; }

		/// <summary>固定数值：怒气 / 治疗 / 能量 / 增益减益的层数或数值</summary>
		public int Value { get; set; }

		/// <summary>数量（抽几张）</summary>
		public int Count { get; set; }

		/// <summary>持续回合（增益/减益）</summary>
		public int Duration { get; set; }

		/// <summary>命中次数（1 = 一次；2 = 「造成 x 点伤害 2 次」）</summary>
		public int HitCount { get; set; } = 1;

		/// <summary>增益 id（<c>Data/buffs.json</c>）</summary>
		public string Buff { get; set; } = "";

		/// <summary>减益 id（<c>Data/buffs.json</c>）</summary>
		public string Debuff { get; set; } = "";

		/// <summary>作用目标：target（单体）/ allEnemies（全体敌人）/ self（自己）</summary>
		public string Target { get; set; } = "";

		/// <summary>抽牌筛选：空 = 随机；lowestCostInstruction = 算力消耗最低的指令卡</summary>
		public string Filter { get; set; } = "";

		/// <summary>触发时机：onPlay（打出）/ onDiscard（弃牌阶段后）。不填默认 onPlay</summary>
		public List<string> Timing { get; set; } = new List<string>();

		/// <summary>
		 /// 该效果在指定时机是否触发。
		/// 没写 timing 的效果**只在 onPlay 触发**（一次性的效果都是打出时）。
		/// </summary>
		public bool MatchesTiming(string timing)
		{
			if (Timing == null || Timing.Count == 0)
			{
				return timing == CardEffectTiming.OnPlay;
			}
			return Timing.Contains(timing);
		}
	}

	/// <summary>效果触发时机</summary>
	public static class CardEffectTiming
	{
		/// <summary>打出时</summary>
		public const string OnPlay = "onPlay";
		/// <summary>弃牌阶段后</summary>
		public const string OnDiscard = "onDiscard";
	}

	/// <summary>效果类型常量</summary>
	public static class CardEffectType
	{
		public const string Damage = "damage";
		public const string Shield = "shield";
		public const string Rage = "rage";
		public const string Heal = "heal";
		public const string Energy = "energy";
		public const string Draw = "draw";
		public const string Buff = "buff";
		public const string Debuff = "debuff";
		/// <summary>保留：回合结束留在手牌</summary>
		public const string Retain = "retain";
		/// <summary>消耗：打出后从牌库移除（不进弃牌堆）</summary>
		public const string Consume = "consume";
	}

	/// <summary>作用目标常量</summary>
	public static class CardEffectTarget
	{
		/// <summary>单体（按卡牌的 target 字段解析）</summary>
		public const string Target = "target";
		/// <summary>全体敌人</summary>
		public const string AllEnemies = "allEnemies";
		/// <summary>自己</summary>
		public const string Self = "self";
	}
}
