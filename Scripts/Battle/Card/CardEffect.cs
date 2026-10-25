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

		/// <summary>
		/// **每级增长**（只对 rage 有意义）—— 怒气 = <c>Value + PerLevel × (等级 − 1)</c>。
		///
		/// 照参考游戏《终末前夜 Morimens》（2026-10-25 主人拍板，全局统一）：
		///   - **基本卡（打击 / 防御）**：<c>value 5 / perLevel 1</c> → Lv1 = 5、Lv3 = 7、Lv6 = 10
		///   - **灵知觉醒卡**：<c>value 25 / perLevel 5</c> → Lv4 = 40、Lv6 = 50
		///   - **其他专属指令卡**：**不给怒气**（参考图里属性面板没有「狂气」行、卡面也没有怒气）
		/// </summary>
		public int PerLevel { get; set; }

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
		/// **不乘等级系数** —— 「攻击力×N%」恒定，六个等级都一样。
		///
		/// 照参考游戏《终末前夜 Morimens》：波吕克斯「圣心」= 攻击力×70%，
		/// wiki 六个等级写的都是 70%（**不是** 70% → 140% 的成长曲线）。
		/// 我们的通用公式是「基础% × (1+0.2×(级−1))」→ Lv6 会翻倍，
		/// 所以这类卡要显式打开这个开关。
		/// </summary>
		public bool IgnoreLevelScale { get; set; }

		/// <summary>
		/// **灵知觉醒：打出卡牌后获得的怒气**（珈伦「凸月之舞」= 5 点）。
		/// </summary>
		public int AwakeningRageGain { get; set; }

		/// <summary>
		/// **灵知觉醒：打出指定那张卡后，对全体敌人施加的减益层数**（按攻击力百分比）。
		/// 珈伦「凸月之舞」= 打出「奇妙料理」后，全体敌人中毒 攻击力×15% 层。
		/// </summary>
		public int AwakeningDebuffPercent { get; set; }

		/// <summary>灵知觉醒：打出**哪张卡**时触发那个减益（衍生卡 id，如 jialun_derived）</summary>
		public string AwakeningTriggerCardId { get; set; } = "";

		/// <summary>灵知觉醒：减益 id（poison 等）</summary>
		public string AwakeningDebuff { get; set; } = "";

		/// <summary>
		/// **保留叠加的最大层数**（珈伦「静默守望」= 2，启灵1 后为 3）。
		/// 回合结束时这张卡还在手里 → 算力消耗 +1、回复与护盾翻倍，叠到上限为止。
		/// </summary>
		public int RetainStackMax { get; set; }

		/// <summary>
		/// **层数效果的 id**（罪印 = <see cref="CardStackIds.SinMark"/>）。
		/// 数据表写法：<c>{ "type": "stack", "stackId": "sin_mark", "count": 3 }</c>
		/// </summary>
		public string StackId { get; set; } = "";

		/// <summary>
		/// **每张手牌额外 +N 层**（净世之路 = 1：「每有 1 张手牌额外获得 1 层罪印」）。
		/// 实际层数 = <c>Count + 手牌数 × CountPerHand</c>。
		/// </summary>
		public int CountPerHand { get; set; }

		/// <summary>
		/// **出血 = 本次伤害的 N%**（圣心 = 100）。
		/// 出血挂在**被打的敌人**身上，回合末造成等量层数的纯粹伤害。
		/// </summary>
		public int PercentOfDamage { get; set; }

		/// <summary>
		/// **出血必须由灵知觉醒授予才生效** ——
		/// 未觉醒的波吕克斯打出圣心**只有罪印的 1%，没有 100% 出血**
		/// （「100% 出血」是灵知觉醒「净世之路」授予的，不是圣心自己的词缀）。
		/// </summary>
		public bool RequireAwakening { get; set; }

		/// <summary>
		/// **灵知觉醒授予的出血：哪张卡的伤害附带出血**（波吕克斯「圣心」= <c>pollux_derived</c>）。
		/// 数据表写法：<c>{ "type": "awakening", "awakeningBleedCardId": "pollux_derived", "awakeningBleedPercent": 100 }</c>
		/// </summary>
		public string AwakeningBleedCardId { get; set; } = "";

		/// <summary>
		/// **灵知觉醒授予的出血：多少百分比**（圣心 = 100）。
		/// </summary>
		public int AwakeningBleedPercent { get; set; }

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

	/// <summary>
	/// **卡牌特有的词缀说明**（不在通用词缀表 <c>Data/affixes.json</c> 里的那些）。
	/// 数据表字段：<c>affixNotes</c>，例如灵知觉醒卡的「灵知觉醒」「罪印」。
	/// </summary>
	public class CardAffixNote
	{
		/// <summary>词缀 id（用于去重 / 查色）</summary>
		public string Id { get; set; } = "";

		/// <summary>显示名（如「罪印」）</summary>
		public string Name { get; set; } = "";

		/// <summary>说明文字</summary>
		public string Description { get; set; } = "";
	}

	/// <summary>
	/// **层数 id 常量** —— 「层数」效果（<c>type = stack</c>）加到**角色身上**的那种。
	/// </summary>
	public static class CardStackIds
	{
		/// <summary>
		/// **罪印**（波吕克斯身上的 buff）——
		/// 照参考游戏《终末前夜 Morimens》「✕罪印」词条：「每层使波吕克斯造成的伤害附加 1% 出血。」
		/// </summary>
		public const string SinMark = "sin_mark";

		/// <summary>层数 id → 显示名</summary>
		public static string DisplayName(string stackId)
		{
			if (string.IsNullOrEmpty(stackId)) return "层数";
			return stackId == SinMark ? "罪印" : stackId;
		}
	}

	/// <summary>效果触发时机</summary>
	public static class CardEffectTiming
	{
		/// <summary>打出时</summary>
		public const string OnPlay = "onPlay";
		/// <summary>弃牌阶段后</summary>
		public const string OnDiscard = "onDiscard";
		/// <summary>
		/// **灵知觉醒触发时**（每次打出卡牌后）。
		/// 珈伦「凸月之舞」的「回复 体质×9% 生命」就是这个时机 ——
		/// 它**不是**打出觉醒卡时回的血，而是之后每打出一张卡牌都回。
		/// </summary>
		public const string OnAwakening = "onAwakening";
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
		/// <summary>
		/// **灵知觉醒**：打出后使角色在本场战斗中获得特殊能力
		/// （珈伦「凸月之舞」= 之后每打出一张卡牌都回复体质×9% 生命并获得 5 点狂气）。
		/// </summary>
		public const string Awakening = "awakening";
		/// <summary>
		/// **保留叠加**：回合结束时若这张卡还在手里，算力消耗 +1、回复与护盾翻倍
		/// （珈伦「静默守望」= 最多叠加 2 次）。
		/// </summary>
		public const string RetainStack = "retainStack";
		/// <summary>
		/// **层数**（罪印等）—— 加到**角色身上**（不是卡上）。
		/// 照参考游戏《终末前夜 Morimens》「✕罪印」：「每层使波吕克斯造成的伤害附加 1% 出血。」
		/// 数据表写法：<c>{ "type": "stack", "stackId": "sin_mark", "count": 3, "countPerHand": 1 }</c>
		/// </summary>
		public const string Stack = "stack";
		/// <summary>
		/// **出血**：这张卡造成的伤害附带 N% 的出血（回合末造成等量层数的纯粹伤害）。
		/// 数据表写法：<c>{ "type": "bleed", "requireAwakening": true }</c>
		/// </summary>
		public const string Bleed = "bleed";
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
