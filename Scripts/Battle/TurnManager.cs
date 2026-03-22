using Godot;
using System;
using System.Collections.Generic;

public partial class TurnManager : Node
{
    public int TurnCount { get; private set; } = 1;
    public bool IsPlayerTurn { get; private set; } = true;

    public event Action OnTurnStarted;
    public event Action OnTurnEnded;
    public event Action OnPlayerTurnStarted;
    public event Action OnEnemyTurnStarted;
    public event Action OnGameOver;

    private Player _player;
    private List<Enemy> _enemies;
    private Func<List<Enemy>> _getEnemiesFunc;

    public void Initialize(Player player, List<Enemy> enemies, Func<List<Enemy>> getEnemiesFunc)
    {
        _player = player;
        _enemies = enemies;
        _getEnemiesFunc = getEnemiesFunc;
    }

    public void StartGame()
    {
        TurnCount = 1;
        IsPlayerTurn = true;
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        TurnCount++;
        _player.StartTurn();
        ProcessEnemyTurnEndEffects();
        OnPlayerTurnStarted?.Invoke();
        OnTurnStarted?.Invoke();
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn) return;

        IsPlayerTurn = false;
        OnTurnEnded?.Invoke();
        ProcessPlayerTurnEndEffects();
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        OnEnemyTurnStarted?.Invoke();
        ProcessEnemyTurnStartEffects();
    }

    public void ProcessEnemyActions(Action<Enemy, Player, List<Enemy>> performAction)
    {
        List<Enemy> enemies = _getEnemiesFunc?.Invoke() ?? _enemies;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                performAction(enemy, _player, enemies);
            }
        }
    }

    public void EndEnemyTurn()
    {
        TurnCount++;
        StartPlayerTurn();
    }

    private void ProcessPlayerTurnEndEffects()
    {
        _player.ProcessTurnEndEffects();
        _player.DiscardHand();
        _player.Shield = 0;
    }

    private void ProcessEnemyTurnEndEffects()
    {
        List<Enemy> enemies = _getEnemiesFunc?.Invoke() ?? _enemies;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                enemy.ProcessTurnEndEffects();
            }
        }
    }

    private void ProcessEnemyTurnStartEffects()
    {
        List<Enemy> enemies = _getEnemiesFunc?.Invoke() ?? _enemies;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                enemy.ProcessTurnStartEffects();
            }
        }
    }

    public bool IsGameOver()
    {
        if (_player.IsDead()) return true;

        List<Enemy> enemies = _getEnemiesFunc?.Invoke() ?? _enemies;
        bool allDead = true;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                allDead = false;
                break;
            }
        }
        return allDead;
    }

    public bool IsPlayerVictory()
    {
        if (_player.IsDead()) return false;

        List<Enemy> enemies = _getEnemiesFunc?.Invoke() ?? _enemies;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                return false;
            }
        }
        return true;
    }

    public bool IsPlayerDefeated()
    {
        return _player.IsDead();
    }
}
