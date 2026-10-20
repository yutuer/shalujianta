using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;

namespace FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 大招数据加载器：Data/ultimates.json 是大招的唯一权威数据源
/// </summary>
public class UltimateLoader
{
    private static Dictionary<string, UltimateSkill> ultimateDatabase = new Dictionary<string, UltimateSkill>();
    private static Dictionary<string, string> characterUltimateMap = new Dictionary<string, string>();
    private static bool isLoaded = false;

    public static void LoadUltimates(string configPath = "res://Data/ultimates.json")
    {
        if (isLoaded) return;

        if (!FileAccess.FileExists(configPath))
        {
            GD.PrintErr($"Ultimates config file not found: {configPath}");
            return;
        }

        using var file = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
        string jsonContent = file.GetAsText();

        ParseJson(jsonContent);
        isLoaded = true;
        GD.Print($"[UltimateLoader] Loaded {ultimateDatabase.Count} ultimates");
    }

    private static void ParseJson(string json)
    {
        Json jsonNode = new Json();
        jsonNode.Parse(json);
        Godot.Collections.Dictionary resultDict = jsonNode.Data.AsGodotDictionary();

        if (resultDict.ContainsKey("ultimates"))
        {
            foreach (Godot.Collections.Dictionary ultDict in resultDict["ultimates"].AsGodotArray())
            {
                UltimateSkill ultimate = CreateUltimateFromDict(ultDict);

                if (!string.IsNullOrEmpty(ultimate.SkillId))
                {
                    ultimateDatabase[ultimate.SkillId] = ultimate;

                    string characterId = ultDict.ContainsKey("characterId") ? ultDict["characterId"].ToString() : "";
                    if (!string.IsNullOrEmpty(characterId))
                    {
                        characterUltimateMap[characterId] = ultimate.SkillId;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 把 JSON 字典转换为 UltimateSkill（含 isAreaAttack/target/applyDebuffName/energyGain/description/nameEn）
    /// </summary>
    private static UltimateSkill CreateUltimateFromDict(Godot.Collections.Dictionary dict)
    {
        UltimateSkill ultimate = new UltimateSkill();

        ultimate.SkillId = GetStringValue(dict, "skillId");
        ultimate.Name = GetStringValue(dict, "name");
        ultimate.Description = GetStringValue(dict, "description");
        ultimate.NameEn = GetStringValue(dict, "nameEn");

        ultimate.RageCost = GetIntValue(dict, "rageCost", 100);
        ultimate.Damage = GetIntValue(dict, "damage", 0);
        ultimate.Heal = GetIntValue(dict, "heal", 0);
        ultimate.Shield = GetIntValue(dict, "shield", 0);
        ultimate.DrawCount = GetIntValue(dict, "drawCount", 0);
        ultimate.BuffValue = GetIntValue(dict, "buffValue", 0);
        ultimate.BuffDuration = GetIntValue(dict, "buffDuration", 0);
        ultimate.EnergyGain = GetIntValue(dict, "energyGain", 0);

        ultimate.IsAreaAttack = GetBoolValue(dict, "isAreaAttack");
        ultimate.Target = GetStringValue(dict, "target").ToLower();
        ultimate.ApplyDebuffName = GetStringValue(dict, "applyDebuffName").ToLower();

        ultimate.Level = 1;
        ultimate.MaxLevel = 6;

        if (dict.ContainsKey("damageBaseCoefficient"))
        {
            ultimate.DamageBaseCoefficient = (float)(double)dict["damageBaseCoefficient"];
        }
        if (dict.ContainsKey("healBaseCoefficient"))
        {
            ultimate.HealBaseCoefficient = (float)(double)dict["healBaseCoefficient"];
        }
        if (dict.ContainsKey("shieldBaseCoefficient"))
        {
            ultimate.ShieldBaseCoefficient = (float)(double)dict["shieldBaseCoefficient"];
        }

        ultimate.DrawCountPerLevel = GetIntValue(dict, "drawCountPerLevel", 0);
        ultimate.BuffValuePerLevel = GetIntValue(dict, "buffValuePerLevel", 0);

        // nameEn 缺省时用 skillId 去掉 "_ultimate" 后首字母大写
        if (string.IsNullOrEmpty(ultimate.NameEn))
        {
            ultimate.NameEn = DeriveNameEn(ultimate.SkillId);
        }

        return ultimate;
    }

    private static string GetStringValue(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.ContainsKey(key))
        {
            return "";
        }

        Variant value = dict[key];
        if (value.VariantType == Variant.Type.Nil)
        {
            return "";
        }

        return value.ToString();
    }

    private static int GetIntValue(Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (!dict.ContainsKey(key))
        {
            return defaultValue;
        }

        Variant value = dict[key];
        if (value.VariantType == Variant.Type.Nil || value.VariantType == Variant.Type.String)
        {
            return defaultValue;
        }

        return (int)(long)value;
    }

    private static bool GetBoolValue(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.ContainsKey(key))
        {
            return false;
        }

        Variant value = dict[key];
        if (value.VariantType == Variant.Type.Nil)
        {
            return false;
        }

        return value.AsBool();
    }

    /// <summary>
    /// 缺省英文名：skillId 去掉 "_ultimate" 后首字母大写（rat_ultimate → Rat）
    /// </summary>
    public static string DeriveNameEn(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return "";
        }

        string name = skillId;
        int index = name.IndexOf("_ultimate", System.StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            name = name.Substring(0, index);
        }

        if (name.Length == 0)
        {
            return "";
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static UltimateSkill GetUltimate(string skillId)
    {
        if (!isLoaded) LoadUltimates();

        if (ultimateDatabase.ContainsKey(skillId))
        {
            return ultimateDatabase[skillId];
        }

        GD.PrintErr($"[UltimateLoader] Ultimate not found: {skillId}");
        return null;
    }

    public static UltimateSkill GetCharacterUltimate(string characterId)
    {
        if (!isLoaded) LoadUltimates();

        if (characterUltimateMap.ContainsKey(characterId))
        {
            string skillId = characterUltimateMap[characterId];
            if (ultimateDatabase.ContainsKey(skillId))
            {
                return ultimateDatabase[skillId];
            }
        }

        GD.PrintErr($"[UltimateLoader] Ultimate not found for character: {characterId}");
        return null;
    }

    public static Dictionary<string, UltimateSkill> GetAllUltimates()
    {
        if (!isLoaded) LoadUltimates();
        return new Dictionary<string, UltimateSkill>(ultimateDatabase);
    }

    public static List<string> GetAllCharacterIds()
    {
        if (!isLoaded) LoadUltimates();
        return new List<string>(characterUltimateMap.Keys);
    }

    public static void Reload()
    {
        isLoaded = false;
        ultimateDatabase.Clear();
        characterUltimateMap.Clear();
        LoadUltimates();
    }
}
