using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.CharacterSystem;

public partial class RageManager : Node
{
    private const int RageOnAttack = 15;
    private const int RageOnDefense = 10;
    private const int RageOnDamaged = 5;
    private const int RageOnTurnEnd = 5;

    private List<CharacterPosition> _characterPositions = new List<CharacterPosition>();

    public override void _Ready()
    {
    }

    public void RegisterCharacterPosition(CharacterPosition position)
    {
        if (!_characterPositions.Contains(position))
        {
            _characterPositions.Add(position);
        }
    }

    public void UnregisterCharacterPosition(CharacterPosition position)
    {
        _characterPositions.Remove(position);
    }

    public void OnAttackCardPlayed(CharacterPosition position)
    {
        if (position != null)
        {
            position.AddRage(RageOnAttack);
        }
    }

    public void OnDefenseCardPlayed(CharacterPosition position)
    {
        if (position != null)
        {
            position.AddRage(RageOnDefense);
        }
    }

    public void OnCharacterDamaged(CharacterPosition position)
    {
        if (position != null)
        {
            position.AddRage(RageOnDamaged);
        }
    }

    public void OnTurnEnd()
    {
        foreach (var position in _characterPositions)
        {
            if (position != null)
            {
                position.AddRage(RageOnTurnEnd);
            }
        }
    }

    public void OnTurnStart()
    {
    }

    public int GetCharacterRage(int positionIndex)
    {
        if (positionIndex >= 0 && positionIndex < _characterPositions.Count)
        {
            return _characterPositions[positionIndex]?.CurrentRage ?? 0;
        }
        return 0;
    }

    public bool CanUseUltimate(int positionIndex)
    {
        if (positionIndex >= 0 && positionIndex < _characterPositions.Count)
        {
            return _characterPositions[positionIndex]?.CanUseUltimate() ?? false;
        }
        return false;
    }

    /// <summary>
    /// 使用指定位置角色的大招
    /// </summary>
    /// <param name="positionIndex">角色位置索引 (0-3)</param>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人数组</param>
    /// <returns>是否成功释放大招</returns>
    public bool UseUltimate(int positionIndex, Player player, Enemy[] enemies)
    {
        GD.Print("[RageManager] UseUltimate called");

        bool success = false;

        if (positionIndex >= 0 && positionIndex < _characterPositions.Count)
        {
            CharacterPosition position = _characterPositions[positionIndex];
            if (position != null)
            {
                success = position.TryUseUltimate(player, enemies, out string logMessage);
                GD.Print($"[RageManager] {logMessage}");
            }
            else
            {
                GD.PrintErr($"[RageManager] 位置{positionIndex}没有对应的角色位置节点");
            }
        }
        else
        {
            GD.PrintErr($"[RageManager] 位置索引越界：{positionIndex}");
        }

        GD.Print("[RageManager] UseUltimate completed");
        return success;
    }

    public List<CharacterPosition> GetCharactersWithFullRage()
    {
        return _characterPositions.FindAll(pos => pos?.CanUseUltimate() == true);
    }

    public void ResetAllRage()
    {
        foreach (var position in _characterPositions)
        {
            if (position != null)
            {
                position.ResetRage();
            }
        }
    }

    public Dictionary<int, int> GetAllRageValues()
    {
        var rageValues = new Dictionary<int, int>();
        for (int i = 0; i < _characterPositions.Count; i++)
        {
            rageValues[i] = _characterPositions[i]?.CurrentRage ?? 0;
        }
        return rageValues;
    }
}
