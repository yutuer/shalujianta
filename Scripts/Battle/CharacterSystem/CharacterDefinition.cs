using Godot;

namespace FishEatFish.Battle.CharacterSystem;

public partial class CharacterDefinition : Resource
{
    [Export]
    public string CharacterId;

    [Export]
    public string Name;

    [Export]
    public string NameEn;

    [Export]
    public CharacterRace Race = CharacterRace.Chaos;

    [Export]
    public CharacterArchetype Archetype = CharacterArchetype.Striker;

    [Export]
    public int BaseHealth = 30;

    [Export]
    public int BaseEnergy = 2;

    [Export]
    public int BaseDrawCount = 2;

    [Export]
    public int BaseAttack = 5;

    [Export]
    public int BaseDefense = 3;

    [Export]
    public int BaseConstitution = 10;

    [Export]
    public int BaseCritRate = 5;

    [Export]
    public int BaseCritDamage = 150;

    [Export]
    public int BaseDeathResistance = 10;

    [Export]
    public int BaseSilverKeyCharge = 2;

    [Export]
    public int BaseRageReturn = 2;

    [Export]
    public int BaseMaxRage = 30;

    [Export]
    public string AttackCardId;

    [Export]
    public string DefenseCardId;

    [Export]
    public string[] SpecialCardIds = new string[3];

    [Export]
    public string UltimateSkillId;

    public CharacterAttributes GetAttributes()
    {
        var attributes = new CharacterAttributes();
        attributes.Attack = BaseAttack;
        attributes.Defense = BaseDefense;
        attributes.Constitution = BaseConstitution;
        attributes.CritRate = BaseCritRate;
        attributes.CritDamage = BaseCritDamage;
        attributes.DeathResistance = BaseDeathResistance;
        attributes.SilverKeyCharge = BaseSilverKeyCharge;
        attributes.RageReturn = BaseRageReturn;
        attributes.MaxRage = BaseMaxRage;
        attributes.Race = Race;
        return attributes;
    }

    public void ApplyRaceDefaults()
    {
        var raceConfig = RaceAttributeConfig.GetConfig(Race);
    }

    public static CharacterDefinition CreateRat()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "rat",
            Name = "鼠",
            NameEn = "Rat",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 25,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 4,
            BaseDefense = 2,
            BaseConstitution = 8,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "rat_attack",
            DefenseCardId = "rat_defense",
            SpecialCardIds = new string[] { "rat_special1", "rat_special2", "rat_special3" },
            UltimateSkillId = "rat_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateOx()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "ox",
            Name = "牛",
            NameEn = "Ox",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 45,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 8,
            BaseDefense = 5,
            BaseConstitution = 12,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "ox_attack",
            DefenseCardId = "ox_defense",
            SpecialCardIds = new string[] { "ox_special1", "ox_special2", "ox_special3" },
            UltimateSkillId = "ox_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateTiger()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "tiger",
            Name = "虎",
            NameEn = "Tiger",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 35,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 3,
            BaseConstitution = 10,
            BaseCritRate = 10,
            BaseCritDamage = 175,
            AttackCardId = "tiger_attack",
            DefenseCardId = "tiger_defense",
            SpecialCardIds = new string[] { "tiger_special1", "tiger_special2", "tiger_special3" },
            UltimateSkillId = "tiger_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateRabbit()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "rabbit",
            Name = "兔",
            NameEn = "Rabbit",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 20,
            BaseEnergy = 3,
            BaseDrawCount = 3,
            BaseAttack = 4,
            BaseDefense = 2,
            BaseConstitution = 6,
            BaseCritRate = 15,
            BaseCritDamage = 200,
            AttackCardId = "rabbit_attack",
            DefenseCardId = "rabbit_defense",
            SpecialCardIds = new string[] { "rabbit_special1", "rabbit_special2", "rabbit_special3" },
            UltimateSkillId = "rabbit_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateDragon()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "dragon",
            Name = "龙",
            NameEn = "Dragon",
            Race = CharacterRace.Abyss,
            Archetype = CharacterArchetype.Mage,
            BaseHealth = 40,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 6,
            BaseDefense = 4,
            BaseConstitution = 11,
            BaseCritRate = 8,
            BaseCritDamage = 160,
            AttackCardId = "dragon_attack",
            DefenseCardId = "dragon_defense",
            SpecialCardIds = new string[] { "dragon_special1", "dragon_special2", "dragon_special3" },
            UltimateSkillId = "dragon_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateSnake()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "snake",
            Name = "蛇",
            NameEn = "Snake",
            Race = CharacterRace.Abyss,
            Archetype = CharacterArchetype.Poison,
            BaseHealth = 25,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 5,
            BaseDefense = 2,
            BaseConstitution = 8,
            BaseCritRate = 12,
            BaseCritDamage = 180,
            AttackCardId = "snake_attack",
            DefenseCardId = "snake_defense",
            SpecialCardIds = new string[] { "snake_special1", "snake_special2", "snake_special3" },
            UltimateSkillId = "snake_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateHorse()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "horse",
            Name = "马",
            NameEn = "Horse",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 30,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 6,
            BaseDefense = 3,
            BaseConstitution = 9,
            BaseCritRate = 10,
            BaseCritDamage = 165,
            AttackCardId = "horse_attack",
            DefenseCardId = "horse_defense",
            SpecialCardIds = new string[] { "horse_special1", "horse_special2", "horse_special3" },
            UltimateSkillId = "horse_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateGoat()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "goat",
            Name = "羊",
            NameEn = "Goat",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Healer,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 4,
            BaseDefense = 4,
            BaseConstitution = 10,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "goat_attack",
            DefenseCardId = "goat_defense",
            SpecialCardIds = new string[] { "goat_special1", "goat_special2", "goat_special3" },
            UltimateSkillId = "goat_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateMonkey()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "monkey",
            Name = "猴",
            NameEn = "Monkey",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Debuffer,
            BaseHealth = 25,
            BaseEnergy = 3,
            BaseDrawCount = 3,
            BaseAttack = 5,
            BaseDefense = 2,
            BaseConstitution = 7,
            BaseCritRate = 8,
            BaseCritDamage = 170,
            AttackCardId = "monkey_attack",
            DefenseCardId = "monkey_defense",
            SpecialCardIds = new string[] { "monkey_special1", "monkey_special2", "monkey_special3" },
            UltimateSkillId = "monkey_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateRooster()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "rooster",
            Name = "鸡",
            NameEn = "Rooster",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 20,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 2,
            BaseConstitution = 6,
            BaseCritRate = 18,
            BaseCritDamage = 185,
            AttackCardId = "rooster_attack",
            DefenseCardId = "rooster_defense",
            SpecialCardIds = new string[] { "rooster_special1", "rooster_special2", "rooster_special3" },
            UltimateSkillId = "rooster_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateDog()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "dog",
            Name = "狗",
            NameEn = "Dog",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 5,
            BaseDefense = 4,
            BaseConstitution = 10,
            BaseCritRate = 6,
            BaseCritDamage = 155,
            AttackCardId = "dog_attack",
            DefenseCardId = "dog_defense",
            SpecialCardIds = new string[] { "dog_special1", "dog_special2", "dog_special3" },
            UltimateSkillId = "dog_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreatePig()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "pig",
            Name = "猪",
            NameEn = "Pig",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Counter,
            BaseHealth = 35,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 5,
            BaseDefense = 5,
            BaseConstitution = 11,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "pig_attack",
            DefenseCardId = "pig_defense",
            SpecialCardIds = new string[] { "pig_special1", "pig_special2", "pig_special3" },
            UltimateSkillId = "pig_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition[] GetAllCharacters()
    {
        return new CharacterDefinition[]
        {
            CreateRat(),
            CreateOx(),
            CreateTiger(),
            CreateRabbit(),
            CreateDragon(),
            CreateSnake(),
            CreateHorse(),
            CreateGoat(),
            CreateMonkey(),
            CreateRooster(),
            CreateDog(),
            CreatePig()
        };
    }
}
