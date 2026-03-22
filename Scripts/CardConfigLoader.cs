using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class CardConfigLoader
{
	private static System.Collections.Generic.Dictionary<string, CardData> cardDatabase = new System.Collections.Generic.Dictionary<string, CardData>();
	private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> deckDatabase = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
	private static string defaultDeckId = "starter";
	private static bool isLoaded = false;

	public static void LoadCards(string configPath = "res://Data/cards.json")
	{
		if (isLoaded) return;

		if (!Godot.FileAccess.FileExists(configPath))
		{
			GD.PrintErr($"Card config file not found: {configPath}");
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
				System.Collections.Generic.List<string> cardIds = new System.Collections.Generic.List<string>();

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

	public static System.Collections.Generic.List<Card> CreateDeck(string deckId = "")
	{
		if (!isLoaded) LoadCards();

		if (string.IsNullOrEmpty(deckId))
		{
			deckId = defaultDeckId;
		}

		System.Collections.Generic.List<Card> deck = new System.Collections.Generic.List<Card>();

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

	public static System.Collections.Generic.Dictionary<string, CardData> GetAllCards()
	{
		if (!isLoaded) LoadCards();
		return new System.Collections.Generic.Dictionary<string, CardData>(cardDatabase);
	}

	public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> GetAllDecks()
	{
		if (!isLoaded) LoadCards();
		return new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(deckDatabase);
	}

	public static System.Collections.Generic.List<string> GetDeckIds()
	{
		if (!isLoaded) LoadCards();
		return new System.Collections.Generic.List<string>(deckDatabase.Keys);
	}

	public static void Reload()
	{
		isLoaded = false;
		cardDatabase.Clear();
		deckDatabase.Clear();
		LoadCards();
	}
}
