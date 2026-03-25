using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FishEatFish.Battle.HexMap
{
    public partial class HexMap
    {
        private readonly Dictionary<HexCoord, HexTile> _tiles;
        public HexCoord Start { get; }
        public HexCoord End { get; }
        public int PlayerLevel { get; }

        public HexMap(Dictionary<HexCoord, HexTile> tiles, HexCoord start, HexCoord end, int playerLevel = 1)
        {
            _tiles = tiles;
            Start = start;
            End = end;
            PlayerLevel = playerLevel;
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
            return _tiles.Values
                .FirstOrDefault(t =>
                    t.TeleportPairId == pairId &&
                    t.Coord != currentCoord)?.Coord ?? currentCoord;
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

        public bool IsReachable(HexCoord start, HexCoord end)
        {
            if (!_tiles.ContainsKey(start) || !_tiles.ContainsKey(end))
                return false;

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
                    if (_tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = _tiles[neighbor];
                        if (tile.CanEnter)
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        public List<HexCoord> FindShortestPath(HexCoord start, HexCoord end)
        {
            if (!_tiles.ContainsKey(start) || !_tiles.ContainsKey(end))
                return new List<HexCoord>();

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
                    if (_tiles.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        var tile = _tiles[neighbor];
                        if (tile.CanEnter)
                        {
                            visited.Add(neighbor);
                            cameFrom[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
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

        public HashSet<HexCoord> GetCriticalPathTiles()
        {
            var path = FindShortestPath(Start, End);
            return new HashSet<HexCoord>(path);
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
