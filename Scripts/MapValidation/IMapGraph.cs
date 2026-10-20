using System.Collections.Generic;

namespace MapValidation
{
	/// <summary>
	/// 地图抽象图接口 —— 校验器只认这个接口，不认任何具体地图类型。
	///
	/// **这个接口是整个模块可移植性的关键**：
	/// 喂进来什么图（方形网格 / 六边形 / 卡牌爬塔的房间图），
	/// 用的都是同一套「有向图可达性 + 死锁检测」算法。
	///
	/// 约定：
	///   - 节点用 **int id** 表示（0 ~ NodeCount-1）
	///   - 边是**有向的**：传送门单向的只有一条边，双向的有两条
	///   - 「消耗型节点」（洞）用 <see cref="GetConsumableNodes"/> 标出
	/// </summary>
	public interface IMapGraph
	{
		/// <summary>节点总数</summary>
		int NodeCount { get; }

		/// <summary>起点节点 id</summary>
		int GetStartNode();

		/// <summary>终点节点 id（BOSS）</summary>
		int GetEndNode();

		/// <summary>某节点的出边目标列表</summary>
		IReadOnlyList<int> GetOutgoingEdges(int node);

		/// <summary>某节点的入边来源列表（反向 BFS 用）</summary>
		IReadOnlyList<int> GetIncomingEdges(int node);

		/// <summary>节点的可读标签（报告里显示用，如 "(3, 5)"）</summary>
		string GetNodeLabel(int node);

		/// <summary>该节点是否不可通行（墙、虚空）</summary>
		bool IsBlocking(int node);

		/// <summary>
		/// 消耗型节点集合（洞：踩上去还能站，但离开后该格被删除）。
		/// 现在没有洞的地图返回空集合即可，**接口必须一开始就留**。
		/// </summary>
		IReadOnlyCollection<int> GetConsumableNodes();

		/// <summary>必经节点集合（required：关键事件格、商店、BOSS 之前的必经点）</summary>
		IReadOnlyCollection<int> GetRequiredNodes();

		/// <summary>边的性质</summary>
		MapEdgeKind GetEdgeKind(int from, int to);
	}
}
