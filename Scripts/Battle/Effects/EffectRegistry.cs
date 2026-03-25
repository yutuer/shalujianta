using System;
using System.Collections.Generic;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Effects;

public static class EffectRegistry
{
    private static Dictionary<string, Type> _effectTypes = new Dictionary<string, Type>();
    private static Dictionary<string, Effect> _effectTemplates = new Dictionary<string, Effect>();
    private static bool _initialized = false;
    private static bool _jsonLoaded = false;

    public static void RegisterEffect(string id, Type effectType)
    {
        _effectTypes[id] = effectType;
    }

    public static void RegisterEffectTemplate(string id, Effect template)
    {
        _effectTemplates[id] = template;
    }

    public static Effect CreateEffect(string effectId)
    {
        var template = EffectConfigLoader.GetEffectTemplate(effectId);
        if (template != null)
        {
            return CreateEffectFromDefinition(template);
        }

        if (_effectTemplates.ContainsKey(effectId))
        {
            return _effectTemplates[effectId].Duplicate() as Effect;
        }

        if (_effectTypes.ContainsKey(effectId))
        {
            return Activator.CreateInstance(_effectTypes[effectId]) as Effect;
        }

        return null;
    }

    public static Effect CreateEffectFromDefinition(EffectDefinition definition)
    {
        if (definition == null) return null;

        Effect effect = definition.Type switch
        {
            EffectType.Damage => new Effects.DamageEffect(),
            EffectType.Shield => new Effects.ShieldEffect(),
            EffectType.Heal => new Effects.HealEffect(),
            EffectType.Draw => new Effects.DrawEffect(),
            EffectType.Energy => new Effects.EnergyEffect(),
            EffectType.ApplyBuff => new Effects.ApplyBuffEffect { BuffId = definition.BuffId },
            EffectType.ApplyDebuff => new Effects.ApplyDebuffEffect { DebuffId = definition.BuffId },
            _ => new Effects.DamageEffect()
        };

        effect.EffectId = definition.EffectId;
        effect.Type = definition.Type;
        effect.Trigger = definition.Trigger;
        effect.Value = definition.Value;
        effect.Duration = definition.Duration;
        effect.TargetFaction = definition.TargetFaction;

        if (effect is Effects.DamageEffect damageEffect)
        {
            damageEffect.IgnoreDefense = definition.IgnoreDefense;
            damageEffect.IgnoreShield = definition.IgnoreShield;
        }

        if (effect is Effects.ShieldEffect shieldEffect)
        {
            shieldEffect.IsRetain = definition.CapAtMaxHealth;
        }

        if (effect is Effects.HealEffect healEffect)
        {
            healEffect.IsPercent = definition.IsPercent;
            healEffect.CapAtMaxHealth = definition.CapAtMaxHealth;
        }

        return effect;
    }

    public static StatusEffect CreateBuff(string buffId)
    {
        var config = EffectConfigLoader.GetBuffConfig(buffId);
        if (config != null)
        {
            return CreateBuffFromConfig(buffId);
        }

        return buffId.ToLower() switch
        {
            "strength" => new StrengthBuff(),
            "defense" => new DefenseBuff(),
            "regeneration" => new RegenerationBuff(),
            "thorns" => new ThornsBuff(),
            "fury" => new FuryBuff(),
            _ => null
        };
    }

    public static StatusEffect CreateBuffFromConfig(string buffId)
    {
        var config = EffectConfigLoader.GetBuffConfig(buffId);
        if (config == null) return null;

        StatusEffect buff = buffId.ToLower() switch
        {
            "strength" => new StrengthBuff(),
            "defense" => new DefenseBuff(),
            "regeneration" => new RegenerationBuff(),
            "thorns" => new ThornsBuff(),
            "fury" => new FuryBuff(),
            "vampirism" => new StrengthBuff(),
            "attack_up" => new StrengthBuff(),
            _ => new StrengthBuff()
        };

        buff.EffectName = config.Name;
        buff.Description = config.Description;
        buff.Duration = config.Duration;
        buff.Initialize();

        return buff;
    }

    public static StatusEffect CreateDebuff(string debuffId)
    {
        var config = EffectConfigLoader.GetDebuffConfig(debuffId);
        if (config != null)
        {
            return CreateDebuffFromConfig(debuffId);
        }

        return debuffId.ToLower() switch
        {
            "weak" => new WeakDebuff(),
            "vulnerable" => new VulnerableDebuff(),
            "poison" => new PoisonDebuff(),
            "slow" => new SlowDebuff(),
            "silence" => new SilenceDebuff(),
            "defensedown" => new DefenseDownDebuff(),
            _ => null
        };
    }

    public static StatusEffect CreateDebuffFromConfig(string debuffId)
    {
        var config = EffectConfigLoader.GetDebuffConfig(debuffId);
        if (config == null) return null;

        StatusEffect debuff = debuffId.ToLower() switch
        {
            "weak" => new WeakDebuff(),
            "vulnerable" => new VulnerableDebuff(),
            "poison" => new PoisonDebuff(),
            "slow" => new SlowDebuff(),
            "silence" => new SilenceDebuff(),
            "defense_down" => new DefenseDownDebuff(),
            "burn" => new PoisonDebuff(),
            "bleed" => new PoisonDebuff(),
            _ => new WeakDebuff()
        };

        debuff.EffectName = config.Name;
        debuff.Description = config.Description;
        debuff.Duration = config.Duration;
        debuff.Initialize();

        return debuff;
    }

    public static void Initialize()
    {
        if (_initialized) return;

        RegisterEffect("damage", typeof(Effects.DamageEffect));
        RegisterEffect("shield", typeof(Effects.ShieldEffect));
        RegisterEffect("heal", typeof(Effects.HealEffect));
        RegisterEffect("draw", typeof(Effects.DrawEffect));
        RegisterEffect("energy", typeof(Effects.EnergyEffect));
        RegisterEffect("apply_buff", typeof(Effects.ApplyBuffEffect));
        RegisterEffect("apply_debuff", typeof(Effects.ApplyDebuffEffect));

        _initialized = true;
    }

    public static void LoadFromJson()
    {
        if (_jsonLoaded) return;

        EffectConfigLoader.LoadAll();
        _jsonLoaded = true;
    }
}
