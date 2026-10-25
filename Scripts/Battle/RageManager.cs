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
    ///
    /// ⚠️ **口径照参考游戏《终末前夜 Morimens》**（2026-10-25 主人拍板，全局统一）：
    ///   - **基本卡（打击 / 防御）**：<c>5 + 1 × (等级 − 1)</c> → Lv1 = 5、Lv3 = 7、Lv6 = 10
    ///   - **灵知觉醒卡**：<c>25 + 5 × (等级 − 1)</c> → Lv4 = 40、Lv6 = 50
    ///   - **其他专属指令卡**：**不给怒气**（参考图属性面板里没有「狂气」行）
    ///
    /// 公式存在**效果列表**里（<c>{"type":"rage","value":5,"perLevel":1}</c>），
    /// 这样「没有 rage 条目」= 0 怒气，语义明确。
    /// </summary>
    /// <param name="card">打出的卡牌</param>
    /// <returns>本次获得的怒气值（不给怒气的卡为 0）</returns>
    public static int GetRageForCard(Card card)
    {
        if (card == null) return 0;

        // 唯一真相源 = 效果列表里的 rage 条目 → 怒气 = value + perLevel × (等级 − 1)
        CardEffect effect = card.FindEffect(CardEffectType.Rage);
        if (effect != null) return CardLevelSystem.CalculateFixedValue(card.Level, effect.Value, effect.PerLevel);

        // ⚠️ **没有 rage 条目 = 这张卡不给怒气**（参考游戏口径：只有基本卡与灵知觉醒卡给）。
        //    以前这里会「按卡位兜底」（攻击牌 15 / 防御牌 10）——
        //    那会让「蟾蜍炖菜」这种专属指令卡白捡 10 点怒气，和参考图对不上。
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
    /// 使用指定位置角色的狂气爆发
    /// </summary>
    /// <param name="positionIndex">角色位置索引 (0-3)</param>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人数组</param>
    /// <returns>是否成功释放狂气爆发</returns>
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
