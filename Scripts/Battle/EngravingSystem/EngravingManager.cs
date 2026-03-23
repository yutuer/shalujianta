using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EngravingManager : Node
{
    private List<EngravingEffect> _engravingLibrary = new List<EngravingEffect>();

    public override void _Ready()
    {
        InitializeEngravingLibrary();
    }

    private void InitializeEngravingLibrary()
    {
        _engravingLibrary.AddRange(EngravingEffect.GetAllEngravingEffects());
    }

    public List<EngravingEffect> GetRandomEngravings(int count)
    {
        List<EngravingEffect> result = new List<EngravingEffect>();
        List<EngravingEffect> available = new List<EngravingEffect>(_engravingLibrary);

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        int actualCount = Mathf.Min(count, available.Count);

        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = rng.RandiRange(0, available.Count - 1);
            result.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        return result;
    }

    public List<Card> GetUnengravedCards(List<Card> allCards)
    {
        return allCards.Where(card => !card.IsEngraved).ToList();
    }

    public List<Card> GetRandomUnengravedCards(List<Card> allCards, int count)
    {
        List<Card> unengraved = GetUnengravedCards(allCards);

        if (unengraved.Count <= count)
        {
            return new List<Card>(unengraved);
        }

        List<Card> result = new List<Card>();
        List<Card> available = new List<Card>(unengraved);

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = rng.RandiRange(0, available.Count - 1);
            result.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        return result;
    }

    public void ApplyEngraving(Card card, EngravingEffect engraving)
    {
        if (card == null || engraving == null) return;

        card.IsEngraved = true;
        card.EngravingType = engraving.Type;
        card.EngravingEffect = engraving;
    }

    public void OnCardPlayed(Card card, Player player, Enemy[] enemies, CharacterPosition character, List<CharacterPosition> allCharacters)
    {
        if (card == null || !card.IsEngraved || card.EngravingEffect == null) return;

        switch (card.EngravingEffect.Type)
        {
            case EngravingType.Shield:
                ApplyShieldEffect(player);
                break;
            case EngravingType.Poison:
                ApplyPoisonEffect(enemies);
                break;
            case EngravingType.Copy:
                ApplyCopyEffect(card, player);
                break;
            case EngravingType.TeamRage:
                ApplyTeamRageEffect(character, allCharacters);
                break;
            case EngravingType.SelfRage:
                ApplySelfRageEffect(character);
                break;
            case EngravingType.Vulnerable:
                ApplyVulnerableEffect(enemies);
                break;
        }
    }

    private void ApplyShieldEffect(Player player)
    {
        if (player != null && player is Player p)
        {
            p.AddShield(5);
        }
    }

    private void ApplyPoisonEffect(Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                PoisonDebuff poison = new PoisonDebuff
                {
                    DamagePerTurn = 3
                };
                poison.Initialize();
                enemy.AddStatusEffect(poison);
            }
        }
    }

    private void ApplyCopyEffect(Card originalCard, Player player)
    {
        if (originalCard == null || player == null) return;

        Card copy = new Card
        {
            CardId = originalCard.CardId + "_copy",
            Name = originalCard.Name + "(复制)",
            Cost = originalCard.Cost,
            Description = originalCard.Description,
            IsAttack = originalCard.IsAttack,
            IsDefense = originalCard.IsDefense,
            Damage = originalCard.Damage,
            ShieldGain = originalCard.ShieldGain,
            EnergyGain = originalCard.EnergyGain,
            HealAmount = originalCard.HealAmount,
            DrawCount = originalCard.DrawCount,
            IsRetain = originalCard.IsRetain,
            IsEngraved = false
        };

        player.Deck.Add(copy);
    }

    private void ApplyTeamRageEffect(CharacterPosition character, List<CharacterPosition> allCharacters)
    {
        if (allCharacters == null) return;

        foreach (var pos in allCharacters)
        {
            if (pos != null && pos != character)
            {
                pos.AddRage(15);
            }
        }
    }

    private void ApplySelfRageEffect(CharacterPosition character)
    {
        if (character != null)
        {
            character.AddRage(30);
        }
    }

    private void ApplyVulnerableEffect(Enemy[] enemies)
    {
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsDead())
            {
                VulnerableDebuff vulnerable = new VulnerableDebuff
                {
                    DamageIncrease = 50
                };
                vulnerable.Initialize();
                enemy.AddStatusEffect(vulnerable);
            }
        }
    }

    public List<EngravingEffect> GetAllEngravingEffects()
    {
        return new List<EngravingEffect>(_engravingLibrary);
    }

    public bool ShouldShowEngravingInsteadOfArtifact()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(1, 100);
        return roll <= 50;
    }
}
