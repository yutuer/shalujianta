namespace FishEatFish.Battle.Card
{
	/// <summary>
	/// **文本里数值的统一配色**（照参考游戏《终末前夜 Morimens》的卡面）。
	///
	/// 参考图实测（`卡牌总览(满级)_波吕克斯.jpg` / `卡牌总览_珈伦.jpg`）：
	///   「造成 <b>37</b> 点伤害」→ 红　「获得 <b>32</b> 点护盾」→ 蓝
	///   「回复 <b>24</b> 点生命」→ 绿　「获得 <b>10</b> 点怒气」→ 橙
	///   而「1 回合的 ✕虚弱」「施加 41 层中毒」里的**层数不着色**（只有结果值着色）。
	///
	/// 主人 2026-10-25 定的口径：**伤害红 / 恢复绿 / 怒气橙**，
	/// 护盾沿用参考图的蓝；**词缀**沿用 `Data/affixes.json` 的既有配色
	/// （有益 = 黄 `#e8a34a`，debuff = 紫 `#c86bd8`）。
	///
	/// ⚠️ 这套色值和战斗日志（`BattleLogWindow.GetColorForLogType`）是**同一份** ——
	/// 战斗日志早就按类型分色了（Damage 红 / Heal 绿 / Shield 蓝），两边共用常量才不会漂。
	/// </summary>
	public static class TextColors
	{
		/// <summary>伤害（红）</summary>
		public const string Damage = "#ff6b6b";

		/// <summary>护盾（蓝）</summary>
		public const string Shield = "#6bb3ff";

		/// <summary>恢复 / 治疗（绿）</summary>
		public const string Heal = "#6bff6b";

		/// <summary>怒气（橙）</summary>
		public const string Rage = "#ffb347";

		/// <summary>算力 / 能量（黄）</summary>
		public const string Energy = "#ffdd6b";

		/// <summary>有益词缀（黄）—— 与 Data/affixes.json 一致</summary>
		public const string BuffAffix = "#e8a34a";

		/// <summary>debuff 词缀（紫）—— 与 Data/affixes.json 一致</summary>
		public const string DebuffAffix = "#c86bd8";

		/// <summary>技能 / 必杀技（紫）</summary>
		public const string Skill = "#dd6bff";
	}
}
