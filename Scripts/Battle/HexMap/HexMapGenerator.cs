using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MapValidation;

namespace FishEatFish.Battle.HexMap
{
    public class HexMapGenerator
    {
        /// <summary>生成失败时的最大重试次数（避免死循环）</summary>
        private const int MaxGenerateAttempts = 20;

        private readonly Random _random;
        private readonly int? _seed;

        /// <summary>簇结构生成器（D12，多簇时非空）</summary>
        private ClusterBuilder _clusterBuilder;

        /// <summary>最近一次生成用掉的重试次数（0 = 一次就成）</summary>
        public int LastRetryCount { get; private set; }

        public HexMapGenerator(int? seed = null)
        {
            _seed = seed;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public HexMap Generate(int radius, int playerLevel)
        {
            return GenerateInternal(radius, playerLevel, 0, null);
        }

        /// <summary>
        /// 按配额生成地图（不经过 spec，便于内部调用与测试）。
       /// </summary>
        public HexMap GenerateWithQuota(int radius, MapQuota quota)
        {
            return GenerateInternal(radius, 1, 0, quota);
        }

        /// <summary>
        /// 按**关卡规格**生成地图（D14）—— 同一份规格 + 同一个种子 → 逐格相同的地图。
        /// </summary>
        /// <param name="spec">关卡规格（配额 + 种子）</param>
        /// <exception cref="System.InvalidOperationException">规格不可满足时明确报错</exception>
        public static HexMap GenerateFromSpec(MapSpec spec)
        {
            if (spec == null)
            {
                throw new System.ArgumentNullException(nameof(spec));
            }

            int radius = RadiusFromTileCount(spec.TotalTiles);

            // 可满足性检查：放不下就明确报错，不静默产出残缺地图
            // （传送门每对占 2 个格子：入口 + 出口）
            int available = Mathf.Max(0, spec.TotalTiles - 2 - spec.Quota.TeleportTileCount);
            spec.Validate(available);

            var generator = new HexMapGenerator(spec.Seed);
            HexMap map = generator.GenerateInternal(radius, 1, 0, spec.Quota, spec.Clusters);

            // 配额核对：实际数量与规格逐项比对
            ReportQuotaMismatch(spec, map);
            GD.Print($"[HexMapGenerator] 按规格生成: {spec.MapId}（seed={spec.Seed}，重试 {generator.LastRetryCount} 次）");
            return map;
        }

        /// <summary>由总格数反推网格半径（网格是 (2r+1) × (2r+1)）</summary>
        public static int RadiusFromTileCount(int totalTiles)
        {
            int side = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(1, totalTiles)));
            if (side % 2 == 0) side += 1;   // 必须是奇数
            return Mathf.Max(1, (side - 1) / 2);
        }

        /// <summary>生成后核对实际事件数量与规格是否一致（不一致只提示，不阻断）</summary>
        private static void ReportQuotaMismatch(MapSpec spec, HexMap map)
        {
            Dictionary<HexEventType, int> counts = MapSpec.CountEvents(map);

            void Compare(string label, HexEventType type, int expected)
            {
                int actual;
                counts.TryGetValue(type, out actual);
                if (actual != expected)
                {
                    GD.Print($"[HexMapGenerator] 配额偏差: {label} 期望 {expected}，实际 {actual}");
                }
            }

            Compare("普通战斗", HexEventType.BattleNormal, spec.Quota.Battle);
            Compare("精英战斗", HexEventType.BattleElite, spec.Quota.Elite);
            Compare("商店", HexEventType.Shop, spec.Quota.Shop);
            Compare("治疗", HexEventType.Heal, spec.Quota.Heal);
            Compare("黑印", HexEventType.GainBlackMark, spec.Quota.BlackMark);
            Compare("沼泽", HexEventType.Swamp, spec.Quota.Swamp);
            Compare("洞", HexEventType.Hole, spec.Quota.Hole);
            Compare("探照灯", HexEventType.Searchlight, spec.Quota.Searchlight);
            Compare("单向门(格)", HexEventType.OneDirectionTele, spec.Quota.TeleportOneWay * 2);
            Compare("双向门(格)", HexEventType.TwoWayTeleport, spec.Quota.TeleportTwoWay * 2);
        }

        /// <summary>把难度配置转成配额（老入口用）</summary>
        private static MapQuota ToQuota(DifficultyConfig config)
        {
            return new MapQuota
            {
                Battle = config.NormalBattleCount,
                Elite = config.EliteBattleCount,
                Shop = config.ShopCount,
                Heal = config.HealCount,
                BlackMark = config.BlackMarkCount,
                Swamp = config.SwampCount,
                Hole = config.HoleCount,
                Searchlight = config.SearchlightCount,
                TeleportOneWay = config.OneDirectionTeleportCount,
                TeleportTwoWay = config.TwoWayTeleportCount
            };
        }

        private HexMap GenerateInternal(int radius, int playerLevel, int attempt, MapQuota quotaOverride, int clusterCount = 1)
        {
            LastRetryCount = attempt;

            // D12：簇数 > 1 时用「多簇布局」（互不连通的几块区域，簇间靠传送门连）
            Dictionary<HexCoord, HexTile> tiles;
            if (clusterCount > 1)
            {
                _clusterBuilder = new ClusterBuilder(_seed);
                int targetTiles = (radius * 2 + 1) * (radius * 2 + 1);
                tiles = _clusterBuilder.Build(radius, clusterCount, targetTiles);
            }
            else
            {
                _clusterBuilder = null;
                tiles = GenerateHexGrid(radius);
            }

            var startCoord = SelectStartTile(tiles, radius);
            var endCoord = SelectEndTile(tiles, startCoord, radius);

            tiles[startCoord].IsStart = true;
            tiles[endCoord].IsEnd = true;
            tiles[endCoord].EventType = HexEventType.BattleBoss;
            tiles[endCoord].TriggerCount = 1;
            tiles[endCoord].DisplayName = "BOSS";
            tiles[endCoord].IconPath = "res://Assets/Icons/skull.svg";
            tiles[endCoord].EnemyConfig = "boss_wave";

            // 配额来源：规格优先，没有规格才按难度配置
            MapQuota quota = quotaOverride ?? ToQuota(GetDifficultyConfig(playerLevel));

            HashSet<HexCoord> criticalSet;
            if (clusterCount > 1)
            {
                // D12 簇模式：**先放传送门** —— 洞的割点判定要用最终图（含传送边）才准
                var protectedTiles = new HashSet<HexCoord> { startCoord, endCoord };
                PlaceTwoWayTeleportTiles(tiles, protectedTiles, quota.TeleportTwoWay);
                PlaceOneDirectionTeleportTiles(tiles, protectedTiles, quota.TeleportOneWay, endCoord);

                var clusterPath = FindShortestPath(tiles, startCoord, endCoord);
                criticalSet = new HashSet<HexCoord>(clusterPath);
            }
            else
            {
                var legacyPath = FindShortestPath(tiles, startCoord, endCoord);
                criticalSet = new HashSet<HexCoord>(legacyPath);
            }

            PlaceHoleTiles(tiles, criticalSet, quota.Hole, startCoord, endCoord, clusterCount);
            // D12：洞全部放好之后统一复检「是不是割点」，不是的撤掉
            if (clusterCount > 1)
            {
                RemoveHolesThatAreNotArticulationPoints(tiles, startCoord, endCoord);
            }

            PlaceSearchlightTiles(tiles, criticalSet, quota.Searchlight);

            PlaceSwampTiles(tiles, criticalSet, quota.Swamp);

            if (clusterCount <= 1)
            {
                PlaceTwoWayTeleportTiles(tiles, criticalSet, quota.TeleportTwoWay);

                PlaceOneDirectionTeleportTiles(tiles, criticalSet, quota.TeleportOneWay, endCoord);
            }

            var nonCriticalEmptyTiles = tiles.Keys
                .Where(c => !criticalSet.Contains(c) && tiles[c].EventType == HexEventType.Empty)
                .ToList();

            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.BattleNormal, quota.Battle);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.BattleElite, quota.Elite);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.Heal, quota.Heal);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.GainBlackMark, quota.BlackMark);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.Shop, quota.Shop);

            if (!VerifyNoAdjacentSameEvents(tiles))
            {
                GD.PrintErr("[HexMapGenerator] 验证失败: 检测到相同事件出现在相邻格子，重新生成地图...");
                if (attempt < MaxGenerateAttempts)
                {
                    return GenerateInternal(radius, playerLevel, attempt + 1, quotaOverride, clusterCount);
                }
                return new HexMap(tiles, startCoord, endCoord, playerLevel);
            }

            EnsurePathConnectivity(tiles, startCoord, endCoord);

            // 「移除所有洞后仍要能到 BOSS」是**旧模型**的规则（那时洞是陷阱格，不是连接点）。
            // D12 的新模型里，洞是簇内两块子区域之间**唯一的连接** —— 移除它本来就该断开，
            // 这正是「消耗后分裂」要的效果。所以簇模式下跳过这条验证，
            // 合法性交给 D13 校验器（它的状态空间搜索会正确判断「有没有能通关的走法」）。
            if (clusterCount <= 1 && !VerifyAllHolesPlacement(tiles, startCoord, endCoord))
            {
                GD.PrintErr("[HexMapGenerator] 验证失败: 移除所有洞穴后无法到达boss，重新生成地图...");
                if (attempt < MaxGenerateAttempts)
                {
                    return GenerateInternal(radius, playerLevel, attempt + 1, quotaOverride, clusterCount);
                }
                return new HexMap(tiles, startCoord, endCoord, playerLevel);
            }

            var map = new HexMap(tiles, startCoord, endCoord, playerLevel);

            // D13 自检：用通用校验器兜底（人工关卡与参数生成共用同一套判定标准）
            MapValidationReport report = new MapValidator().Validate(new HexMapGraph(map));
            if (!report.CanCompleteGoal)
            {
                GD.PrintErr("[HexMapGenerator] 校验失败: 找不到能通关的走法，重新生成地图...");
                GD.PrintErr(report.ToString());
                if (attempt < MaxGenerateAttempts)
                {
                    return GenerateInternal(radius, playerLevel, attempt + 1, quotaOverride, clusterCount);
                }
            }
            else if (!report.IsValid)
            {
                // 只是提示（软锁 / 不可达区域之类），不阻断生成
                GD.Print($"[HexMapGenerator] 校验提示（不阻断）: {report.Issues.Count} 项问题");
                foreach (MapIssue issue in report.Issues)
                {
                    GD.Print($"[HexMapGenerator]   {issue}");
                }
            }

            return map;
        }

        private Dictionary<HexCoord, HexTile> GenerateHexGrid(int radius)
        {
            var tiles = new Dictionary<HexCoord, HexTile>();

            int width = radius * 2 + 1;
            int height = radius * 2 + 1;

            GD.Print($"[HexMapGenerator] 生成矩形网格: 宽度{width}, 高度{height}");

            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row < height; row++)
                {
                    var coord = HexCoord.FromOffset(col, row);
                    tiles[coord] = new HexTile(coord, HexEventType.Empty);
                    var (q, r) = coord.ToAxial();
                    GD.Print($"[HexMapGenerator] 格子 ({col},{row}) -> 轴坐标 ({q},{r})");
                }
            }

            GD.Print($"[HexMapGenerator] 总格子{tiles.Count}");

            return tiles;
        }

        private bool IsEdgeTile(HexCoord coord, int radius)
        {
            int halfSize = radius;
            var (col, row) = coord.ToOffset();
            int width = halfSize * 2 + 1;
            int height = halfSize * 2 + 1;
            return col == 0 || col == width - 1 || row == 0 || row == height - 1;
        }

        private HexCoord SelectRandomEdgeTile(Dictionary<HexCoord, HexTile> tiles, int radius)
        {
            int width = radius * 2 + 1;
            int height = radius * 2 + 1;
            var edgeTiles = new List<HexCoord>();

            for (int col = 0; col < width; col++)
            {
                edgeTiles.Add(HexCoord.FromOffset(col, 0));
                edgeTiles.Add(HexCoord.FromOffset(col, height - 1));
            }
            for (int row = 1; row < height - 1; row++)
            {
                edgeTiles.Add(HexCoord.FromOffset(0, row));
                edgeTiles.Add(HexCoord.FromOffset(width - 1, row));
            }

            edgeTiles = edgeTiles.Where(t => tiles.ContainsKey(t)).ToList();

            if (edgeTiles.Count == 0)
            {
                return HexCoord.FromOffset(0, 0);
            }

            int index = _random.Next(edgeTiles.Count);
            return edgeTiles[index];
        }

        /// <summary>
        /// 选起点：多簇时取**簇 0** 里的随机格（簇模式下没有完整的网格边缘）。
        /// </summary>
        private HexCoord SelectStartTile(Dictionary<HexCoord, HexTile> tiles, int radius)
        {
            if (_clusterBuilder != null && _clusterBuilder.ClusterCount > 1)
            {
                List<HexCoord> pool = _clusterBuilder.GetClusterTiles(0);
                if (pool.Count > 0) return pool[_random.Next(pool.Count)];
            }

            return SelectRandomEdgeTile(tiles, radius);
        }

        /// <summary>
        /// 选终点：多簇时取**其他簇**里离起点最远的格（让传送门真正有用）。
        /// </summary>
        private HexCoord SelectEndTile(Dictionary<HexCoord, HexTile> tiles, HexCoord start, int radius)
        {
            if (_clusterBuilder != null && _clusterBuilder.ClusterCount > 1)
            {
                int startCluster = _clusterBuilder.GetClusterOf(start);
                HexCoord farthest = start;
                int maxDist = 0;

                for (int c = 0; c < _clusterBuilder.ClusterCount; c++)
                {
                    if (c == startCluster) continue;

                    foreach (HexCoord coord in _clusterBuilder.GetClusterTiles(c))
                    {
                        int dist = start.DistanceTo(coord);
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            farthest = coord;
                        }
                    }
                }

                if (maxDist > 0) return farthest;
            }

            return FindFarthestTile(tiles, start);
        }

        private HexCoord FindFarthestTile(Dictionary<HexCoord, HexTile> tiles, HexCoord start)
        {
            HexCoord farthest = start;
            int maxDist = 0;

            foreach (var coord in tiles.Keys)
            {
                int dist = start.DistanceTo(coord);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthest = coord;
                }
            }

            return farthest;
        }

        /// <summary>
        /// 建立传送索引：格子 → 可以直达的格子（传送超边）。
        /// D12 多簇之后，簇与簇之间**只能靠传送门**连通，所以寻路必须把传送算成边。
        ///
        /// 规则：
        ///   - 双向传送门：两个方向都能走
        ///   - 单向传送门：只有「入口 → 出口」能走（出口不能退回来路）
        /// </summary>
        private Dictionary<HexCoord, List<HexCoord>> BuildTeleportIndex(Dictionary<HexCoord, HexTile> tiles)
        {
            Dictionary<HexCoord, List<HexCoord>> index = new Dictionary<HexCoord, List<HexCoord>>();

            void Link(HexCoord from, HexCoord to)
            {
                List<HexCoord> list;
                if (!index.TryGetValue(from, out list))
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

        /// <summary>
        /// 最短路径（邻接 + 传送超边）。
        /// </summary>
        private List<HexCoord> FindShortestPath(Dictionary<HexCoord, HexTile> tiles, HexCoord start, HexCoord end)
        {
            var visited = new HashSet<HexCoord>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            visited.Add(start);

            Dictionary<HexCoord, List<HexCoord>> teleports = BuildTeleportIndex(tiles);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                {
                    return ReconstructPath(cameFrom, end);
                }

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }

                // 传送超边
                List<HexCoord> teleTargets;
                if (teleports.TryGetValue(current, out teleTargets))
                {
                    foreach (HexCoord target in teleTargets)
                    {
                        if (!tiles.ContainsKey(target) || visited.Contains(target)) continue;

                        visited.Add(target);
                        cameFrom[target] = current;
                        queue.Enqueue(target);
                    }
                }
            }

            return new List<HexCoord>();
        }

        private List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            var path = new List<HexCoord> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

		    /// <summary>
		    /// 放置探照灯：踩上去可以「打开开关」，驱散半径 3 格的迷雾（扩大视野）
		    /// </summary>
		    private void PlaceSearchlightTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
		    {
		        if (count <= 0) return;

		        var candidates = tiles.Keys
		            .Where(c => !criticalSet.Contains(c))
		            .ToList();

		        int placed = 0;
		        int attempts = 0;
		        int maxAttempts = candidates.Count * 2;

		        while (placed < count && attempts < maxAttempts && candidates.Count > 0)
		        {
		            attempts++;
		            var coord = candidates[_random.Next(candidates.Count)];

		            // 探照灯不要挨着另一个探照灯
		            bool adjacentToOther = false;
		            foreach (HexCoord n in coord.GetNeighbors())
		            {
		                if (tiles.TryGetValue(n, out HexTile nt) && nt.EventType == HexEventType.Searchlight)
		                {
		                    adjacentToOther = true;
		                    break;
		                }
		            }
		            if (adjacentToOther) continue;

		            tiles[coord].EventType = HexEventType.Searchlight;
		            tiles[coord].TriggerCount = 1;
		            tiles[coord].DisplayName = "探照灯";
		            tiles[coord].IconPath = "res://Assets/Icons/relic_lantern.svg";
		            placed++;
		        }

		        GD.Print($"[HexMapGenerator] 放置探照灯 {placed} 个");
		    }
        private void PlaceHoleTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count,
            HexCoord startCoord, HexCoord endCoord, int clusterCount)
        {
            var candidates = tiles.Keys
                .Where(c => !criticalSet.Contains(c)
                    // 洞不能盖住传送门 / 已有的事件格（否则配额对不上）
                    && (clusterCount > 1 ? tiles[c].EventType == HexEventType.Empty : true))
                .ToList();

            int placed = 0;
            int attempts = 0;
            int maxAttempts = candidates.Count * 2;
            int skippedNeighbors = 0;

            GD.Print($"[HexMapGenerator] 洞候选 {candidates.Count} 个（总格数 {tiles.Count}，关键路径 {criticalSet.Count}）");

            // 簇模式：**优先把洞放在簇的「窄口」上** ——
            // 窄口本来就是两块子区域之间唯一的连接（= 割点），消耗掉它这一簇就裂成两块
            if (clusterCount > 1 && _clusterBuilder != null)
            {
                for (int c = 0; c < _clusterBuilder.ClusterCount && placed < count; c++)
                {
                    HexCoord bottleneck;
                    if (!_clusterBuilder.Bottlenecks.TryGetValue(c, out bottleneck)) continue;
                    if (criticalSet.Contains(bottleneck)) continue;
                    if (!tiles.ContainsKey(bottleneck)) continue;
                    if (tiles[bottleneck].EventType != HexEventType.Empty) continue;

                    if (IsHoleArticulationPoint(tiles, bottleneck, startCoord, endCoord))
                    {
                        PlaceHole(tiles, bottleneck);
                        placed++;
                        GD.Print($"[HexMapGenerator] 洞放在簇 {c} 的窄口 {bottleneck}（割点）");
                    }
                }
            }

            while (placed < count && attempts < maxAttempts && candidates.Count > 0)
            {
                attempts++;
                var coord = candidates[_random.Next(candidates.Count)];

                var neighbors = coord.GetNeighbors()
                    .Where(n => tiles.ContainsKey(n) && !criticalSet.Contains(n))
                    .ToList();

                if (neighbors.Count < 2)
                {
                    skippedNeighbors++;
                    candidates.Remove(coord);
                    continue;
                }

                {                    // D12：洞要落在**割点**上（消耗后该簇才会真的裂成两块）。
                    // 这里先用轻量判据初筛（移除后邻接连通分量增加），
                    // 全部放完之后再统一用 D13 口径复检（IsHoleArticulationPoint）。
                    bool placeable;
                    if (clusterCount > 1)
                    {
                        placeable = CountAdjacentComponents(tiles, coord) > CountAdjacentComponents(tiles, null);
                    }
                    else
                    {
                        placeable = VerifyHolePlacement(tiles, coord, neighbors, criticalSet);
                    }

                    if (placeable)
                    {
                        PlaceHole(tiles, coord);
                        placed++;
                    }
                }

                candidates.Remove(coord);
            }

            GD.Print($"[HexMapGenerator] 放置了 {placed} 个洞穴（邻格不足而跳过 {skippedNeighbors} 次）");
        }

        /// <summary>在某个格子上放一个洞</summary>
        private static void PlaceHole(Dictionary<HexCoord, HexTile> tiles, HexCoord coord)
        {
            tiles[coord].EventType = HexEventType.Hole;
            tiles[coord].TriggerCount = 1;
            tiles[coord].DisplayName = "洞穴";
            tiles[coord].IconPath = "res://Assets/Icons/hole.png";
        }

        /// <summary>
        /// 判断某个格子是不是**割点**（articulation point）：
        /// 移除它之后，图会分裂成更多块。
        ///
        /// ⚠️ 口径说明：这里按**纯邻接边**判定（洞视为普通可走格），
        /// 因为问的是「洞**还在**的时候，它是不是连接两块的关键」。
        /// D13 的簇图分析会把**所有洞**都移除（洞是消耗型节点），那是「洞已消耗后」的地图，
        /// 用在这里会导致「移除洞前后没区别」→ 永远判不出割点。
        /// </summary>
        private bool IsHoleArticulationPoint(Dictionary<HexCoord, HexTile> tiles, HexCoord coord,
            HexCoord startCoord, HexCoord endCoord)
        {
            int compsWithout = CountAdjacentComponents(tiles, coord);
            int compsFull = CountAdjacentComponents(tiles, null);
            return compsWithout > compsFull;
        }

        /// <summary>
        /// 洞全部放好之后**统一复检**：不是割点的洞撤掉。
        ///
        /// 为什么必须后置复检：放洞是一个一个放的，判定第 1 个洞时图里还没有其他洞，
        /// 判定结果与「所有洞都在图上」的最终状态不一致 —— 会出现「生成器认为是割点、
        /// 校验器说不是」的情况。后置复检保证生成器与 D13 校验器**口径完全一致**。
        /// </summary>
        private void RemoveHolesThatAreNotArticulationPoints(Dictionary<HexCoord, HexTile> tiles,
            HexCoord startCoord, HexCoord endCoord)
        {
            List<HexCoord> holes = tiles.Values
                .Where(t => t.EventType == HexEventType.Hole)
                .Select(t => t.Coord)
                .ToList();

            int removed = 0;
            foreach (HexCoord hole in holes)
            {
                if (IsHoleArticulationPoint(tiles, hole, startCoord, endCoord)) continue;

                HexTile tile = tiles[hole];
                tile.EventType = HexEventType.Empty;
                tile.TriggerCount = 0;
                tile.DisplayName = "";
                tile.IconPath = "";
                removed++;
                GD.Print($"[HexMapGenerator] 洞 {hole} 不是割点，撤销");
            }

            if (removed > 0)
            {
                GD.Print($"[HexMapGenerator] 撤销了 {removed} 个非割点的洞");
            }
        }

        /// <summary>
        /// 按**邻接边**算连通分量数（不看传送）。
        /// </summary>
        /// <param name="tiles">格子表</param>
        /// <param name="removeCoord">要临时移除的格子（null = 不移除）</param>
        private int CountAdjacentComponents(Dictionary<HexCoord, HexTile> tiles, HexCoord? removeCoord)
        {
            HashSet<HexCoord> all = new HashSet<HexCoord>(tiles.Keys);
            if (removeCoord.HasValue) all.Remove(removeCoord.Value);

            HashSet<HexCoord> visited = new HashSet<HexCoord>();
            int components = 0;

            foreach (HexCoord seed in all)
            {
                if (visited.Contains(seed)) continue;

                components++;
                Queue<HexCoord> queue = new Queue<HexCoord>();
                queue.Enqueue(seed);
                visited.Add(seed);

                while (queue.Count > 0)
                {
                    HexCoord current = queue.Dequeue();
                    foreach (HexCoord neighbor in current.GetNeighbors())
                    {
                        if (!all.Contains(neighbor) || visited.Contains(neighbor)) continue;

                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return components;
        }

        private bool VerifyAllHolesPlacement(Dictionary<HexCoord, HexTile> tiles, HexCoord startCoord, HexCoord endCoord)
        {
            var allHoles = tiles.Values
                .Where(t => t.EventType == HexEventType.Hole)
                .Select(t => t.Coord)
                .ToList();

            if (allHoles.Count == 0)
                return true;

            // 验证任意洞穴触发组合（尤其是2个hole同时消失的情况）后，起点依旧可以到达boss。
            // 关卡最高只有4个洞穴，组合数最多 2^4-1=15，穷举成本可控。
            int totalCombinations = 1 << allHoles.Count;
            for (int mask = 1; mask < totalCombinations; mask++)
            {
                var removedHoles = new List<HexCoord>();
                var testTiles = new Dictionary<HexCoord, HexTile>(tiles);

                for (int i = 0; i < allHoles.Count; i++)
                {
                    if ((mask & (1 << i)) == 0)
                        continue;

                    var hole = allHoles[i];
                    testTiles.Remove(hole);
                    removedHoles.Add(hole);
                }

                if (!CanReach(testTiles, startCoord, endCoord))
                {
                    GD.Print($"[HexMapGenerator] 最终验证失败: 移除洞穴组合 {string.Join(", ", removedHoles)} 后起点无法到达boss");
                    return false;
                }
            }

            return true;
        }

        private bool VerifyHolePlacement(Dictionary<HexCoord, HexTile> tiles, HexCoord holeCoord,
            List<HexCoord> neighbors, HashSet<HexCoord> criticalSet)
        {
            foreach (var entryNeighbor in neighbors)
            {
                var testTiles = new Dictionary<HexCoord, HexTile>(tiles);
                testTiles.Remove(holeCoord);

                if (!CanReachEndFromEntry(testTiles, entryNeighbor, holeCoord))
                {
                    GD.Print($"[HexMapGenerator] 洞穴 {holeCoord} 验证失败: 从 {entryNeighbor} 进入后无法到达终点");
                    return false;
                }
            }

            if (!VerifyHoleReturnPath(tiles, holeCoord))
            {
                GD.Print($"[HexMapGenerator] 洞穴 {holeCoord} 验证失败: 玩家可能绕道其他洞穴返回起点导致无法到达boss");
                return false;
            }

            return true;
        }

        private bool VerifyHoleReturnPath(Dictionary<HexCoord, HexTile> tiles, HexCoord holeCoord)
        {
            var start = new HexCoord(0, 0);
            var end = FindFarthestTile(tiles, start);

            var testTiles = new Dictionary<HexCoord, HexTile>(tiles);
            testTiles.Remove(holeCoord);

            var reachableFromStart = GetReachableTiles(testTiles, start);

            var holesOnReturnPath = FindHolesOnReturnPath(testTiles, holeCoord, start, reachableFromStart);

            if (holesOnReturnPath.Count == 0)
            {
                return true;
            }

            holesOnReturnPath.Add(holeCoord);

            var allRemovedTiles = new Dictionary<HexCoord, HexTile>(tiles);
            foreach (var h in holesOnReturnPath)
            {
                allRemovedTiles.Remove(h);
            }

            if (!CanReach(allRemovedTiles, start, end))
            {
                GD.Print($"[HexMapGenerator] 验证失败: 移除洞穴组 {string.Join(", ", holesOnReturnPath)} 后无法到达boss");
                return false;
            }

            return true;
        }

        private HashSet<HexCoord> FindHolesOnReturnPath(Dictionary<HexCoord, HexTile> tiles, HexCoord holeCoord, 
            HexCoord start, HashSet<HexCoord> reachableFromStart)
        {
            var holesOnPath = new HashSet<HexCoord>();
            var otherHoles = tiles.Values
                .Where(t => t.EventType == HexEventType.Hole && t.Coord != holeCoord)
                .Select(t => t.Coord)
                .ToHashSet();

            foreach (var neighbor in holeCoord.GetNeighbors())
            {
                if (!tiles.ContainsKey(neighbor))
                    continue;

                if (neighbor == start)
                    continue;

                if (!reachableFromStart.Contains(neighbor))
                    continue;

                if (CanReachThroughHoles(tiles, neighbor, start, otherHoles, holesOnPath))
                {
                    // 找到一条经过其他hole返回起点的路径
                }
            }

            return holesOnPath;
        }

        private bool CanReachThroughHoles(Dictionary<HexCoord, HexTile> tiles, HexCoord from, HexCoord to,
            HashSet<HexCoord> otherHoles, HashSet<HexCoord> holesOnPath)
        {
            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == to)
                    return true;

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (!tiles.ContainsKey(neighbor) || visited.Contains(neighbor))
                        continue;

                    visited.Add(neighbor);

                    if (otherHoles.Contains(neighbor))
                    {
                        holesOnPath.Add(neighbor);
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private bool CanReach(Dictionary<HexCoord, HexTile> tiles, HexCoord from, HexCoord to)
        {
            if (from == to)
                return true;

            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        if (neighbor == to)
                            return true;

                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        private HashSet<HexCoord> FindConnectedHoles(Dictionary<HexCoord, HexTile> tiles, HexCoord startHole)
        {
            var connected = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();

            var existingHoles = tiles.Values
                .Where(t => t.EventType == HexEventType.Hole && t.Coord != startHole)
                .Select(t => t.Coord)
                .ToHashSet();

            foreach (var neighbor in startHole.GetNeighbors())
            {
                if (existingHoles.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    connected.Add(neighbor);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (existingHoles.Contains(neighbor) && !connected.Contains(neighbor))
                    {
                        connected.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return connected;
        }

        private bool CanReachEnd(Dictionary<HexCoord, HexTile> tiles)
        {
            var start = new HexCoord(0, 0);
            var end = FindFarthestTile(tiles, start);

            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                    return true;

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = tiles[neighbor];
                        if (tile != null && tile.CanEnter)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        private bool CanReachEndFromEntry(Dictionary<HexCoord, HexTile> tiles, HexCoord entryCoord, HexCoord excludedFromReturn)
        {
            var start = new HexCoord(0, 0);
            var end = FindFarthestTile(tiles, start);

            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(entryCoord);
            visited.Add(entryCoord);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                    return true;

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = tiles[neighbor];
                        if (tile != null && tile.CanEnter)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        private void PlaceOneDirectionTeleportTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count, HexCoord endCoord)
        {
            // D12 簇模式：单向门从「其他簇」单向进入「终点簇」
            //（方向必须指向终点侧 —— 反了就进去出不来，D13 会报软锁）
            if (_clusterBuilder != null && _clusterBuilder.ClusterCount > 1)
            {
                PlaceOneDirectionTeleportsAcrossClusters(tiles, criticalSet, count, endCoord);
                return;
            }

            int pairsPlaced = 0;
            int attempts = 0;
            int maxAttempts = 50;

            while (pairsPlaced < count && attempts < maxAttempts)
            {
                attempts++;

                var candidates = tiles.Keys
                    .Where(c => !criticalSet.Contains(c) && tiles[c].EventType == HexEventType.Empty)
                    .ToList();

                if (candidates.Count < 2) break;

                Shuffle(candidates);

                var coord1 = candidates[0];
                var coord2 = candidates[1];

                if (coord1.DistanceTo(coord2) > 5)
                {
                    if (!CanPlaceEventAt(tiles, coord1, HexEventType.OneDirectionTele) ||
                        !CanPlaceEventAt(tiles, coord2, HexEventType.OneDirectionTele))
                    {
                        continue;
                    }

                    if (!VerifyOneDirectionTeleportPlacement(tiles, coord1, coord2, criticalSet))
                    {
                        GD.Print($"[HexMapGenerator] 单向传送门对 ({coord1}, {coord2}) 验证失败");
                        continue;
                    }

                    var pairId = $"one_dir_tele_{pairsPlaced}";

                    tiles[coord1].EventType = HexEventType.OneDirectionTele;
                    tiles[coord1].TriggerCount = HexTile.InfiniteTriggers;
                    tiles[coord1].TeleportPairId = pairId;
                    tiles[coord1].TeleportDirection = TeleportDirection.Forward;
                    tiles[coord1].TeleportRole = TeleportRole.Entry;
                    tiles[coord1].DisplayName = "传送入口";
                    tiles[coord1].IconPath = "res://Assets/Icons/teleport_entry.svg";

                    tiles[coord2].EventType = HexEventType.OneDirectionTele;
                    tiles[coord2].TriggerCount = HexTile.InfiniteTriggers;
                    tiles[coord2].TeleportPairId = pairId;
                    tiles[coord2].TeleportDirection = TeleportDirection.Backward;
                    tiles[coord2].TeleportRole = TeleportRole.Exit;
                    tiles[coord2].DisplayName = "传送出口";
                    tiles[coord2].IconPath = "res://Assets/Icons/teleport_exit.svg";

                    pairsPlaced++;
                }
            }

            GD.Print($"[HexMapGenerator] 放置了 {pairsPlaced} 对单向传送门");
        }

        /// <summary>
        /// 簇模式下的单向传送门：从其他簇**单向进入终点簇**。
        /// </summary>
        private void PlaceOneDirectionTeleportsAcrossClusters(Dictionary<HexCoord, HexTile> tiles,
            HashSet<HexCoord> criticalSet, int count, HexCoord endCoord)
        {
            int endCluster = _clusterBuilder.GetClusterOf(endCoord);
            int placed = 0;

            for (int c = 0; c < _clusterBuilder.ClusterCount && placed < count; c++)
            {
                if (c == endCluster) continue;

                List<HexCoord> poolA = _clusterBuilder.GetClusterTiles(c)
                    .Where(x => !criticalSet.Contains(x) && tiles.ContainsKey(x) && tiles[x].EventType == HexEventType.Empty)
                    .ToList();
                List<HexCoord> poolB = _clusterBuilder.GetClusterTiles(endCluster)
                    .Where(x => !criticalSet.Contains(x) && tiles.ContainsKey(x) && tiles[x].EventType == HexEventType.Empty)
                    .ToList();

                if (poolA.Count == 0 || poolB.Count == 0) continue;

                Shuffle(poolA);
                Shuffle(poolB);

                foreach (HexCoord entry in poolA)
                {
                    foreach (HexCoord exit in poolB)
                    {
                        if (entry.DistanceTo(exit) <= 5) continue;
                        // 端点不能已经被别的传送门占用（poolB 是提前算好的，这里要再查一次）
                        if (tiles[entry].EventType != HexEventType.Empty) continue;
                        if (tiles[exit].EventType != HexEventType.Empty) continue;
                        if (!CanPlaceEventAt(tiles, entry, HexEventType.OneDirectionTele)) continue;
                        if (!CanPlaceEventAt(tiles, exit, HexEventType.OneDirectionTele)) continue;

                        string pairId = $"one_dir_tele_{placed}";

                        tiles[entry].EventType = HexEventType.OneDirectionTele;
                        tiles[entry].TriggerCount = HexTile.InfiniteTriggers;
                        tiles[entry].TeleportPairId = pairId;
                        tiles[entry].TeleportDirection = TeleportDirection.Forward;
                        tiles[entry].TeleportRole = TeleportRole.Entry;
                        tiles[entry].DisplayName = "传送入口";
                        tiles[entry].IconPath = "res://Assets/Icons/teleport_entry.svg";

                        tiles[exit].EventType = HexEventType.OneDirectionTele;
                        tiles[exit].TriggerCount = HexTile.InfiniteTriggers;
                        tiles[exit].TeleportPairId = pairId;
                        tiles[exit].TeleportDirection = TeleportDirection.Backward;
                        tiles[exit].TeleportRole = TeleportRole.Exit;
                        tiles[exit].DisplayName = "传送出口";
                        tiles[exit].IconPath = "res://Assets/Icons/teleport_exit.svg";

                        GD.Print($"[HexMapGenerator] 单向门 簇{c} → 终点簇{endCluster}（{entry} → {exit}）");
                        placed++;
                        break;
                    }

                    if (placed >= count) break;
                }
            }

            GD.Print($"[HexMapGenerator] 放置了 {placed} 对单向传送门（簇模式）");
        }

        private bool VerifyOneDirectionTeleportPlacement(Dictionary<HexCoord, HexTile> tiles, HexCoord tele1, HexCoord tele2, HashSet<HexCoord> criticalSet)
        {
            var holes = tiles.Values.Where(t => t.EventType == HexEventType.Hole).ToList();

            foreach (var hole in holes)
            {
                var testTiles = new Dictionary<HexCoord, HexTile>(tiles);
                testTiles.Remove(hole.Coord);

                var start = new HexCoord(0, 0);

                var reachableFromStart = GetReachableTiles(testTiles, start);

                bool tele1ReachableFromStart = reachableFromStart.Contains(tele1);
                bool tele2ReachableFromStart = reachableFromStart.Contains(tele2);

                if (tele1ReachableFromStart != tele2ReachableFromStart)
                {
                    GD.Print($"[HexMapGenerator] 单向传送门验证失败: 洞穴 {hole.Coord} 会将传送门对分割到不同区域");
                    return false;
                }
            }

            return true;
        }

        private void PlaceSwampTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
        {
            var candidates = tiles.Keys
                .Where(c => !criticalSet.Contains(c) && tiles[c].EventType == HexEventType.Empty)
                .ToList();

            Shuffle(candidates);

            for (int i = 0; i < Math.Min(count, candidates.Count); i++)
            {
                var coord = candidates[i];
                if (!CanPlaceEventAt(tiles, coord, HexEventType.Swamp))
                {
                    continue;
                }

                tiles[coord].EventType = HexEventType.Swamp;
                tiles[coord].TriggerCount = HexTile.InfiniteTriggers;
                tiles[coord].Damage = 10;
                tiles[coord].DisplayName = "沼泽";
                tiles[coord].IconPath = "res://Assets/Icons/swamp.png";
            }
        }

        private void PlaceTwoWayTeleportTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
        {
            int pairsPlaced = 0;

            // D12：第一优先用**随机生成树**把各个簇连起来（保证簇图连通，≥ 簇数-1 对）
            if (_clusterBuilder != null && _clusterBuilder.ClusterCount > 1)
            {
                List<int> connected = new List<int> { 0 };
                List<int> remaining = new List<int>();
                for (int i = 1; i < _clusterBuilder.ClusterCount; i++)
                {
                    remaining.Add(i);
                }

                int guard = 0;
                while (remaining.Count > 0 && guard < 200)
                {
                    guard++;

                    int a = connected[_random.Next(connected.Count)];
                    int b = remaining[_random.Next(remaining.Count)];

                    if (TryPlaceTwoWayPair(tiles, criticalSet, a, b, pairsPlaced))
                    {
                        connected.Add(b);
                        remaining.Remove(b);
                        pairsPlaced++;
                    }
                    else
                    {
                        // 这一对放不下：换个 b 再试
                        remaining.Remove(b);
                        remaining.Add(b);
                    }
                }
            }

            // 剩余配额：随机跨簇补足
            int attempts = 0;
            int maxAttempts = 50;

            while (pairsPlaced < count && attempts < maxAttempts)
            {
                attempts++;

                var candidates = tiles.Keys
                    .Where(c => !criticalSet.Contains(c) && tiles[c].EventType == HexEventType.Empty)
                    .ToList();

                if (candidates.Count < 2) break;

                Shuffle(candidates);

                var coord1 = candidates[0];
                var coord2 = candidates[1];

                if (coord1.DistanceTo(coord2) > 5)
                {
                    if (!CanPlaceEventAt(tiles, coord1, HexEventType.TwoWayTeleport) ||
                        !CanPlaceEventAt(tiles, coord2, HexEventType.TwoWayTeleport))
                    {
                        continue;
                    }

                    if (!VerifyTeleportPlacement(tiles, coord1, coord2, criticalSet))
                    {
                        GD.Print($"[HexMapGenerator] 传送门对 ({coord1}, {coord2}) 验证失败");
                        continue;
                    }

                    PlaceTwoWayPair(tiles, coord1, coord2, pairsPlaced);
                    pairsPlaced++;
                }
            }

            GD.Print($"[HexMapGenerator] 放置了 {pairsPlaced} 对双向传送门");
        }

        /// <summary>在两个指定簇之间放一对双向传送门（成功返回 true）</summary>
        private bool TryPlaceTwoWayPair(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet,
            int clusterA, int clusterB, int pairIndex)
        {
            if (_clusterBuilder == null) return false;

            List<HexCoord> poolA = _clusterBuilder.GetClusterTiles(clusterA)
                .Where(c => !criticalSet.Contains(c) && tiles.ContainsKey(c) && tiles[c].EventType == HexEventType.Empty)
                .ToList();
            List<HexCoord> poolB = _clusterBuilder.GetClusterTiles(clusterB)
                .Where(c => !criticalSet.Contains(c) && tiles.ContainsKey(c) && tiles[c].EventType == HexEventType.Empty)
                .ToList();

            if (poolA.Count == 0 || poolB.Count == 0) return false;

            Shuffle(poolA);
            Shuffle(poolB);

            foreach (HexCoord ca in poolA)
            {
                foreach (HexCoord cb in poolB)
                {
                    if (ca.DistanceTo(cb) <= 5) continue;
                    if (!CanPlaceEventAt(tiles, ca, HexEventType.TwoWayTeleport)) continue;
                    if (!CanPlaceEventAt(tiles, cb, HexEventType.TwoWayTeleport)) continue;
                    if (!VerifyTeleportPlacement(tiles, ca, cb, criticalSet)) continue;

                    PlaceTwoWayPair(tiles, ca, cb, pairIndex);
                    GD.Print($"[HexMapGenerator] 簇 {clusterA} ↔ 簇 {clusterB} 传送门 ({ca}, {cb})");
                    return true;
                }
            }

            return false;
        }

        /// <summary>放置一对双向传送门（不校验，由调用方保证）</summary>
        private void PlaceTwoWayPair(Dictionary<HexCoord, HexTile> tiles, HexCoord coord1, HexCoord coord2, int pairIndex)
        {
            var pairId = $"twoway_teleport_{pairIndex}";

            tiles[coord1].EventType = HexEventType.TwoWayTeleport;
            tiles[coord1].TriggerCount = HexTile.InfiniteTriggers;
            tiles[coord1].TeleportPairId = pairId;
            tiles[coord1].DisplayName = "双向传送门A";
            tiles[coord1].IconPath = "res://Assets/Icons/teleport_entry.svg";

            tiles[coord2].EventType = HexEventType.TwoWayTeleport;
            tiles[coord2].TriggerCount = HexTile.InfiniteTriggers;
            tiles[coord2].TeleportPairId = pairId;
            tiles[coord2].DisplayName = "双向传送门B";
            tiles[coord2].IconPath = "res://Assets/Icons/teleport_exit.svg";
        }

        private bool VerifyTeleportPlacement(Dictionary<HexCoord, HexTile> tiles, HexCoord teleport1, HexCoord teleport2, HashSet<HexCoord> criticalSet)
        {
            var holes = tiles.Values.Where(t => t.EventType == HexEventType.Hole).ToList();

            if (holes.Count == 0)
                return true;

            foreach (var hole in holes)
            {
                var testTiles = new Dictionary<HexCoord, HexTile>(tiles);
                testTiles.Remove(hole.Coord);

                var start = new HexCoord(0, 0);
                var end = FindFarthestTile(tiles, start);

                var reachableFromStart = GetReachableTiles(testTiles, start);
                var reachableFrom1 = GetReachableTiles(testTiles, teleport1);
                var reachableFrom2 = GetReachableTiles(testTiles, teleport2);

                bool teleport1ReachableFromStart = reachableFromStart.Contains(teleport1);
                bool teleport2ReachableFromStart = reachableFromStart.Contains(teleport2);

                if (teleport1ReachableFromStart != teleport2ReachableFromStart)
                {
                    GD.Print($"[HexMapGenerator] 传送门验证失败: 洞穴 {hole.Coord} 会将传送门对分割到不同区域");
                    return false;
                }
            }

            return true;
        }

        private HashSet<HexCoord> GetReachableTiles(Dictionary<HexCoord, HexTile> tiles, HexCoord start)
        {
            var reachable = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            reachable.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !reachable.Contains(neighbor))
                    {
                        var tile = tiles[neighbor];
                        if (tile != null && tile.CanEnter)
                        {
                            reachable.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return reachable;
        }

        private void PlaceRandomEvents(Dictionary<HexCoord, HexTile> tiles, List<HexCoord> candidates,
            HexEventType eventType, int count)
        {
            var available = candidates
                .Where(c => tiles[c].EventType == HexEventType.Empty)
                .ToList();

            Shuffle(available);

            int placed = 0;
            for (int i = 0; i < available.Count && placed < count; i++)
            {
                var coord = available[i];
                if (!CanPlaceEventAt(tiles, coord, eventType))
                {
                    continue;
                }

                tiles[coord].EventType = eventType;
                tiles[coord].TriggerCount = GetTriggerCountForEvent(eventType);
                placed++;

                switch (eventType)
                {
                    case HexEventType.BattleNormal:
                        tiles[coord].DisplayName = "遭遇敌人";
                        tiles[coord].IconPath = "res://Assets/Icons/enemy_cultist.svg";
                        tiles[coord].EnemyConfig = "normal_wave";
                        break;
                    case HexEventType.BattleElite:
                        tiles[coord].DisplayName = "精英敌人";
                        tiles[coord].IconPath = "res://Assets/Icons/enemy_elite.svg";
                        tiles[coord].EnemyConfig = "elite_wave";
                        break;
                    case HexEventType.Heal:
                        tiles[coord].DisplayName = "生命之泉";
                        tiles[coord].IconPath = "res://Assets/Icons/heal.png";
                        tiles[coord].HealAmount = 20;
                        break;
                    case HexEventType.GainBlackMark:
                        tiles[coord].DisplayName = "黑印碎片";
                        tiles[coord].IconPath = "res://Assets/Icons/black_mark.png";
                        tiles[coord].BlackMarkGain = 50;
                        break;
                    case HexEventType.Shop:
                        tiles[coord].DisplayName = "神秘商店";
                        tiles[coord].IconPath = "res://Assets/Icons/shop.png";
                        break;
                }
            }
        }

        private int GetTriggerCountForEvent(HexEventType eventType)
        {
            switch (eventType)
            {
                case HexEventType.Shop:
                case HexEventType.TwoWayTeleport:
                case HexEventType.OneDirectionTele:
                    return HexTile.InfiniteTriggers;
                default:
                    return 1;
            }
        }

        private bool CanPlaceEventAt(Dictionary<HexCoord, HexTile> tiles, HexCoord coord, HexEventType eventType)
        {
            if (!tiles.ContainsKey(coord))
                return false;

            foreach (var neighbor in coord.GetNeighbors())
            {
                if (tiles.TryGetValue(neighbor, out var neighborTile) && neighborTile.EventType == eventType)
                {
                    return false;
                }
            }

            return true;
        }

        private bool VerifyNoAdjacentSameEvents(Dictionary<HexCoord, HexTile> tiles)
        {
            foreach (var tile in tiles.Values)
            {
                if (tile.EventType == HexEventType.Empty)
                    continue;

                foreach (var neighbor in tile.Coord.GetNeighbors())
                {
                    if (!tiles.TryGetValue(neighbor, out var neighborTile))
                        continue;

                    if (neighborTile.EventType == tile.EventType)
                    {
                        GD.PrintErr($"[HexMapGenerator] 相邻重复事件: {tile.Coord} 与 {neighbor}, 类型 {tile.EventType}");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool VerifyReachable(HexCoord start, HexCoord end, Dictionary<HexCoord, HexTile> tiles)
        {
            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);

            // D12：多簇之后跨簇只能靠传送门 → 可达性判定必须把传送算成边
            Dictionary<HexCoord, List<HexCoord>> teleports = BuildTeleportIndex(tiles);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                    return true;

                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = tiles[neighbor];
                        if (tile != null && tile.CanEnter)
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                List<HexCoord> teleTargets;
                if (teleports.TryGetValue(current, out teleTargets))
                {
                    foreach (HexCoord target in teleTargets)
                    {
                        if (!tiles.ContainsKey(target) || visited.Contains(target)) continue;
                        if (!tiles[target].CanEnter) continue;

                        queue.Enqueue(target);
                    }
                }
            }

            return false;
        }

        private void EnsurePathConnectivity(Dictionary<HexCoord, HexTile> tiles, HexCoord start, HexCoord end)
        {
            if (!VerifyReachable(start, end, tiles))
            {
                GD.PrintErr("[HexMapGenerator] 警告: 地图存在不可达区域!");
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private DifficultyConfig GetDifficultyConfig(int playerLevel)
        {
            if (playerLevel <= 5)
            {
                return new DifficultyConfig
                {
                    NormalBattleCount = 2,
                    EliteBattleCount = 1,
                    HealCount = 1,
                    BlackMarkCount = 2,
                    ShopCount = 2,
                    SwampCount = 1,
                    HoleCount = 2,
                    SearchlightCount = 1,
                    TwoWayTeleportCount = 1,
                    OneDirectionTeleportCount = 1
                };
            }
            else if (playerLevel <= 10)
            {
                return new DifficultyConfig
                {
                    NormalBattleCount = 3,
                    EliteBattleCount = 2,
                    HealCount = 2,
                    BlackMarkCount = 3,
                    ShopCount = 2,
                    SwampCount = 2,
                    HoleCount = 3,
                    TwoWayTeleportCount = 2,
                    OneDirectionTeleportCount = 2
                };
            }
            else
            {
                return new DifficultyConfig
                {
                    NormalBattleCount = 4,
                    EliteBattleCount = 2,
                    HealCount = 2,
                    BlackMarkCount = 4,
                    ShopCount = 2,
                    SwampCount = 3,
                    HoleCount = 4,
                    TwoWayTeleportCount = 2,
                    OneDirectionTeleportCount = 3
                };
            }
        }

        private class DifficultyConfig
        {
            public int NormalBattleCount { get; set; }
            public int EliteBattleCount { get; set; }
            public int HealCount { get; set; }
            public int BlackMarkCount { get; set; }
            public int ShopCount { get; set; }
            public int SwampCount { get; set; }
            public int HoleCount { get; set; }
            /// <summary>探照灯数量（每关 1~3 个，踩上去可打开开关扩大视野）</summary>
            public int SearchlightCount { get; set; }
            public int TwoWayTeleportCount { get; set; }
            public int OneDirectionTeleportCount { get; set; }
        }
    }
}
