using System.Collections.Generic;

namespace FishEatFish.Battle.MapSystem
{
	/// <summary>
	/// 关卡展示页（D10）里的一个章节（含该章节的关卡列表）
	/// </summary>
	public class LevelChapterMeta
	{
		/// <summary>章节 id</summary>
		public string Id = "";

		/// <summary>章节名（形如 "蜕变"）</summary>
		public string Name = "";

		/// <summary>章节内的关卡</summary>
		public List<LevelNodeMeta> Levels = new List<LevelNodeMeta>();
	}
}
