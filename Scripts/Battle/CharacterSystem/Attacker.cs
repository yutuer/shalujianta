using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Attacker : Node
{
    [Export]
    public CharacterDefinition[] Characters = new CharacterDefinition[4];

    private List<Card> _teamDeck = new List<Card>();

    public int TotalHealth => Characters.Where(c => c != null).Sum(c => c.BaseHealth);
    public int TotalEnergy => Characters.Where(c => c != null).Sum(c => c.BaseEnergy);
    public int TotalAttack => Characters.Where(c => c != null).Sum(c => c.BaseAttack);
    public int TotalDefense => Characters.Where(c => c != null).Sum(c => c.BaseDefense);
    public int TotalDrawCount => Characters.Where(c => c != null).Sum(c => c.BaseDrawCount);

    private Dictionary<Card, CharacterDefinition> _cardToCharacter = new Dictionary<Card, CharacterDefinition>();

    public override void _Ready()
    {
    }

    public void SetCharacters(CharacterDefinition[] characters)
    {
        if (characters == null || characters.Length != 4)
        {
            GD.PrintErr("Attacker requires exactly 4 characters");
            return;
        }

        Characters = characters;
        BuildTeamDeck();
    }

    private void BuildTeamDeck()
    {
        _teamDeck.Clear();
        _cardToCharacter.Clear();

        foreach (var character in Characters)
        {
            if (character == null) continue;

            Card attackCard = CreateCardFromId(character.AttackCardId, character, true, false);
            Card defenseCard = CreateCardFromId(character.DefenseCardId, character, false, true);

            _teamDeck.Add(attackCard);
            _teamDeck.Add(defenseCard);

            _cardToCharacter[attackCard] = character;
            _cardToCharacter[defenseCard] = character;

            foreach (var specialId in character.SpecialCardIds)
            {
                if (string.IsNullOrEmpty(specialId)) continue;
                Card specialCard = CreateCardFromId(specialId, character, false, false);
                _teamDeck.Add(specialCard);
                _cardToCharacter[specialCard] = character;
            }
        }
    }

    private Card CreateCardFromId(string cardId, CharacterDefinition character, bool isAttack, bool isDefense)
    {
        var card = new Card
        {
            CardId = cardId,
            Name = GetCardName(cardId, character),
            Cost = GetCardCost(cardId, character),
            Description = GetCardDescription(cardId, character),
            IsAttack = isAttack,
            IsDefense = isDefense
        };

        if (isAttack)
        {
            card.Damage = character.BaseAttack;
        }
        else if (isDefense)
        {
            card.ShieldGain = character.BaseDefense;
        }

        return card;
    }

    private string GetCardName(string cardId, CharacterDefinition character)
    {
        string charPrefix = character.CharacterId;

        if (cardId.EndsWith("_attack"))
        {
            return character.Name + "牙咬";
        }
        else if (cardId.EndsWith("_defense"))
        {
            return character.Name + "洞藏";
        }
        else
        {
            return character.Name + "特殊技";
        }
    }

    private int GetCardCost(string cardId, CharacterDefinition character)
    {
        if (cardId.EndsWith("_attack")) return 1;
        if (cardId.EndsWith("_defense")) return 1;
        return 2;
    }

    private string GetCardDescription(string cardId, CharacterDefinition character)
    {
        if (cardId.EndsWith("_attack"))
        {
            return $"造成 {character.BaseAttack} 点伤害，获得 15 怒气";
        }
        else if (cardId.EndsWith("_defense"))
        {
            return $"获得 {character.BaseDefense} 点护盾，获得 10 怒气";
        }
        else
        {
            return "使用特殊能力";
        }
    }

    public List<Card> GetTeamDeck()
    {
        return new List<Card>(_teamDeck);
    }

    public CharacterDefinition GetCharacterByCard(Card card)
    {
        if (_cardToCharacter.TryGetValue(card, out CharacterDefinition character))
        {
            return character;
        }
        return null;
    }

    public CharacterDefinition GetCharacterAt(int index)
    {
        if (index >= 0 && index < 4)
        {
            return Characters[index];
        }
        return null;
    }

    public bool HasCharacter(string characterId)
    {
        return Characters.Any(c => c != null && c.CharacterId == characterId);
    }

    public int GetCharacterCount()
    {
        return Characters.Count(c => c != null);
    }
}
