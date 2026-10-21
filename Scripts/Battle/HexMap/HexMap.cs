using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FishEatFish.Battle.HexMap
{
    public partial class HexMap
    {
        private readonly Dictionary<HexCoord, HexTile> _tiles;
        private readonly List<HexCoord> _voidCells;
        public HexCoord Start { get; }
        public HexCoord End { get; }
        public int PlayerLevel { get; }

        /// <summary>
        /// 被挖掉的「空洞」格子 —— 地图里**没有**这些格子，所以那里走不进去。
        /// （与「洞」是两回事：洞有格子，踩上去才塌陷消失。）
        /// </summary>
        public IReadOnlyList<HexCoord> VoidCells => _voidCells;

        public HexMap(Dictionary<HexCoord, HexTile> tiles, HexCoord start, HexCoord end, int playerLevel = 1,
            List<HexCoord> voidCells = null)
        {
            _tiles = tiles;
            Start = start;
            End = end;
            PlayerLevel = playerLevel;
            _voidCells = voidCells ?? new List<HexCoord>();
        }

        public HexTile GetTile(HexCoord coord)
        {
            return _tiles.TryGetValue(coord, out var tile) ? tile : null;
        }

        public bool Contains(HexCoord coord)
        {
            return _tiles.ContainsKey(coord);
        }

        public IEnumerable<HexTile> GetAllTiles()
        {
            return _tiles.Values;
        }

        public IEnumerable<HexCoord> GetAllCoords()
        {
            return _tiles.Keys;
        }

        public int TileCount => _tiles.Count;

        public void RemoveTile(HexCoord coord)
        {
            _tiles.Remove(coord);
        }

        public IEnumerable<HexTile> GetNeighbors(HexCoord coord)
        {
            return coord.GetNeighbors()
                .Where(c => _tiles.ContainsKey(c))
                .Select(c => _tiles[c]);
        }

        public IEnumerable<HexTile> GetHoleTiles()
        {
            return _tiles.Values.Where(t => t.EventType == HexEventType.Hole);
        }

        public IEnumerable<HexTile> GetTeleportTiles()
        {
            return _tiles.Values.Where(t =>
                t.EventType == HexEventType.TwoWayTeleport ||
                t.EventType == HexEventType.OneDirectionTele);
        }

        public HexCoord GetPairedTeleport(HexCoord currentCoord, string pairId)
        {
            var currentTile = GetTile(currentCoord);
            if (currentTile == null)
                return currentCoord;

            foreach (var tile in _tiles.Values)
            {
                if (tile.TeleportPairId != pairId || tile.Coord == currentCoord)
                    continue;

                if (currentTile.EventType == HexEventType.OneDirectionTele)
                {
                    if (tile.TeleportRole == TeleportRole.Exit)
                        return tile.Coord;
                }
                else
                {
                    return tile.Coord;
                }
            }

            return currentCoord;
        }

        public bool CanMoveTo(HexCoord fromCoord, HexCoord toCoord)
        {
            if (!Contains(toCoord))
                return false;

            var targetTile = GetTile(toCoord);
            if (targetTile == null || !targetTile.CanEnter)
                return false;

            return true;
        }

        /// <summary>
        /// 该格的**相邻**可走格（只走六边形相邻，**不含传送门**）。
        ///
        /// ⚠️ 与 <see cref="IsReachable"/> 不是一回事：那个是「地图可达性判定」，
        /// 必须认传送门；这个是「邻居」，语义上就是相邻格。
        /// </summary>
        public List<HexCoord> GetReachableNeighbors(HexCoord coord)
        {
            var neighbors = coord.GetNeighbors();
            var reachable = new List<HexCoord>();

            foreach (var neighbor in neighbors)
            {
                if (CanMoveTo(coord, neighbor))
                {
                    reachable.Add(neighbor);
                }
            }

            return reachable;
        }

        // ===== 可达性 / 寻路（**全项目唯一实现处**）=====
        //
        // ⚠️ 传送门（超边）有两种**语义相反**的处理口径，必须分开：
        //   - **地图可达性判定**（IsReachable / VerifyMapReachable / GetCriticalPathTiles）
        //     → 必须**认**传送门：它是地图的一部分，是跨簇唯一的通道。
        //   - **自动寻路**（HexMapController.MoveTo，玩家点远处格子自动走）
        //     → 必须**不认**：穿传送门要弹窗确认，不能自动走过去。
        // 这两件事以前混在同一个方法里 → 跨簇地图上可达性被误判成「不可达」。

        /// <summary>
        /// 传送门索引：格子 → 可以直达的格子（传送超边）。
        /// 双向门两个方向都连；单向门只连「入口 → 出口」（出口不能退回）。
        /// </summary>
        public static Dictionary<HexCoord, List<HexCoord>> ComputeTeleportIndex(Dictionary<HexCoord, HexTile> tiles)
        {
            var index = new Dictionary<HexCoord, List<HexCoord>>();

            void Link(HexCoord from, HexCoord to)
            {
                if (!index.TryGetValue(from, out var list))
                {
                    list = new List<HexCoord>();
                    index[from] = list;
                }
                if (!list.Contains(to)) list.Add(to);
            }

            foreach (HexTile tile in tiles.Values)
            {
                if (string.IsNullOrEmpty(tile.TeleportPairId)) continue;

                foreach (HexTile other in tiles.Values)
                {
                    if (other.Coord == tile.Coord) continue;
                    if (other.TeleportPairId != tile.TeleportPairId) continue;

                    if (tile.EventType == HexEventType.TwoWayTeleport)
                    {
                        Link(tile.Coord, other.Coord);
                        Link(other.Coord, tile.Coord);
                    }
                    else if (tile.EventType == HexEventType.OneDirectionTele
                             && tile.TeleportRole == TeleportRole.Entry)
                    {
                        Link(tile.Coord, other.Coord);
                    }
                }
            }

            return index;
        }

        /// <summary>展开一个格子的后继（相邻格 + 可选的传送超边），并过滤掉进不去的格。</summary>
        private static IEnumerable<HexCoord> ExpandTargets(Dictionary<HexCoord, HexTile> tiles,
            Dictionary<HexCoord, List<HexCoord>> teleports, HexCoord current)
        {
            foreach (var neighbor in current.GetNeighbors())
            {
                if (!tiles.ContainsKey(neighbor)) continue;
                var tile = tiles[neighbor];
                if (tile == null || !tile.CanEnter) continue;
                yield return neighbor;
            }

            if (teleports == null) yield break;
            if (!teleports.TryGetValue(current, out var targets)) yield break;

            foreach (var target in targets)
            {
                if (!tiles.ContainsKey(target)) continue;
                var tile = tiles[target];
                if (tile == null || !tile.CanEnter) continue;
                yield return target;
            }
        }

        /// <summary>
        /// 从起点出发的可达格集合。
        /// </summary>
        /// <param name="includeTeleports">
        /// true = 把传送门算成超边（**地图可达性判定**用）；
        /// false = 只走六边形相邻（**自动寻路**用）。
        /// </param>
        public static HashSet<HexCoord> ComputeReachableTiles(Dictionary<HexCoord, HexTile> tiles, HexCoord start,
            bool includeTeleports)
        {
            var reachable = new HashSet<HexCoord>();
            if (!tiles.ContainsKey(start)) return reachable;

            var teleports = includeTeleports ? ComputeTeleportIndex(tiles) : null;

            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            reachable.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in ExpandTargets(tiles, teleports, current))
                {
                    if (reachable.Contains(next)) continue;
                    reachable.Add(next);
                    queue.Enqueue(next);
                }
            }

            return reachable;
        }

        /// <summary>
        /// 起点 → 终点的最短路径（返回含起点与终点的序列；走不通返回空表）。
        /// </summary>
        /// <param name="includeTeleports">
        /// true = 把传送门算成超边（**地图可达性判定**用）；
        /// false = 只走六边形相邻（**自动寻路**用 —— 穿传送门要弹窗确认）。
        /// </param>
        public static List<HexCoord> ComputeShortestPath(Dictionary<HexCoord, HexTile> tiles, HexCoord start,
            HexCoord end, bool includeTeleports)
        {
            var result = new List<HexCoord>();
            if (!tiles.ContainsKey(start) || !tiles.ContainsKey(end)) return result;

            var teleports = includeTeleports ? ComputeTeleportIndex(tiles) : null;

            var visited = new HashSet<HexCoord>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == end) return ReconstructPath(cameFrom, end);

                foreach (var next in ExpandTargets(tiles, teleports, current))
                {
                    if (visited.Contains(next)) continue;
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            return result;
        }

        /// <summary>起点到终点是否可达（**认传送门**）。</summary>
        public bool IsReachable(HexCoord start, HexCoord end)
        {
            if (!_tiles.ContainsKey(start) || !_tiles.ContainsKey(end))
                return false;

            return ComputeReachableTiles(_tiles, start, true).Contains(end);
        }

        /// <summary>
        /// 起点 → 终点的最短路径（返回含起点与终点的序列；走不通返回空表）。
        /// </summary>
        /// <param name="includeTeleports">
        /// true = 把传送门算成超边（**地图可达性判定**用）；
        /// false = 只走六边形相邻（**自动寻路**用 —— 穿传送门要弹窗确认）。
        /// </param>
        public List<HexCoord> FindShortestPath(HexCoord start, HexCoord end, bool includeTeleports)
        {
            return ComputeShortestPath(_tiles, start, end, includeTeleports);
        }

        private static List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            var path = new List<HexCoord> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        /// <summary>起点→终点的关键路径（**认传送门**）。</summary>
        public HashSet<HexCoord> GetCriticalPathTiles()
        {
            return new HashSet<HexCoord>(ComputeShortestPath(_tiles, Start, End, true));
        }

        public bool VerifyMapReachable()
        {
            return IsReachable(Start, End);
        }

        public bool VerifyHoleRemoval(HexCoord holeCoord)
        {
            var testTiles = new Dictionary<HexCoord, HexTile>(_tiles);
            testTiles.Remove(holeCoord);

            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(Start);
            visited.Add(Start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == End)
                    return true;

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (testTiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = testTiles[neighbor];
                        if (tile.CanEnter)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        public void ResetAllTiles()
        {
            foreach (var tile in _tiles.Values)
            {
                tile.ResetVisitState();
            }
        }
    }
}
