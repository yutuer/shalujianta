using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;

namespace FishEatFish.Battle.CharacterSystem;

public class CharacterCardLoader
{
    private static Dictionary<string, FishEatFish.Battle.Card.Card> characterCardDatabase = new Dictionary<string, FishEatFish.Battle.Card.Card>();
    private static Dictionary<string, List<string>> characterCardMap = new Dictionary<string, List<string>>();
    private static bool isLoaded = false;

    public static void LoadCharacterCards(string configPath = "res://Data/character_cards.json")
    {
        if (isLoaded) return;

        if (!FileAccess.FileExists(configPath))
        {
            GD.PrintErr($"Character cards config file not found: {configPath}");
            return;
        }

        using var file = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
        string jsonContent = file.GetAsText();

        ParseJson(jsonContent);
        isLoaded = true;
        GD.Print($"[CharacterCardLoader] Loaded {characterCardDatabase.Count} character cards");
    }

    private static void ParseJson(string json)
    {
        Json jsonNode = new Json();
        jsonNode.Parse(json);
        Godot.Collections.Dictionary resultDict = jsonNode.Data.AsGodotDictionary();

        if (resultDict.ContainsKey("cards"))
        {
            foreach (Godot.Collections.Dictionary cardDict in resultDict["cards"].AsGodotArray())
            {
                FishEatFish.Battle.Card.Card card = CreateCardFromDict(cardDict);

                if (!string.IsNullOrEmpty(card.CardId))
                {
                    characterCardDatabase[card.CardId] = card;

                    string characterId = cardDict.ContainsKey("characterId") ? cardDict["characterId"].ToString() : "";
                    if (!string.IsNullOrEmpty(characterId))
                    {
                        if (!characterCardMap.ContainsKey(characterId))
                        {
                            characterCardMap[characterId] = new List<string>();
                        }
                        if (!characterCardMap[characterId].Contains(card.CardId))
                        {
                            characterCardMap[characterId].Add(card.CardId);
                        }
                    }
                }
            }
        }
    }

    private static FishEatFish.Battle.Card.Card CreateCardFromDict(Godot.Collections.Dictionary dict)
    {
        FishEatFish.Battle.Card.Card card = new FishEatFish.Battle.Card.Card();

        card.CardId = dict.ContainsKey("cardId") ? dict["cardId"].ToString() : "";
        card.Name = dict.ContainsKey("name") ? dict["name"].ToString() : "";
        card.Style = dict.ContainsKey("style") ? dict["style"].ToString() : "striker";
        card.Cost = dict.ContainsKey("cost") ? (int)(long)dict["cost"] : 1;
        card.BaseCost = card.Cost;
        card.IsAttack = dict.ContainsKey("isAttack") ? (bool)dict["isAttack"] : false;
        card.IsAreaAttack = dict.ContainsKey("isAreaAttack") ? (bool)dict["isAreaAttack"] : false;
        card.Damage = dict.ContainsKey("damage") ? (int)(long)dict["damage"] : 0;
        card.ShieldGain = dict.ContainsKey("shieldGain") ? (int)(long)dict["shieldGain"] : 0;
        // 百分比口径（对照参考游戏）：伤害 = 角色攻击 × 百分比，护盾 = 角色防御 × 百分比
        card.DamagePercent = dict.ContainsKey("damagePercent") ? (int)(long)dict["damagePercent"] : 0;
        card.ShieldPercent = dict.ContainsKey("shieldPercent") ? (int)(long)dict["shieldPercent"] : 0;
        card.EnergyGain = dict.ContainsKey("energyGainBase") ? (int)(long)dict["energyGainBase"] : 0;
        card.HealAmount = dict.ContainsKey("healAmount") ? (int)(long)dict["healAmount"] : 0;
        card.DrawCount = dict.ContainsKey("drawCountBase") ? (int)(long)dict["drawCountBase"] : 0;
        card.IsRetain = dict.ContainsKey("isRetain") ? (bool)dict["isRetain"] : false;
        card.Level = 1;
        card.MaxLevel = 6;

        if (dict.ContainsKey("damageBaseCoefficient"))
        {
            card.DamageBaseCoefficient = (float)(double)dict["damageBaseCoefficient"];
        }
        if (dict.ContainsKey("shieldBaseCoefficient"))
        {
            card.ShieldBaseCoefficient = (float)(double)dict["shieldBaseCoefficient"];
        }
        if (dict.ContainsKey("healBaseCoefficient"))
        {
            card.HealBaseCoefficient = (float)(double)dict["healBaseCoefficient"];
        }
        if (dict.ContainsKey("counterBaseCoefficient"))
        {
            card.CounterBaseCoefficient = (float)(double)dict["counterBaseCoefficient"];
        }

        card.RageGainPerLevel = dict.ContainsKey("rageGainPerLevel") ? (int)(long)dict["rageGainPerLevel"] : 0;
        card.EnergyGainBase = dict.ContainsKey("energyGainBase") ? (int)(long)dict["energyGainBase"] : 0;
        card.EnergyGainPerLevel = dict.ContainsKey("energyGainPerLevel") ? (int)(long)dict["energyGainPerLevel"] : 0;
        card.DrawCountBase = dict.ContainsKey("drawCountBase") ? (int)(long)dict["drawCountBase"] : 0;
        card.DrawCountPerLevel = dict.ContainsKey("drawCountPerLevel") ? (int)(long)dict["drawCountPerLevel"] : 0;
        card.BuffValueBase = dict.ContainsKey("buffValueBase") ? (int)(long)dict["buffValueBase"] : 0;
        card.BuffValuePerLevel = dict.ContainsKey("buffValuePerLevel") ? (int)(long)dict["buffValuePerLevel"] : 0;
        card.DebuffValueBase = dict.ContainsKey("debuffValueBase") ? (int)(long)dict["debuffValueBase"] : 0;
        card.DebuffValuePerLevel = dict.ContainsKey("debuffValuePerLevel") ? (int)(long)dict["debuffValuePerLevel"] : 0;

        card.ApplyBuffName = dict.ContainsKey("applyBuffName") ? dict["applyBuffName"].ToString() : "";
        card.ApplyBuffDuration = dict.ContainsKey("applyBuffDuration") ? (int)(long)dict["applyBuffDuration"] : 0;
        card.ApplyDebuffName = dict.ContainsKey("applyDebuffName") ? dict["applyDebuffName"].ToString() : "";
        card.ApplyDebuffDuration = dict.ContainsKey("applyDebuffDuration") ? (int)(long)dict["applyDebuffDuration"] : 0;

        if (dict.ContainsKey("target"))
        {
            string target = dict["target"].ToString().ToLower();
            card.Target = target switch
            {
                "front" => TargetType.Front,
                "rear" => TargetType.Rear,
                "random" => TargetType.Position,
                _ => TargetType.None
            };
        }

        return card;
    }

    public static FishEatFish.Battle.Card.Card GetCharacterCard(string cardId)
    {
        if (!isLoaded) LoadCharacterCards();

        if (characterCardDatabase.ContainsKey(cardId))
        {
            return characterCardDatabase[cardId];
        }

        GD.PrintErr($"[CharacterCardLoader] Character card not found: {cardId}");
        return null;
    }

    /// <summary>
    /// 查一张角色卡；**查不到不报错**（调用方自己决定要不要回退）。
    /// 队伍卡组构建时用这个：数据表里没有的卡要走旧的代码兜底，
    /// 不能像 <see cref="GetCharacterCard"/> 那样每次都刷一条错误日志。
    /// </summary>
    /// <param name="cardId">卡牌 id</param>
    /// <param name="card">查到的模板实例（查不到为 null）</param>
    /// <returns>查到了返回 true</returns>
    public static bool TryGetCharacterCard(string cardId, out FishEatFish.Battle.Card.Card card)
    {
        if (!isLoaded) LoadCharacterCards();

        return characterCardDatabase.TryGetValue(cardId, out card);
    }

    /// <summary>
    /// 取一张卡牌的**独立副本**（可选挂上角色属性）。
    ///
    /// ⚠️ <see cref="GetCharacterCard"/> 返回的是**共享的模板实例**，界面直接拿它展示有两个问题：
    ///   1. **数值算不出来**：百分比卡牌要靠角色属性才算得出伤害/护盾，
    ///      模板既没挂属性也没有绝对值 → 卡面 / 列表显示 0；
    ///   2. **多界面共享同一实例**：谁改等级 / 费用都会互相影响。
    /// 所以**界面展示一律走这个工厂**。
    /// </summary>
    /// <param name="cardId">卡牌 id</param>
    /// <param name="attributes">该卡所属角色的属性（可为 null）</param>
    /// <returns>独立副本；查不到返回 null</returns>
    public static FishEatFish.Battle.Card.Card CreateInstance(string cardId, CharacterAttributes attributes = null)
    {
        if (!TryGetCharacterCard(cardId, out FishEatFish.Battle.Card.Card template))
        {
            GD.PrintErr($"[CharacterCardLoader] Character card not found: {cardId}");
            return null;
        }

        FishEatFish.Battle.Card.Card instance = template.Clone();
        if (attributes != null)
        {
            instance.LinkedAttributes = attributes;
        }
        return instance;
    }

    public static List<FishEatFish.Battle.Card.Card> GetCharacterCards(string characterId)
    {
        if (!isLoaded) LoadCharacterCards();

        List<FishEatFish.Battle.Card.Card> cards = new List<FishEatFish.Battle.Card.Card>();

        if (characterCardMap.ContainsKey(characterId))
        {
            foreach (string cardId in characterCardMap[characterId])
            {
                if (characterCardDatabase.ContainsKey(cardId))
                {
                    cards.Add(characterCardDatabase[cardId]);
                }
            }
        }

        return cards;
    }

    public static List<string> GetCharacterCardIds(string characterId)
    {
        if (!isLoaded) LoadCharacterCards();

        if (characterCardMap.ContainsKey(characterId))
        {
            return new List<string>(characterCardMap[characterId]);
        }

        return new List<string>();
    }

    public static Dictionary<string, FishEatFish.Battle.Card.Card> GetAllCharacterCards()
    {
        if (!isLoaded) LoadCharacterCards();
        return new Dictionary<string, FishEatFish.Battle.Card.Card>(characterCardDatabase);
    }

    public static List<string> GetAllCharacterIds()
    {
        if (!isLoaded) LoadCharacterCards();
        return new List<string>(characterCardMap.Keys);
    }

    public static void Reload()
    {
        isLoaded = false;
        characterCardDatabase.Clear();
        characterCardMap.Clear();
        LoadCharacterCards();
    }
}
