using Godot;

/// <summary>
/// 战斗结束怒气处理器
/// 功能：计算战斗结束后各角色获得的怒气值
/// 怒气获取规则：
/// - 基础怒气：胜利30点，失败10点
/// - 击杀奖励：每个敌人+15点
/// - 满血通关：+20点
/// - 无伤通关：+25点
/// - 3回合内速通：+30点
/// </summary>
public partial class BattleEndRageHandler : Node
{
    private const int BaseRageOnVictory = 30;
    private const int BaseRageOnDefeat = 10;
    private const int RageBonusPerEnemyDefeated = 15;
    private const int RageBonusPerFullHealth = 20;
    private const int RageBonusPerNoDamageTaken = 25;
    private const int RageBonusPerSpeedKill = 30;

    private int _enemiesDefeatedThisBattle = 0;
    private int _damageTakenThisBattle = 0;
    private int _turnsUsed = 0;
    private bool _fullHealthAtBattleEnd = false;
    private bool _noDamageTakenThisBattle = false;

    public override void _Ready()
    {
        GD.Print("[BattleEndRageHandler] 战斗结束怒气处理器初始化");
    }

    /// <summary>
    /// 战斗开始时重置统计数据
    /// </summary>
    public void OnBattleStart()
    {
        ResetBattleStats();
        GD.Print("[BattleEndRageHandler] 战斗开始，重置统计数据");
    }

    /// <summary>
    /// 记录击败敌人
    /// </summary>
    public void OnEnemyDefeated()
    {
        _enemiesDefeatedThisBattle++;
        GD.Print($"[BattleEndRageHandler] 击败敌人，当前击杀数: {_enemiesDefeatedThisBattle}");
    }

    /// <summary>
    /// 记录玩家受到的伤害
    /// </summary>
    /// <param name="damageAmount">伤害数值</param>
    public void OnPlayerDamaged(int damageAmount)
    {
        _damageTakenThisBattle += damageAmount;
        if (_damageTakenThisBattle > 0)
        {
            _noDamageTakenThisBattle = false;
        }
    }

    /// <summary>
    /// 记录回合结束时的回合数
    /// </summary>
    /// <param name="turnNumber">当前回合数</param>
    public void OnTurnEnd(int turnNumber)
    {
        _turnsUsed = turnNumber;
    }

    /// <summary>
    /// 战斗结束时计算怒气获取
    /// </summary>
    /// <param name="victory">是否胜利</param>
    /// <param name="playerCurrentHealth">玩家当前生命</param>
    /// <param name="playerMaxHealth">玩家最大生命</param>
    public void OnBattleEnd(bool victory, int playerCurrentHealth, int playerMaxHealth)
    {
        _fullHealthAtBattleEnd = playerCurrentHealth >= playerMaxHealth;
        _noDamageTakenThisBattle = _damageTakenThisBattle == 0;

        var rageResults = CalculateRageGains(victory);
        GD.Print($"[BattleEndRageHandler] 战斗结束，胜利:{victory}，怒气计算结果:");
        foreach (var result in rageResults)
        {
            GD.Print($"  位置{result.PositionIndex}: +{result.TotalRage} 怒气");
        }
    }

    /// <summary>
    /// 计算怒气获取
    /// 算法：
    /// 1. 根据胜负计算基础怒气
    /// 2. 累加各项奖励怒气（击杀、满血、无伤、速通）
    /// 3. 返回4个位置的怒气计算结果
    /// </summary>
    /// <param name="victory">是否胜利</param>
    /// <returns>怒气计算结果数组</returns>
    public RageCalculationResult[] CalculateRageGains(bool victory)
    {
        var results = new RageCalculationResult[4];

        int baseRage = victory ? BaseRageOnVictory : BaseRageOnDefeat;
        GD.Print($"[BattleEndRageHandler] 基础怒气: {baseRage}");

        for (int i = 0; i < 4; i++)
        {
            results[i] = new RageCalculationResult
            {
                PositionIndex = i,
                BaseRage = baseRage,
                BonusRage = 0,
                TotalRage = baseRage,
                Bonuses = new System.Collections.Generic.List<string>()
            };
        }

        int enemyBonus = _enemiesDefeatedThisBattle * RageBonusPerEnemyDefeated;
        if (enemyBonus > 0)
        {
            GD.Print($"[BattleEndRageHandler] 击杀奖励: +{enemyBonus}");
            for (int i = 0; i < 4; i++)
            {
                results[i].BonusRage += enemyBonus;
                results[i].Bonuses.Add($"击杀{_enemiesDefeatedThisBattle}敌人(+{enemyBonus})");
            }
        }

        if (_fullHealthAtBattleEnd)
        {
            GD.Print($"[BattleEndRageHandler] 满血奖励: +{RageBonusPerFullHealth}");
            for (int i = 0; i < 4; i++)
            {
                results[i].BonusRage += RageBonusPerFullHealth;
                results[i].Bonuses.Add($"满血通关(+{RageBonusPerFullHealth})");
            }
        }

        if (_noDamageTakenThisBattle && victory)
        {
            GD.Print($"[BattleEndRageHandler] 无伤奖励: +{RageBonusPerNoDamageTaken}");
            for (int i = 0; i < 4; i++)
            {
                results[i].BonusRage += RageBonusPerNoDamageTaken;
                results[i].Bonuses.Add($"无伤通关(+{RageBonusPerNoDamageTaken})");
            }
        }

        if (_turnsUsed > 0 && _turnsUsed <= 3 && victory)
        {
            GD.Print($"[BattleEndRageHandler] 速通奖励: +{RageBonusPerSpeedKill}");
            for (int i = 0; i < 4; i++)
            {
                results[i].BonusRage += RageBonusPerSpeedKill;
                results[i].Bonuses.Add($"{_turnsUsed}回合速通(+{RageBonusPerSpeedKill})");
            }
        }

        for (int i = 0; i < 4; i++)
        {
            results[i].TotalRage = results[i].BaseRage + results[i].BonusRage;
        }

        return results;
    }

    /// <summary>
    /// 计算指定位置的怒气获取
    /// </summary>
    public int CalculateTotalRageForPosition(int positionIndex, bool victory)
    {
        var results = CalculateRageGains(victory);
        return results[positionIndex].TotalRage;
    }

    /// <summary>
    /// 获取战斗统计信息
    /// </summary>
    public BattleStatistics GetBattleStatistics()
    {
        return new BattleStatistics
        {
            EnemiesDefeated = _enemiesDefeatedThisBattle,
            DamageTaken = _damageTakenThisBattle,
            TurnsUsed = _turnsUsed,
            FullHealthAtEnd = _fullHealthAtBattleEnd,
            NoDamageTaken = _noDamageTakenThisBattle
        };
    }

    /// <summary>
    /// 重置战斗统计数据
    /// </summary>
    private void ResetBattleStats()
    {
        _enemiesDefeatedThisBattle = 0;
        _damageTakenThisBattle = 0;
        _turnsUsed = 0;
        _fullHealthAtBattleEnd = false;
        _noDamageTakenThisBattle = true;
    }
}

/// <summary>
/// 怒气计算结果数据类
/// </summary>
public class RageCalculationResult
{
    /// <summary>角色位置索引 (0-3)</summary>
    public int PositionIndex { get; set; }
    /// <summary>基础怒气值</summary>
    public int BaseRage { get; set; }
    /// <summary>奖励怒气值</summary>
    public int BonusRage { get; set; }
    /// <summary>总怒气值 = 基础 + 奖励</summary>
    public int TotalRage { get; set; }
    /// <summary>奖励详情列表</summary>
    public System.Collections.Generic.List<string> Bonuses { get; set; } = new System.Collections.Generic.List<string>();
}

/// <summary>
/// 战斗统计数据类
/// </summary>
public class BattleStatistics
{
    /// <summary>击败敌人数</summary>
    public int EnemiesDefeated { get; set; }
    /// <summary>受到的总伤害</summary>
    public int DamageTaken { get; set; }
    /// <summary>使用的回合数</summary>
    public int TurnsUsed { get; set; }
    /// <summary>战斗结束时是否满血</summary>
    public bool FullHealthAtEnd { get; set; }
    /// <summary>是否无伤通关</summary>
    public bool NoDamageTaken { get; set; }
}
