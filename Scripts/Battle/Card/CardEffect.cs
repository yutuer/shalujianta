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

		/// <summary>
		/// **按施法者「防御力」算的百分比**（D72）。
		///
		/// 灰机 wiki 有一类减益的数值是按**防御力**算的：
		/// 「使所有敌人临时降低 **防御力×N%** 点力量」（徐「寄吻于扇」／菲茵特「养分汲取」／图鲁「螺湮重临」）。
		///
		/// ⚠️ 本项目的减益百分比**默认按攻击力算**（<see cref="Percent"/>）→
		/// 所以这一类要单独用 <see cref="DefensePercent"/> 表达，**别混进 Percent**。
		/// ⚠️ 基数是**施法者自己**的防御力（不是目标的）——
		///    否则同一个卡打在防御力不同的敌人身上数值会变，**卡面就没法显示确切值**了。
		/// </summary>
		public int DefensePercent { get; set; }

		/// <summary>
		/// **临时**（反击用）—— true = 临时反击，**到本回合结束清除**（主人 2026-11-07 拍板）。
		/// 只有 <see cref="CardEffectType.Counter"/> 用得到。
		/// </summary>
		public bool Temporary { get; set; }

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

		/// <summary>
		/// **洗入抽牌堆的卡牌 id**（D73）。
		///
		/// 灰机 wiki 里有一类卡不是「打出」，而是**洗进抽牌堆**：
		///   - 詹金「集结鼠群」：「将 1 张造成 攻击力×30% 点伤害 X+4 次伤害的「鼠群冲击」洗入抽牌堆顶部。」
		///   - 卡斯托尔「蔽日之羽」/「不屈的孤嚎鸟」：「将 1 张「黑羽」洗入抽牌堆。」
		///
		/// 洗进去的那张是**别的卡牌条目**（衍生卡），所以要单独存 id。
		/// 数据表写法：<c>{ "type": "shuffleIntoDeck", "shuffleCardId": "jenkin_mouse_shock", "shuffleCount": 1 }</c>
		/// </summary>
		public string ShuffleCardId { get; set; } = "";

		/// <summary>洗入抽牌堆的张数（配合 <see cref="ShuffleCardId"/>）</summary>
		public int ShuffleCount { get; set; }

		/// <summary>
		/// 洗入时**覆盖的算力消耗**（-1 = 不覆盖，沿用模板卡自身的费用）。
		///
		/// 詹金「雾都街童」：「将 3 张附加{{词条|消耗}}、**算力消耗为 0** 的「布朗出动！」置入手牌。」
		/// </summary>
		public int ShuffleCost { get; set; } = -1;

		/// <summary>
		/// 洗入 / 置入手牌时是否**附加「消耗」词缀**（打出即从牌库移除）。
		///
		/// 詹金「雾都街童」：「将 3 张附加{{词条|消耗}}、算力消耗为 0 的「布朗出动！」置入手牌。」
		/// </summary>
		public bool ShuffleConsume { get; set; }

		/// <summary>
		/// **弃牌换护盾**：每弃掉 1 张牌，护盾提高 N%（D73：温柯尔「精神重建」）。
		///
		/// wiki 原文：「获得 [防御力*20%] 点护盾，**弃掉所有手牌，每弃掉 1 张牌获得的护盾提高 50%**。」
		/// → 本效果 = 先算基础护盾，再按弃牌张数 × N% 放大。
		///
		/// ⚠️ **它仍然是「shield」效果**（不是另一个效果类型）——
		///    否则卡面正文里的 <c>{shield}</c> 占位符找不到 shield 条目 → 数值会算成 0。
		/// ⚠️ 「保留」的牌**不弃**（与 <c>Player.DiscardHand</c> 的口径一致）。
		/// </summary>
		public int DiscardShieldBonusPercent { get; set; }

		/// <summary>
		/// **按「最大生命」算的护盾百分比**（D73：墨菲「圣女作成」）。
		///
		/// wiki 原文：「获得总计（**最大生命 5%** + [防御力*10%]）点护盾和 1 点算力。」
		///
		/// ⚠️ 本项目的「最大生命」= <see cref="CharacterSystem.CharacterAttributes.MaxHealth"/>
		///    = **体质**（<c>MaxHealth =&gt; Constitution</c>）→ 基数取体质。
		/// ⚠️ 与 <see cref="Percent"/>（防御力百分比）**分开写**，别混进同一个字段。
		/// </summary>
		public int HealthPercent { get; set; }

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
		/// **灵知觉醒授予的「反击提高百分比」**（诺缔拉「维修大师」= 50）。
		///
		/// wiki 原文：「{{词条|灵知觉醒}}：诺缔拉施加的护盾和{{词条|反击}}提高 50%。」
		/// → 作用在**获得的那一刻**（觉醒之前攒下的不被追溯放大）。
		/// </summary>
		public int AwakeningCounterPercent { get; set; }

		/// <summary>
		/// **灵知觉醒授予的「护盾提高百分比」**（诺缔拉「维修大师」= 50）。
		/// 和 <see cref="AwakeningCounterPercent"/> 是同一句话里的两半。
		/// </summary>
		public int AwakeningShieldPercent { get; set; }
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

		/// <summary>
		/// **反击**（诺缔拉身上的 buff）——
		/// 照参考游戏《终末前夜 Morimens》「反击」词条：
		/// 「承受主动伤害时，对伤害来源造成等量层数的纯粹伤害。」
		///
		/// ⚠️ 它的「层数」是 **float**（来自「攻击力×N%」），
		/// 所以**不走 <see cref="CardEffectType.Stack"/> 那套 int 层数**，
		/// 而是 <c>CharacterPosition.CounterStacks</c> 单独一套。
		/// 这个 id 只用于**显示用**的状态图标。
		/// </summary>
		public const string Counter = "counter";

		/// <summary>
		/// **触腕**（珊身上的层数）——「深海」界域的独特资源：
		/// 打出「美的怜悯」获得 1 条触腕，触腕伤害随触腕数量成长。
		/// </summary>
		public const string TouchArm = "touch_arm";

		/// <summary>
		/// **胚胎融合**（泰旖丝身上的层数）——「血肉」界域的资源：
		/// 打出「圣血的本能」+40 层，到上限后把一张「胚胎」置入手牌。
		/// </summary>
		public const string EmbryoFusion = "embryo_fusion";

		/// <summary>层数 id → 显示名</summary>
		public static string DisplayName(string stackId)
		{
			if (string.IsNullOrEmpty(stackId)) return "层数";
			return stackId switch
			{
				SinMark => "罪印",
				Counter => "反击",
				TouchArm => "触腕",
				EmbryoFusion => "胚胎融合",
				_ => stackId
			};
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

		/// <summary>
		/// **本场战斗死亡抵抗触发衰减率**（菲茵特「无边星彩」灵知觉醒）。
		///
		/// wiki 原文：「本场战斗{{词条|死亡抵抗}}触发衰减率从 50% 降低为 45%。」
		/// 数据表写法：<c>{ "type": "deathResistanceDecay", "value": 45 }</c>（value = 百分比）。
		/// </summary>
		public const string DeathResistanceDecay = "deathResistanceDecay";

		/// <summary>
		/// **本场战斗触发过死亡抵抗则额外生效 1 次**（菲茵特「幼芽萌生」）。
		///
		/// wiki 原文：「获得 [防御力*15%] 点护盾，回复 [体质*15%] 点生命。
		/// **若在本场战斗中触发过{{词条|死亡抵抗}}，额外生效 1 次。**」
		///
		/// 数据表写法：<c>{ "type": "repeatOnDeathResistance", "count": 2 }</c>
		/// —— 放在效果列表**第一位**，引擎先扫它决定整串效果要跑几遍。
		/// </summary>
		public const string RepeatOnDeathResistance = "repeatOnDeathResistance";

		/// <summary>
		/// **获得反击层数**（诺缔拉「打击」/「高热区禁入」/「整装待发」）。
		///
		/// wiki 原文：「获得 <font color=#b75a64>[攻击力*30%]</font> 层{{词条|反击}}。」
		/// → 层数 = **攻击力 × 百分比 × 等级系数**（照卡牌其它百分比字段的口径）。
		///
		/// 数据表写法：<c>{ "type": "counter", "percent": 15, "temporary": false }</c>
		/// —— <c>percent</c> 是**攻击力百分比**（1 级口径），
		/// <c>temporary</c> = true 时是**临时反击**（到本回合结束清除）。
		/// </summary>
		public const string Counter = "counter";

		/// <summary>
		/// **触发反击**（诺缔拉「整装待发」：「对所有敌人触发 100% {{词条|反击}}」）。
		///
		/// → 对每个目标造成「当前队伍总反击 × 百分比」的**纯粹伤害**。
		///
		/// 数据表写法：<c>{ "type": "triggerCounter", "percent": 100, "target": "allEnemies" }</c>
		/// </summary>
		public const string TriggerCounter = "triggerCounter";

		/// <summary>
		/// **驱散自身减益**（图鲁「螺湮重临」：「驱散自身{{词条|虚弱}}状态。」）。
		///
		/// → 移除**玩家自己**身上指定 id 的减益（weak / poison / …）。
		/// ⚠️ 减益 id 复用 <see cref="CardEffect.Debuff"/> 字段（不新增字段 → 加载器不用改）。
		///
		/// 数据表写法：<c>{ "type": "disperseSelfDebuff", "debuff": "weak" }</c>
		/// </summary>
		public const string DisperseSelfDebuff = "disperseSelfDebuff";

		/// <summary>
		/// **触发目标身上的中毒**（徐「入骨相思」：「造成 攻击力×300% 点必定暴击的伤害并**触发 80% 中毒**」）。
		///
		/// 灰机 wiki 词条「中毒」：「回合结束时受到等量层数的**纯粹伤害**。」
		/// → 主人口径（2026-11-13）：「对方身上有多少层的毒，就给对方造成 **80% × 毒层数** 的伤害」。
		///
		/// → 所以本效果 = **按目标身上的中毒层数 × 百分比**造成**纯粹伤害**（不走防御、不吃易伤/破甲）。
		/// ⚠️ **不消耗毒层**（主人 2026-11-13 拍板）：结算完毒层还在，
		///    回合末仍会照常结算一次中毒（等于同一份毒吃两遍）。
		/// ⚠️ 百分比**不随卡牌等级变**（wiki 效果1~6 都是 80%）→ 这里**不乘等级系数**。
		///
		/// 数据表写法：<c>{ "type": "triggerPoison", "percent": 80, "target": "allEnemies" }</c>
		/// </summary>
		public const string TriggerPoison = "triggerPoison";

		/// <summary>
		/// **洗牌入抽牌堆**（D73）——
		/// 把一张**衍生卡**洗进抽牌堆（不是置入手牌）。
		///
		/// 灰机 wiki：
		///   - 詹金「集结鼠群」：「将 1 张造成 攻击力×30% 点伤害 X+4 次伤害的「鼠群冲击」洗入抽牌堆顶部。」
		///   - 卡斯托尔「蔽日之羽」：「将 1 张「黑羽」洗入抽牌堆。」
		///
		/// 数据表写法：
		/// <c>{ "type": "shuffleIntoDeck", "shuffleCardId": "jenkin_mouse_shock", "shuffleCount": 1 }</c>
		/// ⚠️ 洗入的卡是**独立实例**（从模板克隆），不是共享同一个 Card 对象。
		/// </summary>
		public const string ShuffleIntoDeck = "shuffleIntoDeck";


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
