using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FishEatFish.Battle.HexMap
{
	/// <summary>
	/// 地图簇结构生成器（D12）。
	///
	/// **簇（cluster）= 互不连通的几块区域**。参考图 R-05 / R-06 里地图不是一整块，
	/// 而是几块分离的六边形区域，中间夹着黑色空隙，靠「隧道」/「→」跨过去。
	///
	/// 关键性质（与 D13 的簇图分析口径一致）：
	///   - 把**洞节点**移除后，在「邻接边 + **双向**传送超边」下的连通分量 = 簇
	///   - **单向门不算连接边**（它是有向的）→ 单向门两侧属于不同的簇
	///   - 洞也不算（消耗型的，用掉即断）
	///
	/// 生成方式：每簇用**随机游走生长**出一片连通格子；
	/// 生长时**禁止与已存在的其他簇相邻** → 天然满足「任意两簇之间不存在相邻格子对」。
	/// 簇之间靠**传送门**连接（不是靠留空隙）。
	/// </summary>
	public class ClusterBuilder
	{
		private readonly Random _random;

		/// <summary>每个格子属于哪个簇（-1 = 不在图上）</summary>
		public Dictionary<HexCoord, int> ClusterOf = new Dictionary<HexCoord, int>();

		/// <summary>各簇包含的格子</summary>
		public List<HashSet<HexCoord>> Clusters = new List<HashSet<HexCoord>>();

		/// <summary>各簇的「窄口」格子（两块子区域之间唯一的连接 = 割点，洞就放这里）</summary>
		public Dictionary<int, HexCoord> Bottlenecks = new Dictionary<int, HexCoord>();

		public ClusterBuilder(int? seed = null)
		{
			_random = seed.HasValue ? new Random(seed.Value) : new Random();
		}

		/// <summary>
		/// 生成 N 个互不连通的簇。
		/// </summary>
		/// <param name="radius">网格半径（决定可用范围）</param>
		/// <param name="clusterCount">簇数（1 = 整块，不分簇）</param>
		/// <param name="totalTiles">目标总格数</param>
		/// <returns>格子字典（值为空事件类型）</returns>
		public Dictionary<HexCoord, HexTile> Build(int radius, int clusterCount, int totalTiles)
		{
			// 簇之间要留**隔离带**（否则长不开），预留出来再分给各簇
			const int isolationPerGap = 4;

			int count = Mathf.Max(1, clusterCount);
			int perCluster = Mathf.Max(3, (totalTiles - (count - 1) * isolationPerGap) / count);

			// 生长可能失败（空间不够 / 中心太近）→ 换一组中心重试
			for (int attempt = 0; attempt < 12; attempt++)
			{
				ClusterOf.Clear();
				Clusters.Clear();

				int side = radius * 2 + 1;
				List<HexCoord> centers = PickClusterCenters(side, count);

				bool allGrown = true;
				for (int c = 0; c < count; c++)
				{
					HashSet<HexCoord> cluster = GrowCluster(centers[c], perCluster, side, c, out HexCoord bottleneck);
					Clusters.Add(cluster);

					if (cluster.Count < 3) allGrown = false;
					if (bottleneck != default) Bottlenecks[c] = bottleneck;

					// 关键：**立刻登记**这一簇的格子 ——
					// 否则生长下一个簇时看不到已存在的簇，「禁止跨簇相邻」就会失效。
					foreach (HexCoord coord in cluster)
					{
						ClusterOf[coord] = c;
					}
				}

				if (allGrown)
				{
					Dictionary<HexCoord, HexTile> tiles = new Dictionary<HexCoord, HexTile>();
					for (int c = 0; c < Clusters.Count; c++)
					{
						foreach (HexCoord coord in Clusters[c])
						{
							tiles[coord] = new HexTile(coord, HexEventType.Empty);
							ClusterOf[coord] = c;
						}
					}

					GD.Print($"[ClusterBuilder] 生成 {Clusters.Count} 个簇，格数分布: "
						+ string.Join(", ", Clusters.ConvertAll(c => c.Count.ToString())));

					return tiles;
				}
			}

			GD.Print("[ClusterBuilder] 有簇生长失败，退回单块布局");
			ClusterOf.Clear();
			Clusters.Clear();
			return BuildSingle(radius, totalTiles);
		}

		/// <summary>整块布局（clusters = 1 时的等价路径）</summary>
		private Dictionary<HexCoord, HexTile> BuildSingle(int radius, int totalTiles)
		{
			ClusterOf.Clear();
			Clusters.Clear();

			int side = radius * 2 + 1;
			Dictionary<HexCoord, HexTile> tiles = new Dictionary<HexCoord, HexTile>();

			for (int col = 0; col < side; col++)
			{
				for (int row = 0; row < side; row++)
				{
					HexCoord coord = HexCoord.FromOffset(col, row);
					tiles[coord] = new HexTile(coord, HexEventType.Empty);
					ClusterOf[coord] = 0;
				}
			}

			Clusters.Add(new HashSet<HexCoord>(tiles.Keys));
			return tiles;
		}

		/// <summary>挑 N 个彼此拉开的随机中心</summary>
		private List<HexCoord> PickClusterCenters(int side, int count)
		{
			List<HexCoord> centers = new List<HexCoord>();
			int attempts = 0;
			int maxAttempts = 200;

			while (centers.Count < count && attempts < maxAttempts)
			{
				attempts++;
				HexCoord candidate = HexCoord.FromOffset(
					_random.Next(0, side),
					_random.Next(0, side));

				// 中心之间至少隔开 2 格，否则簇会长到一起
				bool tooClose = false;
				foreach (HexCoord c in centers)
				{
					if (c.DistanceTo(candidate) < 2)
					{
						tooClose = true;
						break;
					}
				}

				if (tooClose) continue;
				centers.Add(candidate);
			}

			// 补足（空间实在不够就允许重叠，后面生长时会自然分开）
			while (centers.Count < count)
			{
				centers.Add(HexCoord.FromOffset(_random.Next(0, side), _random.Next(0, side)));
			}

			return centers;
		}

		/// <summary>
		/// 生成一个簇。
		///
		/// **结构 = 两块子区域 + 一个「窄口」格子**：
		/// 窄口是两块子区域之间**唯一的连接** → 它在邻接图上是**割点**。
		/// 把洞放在窄口上，玩家消耗掉它之后，这一簇就真的裂成两块（D12 的核心机制）。
		///
		/// 注意：单纯用「随机游走」长出来的簇内部全是环，一个割点都没有 ——
		/// 所以必须显式造出窄口。
		/// </summary>
		/// <param name="bottleneck">输出：窄口格子的坐标</param>
		private HashSet<HexCoord> GrowCluster(HexCoord center, int targetSize, int side, int clusterId,
			out HexCoord bottleneck)
		{
			HashSet<HexCoord> cluster = new HashSet<HexCoord>();
			List<HexCoord> frontier = new List<HexCoord>();
			bottleneck = default;

			if (IsInBounds(center, side))
			{
				cluster.Add(center);
				frontier.Add(center);
			}

			// ---------- 阶段 1：长出第一块子区域 ----------
			int firstHalf = Mathf.Max(2, targetSize / 2);
			GrowPart(cluster, frontier, firstHalf, side, clusterId, null);

			HashSet<HexCoord> firstHalfSet = new HashSet<HexCoord>(cluster);

			// ---------- 阶段 2：找一个「窄口」 ----------
			// 窄口 = 与第一块相邻、但不属于第一块的空格；优先选「与第一块只相邻 1 个格」的
			HashSet<HexCoord> candidates = new HashSet<HexCoord>();
			foreach (HexCoord c in firstHalfSet)
			{
				foreach (HexCoord n in c.GetNeighbors())
				{
					if (!IsInBounds(n, side)) continue;
					if (cluster.Contains(n)) continue;
					if (IsAdjacentToOtherCluster(n, clusterId)) continue;

					candidates.Add(n);
				}
			}

			HexCoord? chosen = null;
			int bestTouch = int.MaxValue;
			foreach (HexCoord c in candidates)
			{
				int touch = 0;
				foreach (HexCoord n in c.GetNeighbors())
				{
					if (firstHalfSet.Contains(n)) touch++;
				}

				if (touch < bestTouch)
				{
					bestTouch = touch;
					chosen = c;
				}
			}

			if (!chosen.HasValue)
			{
				// 没有可用窄口 → 整簇退化成一个连通块（不会有割点）
				return cluster;
			}

			bottleneck = chosen.Value;
			cluster.Add(bottleneck);

			// ---------- 阶段 3：从窄口长出第二块子区域 ----------
			// 第二块的格子**不能与第一块相邻**（只能通过窄口连），否则窄口就不是割点了
			List<HexCoord> secondFrontier = new List<HexCoord> { bottleneck };
			GrowPart(cluster, secondFrontier, targetSize - cluster.Count, side, clusterId, firstHalfSet);

			return cluster;
		}

		/// <summary>
		/// 从 frontier 出发随机游走生长，直到达到目标数量。
		/// </summary>
		/// <param name="forbidden">这些格子不许直接相邻（用于保证第二块只能通过窄口连）</param>
		private void GrowPart(HashSet<HexCoord> cluster, List<HexCoord> frontier, int targetCount,
			int side, int clusterId, HashSet<HexCoord> forbidden)
		{
			int attempts = 0;
			int maxAttempts = Mathf.Max(16, targetCount * 40);

			while (cluster.Count < targetCount && frontier.Count > 0 && attempts < maxAttempts)
			{
				attempts++;

				int index = _random.Next(frontier.Count);
				HexCoord current = frontier[index];
				HexCoord direction = HexCoord.Directions[_random.Next(HexCoord.Directions.Length)];
				HexCoord next = current + direction;

				if (!IsInBounds(next, side)) continue;
				if (cluster.Contains(next)) continue;

				// 关键：不能与**别的簇**相邻
				if (IsAdjacentToOtherCluster(next, clusterId)) continue;

				// 不能与 forbidden 集合直接相邻（否则会绕过窄口连上）
				if (forbidden != null)
				{
					bool touchesForbidden = false;
					foreach (HexCoord n in next.GetNeighbors())
					{
						if (forbidden.Contains(n)) { touchesForbidden = true; break; }
					}
					if (touchesForbidden) continue;
				}

				cluster.Add(next);
				frontier.Add(next);
			}
		}

		/// <summary>该格是否与某个「非本簇」的格子相邻</summary>
		private bool IsAdjacentToOtherCluster(HexCoord coord, int ownClusterId)
		{
			foreach (HexCoord neighbor in coord.GetNeighbors())
			{
				int owner;
				if (!ClusterOf.TryGetValue(neighbor, out owner)) continue;
				if (owner != ownClusterId) return true;
			}
			return false;
		}

		/// <summary>该格所属的簇 id（-1 = 不在图上）</summary>
		public int GetClusterOf(HexCoord coord)
		{
			int id;
			return ClusterOf.TryGetValue(coord, out id) ? id : -1;
		}

		/// <summary>某簇的全部格子</summary>
		public List<HexCoord> GetClusterTiles(int clusterId)
		{
			List<HexCoord> result = new List<HexCoord>();
			if (clusterId < 0 || clusterId >= Clusters.Count) return result;

			result.AddRange(Clusters[clusterId]);
			result.Sort((a, b) =>
			{
				int byQ = a.Q.CompareTo(b.Q);
				return byQ != 0 ? byQ : a.R.CompareTo(b.R);
			});
			return result;
		}

		/// <summary>两个格子是否属于不同簇</summary>
		public bool IsCrossCluster(HexCoord a, HexCoord b)
		{
			int ca = GetClusterOf(a);
			int cb = GetClusterOf(b);
			return ca >= 0 && cb >= 0 && ca != cb;
		}

		/// <summary>簇数</summary>
		public int ClusterCount => Clusters.Count;

		private static bool IsInBounds(HexCoord coord, int side)
		{
			(int col, int row) = coord.ToOffset();
			return col >= 0 && col < side && row >= 0 && row < side;
		}
	}
}
