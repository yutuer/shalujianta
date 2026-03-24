using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.Card;

namespace FishEatFish.Battle;

public static class CardEffectResolver
{
    public static void ResolveCardEffects(FishEatFish.Battle.Card.Card card, Player player, List<Enemy> enemies, System.Action<string, LogType> logCallback)
    {
        ApplyStatEffects(card, player, logCallback);
        ApplyDrawEffects(card, player, logCallback);
        ApplyStatusEffects(card, player, enemies, logCallback);
    }

    private static void ApplyStatEffects(FishEatFish.Battle.Card.Card card, Player player, System.Action<string, LogType> logCallback)
    {
        if (card.ShieldGain > 0)
        {
            player.AddShield(card.ShieldGain);
            logCallback($"获得{card.ShieldGain}点护盾", LogType.Shield);
        }

        if (card.EnergyGain > 0)
        {
            player.CurrentEnergy += card.EnergyGain;
            logCallback($"获得{card.EnergyGain}点能量", LogType.Energy);
        }

        if (card.HealAmount > 0)
        {
            player.Heal(card.HealAmount);
            logCallback($"回复{card.HealAmount}点生命", LogType.Heal);
        }
    }

    private static void ApplyDrawEffects(FishEatFish.Battle.Card.Card card, Player player, System.Action<string, LogType> logCallback)
    {
        if (card.DrawCount > 0)
        {
            player.DrawCards(card.DrawCount);
            logCallback($"抽了{card.DrawCount}张牌", LogType.System);
        }
    }

    private static void ApplyStatusEffects(FishEatFish.Battle.Card.Card card, Player player, List<Enemy> enemies, System.Action<string, LogType> logCallback)
    {
        if (!string.IsNullOrEmpty(card.ApplyBuffName))
        {
            StatusEffect buff = card.CreateBuffFromCard();
            if (buff != null)
            {
                Enemy target = GetTargetFromCard(card, enemies);
                if (target != null)
                {
                    target.AddStatusEffect(buff);
                    logCallback($"给{target.EnemyName}附加了增益效果", LogType.System);
                }
            }
        }

        if (!string.IsNullOrEmpty(card.ApplyDebuffName))
        {
            StatusEffect debuff = card.CreateDebuffFromCard();
            if (debuff != null)
            {
                Enemy target = GetTargetFromCard(card, enemies);
                if (target != null)
                {
                    target.AddStatusEffect(debuff);
                    logCallback($"给{target.EnemyName}附加了减益效果", LogType.System);
                }
            }
        }
    }

    public static Enemy GetTargetFromCard(FishEatFish.Battle.Card.Card card, List<Enemy> enemies)
    {
        enemies.RemoveAll(e => e.IsDead);

        if (enemies.Count == 0) return null;

        switch (card.Target)
        {
            case TargetType.Front:
                return GetFrontmostEnemy(enemies);
            case TargetType.Rear:
                return GetRearmostEnemy(enemies);
            case TargetType.Position:
                return GetEnemyAtPosition(card.TargetPosition, enemies);
            default:
                return GetFrontmostEnemy(enemies);
        }
    }

    private static Enemy GetFrontmostEnemy(List<Enemy> enemies)
    {
        Enemy frontmost = null;
        int minPosition = int.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.Position < minPosition)
            {
                minPosition = enemy.Position;
                frontmost = enemy;
            }
        }

        return frontmost ?? enemies[0];
    }

    private static Enemy GetRearmostEnemy(List<Enemy> enemies)
    {
        Enemy rearMost = null;
        int maxPosition = -1;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.Position > maxPosition)
            {
                maxPosition = enemy.Position;
                rearMost = enemy;
            }
        }

        return rearMost ?? enemies[0];
    }

    private static Enemy GetEnemyAtPosition(int position, List<Enemy> enemies)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Position == position)
            {
                return enemy;
            }
        }
        return null;
    }

    public static void ApplyCardDamage(FishEatFish.Battle.Card.Card card, List<Enemy> enemies, System.Action<string, LogType> logCallback)
    {
        enemies.RemoveAll(e => e.IsDead);
        if (!card.IsAttack || enemies.Count == 0) return;

        if (card.IsAreaAttack)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.TakeDamage(card.Damage);
                logCallback($"对{enemy.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
            }
            return;
        }

        Enemy target = GetTargetFromCard(card, enemies);
        if (target == null)
        {
            if (card.Target == Card.TargetType.Position)
            {
                logCallback($"{card.TargetPosition}号位置没有存活的敌人", LogType.System);
            }
            return;
        }

        target.TakeDamage(card.Damage);
        string targetDescription = card.Target switch
        {
            Card.TargetType.Front => $"最前的敌人{target.EnemyName}",
            Card.TargetType.Rear => $"最后的敌人{target.EnemyName}",
            _ => target.EnemyName
        };

        logCallback($"对{targetDescription}造成{card.Damage}点伤害", LogType.Damage);
    }
}
