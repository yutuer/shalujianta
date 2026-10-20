namespace FishEatFish.Battle.MapSystem
{
	/// <summary>
	/// 关卡展示页（D10）里的一个关卡节点 —— 展示用元数据，来自 Data/levels.json。
	///
	/// 注意：关卡的**玩法数据**（敌人波次、奖励）仍在 MapDefinition 里；
	/// 这里只管「页面上怎么显示它」。
	/// </summary>
	public class LevelNodeMeta
	{
		/// <summary>对应的 MapDefinition.MapId</summary>
		public string MapId = "";

		/// <summary>关卡号（形如 "2-1"）</summary>
		public string Number = "";

		/// <summary>关卡标题</summary>
		public string Title = "";

		/// <summary>难度档（normal / hard / dream）</summary>
		public string Difficulty = "normal";

		/// <summary>推荐等级</summary>
		public int RecommendedLevel = 1;

		/// <summary>是否已解锁</summary>
		public bool Unlocked = true;

		/// <summary>所属章节</summary>
		public LevelChapterMeta Chapter;
	}
}
