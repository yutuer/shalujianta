using Godot;
using System.Collections.Generic;

public partial class EnemyManager
{
    private List<Enemy> _enemies = new List<Enemy>();

    public void SetEnemies(List<Enemy> enemies)
    {
        _enemies = enemies;
    }

    public List<Enemy> GetLivingEnemies()
    {
        List<Enemy> livingEnemies = new List<Enemy>();

        foreach (Enemy enemy in _enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                livingEnemies.Add(enemy);
            }
        }

        return livingEnemies;
    }

    public bool HasLivingEnemies()
    {
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                return true;
            }
        }

        return false;
    }

    public Enemy GetFrontmostEnemy()
    {
        Enemy frontmost = null;
        int minPosition = int.MaxValue;

        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position < minPosition)
            {
                minPosition = enemy.Position;
                frontmost = enemy;
            }
        }

        return frontmost;
    }

    public Enemy GetRearmostEnemy()
    {
        Enemy rearMost = null;
        int maxPosition = int.MinValue;

        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position > maxPosition)
            {
                maxPosition = enemy.Position;
                rearMost = enemy;
            }
        }

        return rearMost;
    }

    public Enemy GetEnemyAtPosition(int position)
    {
        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position == position)
            {
                return enemy;
            }
        }

        return null;
    }

    public Enemy ResolveTarget(TargetType target, int targetPosition = 0)
    {
        return target switch
        {
            TargetType.Front => GetFrontmostEnemy(),
            TargetType.Rear => GetRearmostEnemy(),
            TargetType.Position => GetEnemyAtPosition(targetPosition),
            _ => GetFrontmostEnemy()
        };
    }
}
