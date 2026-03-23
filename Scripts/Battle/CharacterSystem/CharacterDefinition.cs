using Godot;

public partial class CharacterDefinition : Resource
{
    [Export]
    public string CharacterId;

    [Export]
    public string Name;

    [Export]
    public string NameEn;

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
    public string AttackCardId;

    [Export]
    public string DefenseCardId;

    [Export]
    public string[] SpecialCardIds = new string[3];

    [Export]
    public string UltimateSkillId;

    public static CharacterDefinition CreateRat()
    {
        return new CharacterDefinition
        {
            CharacterId = "rat",
            Name = "鼠",
            NameEn = "Rat",
            BaseHealth = 25,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 4,
            BaseDefense = 2,
            AttackCardId = "rat_attack",
            DefenseCardId = "rat_defense",
            SpecialCardIds = new string[] { "rat_special1", "rat_special2", "rat_special3" },
            UltimateSkillId = "rat_ultimate"
        };
    }

    public static CharacterDefinition CreateOx()
    {
        return new CharacterDefinition
        {
            CharacterId = "ox",
            Name = "牛",
            NameEn = "Ox",
            BaseHealth = 45,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 8,
            BaseDefense = 5,
            AttackCardId = "ox_attack",
            DefenseCardId = "ox_defense",
            SpecialCardIds = new string[] { "ox_special1", "ox_special2", "ox_special3" },
            UltimateSkillId = "ox_ultimate"
        };
    }

    public static CharacterDefinition CreateTiger()
    {
        return new CharacterDefinition
        {
            CharacterId = "tiger",
            Name = "虎",
            NameEn = "Tiger",
            BaseHealth = 35,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 3,
            AttackCardId = "tiger_attack",
            DefenseCardId = "tiger_defense",
            SpecialCardIds = new string[] { "tiger_special1", "tiger_special2", "tiger_special3" },
            UltimateSkillId = "tiger_ultimate"
        };
    }

    public static CharacterDefinition CreateRabbit()
    {
        return new CharacterDefinition
        {
            CharacterId = "rabbit",
            Name = "兔",
            NameEn = "Rabbit",
            BaseHealth = 20,
            BaseEnergy = 3,
            BaseDrawCount = 3,
            BaseAttack = 4,
            BaseDefense = 2,
            AttackCardId = "rabbit_attack",
            DefenseCardId = "rabbit_defense",
            SpecialCardIds = new string[] { "rabbit_special1", "rabbit_special2", "rabbit_special3" },
            UltimateSkillId = "rabbit_ultimate"
        };
    }

    public static CharacterDefinition CreateDragon()
    {
        return new CharacterDefinition
        {
            CharacterId = "dragon",
            Name = "龙",
            NameEn = "Dragon",
            BaseHealth = 40,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 6,
            BaseDefense = 4,
            AttackCardId = "dragon_attack",
            DefenseCardId = "dragon_defense",
            SpecialCardIds = new string[] { "dragon_special1", "dragon_special2", "dragon_special3" },
            UltimateSkillId = "dragon_ultimate"
        };
    }

    public static CharacterDefinition CreateSnake()
    {
        return new CharacterDefinition
        {
            CharacterId = "snake",
            Name = "蛇",
            NameEn = "Snake",
            BaseHealth = 25,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 5,
            BaseDefense = 2,
            AttackCardId = "snake_attack",
            DefenseCardId = "snake_defense",
            SpecialCardIds = new string[] { "snake_special1", "snake_special2", "snake_special3" },
            UltimateSkillId = "snake_ultimate"
        };
    }

    public static CharacterDefinition CreateHorse()
    {
        return new CharacterDefinition
        {
            CharacterId = "horse",
            Name = "马",
            NameEn = "Horse",
            BaseHealth = 30,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 6,
            BaseDefense = 3,
            AttackCardId = "horse_attack",
            DefenseCardId = "horse_defense",
            SpecialCardIds = new string[] { "horse_special1", "horse_special2", "horse_special3" },
            UltimateSkillId = "horse_ultimate"
        };
    }

    public static CharacterDefinition CreateGoat()
    {
        return new CharacterDefinition
        {
            CharacterId = "goat",
            Name = "羊",
            NameEn = "Goat",
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 4,
            BaseDefense = 4,
            AttackCardId = "goat_attack",
            DefenseCardId = "goat_defense",
            SpecialCardIds = new string[] { "goat_special1", "goat_special2", "goat_special3" },
            UltimateSkillId = "goat_ultimate"
        };
    }

    public static CharacterDefinition CreateMonkey()
    {
        return new CharacterDefinition
        {
            CharacterId = "monkey",
            Name = "猴",
            NameEn = "Monkey",
            BaseHealth = 25,
            BaseEnergy = 3,
            BaseDrawCount = 3,
            BaseAttack = 5,
            BaseDefense = 2,
            AttackCardId = "monkey_attack",
            DefenseCardId = "monkey_defense",
            SpecialCardIds = new string[] { "monkey_special1", "monkey_special2", "monkey_special3" },
            UltimateSkillId = "monkey_ultimate"
        };
    }

    public static CharacterDefinition CreateRooster()
    {
        return new CharacterDefinition
        {
            CharacterId = "rooster",
            Name = "鸡",
            NameEn = "Rooster",
            BaseHealth = 20,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 2,
            AttackCardId = "rooster_attack",
            DefenseCardId = "rooster_defense",
            SpecialCardIds = new string[] { "rooster_special1", "rooster_special2", "rooster_special3" },
            UltimateSkillId = "rooster_ultimate"
        };
    }

    public static CharacterDefinition CreateDog()
    {
        return new CharacterDefinition
        {
            CharacterId = "dog",
            Name = "狗",
            NameEn = "Dog",
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 5,
            BaseDefense = 4,
            AttackCardId = "dog_attack",
            DefenseCardId = "dog_defense",
            SpecialCardIds = new string[] { "dog_special1", "dog_special2", "dog_special3" },
            UltimateSkillId = "dog_ultimate"
        };
    }

    public static CharacterDefinition CreatePig()
    {
        return new CharacterDefinition
        {
            CharacterId = "pig",
            Name = "猪",
            NameEn = "Pig",
            BaseHealth = 35,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 5,
            BaseDefense = 5,
            AttackCardId = "pig_attack",
            DefenseCardId = "pig_defense",
            SpecialCardIds = new string[] { "pig_special1", "pig_special2", "pig_special3" },
            UltimateSkillId = "pig_ultimate"
        };
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
