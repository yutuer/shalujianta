using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;

namespace FishEatFish.Battle.CharacterSystem;

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

    private static UltimateSkill CreateUltimateFromDict(Godot.Collections.Dictionary dict)
    {
        UltimateSkill ultimate = new UltimateSkill();

        ultimate.SkillId = dict.ContainsKey("skillId") ? dict["skillId"].ToString() : "";
        ultimate.Name = dict.ContainsKey("name") ? dict["name"].ToString() : "";
        ultimate.RageCost = dict.ContainsKey("rageCost") ? (int)(long)dict["rageCost"] : 100;
        ultimate.Damage = dict.ContainsKey("damage") ? (int)(long)dict["damage"] : 0;
        ultimate.Heal = dict.ContainsKey("heal") ? (int)(long)dict["heal"] : 0;
        ultimate.Shield = dict.ContainsKey("shield") ? (int)(long)dict["shield"] : 0;
        ultimate.DrawCount = dict.ContainsKey("drawCount") ? (int)(long)dict["drawCount"] : 0;
        ultimate.BuffValue = dict.ContainsKey("buffValue") ? (int)(long)dict["buffValue"] : 0;
        ultimate.BuffDuration = dict.ContainsKey("buffDuration") ? (int)(long)dict["buffDuration"] : 0;
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

        ultimate.DrawCountPerLevel = dict.ContainsKey("drawCountPerLevel") ? (int)(long)dict["drawCountPerLevel"] : 0;
        ultimate.BuffValuePerLevel = dict.ContainsKey("buffValuePerLevel") ? (int)(long)dict["buffValuePerLevel"] : 0;

        if (dict.ContainsKey("target"))
        {
            string target = dict["target"].ToString().ToLower();
            if (target == "front")
            {
                ultimate.Damage = ultimate.Damage > 0 ? ultimate.Damage : 0;
            }
        }

        return ultimate;
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
