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

        // 卡牌类型（指令 / 衍生 / 灵知觉醒）与词缀数组 —— 卡牌总览的文案与显示要用
        card.CardType = ParseCardType(dict.ContainsKey("cardType") ? dict["cardType"].ToString() : "");
        card.Affixes.Clear();
        if (dict.ContainsKey("affixes") && dict["affixes"].Obj != null)
        {
            foreach (var affix in dict["affixes"].AsGodotArray())
            {
                string value = affix.ToString();
                if (!string.IsNullOrEmpty(value)) card.Affixes.Add(value);
            }
        }
        // 策划写的专属说明（灵知觉醒卡那一大段）
        card.TemplateDescription = dict.ContainsKey("description") ? dict["description"].ToString() : "";

        // 卡牌自带怒气（灵知觉醒这类：参考图净世之路 50 点）
        card.RageGain = dict.ContainsKey("rageGain") ? (int)(long)dict["rageGain"] : 0;

        // 命中次数（1 = 一次；2 = 「造成 x 点伤害 2 次」）
        card.HitCount = dict.ContainsKey("hitCount") ? (int)(long)dict["hitCount"] : 1;
        if (card.HitCount < 1) card.HitCount = 1;

        // 抽牌筛选方式（空 = 普通随机抽牌；lowestCostInstruction = 抽算力消耗最低的指令卡）
        card.DrawFilter = dict.ContainsKey("drawFilter") ? dict["drawFilter"].ToString() : "";

        // 卡牌特有的词缀说明（灵知觉醒 / 罪印…）——长按卡牌弹的「词缀说明」面板用
        card.AffixNotes.Clear();
        if (dict.ContainsKey("affixNotes") && dict["affixNotes"].Obj != null)
        {
            foreach (var noteObj in dict["affixNotes"].AsGodotArray())
            {
                if (noteObj.Obj == null) continue;
                Godot.Collections.Dictionary noteDict = noteObj.AsGodotDictionary();
                card.AffixNotes.Add(new CardAffixNote
                {
                    Id = noteDict.ContainsKey("id") ? noteDict["id"].ToString() : "",
                    Name = noteDict.ContainsKey("name") ? noteDict["name"].ToString() : "",
                    Description = noteDict.ContainsKey("description") ? noteDict["description"].ToString() : ""
                });
            }
        }

        // **效果列表**（唯一真相源）—— 战斗结算与卡面数值都从这里取
        card.Effects.Clear();
        if (dict.ContainsKey("effects") && dict["effects"].Obj != null)
        {
            foreach (var effectObj in dict["effects"].AsGodotArray())
            {
                if (effectObj.Obj == null) continue;
                Godot.Collections.Dictionary effectDict = effectObj.AsGodotDictionary();

                CardEffect effect = new CardEffect
                {
                    Type = effectDict.ContainsKey("type") ? effectDict["type"].ToString() : "",
                    Percent = effectDict.ContainsKey("percent") ? (int)(long)effectDict["percent"] : 0,
                    Value = effectDict.ContainsKey("value") ? (int)(long)effectDict["value"] : 0,
                    Count = effectDict.ContainsKey("count") ? (int)(long)effectDict["count"] : 0,
                    Duration = effectDict.ContainsKey("duration") ? (int)(long)effectDict["duration"] : 0,
                    HitCount = effectDict.ContainsKey("hitCount") ? (int)(long)effectDict["hitCount"] : 1,
                    Buff = effectDict.ContainsKey("buff") ? effectDict["buff"].ToString() : "",
                    Debuff = effectDict.ContainsKey("debuff") ? effectDict["debuff"].ToString() : "",
                    Target = effectDict.ContainsKey("target") ? effectDict["target"].ToString() : "",
                    Filter = effectDict.ContainsKey("filter") ? effectDict["filter"].ToString() : ""
                };

                if (effectDict.ContainsKey("timing") && effectDict["timing"].Obj != null)
                {
                    foreach (var timing in effectDict["timing"].AsGodotArray())
                    {
                        string timingValue = timing.ToString();
                        if (!string.IsNullOrEmpty(timingValue)) effect.Timing.Add(timingValue);
                    }
                }

                card.Effects.Add(effect);
            }
        }

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
    /// 解析卡牌类型：<c>instruction</c>（默认）/ <c>derived</c> / <c>awakening</c>。
    /// 认不出来的一律当「指令」并打错误日志（数据表写错要能看见）。
    /// </summary>
    private static CardType ParseCardType(string value)
    {
        switch (value.ToLower())
        {
            case "instruction":
            case "":
                return CardType.Instruction;
            case "derived":
                return CardType.Derived;
            case "awakening":
                return CardType.Awakening;
            default:
                GD.PrintErr($"[CharacterCardLoader] 未知卡牌类型: {value}（按指令卡处理）");
                return CardType.Instruction;
        }
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
