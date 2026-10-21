using System.Collections.Generic;
using System.Text;

namespace MapValidation
{
	/// <summary>
	/// 地图校验报告 —— 结构化诊断，不是 bool。
	///
	/// 包含：问题列表（逐条定位到格子）+ 统计（节点数/边数/簇数/各类型问题数）。
	/// </summary>
	public class MapValidationReport
	{
		/// <summary>全部问题</summary>
		public List<MapIssue> Issues = new List<MapIssue>();

		/// <summary>节点总数</summary>
		public int NodeCount;

		/// <summary>边总数（按有向边计）</summary>
		public int EdgeCount;

		/// <summary>簇数量（按「邻接 + 双向传送」缩出来的区域数）</summary>
		public int ClusterCount;

		/// <summary>从起点能到达的节点数</summary>
		public int ReachableCount;

		/// <summary>能到达终点的节点数</summary>
		public int CanReachGoalCount;

		/// <summary>是否存在通关走法（状态空间判定结果；无洞时等价于「终点可达」）</summary>
		public bool CanCompleteGoal = true;

		/// <summary>
		/// **无论怎么走都能到终点**（全称判定）——
		/// 没有任何可达状态是死局。比 <see cref="CanCompleteGoal"/>（存在性）更强。
		/// </summary>
		public bool CanAlwaysReachGoal = true;

		/// <summary>死局状态的数量（走进去就再也到不了终点的那种状态）</summary>
		public int DeadEndStateCount;

		/// <summary>状态空间搜索的状态数（无洞时为 0）</summary>
		public int ExploredStateCount;

		/// <summary>按类型统计问题数</summary>
		public Dictionary<MapIssueType, int> IssueCounts = new Dictionary<MapIssueType, int>();

		/// <summary>是否没有任何问题</summary>
		public bool IsValid
		{
			get { return Issues.Count == 0; }
		}

		/// <summary>某类型的问题数</summary>
		public int CountOf(MapIssueType type)
		{
			int n;
			return IssueCounts.TryGetValue(type, out n) ? n : 0;
		}

		/// <summary>追加一条问题并更新统计</summary>
		public void AddIssue(MapIssue issue)
		{
			if (issue == null) return;

			Issues.Add(issue);
			int n;
			IssueCounts.TryGetValue(issue.Type, out n);
			IssueCounts[issue.Type] = n + 1;
		}

		/// <summary>可读报告文本</summary>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"地图校验报告：节点 {NodeCount}，边 {EdgeCount}，簇 {ClusterCount}，"
				+ $"起点可达 {ReachableCount}，能到终点 {CanReachGoalCount}，"
				+ $"可通关 {(CanCompleteGoal ? "是" : "否")}，状态空间 {ExploredStateCount}，"
				+ $"问题 {Issues.Count} 项");

			foreach (MapIssue issue in Issues)
			{
				sb.AppendLine("  - " + issue.ToString());
			}

			return sb.ToString();
		}
	}
}
