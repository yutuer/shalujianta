using System.Collections.Generic;

namespace MapValidation
{
	/// <summary>
	/// 地图校验器 —— **纯算法，不引用任何 Godot 类型**。
	///
	/// 两层判定：
	///   第 1 层 · 静态分析（O(V+E)）：通不了关 / 软锁 / 不可达区域 / 关键格不可达 / 死路 / 孤岛
	///   第 2 层 · 状态空间搜索（有洞时必做）：消耗型格子导致的状态死锁
	///   另有 · 簇图分析：旁支必需格不可达（「拐进去就回不来」）
	///
	/// 整个目录（除 HexMapGraph 适配器）可直接拷到任何项目使用。
	/// </summary>
	public class MapValidator
	{
		/// <summary>同一类问题最多逐条列出多少个（超出用一条汇总）</summary>
		public const int MaxIssuesPerType = 20;

		private readonly ClusterGraphAnalyzer _clusterAnalyzer = new ClusterGraphAnalyzer();

		/// <summary>
		/// 校验一张地图。
		/// </summary>
		/// <param name="graph">抽象图</param>
		/// <returns>结构化报告</returns>
		public MapValidationReport Validate(IMapGraph graph)
		{
			MapValidationReport report = new MapValidationReport();
			report.NodeCount = graph.NodeCount;

			if (graph.NodeCount <= 0) return report;

			int start = graph.GetStartNode();
			int end = graph.GetEndNode();

			// ===== 静态层 =====
			bool[] reachable = ForwardReach(graph, start);
			bool[] canReachGoal = BackwardReach(graph, end);

			int reachableCount = 0;
			int canReachCount = 0;
			for (int i = 0; i < graph.NodeCount; i++)
			{
				if (reachable[i]) reachableCount++;
				if (canReachGoal[i]) canReachCount++;
			}
			report.ReachableCount = reachableCount;
			report.CanReachGoalCount = canReachCount;

			// 边数
			int edgeCount = 0;
			for (int i = 0; i < graph.NodeCount; i++)
			{
				edgeCount += graph.GetOutgoingEdges(i).Count;
			}
			report.EdgeCount = edgeCount;

			// 1. 通不了关
			if (end >= 0 && end < graph.NodeCount && !reachable[end])
			{
				report.AddIssue(new MapIssue(MapIssueType.UnreachableGoal, end, graph.GetNodeLabel(end),
					"从起点走不到终点（BOSS）"));
			}

			// 2. 软锁：进得去但出不来
			List<int> softLockNodes = new List<int>();
			for (int i = 0; i < graph.NodeCount; i++)
			{
				if (graph.IsBlocking(i)) continue;
				if (i == end) continue;
				if (!reachable[i]) continue;      // 进不去的归「不可达区域」
				if (canReachGoal[i]) continue;     // 出得来的没问题
				softLockNodes.Add(i);
			}
			AddIssues(report, graph, MapIssueType.SoftLock, softLockNodes,
				"从这里进去之后就再也到不了终点（软锁）");

			// 3. 不可达区域
			List<int> unreachableNodes = new List<int>();
			for (int i = 0; i < graph.NodeCount; i++)
			{
				if (graph.IsBlocking(i)) continue;
				if (!reachable[i]) unreachableNodes.Add(i);
			}
			AddIssues(report, graph, MapIssueType.UnreachableRegion, unreachableNodes,
				"从起点永远走不到这里（不可达区域）");

			// 4. 关键格不可达
			List<int> requiredNodes = new List<int>(graph.GetRequiredNodes());
			List<int> badRequired = new List<int>();
			foreach (int r in requiredNodes)
			{
				if (r < 0 || r >= graph.NodeCount) continue;
				if (!reachable[r]) badRequired.Add(r);
			}
			AddIssues(report, graph, MapIssueType.RequiredUnreachable, badRequired,
				"关键格从起点不可达");

			// 5. 死路：出度 1 且唯一出边是单向
			List<int> deadEnds = new List<int>();
			for (int i = 0; i < graph.NodeCount; i++)
			{
				if (graph.IsBlocking(i)) continue;

				IReadOnlyList<int> outs = graph.GetOutgoingEdges(i);
				if (outs.Count != 1) continue;

				if (graph.GetEdgeKind(i, outs[0]) == MapEdgeKind.OneWay)
				{
					deadEnds.Add(i);
				}
			}
			AddIssues(report, graph, MapIssueType.DeadEnd, deadEnds,
				"唯一的出路是单向的，进去就是死胡同");

			// 6. 孤岛 / 多簇（弱连通分量）
			CheckIslands(graph, start, end, report);

			// ===== 簇图层：旁支必需格不可达 =====
			ClusterGraphAnalyzer.Result clusters = _clusterAnalyzer.Analyze(graph);
			report.ClusterCount = clusters.ClusterCount;

			List<int> branchRequired = new List<int>();
			foreach (int r in requiredNodes)
			{
				if (r < 0 || r >= graph.NodeCount) continue;

				int c = clusters.ClusterOf[r];
				if (c < 0 || c >= clusters.SafeCluster.Length) continue;
				if (!clusters.SafeCluster[c]) branchRequired.Add(r);
			}
			AddIssues(report, graph, MapIssueType.BranchRequiredUnreachable, branchRequired,
				"关键格落在只能单向进入的旁支簇里，拐进去就回不来");

			// ===== 状态空间层：有洞时必做 =====
			HashSet<int> consumables = new HashSet<int>(graph.GetConsumableNodes());
			if (consumables.Count > 0)
			{
				StateSearchResult stateResult = SearchStateSpace(graph, consumables, requiredNodes);
				report.ExploredStateCount = stateResult.ExploredStates;
				report.CanCompleteGoal = stateResult.CanComplete;

				if (!stateResult.CanComplete)
				{
					report.AddIssue(new MapIssue(MapIssueType.StateDeadlock, -1, "",
						$"消耗型格子（{consumables.Count} 个）用掉之后找不到能通关的走法"
						+ $"（已枚举 {stateResult.ExploredStates} 个状态）"));
				}
			}
			else
			{
				report.CanCompleteGoal = end >= 0 && end < graph.NodeCount && reachable[end];
			}

			return report;
		}

		// ===== 静态 BFS =====

		/// <summary>正向可达集（从起点出发）</summary>
		public static bool[] ForwardReach(IMapGraph graph, int start)
		{
			bool[] seen = new bool[graph.NodeCount];
			if (start < 0 || start >= graph.NodeCount) return seen;

			Queue<int> queue = new Queue<int>();
			queue.Enqueue(start);
			seen[start] = true;

			while (queue.Count > 0)
			{
				int cur = queue.Dequeue();
				IReadOnlyList<int> outs = graph.GetOutgoingEdges(cur);
				for (int i = 0; i < outs.Count; i++)
				{
					int v = outs[i];
					if (v < 0 || v >= graph.NodeCount) continue;
					if (graph.IsBlocking(v)) continue;
					if (seen[v]) continue;

					seen[v] = true;
					queue.Enqueue(v);
				}
			}

			return seen;
		}

		/// <summary>反向可达集（能到终点的节点）</summary>
		public static bool[] BackwardReach(IMapGraph graph, int end)
		{
			bool[] seen = new bool[graph.NodeCount];
			if (end < 0 || end >= graph.NodeCount) return seen;

			Queue<int> queue = new Queue<int>();
			queue.Enqueue(end);
			seen[end] = true;

			while (queue.Count > 0)
			{
				int cur = queue.Dequeue();
				IReadOnlyList<int> ins = graph.GetIncomingEdges(cur);
				for (int i = 0; i < ins.Count; i++)
				{
					int v = ins[i];
					if (v < 0 || v >= graph.NodeCount) continue;
					if (graph.IsBlocking(v)) continue;
					if (seen[v]) continue;

					seen[v] = true;
					queue.Enqueue(v);
				}
			}

			return seen;
		}

		// ===== 孤岛判定 =====

		private void CheckIslands(IMapGraph graph, int start, int end, MapValidationReport report)
		{
			int n = graph.NodeCount;
			int[] parent = new int[n];
			for (int i = 0; i < n; i++) parent[i] = i;

			// 所有边都当无向边合并（弱连通）
			for (int u = 0; u < n; u++)
			{
				if (graph.IsBlocking(u)) continue;

				IReadOnlyList<int> outs = graph.GetOutgoingEdges(u);
				for (int i = 0; i < outs.Count; i++)
				{
					int v = outs[i];
					if (v < 0 || v >= n || graph.IsBlocking(v)) continue;
					Union(parent, u, v);
				}
			}

			Dictionary<int, List<int>> components = new Dictionary<int, List<int>>();
			for (int i = 0; i < n; i++)
			{
				if (graph.IsBlocking(i)) continue;

				int root = Find(parent, i);
				List<int> list;
				if (!components.TryGetValue(root, out list))
				{
					list = new List<int>();
					components[root] = list;
				}
				list.Add(i);
			}

			if (components.Count <= 1) return;

			List<int> isolated = new List<int>();
			foreach (KeyValuePair<int, List<int>> kv in components)
			{
				List<int> nodes = kv.Value;
				bool hasStart = nodes.Contains(start);
				bool hasEnd = nodes.Contains(end);
				if (hasStart && hasEnd) continue;

				isolated.AddRange(nodes);
			}

			if (isolated.Count > 0)
			{
				AddIssues(report, graph, MapIssueType.IsolatedIsland, isolated,
					"这块区域与主线完全断开（孤岛）");
			}

			report.AddIssue(new MapIssue(MapIssueType.MultiCluster, -1, "",
				$"地图被切成了 {components.Count} 块（弱连通分量）"));
		}

		// ===== 状态空间搜索 =====

		/// <summary>状态空间搜索的结果</summary>
		public class StateSearchResult
		{
			/// <summary>是否存在能通关（且访问了全部关键格）的走法</summary>
			public bool CanComplete;

			/// <summary>枚举过的状态数</summary>
			public int ExploredStates;
		}

		/// <summary>
		/// 状态空间搜索 —— 判定「消耗型格子（洞）用掉之后还能不能通关」。
		///
		/// 状态 = (当前位置, 已消耗洞集合, 进入当前洞的来源格, 已访问关键格集合)
		///
		/// 转移规则（与主人确认的时序一致）：
		///   普通格 x → 走到未消耗的洞 D：进入 D，记下来源 x，**禁止退回 x**，D 尚未消耗
		///   在洞 D 内 → 走到 D 的其他邻格 z：**D 被消耗（删除）**
		///   在洞 D 内 → 退回来源格：**禁止**
		///   任意 → 走到已消耗的洞：**不可走**（格子已删除）
		///
		/// 洞数 k ≤ 3 时状态数 = N × 2^k × 常数 × 2^required，完全可枚举。
		/// </summary>
		public StateSearchResult SearchStateSpace(IMapGraph graph, HashSet<int> consumables, List<int> requiredNodes)
		{
			StateSearchResult result = new StateSearchResult();
			result.CanComplete = false;

			int end = graph.GetEndNode();
			int start = graph.GetStartNode();
			if (start < 0 || start >= graph.NodeCount) return result;

			// 洞 → 位索引
			List<int> holeList = new List<int>(consumables);
			Dictionary<int, int> holeBit = new Dictionary<int, int>();
			for (int i = 0; i < holeList.Count; i++)
			{
				holeBit[holeList[i]] = i;
			}

			// required → 位索引
			Dictionary<int, int> requiredBit = new Dictionary<int, int>();
			for (int i = 0; i < requiredNodes.Count; i++)
			{
				requiredBit[requiredNodes[i]] = i;
			}

			int requiredMask = (1 << requiredNodes.Count) - 1;

			// 状态：(pos, consumedMask, sourceId, visitedMask)
			HashSet<long> visited = new HashSet<long>();
			Queue<long[]> queue = new Queue<long[]>();

			// 起点本身可能就是洞（不太可能，但按规则处理）
			int startVisited = 0;
			if (requiredBit.ContainsKey(start)) startVisited |= 1 << requiredBit[start];

			queue.Enqueue(new long[] { start, 0, -1, startVisited });
			visited.Add(Pack(start, 0, -1, startVisited));

			while (queue.Count > 0)
			{
				long[] state = queue.Dequeue();
				int pos = (int)state[0];
				int consumedMask = (int)state[1];
				int sourceId = (int)state[2];
				int visitedMask = (int)state[3];

				// 终止判定：站在终点，且关键格全部访问过
				if (pos == end && (visitedMask & requiredMask) == requiredMask)
				{
					result.CanComplete = true;
					result.ExploredStates = visited.Count;
					return result;
				}

				bool posIsHole = holeBit.ContainsKey(pos);
				int posHoleBit = posIsHole ? holeBit[pos] : -1;

				IReadOnlyList<int> outs = graph.GetOutgoingEdges(pos);
				for (int i = 0; i < outs.Count; i++)
				{
					int z = outs[i];
					if (z < 0 || z >= graph.NodeCount) continue;
					if (graph.IsBlocking(z)) continue;

					bool zIsHole = holeBit.ContainsKey(z);
					int zHoleBit = zIsHole ? holeBit[z] : -1;

					// 已消耗的洞 = 格子已删除，不可走
					if (zIsHole && (consumedMask & (1 << zHoleBit)) != 0) continue;

					int nextConsumed = consumedMask;
					int nextSource = -1;

					if (posIsHole)
					{
						// 在洞内：不能退回进入来源格
						if (z == sourceId) continue;

						// 离开洞 → 该洞被消耗
						nextConsumed = consumedMask | (1 << posHoleBit);

						// 若走进另一个洞，来源格 = 刚离开的这个洞格子
						nextSource = zIsHole ? pos : -1;
					}
					else
					{
						// 普通格 → 走进未消耗的洞：进入它，来源 = 当前格
						nextSource = zIsHole ? pos : -1;
					}

					int nextVisited = visitedMask;
					if (requiredBit.ContainsKey(z)) nextVisited |= 1 << requiredBit[z];

					long key = Pack(z, nextConsumed, nextSource, nextVisited);
					if (visited.Add(key))
					{
						queue.Enqueue(new long[] { z, nextConsumed, nextSource, nextVisited });
					}
				}
			}

			result.ExploredStates = visited.Count;
			return result;
		}

		/// <summary>把状态打包成一个 long（用于哈希集合）</summary>
		private static long Pack(int pos, int consumedMask, int sourceId, int visitedMask)
		{
			long v = (long)(uint)pos;
			v |= (long)(uint)consumedMask << 16;
			v |= (long)(uint)(sourceId + 1) << 32;
			v |= (long)(uint)visitedMask << 40;
			return v;
		}

		// ===== 工具 =====

		private void AddIssues(MapValidationReport report, IMapGraph graph, MapIssueType type,
			List<int> nodes, string reason)
		{
			if (nodes == null || nodes.Count == 0) return;

			int listed = nodes.Count <= MaxIssuesPerType ? nodes.Count : MaxIssuesPerType;
			for (int i = 0; i < listed; i++)
			{
				int node = nodes[i];
				report.AddIssue(new MapIssue(type, node, graph.GetNodeLabel(node), reason));
			}

			if (nodes.Count > listed)
			{
				report.AddIssue(new MapIssue(type, -1, "",
					$"{reason}（同类问题共 {nodes.Count} 处，此处只列出前 {listed} 处）"));
			}
		}

		private static int Find(int[] parent, int x)
		{
			while (parent[x] != x)
			{
				parent[x] = parent[parent[x]];
				x = parent[x];
			}
			return x;
		}

		private static void Union(int[] parent, int a, int b)
		{
			int ra = Find(parent, a);
			int rb = Find(parent, b);
			if (ra == rb) return;

			if (ra < rb) parent[rb] = ra;
			else parent[ra] = rb;
		}
	}
}
