namespace MapValidation
{
	/// <summary>
	/// 地图问题类型（对应开发计划里列的 7 类问题）
	/// </summary>
	public enum MapIssueType
	{
		/// <summary>通不了关：起点 → 终点在有向图上不可达</summary>
		UnreachableGoal = 0,

		/// <summary>软锁：某格从起点可达，但走进去后再也到不了终点</summary>
		SoftLock = 1,

		/// <summary>不可达区域：地图上有格子永远去不了</summary>
		UnreachableRegion = 2,

		/// <summary>关键格不可达：required 节点不在起点可达集里</summary>
		RequiredUnreachable = 3,

		/// <summary>死路：出度为 1，且唯一出边是单向的</summary>
		DeadEnd = 4,

		/// <summary>孤岛：存在与主线弱连通但走不通的区域</summary>
		IsolatedIsland = 5,

		/// <summary>状态死锁：消耗型格子（洞）用过之后原路不通，找不到通关走法</summary>
		StateDeadlock = 6,

		/// <summary>旁支必需格不可达：required 落在「只能单向进入」的旁支簇里</summary>
		BranchRequiredUnreachable = 7,

		/// <summary>多簇提示：地图被切成了多块（信息项，不一定是错误）</summary>
		MultiCluster = 8,

		/// <summary>
		/// 死局状态：**存在某个可达状态，从那里出发无论怎么走都到不了终点**。
		/// （比 StateDeadlock 更强 —— 那个是"没有走法"，这个是"不是所有走法都行"）
		/// </summary>
		DeadEndState = 9
	}
}
