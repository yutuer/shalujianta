using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FishEatFish.Battle.HexMap
{
    public class HexMapGenerator
    {
        private readonly Random _random;

        public HexMapGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public HexMap Generate(int radius, int playerLevel)
        {
            var tiles = GenerateHexGrid(radius);

            var startCoord = SelectRandomEdgeTile(tiles, radius);
            var endCoord = FindFarthestTile(tiles, startCoord);

            tiles[startCoord].IsStart = true;
            tiles[endCoord].IsEnd = true;
            tiles[endCoord].EventType = HexEventType.BattleBoss;
            tiles[endCoord].TriggerType = EventTriggerType.OneTime;
            tiles[endCoord].DisplayName = "BOSS";
            tiles[endCoord].IconPath = "res://Assets/Icons/boss.png";

            var criticalPath = FindShortestPath(tiles, startCoord, endCoord);
            var criticalSet = new HashSet<HexCoord>(criticalPath);

            var difficultyConfig = GetDifficultyConfig(playerLevel);

            PlaceHoleTiles(tiles, criticalSet, difficultyConfig.HoleCount);

            PlaceSwampTiles(tiles, criticalSet, difficultyConfig.SwampCount);

            PlaceTwoWayTeleportTiles(tiles, criticalSet, difficultyConfig.TwoWayTeleportCount);

            PlaceOneDirectionTeleportTiles(tiles, criticalSet, difficultyConfig.OneDirectionTeleportCount);

            var nonCriticalEmptyTiles = tiles.Keys
                .Where(c => !criticalSet.Contains(c) && tiles[c].EventType == HexEventType.Empty)
                .ToList();

            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.BattleNormal, difficultyConfig.NormalBattleCount);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.BattleElite, difficultyConfig.EliteBattleCount);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.Heal, difficultyConfig.HealCount);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.GainBlackMark, difficultyConfig.BlackMarkCount);
            PlaceRandomEvents(tiles, nonCriticalEmptyTiles, HexEventType.Shop, difficultyConfig.ShopCount);

            EnsurePathConnectivity(tiles, startCoord, endCoord);

            if (!VerifyAllHolesPlacement(tiles, startCoord, endCoord))
            {
                GD.PrintErr("[HexMapGenerator] 验证失败: 移除所有洞穴后无法到达boss，重新生成地图...");
                return Generate(radius, playerLevel);
            }

            return new HexMap(tiles, startCoord, endCoord, playerLevel);
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

        private List<HexCoord> FindShortestPath(Dictionary<HexCoord, HexTile> tiles, HexCoord start, HexCoord end)
        {
            var visited = new HashSet<HexCoord>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            visited.Add(start);

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

        private void PlaceHoleTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
        {
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

                var neighbors = coord.GetNeighbors()
                    .Where(n => tiles.ContainsKey(n) && !criticalSet.Contains(n))
                    .ToList();

                if (neighbors.Count >= 2)
                {
                    if (VerifyHolePlacement(tiles, coord, neighbors, criticalSet))
                    {
                        tiles[coord].EventType = HexEventType.Hole;
                        tiles[coord].TriggerType = EventTriggerType.Terrain;
                        tiles[coord].DisplayName = "洞穴";
                        tiles[coord].IconPath = "res://Assets/Icons/hole.png";
                        placed++;
                    }
                }

                candidates.Remove(coord);
            }

            GD.Print($"[HexMapGenerator] 放置了 {placed} 个洞穴");
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

        private void PlaceOneDirectionTeleportTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
        {
            int pairsPlaced = 0;
            int attempts = 0;
            int maxAttempts = 50;

            while (pairsPlaced < count / 2 && attempts < maxAttempts)
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
                    if (!VerifyOneDirectionTeleportPlacement(tiles, coord1, coord2, criticalSet))
                    {
                        GD.Print($"[HexMapGenerator] 单向传送门对 ({coord1}, {coord2}) 验证失败");
                        continue;
                    }

                    var pairId = $"one_dir_tele_{pairsPlaced}";

                    tiles[coord1].EventType = HexEventType.OneDirectionTele;
                    tiles[coord1].TriggerType = EventTriggerType.Terrain;
                    tiles[coord1].TeleportPairId = pairId;
                    tiles[coord1].TeleportDirection = TeleportDirection.Forward;
                    tiles[coord1].DisplayName = "单向传送门1→2";
                    tiles[coord1].IconPath = "res://Assets/Icons/one_dir_tele.png";

                    tiles[coord2].EventType = HexEventType.OneDirectionTele;
                    tiles[coord2].TriggerType = EventTriggerType.Terrain;
                    tiles[coord2].TeleportPairId = pairId;
                    tiles[coord2].TeleportDirection = TeleportDirection.Backward;
                    tiles[coord2].DisplayName = "单向传送门2→1";
                    tiles[coord2].IconPath = "res://Assets/Icons/one_dir_tele.png";

                    pairsPlaced++;
                }
            }

            GD.Print($"[HexMapGenerator] 放置了 {pairsPlaced} 对单向传送门");
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
                tiles[coord].EventType = HexEventType.Swamp;
                tiles[coord].TriggerType = EventTriggerType.Terrain;
                tiles[coord].Damage = 10;
                tiles[coord].DisplayName = "沼泽";
                tiles[coord].IconPath = "res://Assets/Icons/swamp.png";
            }
        }

        private void PlaceTwoWayTeleportTiles(Dictionary<HexCoord, HexTile> tiles, HashSet<HexCoord> criticalSet, int count)
        {
            int pairsPlaced = 0;
            int attempts = 0;
            int maxAttempts = 50;

            var existingHoles = tiles.Values.Where(t => t.EventType == HexEventType.Hole).ToList();

            while (pairsPlaced < count / 2 && attempts < maxAttempts)
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
                    if (!VerifyTeleportPlacement(tiles, coord1, coord2, criticalSet))
                    {
                        GD.Print($"[HexMapGenerator] 传送门对 ({coord1}, {coord2}) 验证失败");
                        continue;
                    }

                    var pairId = $"twoway_teleport_{pairsPlaced}";

                    tiles[coord1].EventType = HexEventType.TwoWayTeleport;
                    tiles[coord1].TriggerType = EventTriggerType.Terrain;
                    tiles[coord1].TeleportPairId = pairId;
                    tiles[coord1].DisplayName = "双向传送门A";
                    tiles[coord1].IconPath = "res://Assets/Icons/teleport_two_way.png";

                    tiles[coord2].EventType = HexEventType.TwoWayTeleport;
                    tiles[coord2].TriggerType = EventTriggerType.Terrain;
                    tiles[coord2].TeleportPairId = pairId;
                    tiles[coord2].DisplayName = "双向传送门B";
                    tiles[coord2].IconPath = "res://Assets/Icons/teleport_two_way.png";

                    pairsPlaced++;
                }
            }

            GD.Print($"[HexMapGenerator] 放置了 {pairsPlaced} 对双向传送门");
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
            Shuffle(candidates);

            for (int i = 0; i < Math.Min(count, candidates.Count); i++)
            {
                var coord = candidates[i];
                tiles[coord].EventType = eventType;
                tiles[coord].TriggerType = GetTriggerTypeForEvent(eventType);

                switch (eventType)
                {
                    case HexEventType.BattleNormal:
                        tiles[coord].DisplayName = "遭遇敌人";
                        tiles[coord].IconPath = "res://Assets/Icons/enemy.png";
                        tiles[coord].EnemyConfig = "normal_wave";
                        break;
                    case HexEventType.BattleElite:
                        tiles[coord].DisplayName = "精英敌人";
                        tiles[coord].IconPath = "res://Assets/Icons/elite.png";
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
                        tiles[coord].BlackMarkGain = 5;
                        break;
                    case HexEventType.Shop:
                        tiles[coord].DisplayName = "神秘商店";
                        tiles[coord].IconPath = "res://Assets/Icons/shop.png";
                        break;
                }
            }
        }

        private EventTriggerType GetTriggerTypeForEvent(HexEventType eventType)
        {
            switch (eventType)
            {
                case HexEventType.Shop:
                case HexEventType.BattleNormal:
                case HexEventType.BattleElite:
                case HexEventType.BattleBoss:
                case HexEventType.Heal:
                case HexEventType.GainBlackMark:
                    return EventTriggerType.OneTime;
                case HexEventType.Swamp:
                case HexEventType.TwoWayTeleport:
                case HexEventType.OneDirectionTele:
                case HexEventType.Hole:
                    return EventTriggerType.Terrain;
                default:
                    return EventTriggerType.OneTime;
            }
        }

        private bool VerifyReachable(HexCoord start, HexCoord end, Dictionary<HexCoord, HexTile> tiles)
        {
            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);

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
                    ShopCount = 1,
                    SwampCount = 1,
                    HoleCount = 2,
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
                    ShopCount = 1,
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
            public int TwoWayTeleportCount { get; set; }
            public int OneDirectionTeleportCount { get; set; }
        }
    }
}
