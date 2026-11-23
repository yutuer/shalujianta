using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Card;

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
            // ⚠️ **随机敌人**（D73：詹金「鼠群冲击」：「对随机敌人造成…伤害」）
            TargetType.Random => GetRandomEnemy(),
            // ⚠️ **生命最高 / 最低**（D74）：宁菲亚「夜潮汹涌」打生命最高的；
            //    庞托斯「无休猎宴」打生命最低的。
            TargetType.HighestHp => GetHighestHpEnemy(),
            TargetType.LowestHp => GetLowestHpEnemy(),
            _ => GetFrontmostEnemy()
        };
    }

    /// <summary>
    /// 随机挑一个**存活**的敌人（D73）。
    ///
    /// wiki 原文：「对**随机敌人**造成 攻击力×30% 点伤害 X+4 次。」
    /// → 每次命中都可能是**不同的敌人**（所以走的是「对随机敌人」这个目标，而不是固定前排）。
    /// </summary>
    private Enemy GetRandomEnemy()
    {
        List<Enemy> alive = GetLivingEnemies();
        if (alive == null || alive.Count == 0) return null;

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        return alive[rng.RandiRange(0, alive.Count - 1)];
    }

    /// <summary>
    /// **生命最高的存活敌人**（D74：宁菲亚「夜潮汹涌」）。
    ///
    /// wiki 原文：「对**生命最高**的敌人造成 [攻击力*22.5%] 点伤害并施加等量中毒。」
    /// </summary>
    public Enemy GetHighestHpEnemy()
    {
        Enemy best = null;
        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (best == null || enemy.CurrentHealth > best.CurrentHealth) best = enemy;
        }
        return best;
    }

    /// <summary>
    /// **生命最低的存活敌人**（D74：庞托斯「无休猎宴」）。
    ///
    /// wiki 原文：「对**生命最低**的敌人造成等同于 3000% 触腕伤害的固定伤害。」
    /// </summary>
    public Enemy GetLowestHpEnemy()
    {
        Enemy best = null;
        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (best == null || enemy.CurrentHealth < best.CurrentHealth) best = enemy;
        }
        return best;
    }
}
