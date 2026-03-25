using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.Effects;

public static class EffectConfigLoader
{
    private static Dictionary<string, EffectDefinition> _effectTemplates = new Dictionary<string, EffectDefinition>();
    private static Dictionary<string, BuffConfig> _buffConfigs = new Dictionary<string, BuffConfig>();
    private static Dictionary<string, DebuffConfig> _debuffConfigs = new Dictionary<string, DebuffConfig>();
    private static bool _loaded = false;

    public static void LoadAll()
    {
        if (_loaded) return;

        LoadEffects();
        LoadBuffs();
        LoadDebuffs();

        _loaded = true;
        GD.Print("[EffectConfigLoader] All effect configurations loaded successfully");
    }

    private static void LoadEffects()
    {
        try
        {
            var file = FileAccess.Open("res://Data/effects.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[EffectConfigLoader] Failed to open effects.json");
                return;
            }

            string jsonString = file.GetAsText();
            file.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"[EffectConfigLoader] Failed to parse effects.json: {parseResult}");
                return;
            }

            var data = json.Data.AsGodotDictionary();
            if (data.ContainsKey("effects"))
            {
                var effectsDict = data["effects"].AsGodotDictionary();
                foreach (var key in effectsDict.Keys)
                {
                    string effectId = key.ToString();
                    var effectData = effectsDict[key].AsGodotDictionary();
                    var effectDef = ParseEffectDefinition(effectId, effectData);
                    _effectTemplates[effectId] = effectDef;
                }
            }

            GD.Print($"[EffectConfigLoader] Loaded {_effectTemplates.Count} effect templates");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[EffectConfigLoader] Error loading effects.json: {e.Message}");
        }
    }

    private static void LoadBuffs()
    {
        try
        {
            var file = FileAccess.Open("res://Data/buffs.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[EffectConfigLoader] Failed to open buffs.json");
                return;
            }

            string jsonString = file.GetAsText();
            file.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"[EffectConfigLoader] Failed to parse buffs.json: {parseResult}");
                return;
            }

            var data = json.Data.AsGodotDictionary();
            if (data.ContainsKey("buffs"))
            {
                var buffsDict = data["buffs"].AsGodotDictionary();
                foreach (var key in buffsDict.Keys)
                {
                    string buffId = key.ToString();
                    var buffData = buffsDict[key].AsGodotDictionary();
                    var buffConfig = ParseBuffConfig(buffId, buffData);
                    _buffConfigs[buffId] = buffConfig;
                }
            }

            GD.Print($"[EffectConfigLoader] Loaded {_buffConfigs.Count} buff configs");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[EffectConfigLoader] Error loading buffs.json: {e.Message}");
        }
    }

    private static void LoadDebuffs()
    {
        try
        {
            var file = FileAccess.Open("res://Data/debuffs.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[EffectConfigLoader] Failed to open debuffs.json");
                return;
            }

            string jsonString = file.GetAsText();
            file.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"[EffectConfigLoader] Failed to parse debuffs.json: {parseResult}");
                return;
            }

            var data = json.Data.AsGodotDictionary();
            if (data.ContainsKey("debuffs"))
            {
                var debuffsDict = data["debuffs"].AsGodotDictionary();
                foreach (var key in debuffsDict.Keys)
                {
                    string debuffId = key.ToString();
                    var debuffData = debuffsDict[key].AsGodotDictionary();
                    var debuffConfig = ParseDebuffConfig(debuffId, debuffData);
                    _debuffConfigs[debuffId] = debuffConfig;
                }
            }

            GD.Print($"[EffectConfigLoader] Loaded {_debuffConfigs.Count} debuff configs");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[EffectConfigLoader] Error loading debuffs.json: {e.Message}");
        }
    }

    private static EffectDefinition ParseEffectDefinition(string effectId, Godot.Collections.Dictionary data)
    {
        var effectDef = new EffectDefinition
        {
            EffectId = effectId
        };

        if (data.ContainsKey("type"))
        {
            string typeStr = data["type"].ToString().ToLower();
            effectDef.Type = ParseEffectType(typeStr);
        }

        if (data.ContainsKey("value"))
        {
            effectDef.Value = data["value"].AsInt32();
        }

        if (data.ContainsKey("trigger"))
        {
            effectDef.Trigger = ParseTriggerType(data["trigger"].ToString());
        }

        if (data.ContainsKey("target"))
        {
            effectDef.TargetFaction = ParseTargetFaction(data["target"].ToString());
        }

        if (data.ContainsKey("ignore_defense"))
        {
            effectDef.IgnoreDefense = data["ignore_defense"].AsBool();
        }

        if (data.ContainsKey("ignore_shield"))
        {
            effectDef.IgnoreShield = data["ignore_shield"].AsBool();
        }

        if (data.ContainsKey("buff_id"))
        {
            effectDef.BuffId = data["buff_id"].ToString();
        }

        if (data.ContainsKey("is_debuff"))
        {
            effectDef.IsDebuff = data["is_debuff"].AsBool();
        }

        if (data.ContainsKey("is_percent"))
        {
            effectDef.IsPercent = data["is_percent"].AsBool();
        }

        if (data.ContainsKey("cap_at_max_health"))
        {
            effectDef.CapAtMaxHealth = data["cap_at_max_health"].AsBool();
        }

        return effectDef;
    }

    private static BuffConfig ParseBuffConfig(string buffId, Godot.Collections.Dictionary data)
    {
        var buffConfig = new BuffConfig
        {
            BuffId = buffId
        };

        if (data.ContainsKey("name"))
        {
            buffConfig.Name = data["name"].ToString();
        }

        if (data.ContainsKey("description"))
        {
            buffConfig.Description = data["description"].ToString();
        }

        if (data.ContainsKey("duration"))
        {
            buffConfig.Duration = data["duration"].AsInt32();
        }

        if (data.ContainsKey("effects"))
        {
            var effectsArray = data["effects"].AsGodotArray();
            foreach (var effectData in effectsArray)
            {
                var effectDef = ParseEffectDefinition(effectData.AsGodotDictionary()["effectId"].ToString(), effectData.AsGodotDictionary());
                buffConfig.Effects.Add(effectDef);
            }
        }

        return buffConfig;
    }

    private static DebuffConfig ParseDebuffConfig(string debuffId, Godot.Collections.Dictionary data)
    {
        var debuffConfig = new DebuffConfig
        {
            DebuffId = debuffId
        };

        if (data.ContainsKey("name"))
        {
            debuffConfig.Name = data["name"].ToString();
        }

        if (data.ContainsKey("description"))
        {
            debuffConfig.Description = data["description"].ToString();
        }

        if (data.ContainsKey("duration"))
        {
            debuffConfig.Duration = data["duration"].AsInt32();
        }

        if (data.ContainsKey("effects"))
        {
            var effectsArray = data["effects"].AsGodotArray();
            foreach (var effectData in effectsArray)
            {
                var effectDef = ParseEffectDefinition(effectData.AsGodotDictionary()["effectId"].ToString(), effectData.AsGodotDictionary());
                debuffConfig.Effects.Add(effectDef);
            }
        }

        return debuffConfig;
    }

    private static EffectType ParseEffectType(string typeStr)
    {
        return typeStr switch
        {
            "damage" => EffectType.Damage,
            "shield" => EffectType.Shield,
            "heal" => EffectType.Heal,
            "draw" => EffectType.Draw,
            "energy" => EffectType.Energy,
            "apply_buff" => EffectType.ApplyBuff,
            "apply_debuff" => EffectType.ApplyDebuff,
            "modify_attack" => EffectType.ModifyDamage,
            "modify_defense" => EffectType.ModifyShield,
            "modify_damage_received" => EffectType.ModifyDamage,
            _ => EffectType.Custom
        };
    }

    private static TriggerType ParseTriggerType(string triggerStr)
    {
        return triggerStr.ToLower() switch
        {
            "immediate" => TriggerType.Immediate,
            "on_turn_start" => TriggerType.OnTurnStart,
            "on_turn_end" => TriggerType.OnTurnEnd,
            "on_damage_dealt" => TriggerType.OnDamageDealt,
            "on_damage_received" => TriggerType.OnDamageReceived,
            "on_heal" => TriggerType.OnHeal,
            "on_kill" => TriggerType.OnKill,
            "on_card_played" => TriggerType.OnCardPlayed,
            _ => TriggerType.Immediate
        };
    }

    private static TargetFaction ParseTargetFaction(string targetStr)
    {
        return targetStr.ToLower() switch
        {
            "enemy_front" or "enemy_rear" or "enemy_all" or "enemy_random" or "enemy" => TargetFaction.Enemy,
            "self" => TargetFaction.Self,
            "friendly_all" or "friendly" => TargetFaction.Friendly,
            _ => TargetFaction.Any
        };
    }

    public static EffectDefinition GetEffectTemplate(string effectId)
    {
        if (_effectTemplates.ContainsKey(effectId))
        {
            return _effectTemplates[effectId];
        }
        return null;
    }

    public static BuffConfig GetBuffConfig(string buffId)
    {
        if (_buffConfigs.ContainsKey(buffId))
        {
            return _buffConfigs[buffId];
        }
        return null;
    }

    public static DebuffConfig GetDebuffConfig(string debuffId)
    {
        if (_debuffConfigs.ContainsKey(debuffId))
        {
            return _debuffConfigs[debuffId];
        }
        return null;
    }

    public static StatusEffect CreateBuffFromConfig(string buffId)
    {
        var config = GetBuffConfig(buffId);
        if (config == null) return null;

        var buff = EffectRegistry.CreateBuff(buffId);
        if (buff != null)
        {
            buff.Initialize();
        }
        return buff;
    }

    public static StatusEffect CreateDebuffFromConfig(string debuffId)
    {
        var config = GetDebuffConfig(debuffId);
        if (config == null) return null;

        var debuff = EffectRegistry.CreateDebuff(debuffId);
        if (debuff != null)
        {
            debuff.Initialize();
        }
        return debuff;
    }
}

public class BuffConfig
{
    public string BuffId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Duration { get; set; } = 1;
    public List<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();
}

public class DebuffConfig
{
    public string DebuffId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Duration { get; set; } = 1;
    public List<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();
}
