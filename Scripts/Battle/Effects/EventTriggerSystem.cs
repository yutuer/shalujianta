using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public static class EventTriggerSystem
{
    public static void TriggerEvent(TriggerType triggerType, EffectContext context)
    {
        switch (triggerType)
        {
            case TriggerType.OnTurnStart:
                OnTurnStart(context);
                break;
            case TriggerType.OnTurnEnd:
                OnTurnEnd(context);
                break;
            case TriggerType.OnDamageDealt:
                OnDamageDealt(context);
                break;
            case TriggerType.OnDamageReceived:
                OnDamageReceived(context);
                break;
            case TriggerType.OnHeal:
                OnHeal(context);
                break;
            case TriggerType.OnKill:
                OnKill(context);
                break;
            case TriggerType.OnCardPlayed:
                OnCardPlayed(context);
                break;
        }
    }

    private static void OnTurnStart(EffectContext context)
    {
        if (context.Owner is Enemy enemy)
        {
            foreach (var statusEffect in enemy.StatusEffects)
            {
                statusEffect.OnTurnStartEnemy(enemy);
            }
        }
        else if (context.Owner is Player player)
        {
            foreach (var statusEffect in player.StatusEffects)
            {
                statusEffect.OnTurnStartPlayer(player);
            }
        }
    }

    private static void OnTurnEnd(EffectContext context)
    {
        if (context.Owner is Enemy enemy)
        {
            var expiredEffects = new List<Buffs.StatusEffect>();
            foreach (var statusEffect in enemy.StatusEffects)
            {
                statusEffect.OnTurnEndEnemy(enemy);
                if (statusEffect.IsExpired())
                {
                    expiredEffects.Add(statusEffect);
                }
            }
            foreach (var expired in expiredEffects)
            {
                expired.OnRemoveEnemy(enemy);
                enemy.RemoveStatusEffect(expired);
            }
        }
        else if (context.Owner is Player player)
        {
            var expiredEffects = new List<Buffs.StatusEffect>();
            foreach (var statusEffect in player.StatusEffects)
            {
                statusEffect.OnTurnEndPlayer(player);
                if (statusEffect.IsExpired())
                {
                    expiredEffects.Add(statusEffect);
                }
            }
            foreach (var expired in expiredEffects)
            {
                expired.OnRemovePlayer(player);
                player.RemoveStatusEffect(expired);
            }
        }
    }

    private static void OnDamageDealt(EffectContext context)
    {
        if (context.Owner is Enemy attacker && context.Target != null)
        {
            foreach (var statusEffect in attacker.StatusEffects)
            {
                statusEffect.ModifyDamageEnemy(context.Value, attacker, context.Target as Enemy);
            }
        }
    }

    private static void OnDamageReceived(EffectContext context)
    {
        if (context.Target is Enemy defender && context.Owner != null)
        {
            foreach (var statusEffect in defender.StatusEffects)
            {
                int modifiedDamage = statusEffect.ModifyDamageEnemy(context.Value, context.Owner as Enemy, defender);
                context.Value = modifiedDamage;
            }
        }
    }

    private static void OnHeal(EffectContext context)
    {
        if (context.Target is Player player)
        {
            foreach (var statusEffect in player.StatusEffects)
            {
                int modifiedHeal = statusEffect.ModifyHealing(context.Value, player);
                context.Value = modifiedHeal;
            }
        }
    }

    private static void OnKill(EffectContext context)
    {
    }

    private static void OnCardPlayed(EffectContext context)
    {
    }

    public static void ApplyStatusEffectsFromList(List<Buffs.StatusEffect> effects, IUnit owner)
    {
        foreach (var effect in effects)
        {
            if (owner is Enemy enemy)
            {
                effect.OnApplyEnemy(enemy);
            }
            else if (owner is Player player)
            {
                effect.OnApplyPlayer(player);
            }
        }
    }
}
