using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

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

        // 「每个角色各自的两张基本卡」是硬要求（主人 2026-10-24 明确）：
        // 数值模板可以一样，但**条目 / 词条 / 运行时实例必须各角色一份**。
        // 这里放两道守卫，任何一道触发都会打错误日志，不静默。
        var basicCardOwners = new Dictionary<string, string>();

        foreach (var character in Characters)
        {
            if (character == null) continue;

            Card attackCard = CreateCardFromId(character.AttackCardId, character, true, false);
            Card defenseCard = CreateCardFromId(character.DefenseCardId, character, false, true);

            _teamDeck.Add(attackCard);
            _teamDeck.Add(defenseCard);

            _cardToCharacter[attackCard] = character;
            _cardToCharacter[defenseCard] = character;

            CheckBasicCardOwnership(basicCardOwners, character.AttackCardId, character, "打击");
            CheckBasicCardOwnership(basicCardOwners, character.DefenseCardId, character, "防御");

            foreach (var specialId in character.SpecialCardIds)
            {
                if (string.IsNullOrEmpty(specialId)) continue;
                Card specialCard = CreateCardFromId(specialId, character, false, false);
                _teamDeck.Add(specialCard);
                _cardToCharacter[specialCard] = character;
            }
        }

        // 守卫二：卡组里不得出现**同一个 Card 实例**两次
        // （共享实例会让「这张牌升级了 / 改费用了」污染到别的卡）。
        var seenInstances = new HashSet<Card>();
        foreach (var card in _teamDeck)
        {
            if (card == null) continue;
            if (!seenInstances.Add(card))
            {
                GD.PrintErr($"[Attacker] 卡组里出现重复的卡牌实例（{card.CardId}）—— 每张卡必须是独立实例");
            }
        }

        // 卡组快照（一行）：卡牌 id + 实例 id + 实际伤害 / 护盾
        // —— 数值出问题时先看这行，再去查数据表；实例 id 能直接看出「两张卡是不是同一个对象」。
        if (_teamDeck.Count > 0)
        {
            string snapshot = string.Join(" | ", _teamDeck.Select(c =>
                $"{c.CardId}#{c.GetInstanceId()} 伤{c.GetFinalDamage(c.LinkedAttributes)}盾{c.GetFinalShield(c.LinkedAttributes)}"));
            GD.Print($"[Attacker] 卡组({_teamDeck.Count} 张): {snapshot}");
        }
    }

    /// <summary>
    /// 守卫一：**同一张基本卡 id 不得被两个角色共用**。
    /// 共用了就等于退回「所有角色共用两张牌」，直接违背主人的要求。
    /// </summary>
    private void CheckBasicCardOwnership(Dictionary<string, string> owners, string cardId, CharacterDefinition character, string slot)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        if (owners.TryGetValue(cardId, out string ownerId))
        {
            GD.PrintErr($"[Attacker] 基本卡「{cardId}」被角色「{ownerId}」和「{character.CharacterId}」共用 —— 每个角色的「{slot}」必须是各自独立的卡");
            return;
        }

        owners[cardId] = character.CharacterId;
    }

    /// <summary>
    /// 按卡牌 id 造一张队伍卡。
    ///
    /// ⚠️ 这里曾经是**纯代码硬编码**（名字拼「角色名+特殊技」、费用按后缀猜、特殊技没有伤害），
    /// 结果是 <c>Data/character_cards.json</c> 里那 96 张卡的数据**完全没进战斗**：
    /// 12 张特殊技全是「使用特殊能力」的**空牌**（不打伤害、不给护盾、照样扣 2 点算力），
    /// 卡上的增益字段（applyBuffName）也永远读不到 → 「卡牌生效 → 状态图标」整条链是断的。
    ///
    /// 现在改成**以数据表为准**：卡名 / 费用 / 伤害 / 护盾 / 抽牌 / 能量 / 增益减益 全部来自 JSON；
    /// 数据表里没有的卡才回退到旧的代码兜底（并打警告，便于发现漏配）。
    /// </summary>
    private Card CreateCardFromId(string cardId, CharacterDefinition character, bool isAttack, bool isDefense)
    {
        Card card = null;

        if (string.IsNullOrEmpty(cardId))
        {
            GD.PrintErr($"[Attacker] 角色「{character.CharacterId}」的基本卡 id 为空（数据表 characters.json 缺 attackCardId / defenseCardId）");
        }
        else if (CharacterCardLoader.TryGetCharacterCard(cardId, out Card template))
        {
            // 复制一份：模板是共享实例，战斗里会改费用 / 刻印 / 等级
            card = template.Clone();

            // 怒气口径：战斗实际发的是 RageManager 里的 15 / 10 点（按攻击 / 防御牌区分），
            // 数据表的 rageGainPerLevel 是**每级成长值**、与实现不同口径，
            // 直接搬过来会让卡面显示一个和实际不符的怒气数字 → 不搬（卡面改按实现口径显示）。
            card.RageGainPerLevel = 0;

            // 卡牌挂上**它自己角色的属性** —— 伤害/护盾按「属性 × 百分比」算，
            // 卡面与战斗结算都走 Card.GetFinalDamage/GetFinalShield（同一个函数，不会漂）。
            card.LinkedAttributes = character.GetAttributes();
        }
        else
        {
            card = new Card
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

            GD.PrintErr($"[Attacker] 卡牌 {cardId} 不在 character_cards.json 里，已回退到代码兜底");
        }

        // IsDefense 数据表里没有这一项（只有 isAttack），按卡位语义补上
        if (isDefense)
        {
            card.IsDefense = true;
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
