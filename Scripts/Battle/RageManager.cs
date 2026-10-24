using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.CharacterSystem;

public partial class RageManager : Node
{
    public const int RageOnAttack = 15;
    public const int RageOnDefense = 10;
    public const int RageOnDamaged = 5;
    public const int RageOnTurnEnd = 5;

    /// <summary>
    /// 打出这张牌能拿到多少怒气 —— **唯一真相源**。
    ///
    /// 卡面文案（<see cref="DescriptionGenerator"/>）与战斗结算（<c>BattleManager</c>）
    /// 都从这里取数，避免出现「卡面写 5 点、实际给 15 点」这种自相矛盾。
    /// </summary>
    /// <param name="card">打出的卡牌</param>
    /// <returns>本次获得的怒气值（不是攻击牌 / 防御牌则为 0）</returns>
    public static int GetRageForCard(Card card)
    {
        if (card == null) return 0;
        // 卡牌**自带**的怒气优先（灵知觉醒这类：参考图「净世之路」50 点）——
        // 它不属于「攻击牌 15 / 防御牌 10」那套按卡位给的默认值。
        if (card.RageGain > 0) return card.RageGain;
        if (card.IsAttack) return RageOnAttack;
        // 护盾判定用「实际护盾值」而不是卡牌上的绝对字段（现在是百分比口径）
        if (card.IsDefense || card.GetFinalShield(card.LinkedAttributes) > 0) return RageOnDefense;
        return 0;
    }

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
