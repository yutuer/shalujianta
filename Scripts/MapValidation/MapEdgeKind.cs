namespace MapValidation
{
	/// <summary>
	/// 图上边的性质（用于「死路」判定：出度为 1 且唯一出边是单向 → 死路）
	/// </summary>
	public enum MapEdgeKind
	{
		/// <summary>普通邻接边（双向）</summary>
		Normal = 0,

		/// <summary>单向边（走过去回不来）</summary>
		OneWay = 1,

		/// <summary>双向超边（例如双向传送门）</summary>
		TwoWay = 2
	}
}
