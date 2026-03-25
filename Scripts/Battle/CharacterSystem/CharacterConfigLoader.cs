using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;

public class CharacterConfigLoader
{
    private static Dictionary<string, CharacterDefinition> characterDatabase = new Dictionary<string, CharacterDefinition>();
    private static Dictionary<string, UltimateSkill> ultimateSkillDatabase = new Dictionary<string, UltimateSkill>();
    private static bool isLoaded = false;

    public static void LoadCharacters(string configPath = "res://Data/characters.json")
    {
        if (isLoaded) return;

        if (!Godot.FileAccess.FileExists(configPath))
        {
            GD.PrintErr($"Character config file not found: {configPath}");
            return;
        }

        using var file = Godot.FileAccess.Open(configPath, Godot.FileAccess.ModeFlags.Read);
        string jsonContent = file.GetAsText();

        ParseJson(jsonContent);
        isLoaded = true;
    }

    private static void ParseJson(string json)
    {
        Json jsonNode = new Json();
        jsonNode.Parse(json);
        Godot.Collections.Dictionary resultDict = jsonNode.Data.AsGodotDictionary();

        if (resultDict.ContainsKey("characters"))
        {
            foreach (Godot.Collections.Dictionary charDict in resultDict["characters"].AsGodotArray())
            {
                var specialCardIds = new string[3];
                if (charDict.ContainsKey("specialCardIds"))
                {
                    var specialValue = charDict["specialCardIds"];
                    if (specialValue.Obj != null)
                    {
                        var specialArray = specialValue.AsGodotArray();
                        for (int i = 0; i < Mathf.Min(3, specialArray.Count); i++)
                        {
                            specialCardIds[i] = specialArray[i].ToString();
                        }
                    }
                }

                CharacterDefinition charDef = new CharacterDefinition
                {
                    CharacterId = charDict.ContainsKey("characterId") ? charDict["characterId"].ToString() : "",
                    Name = charDict.ContainsKey("name") ? charDict["name"].ToString() : "",
                    NameEn = charDict.ContainsKey("nameEn") ? charDict["nameEn"].ToString() : "",
                    BaseHealth = charDict.ContainsKey("baseHealth") ? (int)(long)charDict["baseHealth"] : 30,
                    BaseEnergy = charDict.ContainsKey("baseEnergy") ? (int)(long)charDict["baseEnergy"] : 2,
                    BaseDrawCount = charDict.ContainsKey("baseDrawCount") ? (int)(long)charDict["baseDrawCount"] : 2,
                    BaseAttack = charDict.ContainsKey("baseAttack") ? (int)(long)charDict["baseAttack"] : 5,
                    BaseDefense = charDict.ContainsKey("baseDefense") ? (int)(long)charDict["baseDefense"] : 3,
                    AttackCardId = charDict.ContainsKey("attackCardId") ? charDict["attackCardId"].ToString() : "",
                    DefenseCardId = charDict.ContainsKey("defenseCardId") ? charDict["defenseCardId"].ToString() : "",
                    SpecialCardIds = specialCardIds,
                    UltimateSkillId = charDict.ContainsKey("ultimateSkillId") ? charDict["ultimateSkillId"].ToString() : ""
                };

                if (!string.IsNullOrEmpty(charDef.CharacterId))
                {
                    characterDatabase[charDef.CharacterId] = charDef;
                }

                if (charDict.ContainsKey("ultimateSkill"))
                {
                    var ultValue = charDict["ultimateSkill"];
                    if (ultValue.Obj != null)
                    {
                    Godot.Collections.Dictionary ultDict = charDict["ultimateSkill"].AsGodotDictionary();
                    UltimateSkill ultimate = new UltimateSkill
                    {
                        SkillId = ultDict.ContainsKey("skillId") ? ultDict["skillId"].ToString() : "",
                        Name = ultDict.ContainsKey("name") ? ultDict["name"].ToString() : "",
                        Description = ultDict.ContainsKey("description") ? ultDict["description"].ToString() : "",
                        RageCost = ultDict.ContainsKey("rageCost") ? (int)(long)ultDict["rageCost"] : 100,
                        Damage = ultDict.ContainsKey("damage") ? (int)(long)ultDict["damage"] : 0,
                        Heal = ultDict.ContainsKey("heal") ? (int)(long)ultDict["heal"] : 0,
                        Shield = ultDict.ContainsKey("shield") ? (int)(long)ultDict["shield"] : 0,
                        DrawCount = ultDict.ContainsKey("drawCount") ? (int)(long)ultDict["drawCount"] : 0,
                        BuffValue = ultDict.ContainsKey("buffValue") ? (int)(long)ultDict["buffValue"] : 0,
                        BuffDuration = ultDict.ContainsKey("buffDuration") ? (int)(long)ultDict["buffDuration"] : 0
                    };

                    if (!string.IsNullOrEmpty(ultimate.SkillId))
                    {
                        ultimateSkillDatabase[ultimate.SkillId] = ultimate;
                    }
                    }
                }
            }
        }
    }

    public static CharacterDefinition GetCharacter(string characterId)
    {
        if (!isLoaded) LoadCharacters();

        if (characterDatabase.ContainsKey(characterId))
        {
            return characterDatabase[characterId];
        }

        GD.PrintErr($"Character not found: {characterId}");
        return null;
    }

    public static UltimateSkill GetUltimateSkill(string skillId)
    {
        if (!isLoaded) LoadCharacters();

        if (ultimateSkillDatabase.ContainsKey(skillId))
        {
            return ultimateSkillDatabase[skillId];
        }

        GD.PrintErr($"Ultimate skill not found: {skillId}");
        return null;
    }

    public static Dictionary<string, CharacterDefinition> GetAllCharacters()
    {
        if (!isLoaded) LoadCharacters();
        return new Dictionary<string, CharacterDefinition>(characterDatabase);
    }

    public static Dictionary<string, UltimateSkill> GetAllUltimateSkills()
    {
        if (!isLoaded) LoadCharacters();
        return new Dictionary<string, UltimateSkill>(ultimateSkillDatabase);
    }

    public static void Reload()
    {
        isLoaded = false;
        characterDatabase.Clear();
        ultimateSkillDatabase.Clear();
        LoadCharacters();
    }
}
