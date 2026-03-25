using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Effects;

public static class EffectResolver
{
    public static void ResolveEffects(List<Effect> effects, EffectContext context)
    {
        foreach (var effect in effects)
        {
            if (effect.Trigger == context.CurrentTrigger || effect.Trigger == TriggerType.Immediate)
            {
                ApplyEffect(effect, context);
            }
        }
    }

    public static void ApplyEffect(Effect effect, EffectContext context)
    {
        switch (effect)
        {
            case Effects.DamageEffect damageEffect:
                ApplyDamage(damageEffect, context);
                break;
            case Effects.ShieldEffect shieldEffect:
                ApplyShield(shieldEffect, context);
                break;
            case Effects.HealEffect healEffect:
                ApplyHeal(healEffect, context);
                break;
            case Effects.EnergyEffect energyEffect:
                ApplyEnergy(energyEffect, context);
                break;
            case Effects.DrawEffect drawEffect:
                ApplyDraw(drawEffect, context);
                break;
            case Effects.ApplyBuffEffect applyBuff:
                ApplyBuff(applyBuff, context);
                break;
            case Effects.ApplyDebuffEffect applyDebuff:
                ApplyDebuff(applyDebuff, context);
                break;
            default:
                effect.Apply(context);
                break;
        }
    }

    private static void ApplyDamage(Effects.DamageEffect effect, EffectContext context)
    {
        var targets = effect.ResolveTargets(context);
        foreach (var target in targets)
        {
            int damage = effect.Value;
            target.TakeDamage(damage);
        }
    }

    private static void ApplyShield(Effects.ShieldEffect effect, EffectContext context)
    {
        var targets = effect.ResolveTargets(context);
        foreach (var target in targets)
        {
            target.AddShield(effect.Value);
        }
    }

    private static void ApplyHeal(Effects.HealEffect effect, EffectContext context)
    {
        var targets = effect.ResolveTargets(context);
        foreach (var target in targets)
        {
            int healAmount = effect.IsPercent
                ? (int)(target.MaxHealth * effect.Value / 100.0f)
                : effect.Value;
            target.Heal(healAmount);
        }
    }

    private static void ApplyEnergy(Effects.EnergyEffect effect, EffectContext context)
    {
        if (context.Owner is Player player)
        {
            player.CurrentEnergy += effect.Value;
        }
    }

    private static void ApplyDraw(Effects.DrawEffect effect, EffectContext context)
    {
        if (context.Owner is Player player)
        {
            player.DrawCards(effect.Value);
        }
    }

    private static void ApplyBuff(Effects.ApplyBuffEffect effect, EffectContext context)
    {
        var targets = effect.ResolveTargets(context);
        foreach (var target in targets)
        {
            StatusEffect buff = EffectRegistry.CreateBuff(effect.BuffId);
            if (buff != null)
            {
                buff.Initialize();
                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(buff);
                }
            }
        }
    }

    private static void ApplyDebuff(Effects.ApplyDebuffEffect effect, EffectContext context)
    {
        var targets = effect.ResolveTargets(context);
        foreach (var target in targets)
        {
            StatusEffect debuff = EffectRegistry.CreateDebuff(effect.DebuffId);
            if (debuff != null)
            {
                debuff.Initialize();
                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(debuff);
                }
            }
        }
    }

    public static Enemy GetTarget(string targetType, Enemy[] enemies)
    {
        enemies = enemies.Where(e => !e.IsDead).ToArray();
        if (enemies.Length == 0) return null;

        return targetType switch
        {
            "enemy_front" => GetFrontmost(enemies),
            "enemy_rear" => GetRearmost(enemies),
            "enemy_random" => enemies[GD.Randi() % enemies.Length],
            "enemy_all" => enemies[0],
            _ => GetFrontmost(enemies)
        };
    }

    private static Enemy GetFrontmost(Enemy[] enemies)
    {
        return enemies.OrderBy(e => e.Position).FirstOrDefault();
    }

    private static Enemy GetRearmost(Enemy[] enemies)
    {
        return enemies.OrderByDescending(e => e.Position).FirstOrDefault();
    }
}
