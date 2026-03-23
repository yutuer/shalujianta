using Godot;
using System.Collections.Generic;

public partial class MapManager : Node
{
    private MapDefinition _currentMap;
    private int _currentBattleIndex = 0;
    private bool _isMapCompleted = false;

    private int _silverKeyValue = 0;
    private Dictionary<int, int> _characterRageValues = new Dictionary<int, int>();

    private List<MapDefinition> _availableMaps = new List<MapDefinition>();

    public override void _Ready()
    {
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
    }

    public void CompleteMap()
    {
        _isMapCompleted = true;
        if (_currentMap != null)
        {
            _currentMap.IsCompleted = true;
        }
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
}
