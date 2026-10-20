using System.Collections.Generic;
using FishEatFish.Battle.HexMap;

namespace MapValidation
{
	/// <summary>
	/// 适配器：把项目的六边形地图 <see cref="HexMap"/> 包装成 <see cref="IMapGraph"/>。
	///
	/// **只有这个文件依赖项目内的地图类型** —— 其余文件（IMapGraph / MapValidator /
	/// ClusterGraphAnalyzer / MapValidationReport / MapIssue / MapIssueType / MapEdgeKind）
	/// 整目录拷走即可复用。
	///
	/// 边的构造规则：
	///   - **邻接**：六边形相邻格，双向
	///   - **双向传送**：两格之间两条有向边（kind = TwoWay，缩簇时算连接边）
	///   - **单向传送**：只有「入口 → 出口」一条有向边（kind = OneWay，缩簇时不算连接边）
	///   - **洞**（消耗型）：节点被标为 consumable，消耗后格子消失
	/// </summary>
	public class HexMapGraph : IMapGraph
	{
		private readonly HexMap _map;
		private readonly Dictionary<HexCoord, int> _coordToId = new Dictionary<HexCoord, int>();
		private readonly List<HexCoord> _idToCoord = new List<HexCoord>();
		private readonly List<List<int>> _outgoing = new List<List<int>>();
		private readonly List<List<int>> _incoming = new List<List<int>>();
		private readonly Dictionary<long, MapEdgeKind> _edgeKinds = new Dictionary<long, MapEdgeKind>();
		private readonly HashSet<int> _consumables = new HashSet<int>();
		private readonly HashSet<int> _required = new HashSet<int>();

		/// <summary>
		/// 构造。
		/// </summary>
		/// <param name="map">六边形地图</param>
		/// <param name="requiredCoords">
		/// 必经格坐标；不传时默认为「终点（BOSS）」。
		/// </param>
		public HexMapGraph(HexMap map, IEnumerable<HexCoord> requiredCoords = null)
		{
			_map = map;
			if (_map == null) return;

			BuildNodes();
			BuildEdges();
			MarkConsumables();

			if (requiredCoords != null)
			{
				foreach (HexCoord c in requiredCoords)
				{
					int id = IdOf(c);
					if (id >= 0) _required.Add(id);
				}
			}
			else
			{
				int endId = IdOf(_map.End);
				if (endId >= 0) _required.Add(endId);
			}
		}

		private void BuildNodes()
		{
			foreach (HexTile tile in _map.GetAllTiles())
			{
				if (_coordToId.ContainsKey(tile.Coord)) continue;

				_idToCoord.Add(tile.Coord);
				_coordToId[tile.Coord] = _idToCoord.Count - 1;
			}

			for (int i = 0; i < _idToCoord.Count; i++)
			{
				_outgoing.Add(new List<int>());
				_incoming.Add(new List<int>());
			}
		}

		private void BuildEdges()
		{
			foreach (HexTile tile in _map.GetAllTiles())
			{
				int from = IdOf(tile.Coord);
				if (from < 0) continue;

				// 邻接边（双向）
				foreach (HexTile neighbor in _map.GetNeighbors(tile.Coord))
				{
					int to = IdOf(neighbor.Coord);
					if (to < 0 || to == from) continue;

					AddEdge(from, to, MapEdgeKind.Normal);
					AddEdge(to, from, MapEdgeKind.Normal);
				}

				// 传送边
				if (string.IsNullOrEmpty(tile.TeleportPairId)) continue;

				// GetPairedTeleport 找不到配对时返回自己
				HexCoord paired = _map.GetPairedTeleport(tile.Coord, tile.TeleportPairId);
				if (paired == tile.Coord) continue;

				int target = IdOf(paired);
				if (target < 0) continue;

				if (tile.EventType == HexEventType.TwoWayTeleport)
				{
					// 双向传送：两格之间互相可达
					AddEdge(from, target, MapEdgeKind.TwoWay);
					AddEdge(target, from, MapEdgeKind.TwoWay);
				}
				else if (tile.EventType == HexEventType.OneDirectionTele && tile.TeleportRole == TeleportRole.Entry)
				{
					// 单向传送：只有入口 → 出口
					AddEdge(from, target, MapEdgeKind.OneWay);
				}
			}
		}

		private void MarkConsumables()
		{
			foreach (HexTile tile in _map.GetAllTiles())
			{
				if (tile.EventType != HexEventType.Hole) continue;

				int id = IdOf(tile.Coord);
				if (id >= 0) _consumables.Add(id);
			}
		}

		private void AddEdge(int from, int to, MapEdgeKind kind)
		{
			if (from < 0 || to < 0) return;
			if (from == to) return;

			if (!_outgoing[from].Contains(to)) _outgoing[from].Add(to);
			if (!_incoming[to].Contains(from)) _incoming[to].Add(from);

			// 边的性质：同向同源同目标的边，取「更单向」的那种（OneWay 优先）
			long key = EdgeKey(from, to);
			MapEdgeKind existing;
			if (!_edgeKinds.TryGetValue(key, out existing) || kind == MapEdgeKind.OneWay)
			{
				_edgeKinds[key] = kind;
			}
		}

		private static long EdgeKey(int from, int to)
		{
			return ((long)from << 32) | (uint)to;
		}

		private int IdOf(HexCoord coord)
		{
			int id;
			return _coordToId.TryGetValue(coord, out id) ? id : -1;
		}

		// ===== IMapGraph 实现 =====

		public int NodeCount => _idToCoord.Count;

		public int GetStartNode() => IdOf(_map?.Start ?? default(HexCoord));

		public int GetEndNode() => IdOf(_map?.End ?? default(HexCoord));

		public IReadOnlyList<int> GetOutgoingEdges(int node)
		{
			if (node < 0 || node >= _outgoing.Count) return new List<int>();
			return _outgoing[node];
		}

		public IReadOnlyList<int> GetIncomingEdges(int node)
		{
			if (node < 0 || node >= _incoming.Count) return new List<int>();
			return _incoming[node];
		}

		public string GetNodeLabel(int node)
		{
			if (node < 0 || node >= _idToCoord.Count) return "?";
			return _idToCoord[node].ToString();
		}

		public bool IsBlocking(int node)
		{
			if (node < 0 || node >= _idToCoord.Count) return true;

			HexTile tile = _map.GetTile(_idToCoord[node]);
			if (tile == null) return true;

			// 已消失的格子（洞被消耗后）不可通行
			return tile.IsDisappeared;
		}

		public IReadOnlyCollection<int> GetConsumableNodes() => _consumables;

		public IReadOnlyCollection<int> GetRequiredNodes() => _required;

		public MapEdgeKind GetEdgeKind(int from, int to)
		{
			MapEdgeKind kind;
			return _edgeKinds.TryGetValue(EdgeKey(from, to), out kind) ? kind : MapEdgeKind.Normal;
		}
	}
}
