using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : Node
{
    private static MapManager _instance;
    public static MapManager Instance => _instance;

    private MapDefinition _currentMap;
    private int _currentBattleIndex = 0;
    private bool _isMapCompleted = false;

    private int _silverKeyValue = 0;
    private Dictionary<int, int> _characterRageValues = new Dictionary<int, int>();
    private int _maxCreatureCount = 10;

    private List<MapDefinition> _availableMaps = new List<MapDefinition>();

    /// <summary>
    /// 地图缓存（按 mapId）—— 关卡是**固定内容**：同一关卡重复进入要拿到同一张图。
    /// 放在 autoload 里，所以退出关卡再回来、战斗往返都复用同一张。
    /// </summary>
    private readonly Dictionary<string, FishEatFish.Battle.HexMap.HexMap> _mapCache
        = new Dictionary<string, FishEatFish.Battle.HexMap.HexMap>();

    /// <summary>上次完成这张图的日期（yyyy-MM-dd），空串 = 从未完成过</summary>
    private string _lastCompletionDate = "";

    public int MaxCreatureCount => _maxCreatureCount;

    /// <summary>地图进度变化时触发（战斗推进 / 关卡开始 / 关卡完成），供评分项面板等订阅者刷新</summary>
    public event Action OnProgressChanged;

    /// <summary>今天是不是这张图的首次游玩（用来判定「每日首次游玩」类评分项）</summary>
    public bool IsFirstPlayToday => _lastCompletionDate != DateTime.Now.ToString("yyyy-MM-dd");

    /// <summary>今天是否已经玩过这张图</summary>
    public bool HasPlayedToday => !IsFirstPlayToday;

    /// <summary>上次完成这张图的日期（yyyy-MM-dd）</summary>
    public string LastCompletionDate => _lastCompletionDate;

    public override void _Ready()
    {
        _instance = this;
        InitializeMaps();
    }

    private void InitializeMaps()
    {
        _availableMaps.AddRange(MapDefinition.GetAllMaps());
    }

    public void StartMap(MapDefinition map)
    {
        _currentMap = map;
        _currentBattleIndex = 0;
        _isMapCompleted = false;
        _silverKeyValue = 0;
        _characterRageValues.Clear();
        _maxCreatureCount = 10;
        OnProgressChanged?.Invoke();
    }

    public void SetMaxCreatureCount(int count)
    {
        _maxCreatureCount = Mathf.Max(1, count);
        GD.Print($"[MapManager] SetMaxCreatureCount: {_maxCreatureCount}");
    }

    public bool CanAcquireArtifact()
    {
        if (FishEatFish.Shop.BlackMarkShopManager.Instance == null)
        {
            return false;
        }
        var ownedCount = FishEatFish.Shop.BlackMarkShopManager.Instance.GetOwnedArtifactsCount();
        return ownedCount < _maxCreatureCount;
    }

    public MapDefinition GetCurrentMap()
    {
        return _currentMap;
    }

    public int GetCurrentBattleIndex()
    {
        return _currentBattleIndex;
    }

    public int GetTotalBattleCount()
    {
        return _currentMap?.BattleCount ?? 0;
    }

    public bool HasNextBattle()
    {
        return _currentMap != null && _currentBattleIndex < _currentMap.BattleCount - 1;
    }

    public void CompleteBattle()
    {
        if (HasNextBattle())
        {
            _currentBattleIndex++;
        }
        else
        {
            CompleteMap();
        }
        OnProgressChanged?.Invoke();
    }

    public void CompleteMap()
    {
        _isMapCompleted = true;
        if (_currentMap != null)
        {
            _currentMap.IsCompleted = true;
            // 地图完成 = 所有战斗都打完了：战斗索引推进到总数，
            // 这样「剩余战斗数」才算得对（评分项面板的 remaining_battles 判定依赖它）
            _currentBattleIndex = _currentMap.BattleCount;
        }
        // 记下今天的日期：今天再进这张图就不再是「每日首次游玩」了
        _lastCompletionDate = DateTime.Now.ToString("yyyy-MM-dd");
        GD.Print($"[MapManager] 关卡完成，记录完成日期: {_lastCompletionDate}");
    }

    public bool IsMapCompleted()
    {
        return _isMapCompleted;
    }

    public int GetMapReward()
    {
        return _currentMap?.GoldReward ?? 0;
    }

    public void AddSilverKey(int value)
    {
        _silverKeyValue = value;
        OnProgressChanged?.Invoke();
    }

    public int GetSilverKey()
    {
        return _silverKeyValue;
    }

    public void ResetSilverKey()
    {
        _silverKeyValue = 0;
    }

    public void SetCharacterRage(int positionIndex, int rage)
    {
        _characterRageValues[positionIndex] = rage;
    }

    public int GetCharacterRage(int positionIndex)
    {
        if (_characterRageValues.TryGetValue(positionIndex, out int rage))
        {
            return rage;
        }
        return 0;
    }

    public Dictionary<int, int> GetAllCharacterRage()
    {
        return new Dictionary<int, int>(_characterRageValues);
    }

    public void ResetAllCharacterRage()
    {
        _characterRageValues.Clear();
    }

    public List<MapDefinition> GetAvailableMaps()
    {
        return new List<MapDefinition>(_availableMaps);
    }

    public void UnlockMap(string mapId)
    {
        var map = _availableMaps.Find(m => m.MapId == mapId);
        if (map != null)
        {
            map.IsUnlocked = true;
        }
    }

    public bool IsMapUnlocked(string mapId)
    {
        var map = _availableMaps.Find(m => m.MapId == mapId);
        return map?.IsUnlocked ?? false;
    }

    public void ResetMapProgress()
    {
        _currentMap = null;
        _currentBattleIndex = 0;
        _isMapCompleted = false;
        _silverKeyValue = 0;
        _characterRageValues.Clear();
    }

    // ===== 地图缓存（D14：关卡是固定内容，重复进入拿同一张图）=====

    /// <summary>取缓存的地图；没有则返回 null</summary>
    public FishEatFish.Battle.HexMap.HexMap GetCachedMap(string mapId)
    {
        if (string.IsNullOrEmpty(mapId)) return null;

        FishEatFish.Battle.HexMap.HexMap map;
        return _mapCache.TryGetValue(mapId, out map) ? map : null;
    }

    /// <summary>把地图放进缓存</summary>
    public void CacheMap(string mapId, FishEatFish.Battle.HexMap.HexMap map)
    {
        if (string.IsNullOrEmpty(mapId) || map == null) return;

        _mapCache[mapId] = map;
        GD.Print($"[MapManager] 缓存地图: {mapId}（缓存数 {_mapCache.Count}）");
    }

    /// <summary>清空地图缓存</summary>
    public void ClearMapCache()
    {
        _mapCache.Clear();
        GD.Print("[MapManager] 地图缓存已清空");
    }
}
