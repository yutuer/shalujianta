namespace FishEatFish.Battle.HexMap
{
	/// <summary>
	/// 关卡配额 —— 「这张地图要放多少个什么东西」。
	///
	/// 配额是关卡的**唯一可变描述**：同一份配额 + 同一个种子 → 同一张地图。
	/// </summary>
	public class MapQuota
	{
		/// <summary>普通战斗格数量</summary>
		public int Battle;

		/// <summary>精英战斗格数量</summary>
		public int Elite;

		/// <summary>商店格数量</summary>
		public int Shop;

		/// <summary>治疗格数量</summary>
		public int Heal;

		/// <summary>黑印格数量</summary>
		public int BlackMark;

		/// <summary>沼泽格数量</summary>
		public int Swamp;

		/// <summary>洞（消耗型格）数量</summary>
		public int Hole;

		/// <summary>探照灯数量</summary>
		public int Searchlight;

		/// <summary>单向门**对数**（跨簇、只能一个方向、可重复用，每对占 2 个格子）</summary>
		public int TeleportOneWay;

		/// <summary>双向传送门**对数**（跨簇、可来回，每对占 2 个格子）</summary>
		public int TeleportTwoWay;

		/// <summary>配额要求的**事件格**总数（不含传送门占的格子，传送门单独算）</summary>
		public int TotalEventCount
		{
			get
			{
				return Battle + Elite + Shop + Heal + BlackMark + Swamp
					+ Hole + Searchlight;
			}
		}

		/// <summary>传送门占用的格子总数（每对 2 格）</summary>
		public int TeleportTileCount
		{
			get { return (TeleportOneWay + TeleportTwoWay) * 2; }
		}

		public override string ToString()
		{
			return $"战斗{Battle}/精英{Elite}/商店{Shop}/治疗{Heal}/黑印{BlackMark}"
				+ $"/沼泽{Swamp}/洞{Hole}/探照灯{Searchlight}"
				+ $"/单向门{TeleportOneWay}/双向门{TeleportTwoWay}";
		}
	}
}
