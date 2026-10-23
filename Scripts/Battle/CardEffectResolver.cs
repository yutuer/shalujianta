using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.Card;

namespace FishEatFish.Battle;

/// <summary>
/// 卡牌效果的**旧结算路径**（当前战斗主循环走的是
/// <c>BattleManager.ApplyCardEffects / ApplyCardDamage</c>，本类**没有调用点**）。
///
/// 保留它是为了让「数值口径」在这条路径上也跟主循环一致：
/// 百分比化之后 <c>card.Damage / card.ShieldGain</c> 恒为 0，
/// 哪天有人把它接回去也不会静默算成 0 伤害。
/// </summary>
public static class CardEffectResolver
{
    public static void ResolveCardEffects(FishEatFish.Battle.Card.Card card, Player player, List<Enemy> enemies, System.Action<string, LogType> logCallback)
    {
        ResolveNewEffects(card, player, enemies, logCallback);
        ApplyStatEffects(card, player, logCallback);
        ApplyDrawEffects(card, player, logCallback);
        ApplyStatusEffects(card, player, enemies, logCallback);
    }

    private static void ResolveNewEffects(FishEatFish.Battle.Card.Card card, Player player, List<Enemy> enemies, System.Action<string, LogType> logCallback)
    {
        var effects = card.GetEffects();
        if (effects.Count == 0) return;

        var context = new EffectContext
        {
            Owner = player,
            Source = player,
            CurrentTrigger = TriggerType.Immediate,
            Metadata = new Dictionary<string, object>
            {
                { "LogCallback", logCallback },
                { "Enemies", enemies }
            }
        };

        foreach (var effect in effects)
        {
            effect.Value = GetEffectValueOverride(card, effect);
            EffectResolver.ApplyEffect(effect, context);
        }
    }

    private static int GetEffectValueOverride(FishEatFish.Battle.Card.Card card, Effect effect)
    {
        // ⚠️ 伤害/护盾走 GetFinalDamage/GetFinalShield（百分比口径）：
        // 百分比化之后 card.Damage / card.ShieldGain 恒为 0，直接读会永远拿 0 覆盖效果值。
        switch (effect.Type)
        {
            case EffectType.Damage:
            {
                int damage = card.GetFinalDamage(card.LinkedAttributes);
                return damage > 0 ? damage : effect.Value;
            }
            case EffectType.Shield:
            {
                int shield = card.GetFinalShield(card.LinkedAttributes);
                return shield > 0 ? shield : effect.Value;
            }
            case EffectType.Heal:
                return card.HealAmount > 0 ? card.HealAmount : effect.Value;
            case EffectType.Energy:
                return card.EnergyGain > 0 ? card.EnergyGain : effect.Value;
            case EffectType.Draw:
                return card.DrawCount > 0 ? card.DrawCount : effect.Value;
            default:
                return effect.Value;
        }
    }

    private static void ApplyStatEffects(FishEatFish.Battle.Card.Card card, Player player, System.Action<string, LogType> logCallback)
    {
        int finalShield = card.GetFinalShield(card.LinkedAttributes);
        if (finalShield > 0)
        {
            player.AddShield(finalShield);
            logCallback($"获得{finalShield}点护盾", LogType.Shield);
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

        int finalDamage = card.GetFinalDamage(card.LinkedAttributes);

        if (card.IsAreaAttack)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.TakeDamage(finalDamage);
                logCallback($"对{enemy.EnemyName}造成{finalDamage}点伤害", LogType.Damage);
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

        target.TakeDamage(finalDamage);
        string targetDescription = card.Target switch
        {
            Card.TargetType.Front => $"最前的敌人{target.EnemyName}",
            Card.TargetType.Rear => $"最后的敌人{target.EnemyName}",
            _ => target.EnemyName
        };

        logCallback($"对{targetDescription}造成{finalDamage}点伤害", LogType.Damage);
    }
}
