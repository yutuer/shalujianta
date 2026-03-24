using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.Card;

public class CardConfigLoader
{
	private static Dictionary<string, CardData> cardDatabase = new Dictionary<string, CardData>();
	private static Dictionary<string, List<string>> deckDatabase = new Dictionary<string, List<string>>();
	private static string defaultDeckId = "starter";
	private static bool isLoaded = false;

	public static void LoadCards(string configPath = "res://Data/cards.json")
	{
		if (isLoaded) return;

		if (!FileAccess.FileExists(configPath))
		{
			GD.PrintErr($"Card config file not found: {configPath}");
			return;
		}

		using var file = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
		string jsonContent = file.GetAsText();

		ParseJson(jsonContent);
		isLoaded = true;
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
				CardData cardData = new CardData
            {
                CardId = cardDict.ContainsKey("cardId") ? cardDict["cardId"].ToString() : "",
                Name = cardDict.ContainsKey("name") ? cardDict["name"].ToString() : "",
                Description = cardDict.ContainsKey("description") ? cardDict["description"].ToString() : "",
                Cost = cardDict.ContainsKey("cost") ? (int)(long)cardDict["cost"] : 1,
                BaseCost = cardDict.ContainsKey("baseCost") ? (int)(long)cardDict["baseCost"] : 1,
                IsAttack = cardDict.ContainsKey("isAttack") ? (bool)cardDict["isAttack"] : false,
                IsAreaAttack = cardDict.ContainsKey("isAreaAttack") ? (bool)cardDict["isAreaAttack"] : false,
                Target = cardDict.ContainsKey("target") ? (TargetType)(int)(long)cardDict["target"] : TargetType.None,
                TargetPosition = cardDict.ContainsKey("targetPosition") ? (int)(long)cardDict["targetPosition"] : -1,
                Damage = cardDict.ContainsKey("damage") ? (int)(long)cardDict["damage"] : 0,
                ShieldGain = cardDict.ContainsKey("shieldGain") ? (int)(long)cardDict["shieldGain"] : 0,
                EnergyGain = cardDict.ContainsKey("energyGain") ? (int)(long)cardDict["energyGain"] : 0,
                HealAmount = cardDict.ContainsKey("healAmount") ? (int)(long)cardDict["healAmount"] : 0,
                IsRetain = cardDict.ContainsKey("isRetain") ? (bool)cardDict["isRetain"] : false,
                SpritePath = cardDict.ContainsKey("spritePath") ? cardDict["spritePath"].ToString() : ""
            };

				if (!string.IsNullOrEmpty(cardData.CardId))
				{
					cardDatabase[cardData.CardId] = cardData;
				}
			}
		}

		if (resultDict.ContainsKey("decks"))
		{
			Godot.Collections.Dictionary decks = resultDict["decks"].AsGodotDictionary();
			foreach (var deckPair in decks)
			{
				string deckId = deckPair.Key.ToString();
				List<string> cardIds = new List<string>();

				foreach (var cardId in deckPair.Value.AsGodotArray())
				{
					cardIds.Add(cardId.ToString());
				}

				deckDatabase[deckId] = cardIds;
			}
		}

		if (resultDict.ContainsKey("defaultDeck"))
		{
			defaultDeckId = resultDict["defaultDeck"].ToString();
		}
	}

	public static CardData GetCardData(string cardId)
	{
		if (!isLoaded) LoadCards();

		if (cardDatabase.ContainsKey(cardId))
		{
			return cardDatabase[cardId];
		}

		GD.PrintErr($"Card not found: {cardId}");
		return null;
	}

	public static List<Card> CreateDeck(string deckId = "")
	{
		if (!isLoaded) LoadCards();

		if (string.IsNullOrEmpty(deckId))
		{
			deckId = defaultDeckId;
		}

		List<Card> deck = new List<Card>();

		if (deckDatabase.ContainsKey(deckId))
		{
			foreach (string cardId in deckDatabase[deckId])
			{
				CardData cardData = GetCardData(cardId);
				if (cardData != null)
				{
					deck.Add(cardData.CreateCard());
				}
			}
		}
		else
		{
			GD.PrintErr($"Deck not found: {deckId}");
		}

		return deck;
	}

	public static Dictionary<string, CardData> GetAllCards()
	{
		if (!isLoaded) LoadCards();
		return new Dictionary<string, CardData>(cardDatabase);
	}

	public static Dictionary<string, List<string>> GetAllDecks()
	{
		if (!isLoaded) LoadCards();
		return new Dictionary<string, List<string>>(deckDatabase);
	}

	public static List<string> GetDeckIds()
	{
		if (!isLoaded) LoadCards();
		return new List<string>(deckDatabase.Keys);
	}

	public static void Reload()
	{
		isLoaded = false;
		cardDatabase.Clear();
		deckDatabase.Clear();
		LoadCards();
	}
}
