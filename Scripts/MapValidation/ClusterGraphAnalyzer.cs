using System.Collections.Generic;

namespace MapValidation
{
	/// <summary>
	/// 簇图分析 —— **这是地图机制（多簇 + 洞消耗分裂）的原生模型**，不只是预检。
	///
	/// 建模：
	///   1. 移除所有**洞节点**（消耗型，用掉即断）
	///   2. 缩簇：只把「邻接边 + **双向**传送超边」算作连接边
	///      - 单向门**不算**连接边（它是有向的）
	///      - 洞**不算**（消耗型的）
	///   3. 簇图的边有三种：双向传送门（双向、可重复用）、单向门（单向、可重复用）、洞（单向、不可重复用）
	///   4. 簇图上做**有向 BFS**：正向 from 起点簇、反向 from 终点簇
	///      → 「能到的簇」∩ 「能到终点的簇」= 安全簇
	///      → required 落在安全簇之外 = **旁支必需格不可达**（拐进去就回不来）
	///
	/// 这给出**乐观上界**（假设洞都还在），用于快速筛掉肯定不通的情况；
	/// 真正的判定由状态空间搜索兜底。
	/// </summary>
	public class ClusterGraphAnalyzer
	{
		/// <summary>分析结果</summary>
		public class Result
		{
			/// <summary>每个节点所属的簇 id</summary>
			public int[] ClusterOf = new int[0];

			/// <summary>簇数量</summary>
			public int ClusterCount;

			/// <summary>从起点簇能到达的簇</summary>
			public bool[] ReachableFromStart = new bool[0];

			/// <summary>能到达终点簇的簇</summary>
			public bool[] CanReachGoal = new bool[0];

			/// <summary>落在「能到 ∩ 能到终点」里的簇（安全簇）</summary>
			public bool[] SafeCluster = new bool[0];
		}

		/// <summary>
		/// 计算簇划分与簇图可达性。
		/// </summary>
		/// <param name="graph">抽象图</param>
		/// <returns>分析结果</returns>
		public Result Analyze(IMapGraph graph)
		{
			int n = graph.NodeCount;
			Result result = new Result
			{
				ClusterOf = new int[n]
			};

			// 洞集合（消耗型节点）
			HashSet<int> consumables = new HashSet<int>(graph.GetConsumableNodes());

			// ===== 1. 缩簇：并查集，只合并「邻接边 + 双向传送边」 =====
			int[] parent = new int[n];
			for (int i = 0; i < n; i++) parent[i] = i;

			for (int u = 0; u < n; u++)
			{
				if (graph.IsBlocking(u) || consumables.Contains(u)) continue;

				IReadOnlyList<int> outs = graph.GetOutgoingEdges(u);
				for (int i = 0; i < outs.Count; i++)
				{
					int v = outs[i];
					if (v < 0 || v >= n) continue;
					if (graph.IsBlocking(v) || consumables.Contains(v)) continue;

					// 只有「双向边」才算连接边：邻接(Normal) 与 双向传送(TwoWay)
					MapEdgeKind kind = graph.GetEdgeKind(u, v);
					if (kind == MapEdgeKind.OneWay) continue;

					Union(parent, u, v);
				}
			}

			// 把并查集根映射成连续的簇 id
			Dictionary<int, int> rootToCluster = new Dictionary<int, int>();
			for (int i = 0; i < n; i++)
			{
				if (graph.IsBlocking(i)) continue;

				int root = Find(parent, i);
				int cluster;
				if (!rootToCluster.TryGetValue(root, out cluster))
				{
					cluster = rootToCluster.Count;
					rootToCluster[root] = cluster;
				}
				result.ClusterOf[i] = cluster;
			}
			result.ClusterCount = rootToCluster.Count;

			// ===== 2. 簇图的有向 BFS =====
			int clusterCount = result.ClusterCount;
			result.ReachableFromStart = new bool[clusterCount];
			result.CanReachGoal = new bool[clusterCount];
			result.SafeCluster = new bool[clusterCount];

			// 簇图邻接表（有向）
			List<List<int>> clusterOut = new List<List<int>>(clusterCount);
			List<List<int>> clusterIn = new List<List<int>>(clusterCount);
			for (int i = 0; i < clusterCount; i++)
			{
				clusterOut.Add(new List<int>());
				clusterIn.Add(new List<int>());
			}

			for (int u = 0; u < n; u++)
			{
				if (graph.IsBlocking(u)) continue;

				int cu = result.ClusterOf[u];
				IReadOnlyList<int> outs = graph.GetOutgoingEdges(u);
				for (int i = 0; i < outs.Count; i++)
				{
					int v = outs[i];
					if (v < 0 || v >= n || graph.IsBlocking(v)) continue;

					int cv = result.ClusterOf[v];
					if (cu == cv) continue;
					if (!clusterOut[cu].Contains(cv)) clusterOut[cu].Add(cv);
					if (!clusterIn[cv].Contains(cu)) clusterIn[cv].Add(cu);
				}
			}

			// 正向：起点簇能到哪些簇
			int startCluster = ClusterOfSafe(result, graph.GetStartNode());
			if (startCluster >= 0)
			{
				MarkClusters(clusterOut, startCluster, result.ReachableFromStart);
			}

			// 反向：哪些簇能到终点簇
			int endCluster = ClusterOfSafe(result, graph.GetEndNode());
			if (endCluster >= 0)
			{
				MarkClusters(clusterIn, endCluster, result.CanReachGoal);
			}

			for (int i = 0; i < clusterCount; i++)
			{
				result.SafeCluster[i] = result.ReachableFromStart[i] && result.CanReachGoal[i];
			}

			return result;
		}

		private static int ClusterOfSafe(Result result, int node)
		{
			if (node < 0 || node >= result.ClusterOf.Length) return -1;
			return result.ClusterOf[node];
		}

		private static void MarkClusters(List<List<int>> adjacency, int start, bool[] marks)
		{
			if (start < 0 || start >= marks.Length) return;

			Stack<int> stack = new Stack<int>();
			stack.Push(start);
			marks[start] = true;

			while (stack.Count > 0)
			{
				int cur = stack.Pop();
				List<int> next = adjacency[cur];
				for (int i = 0; i < next.Count; i++)
				{
					int c = next[i];
					if (!marks[c])
					{
						marks[c] = true;
						stack.Push(c);
					}
				}
			}
		}

		// ===== 并查集 =====

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
