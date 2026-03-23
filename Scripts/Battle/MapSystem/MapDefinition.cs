using Godot;

public enum MapDifficulty
{
    Easy,
    Normal,
    Hard
}

public partial class MapDefinition : Resource
{
    [Export]
    public string MapId;

    [Export]
    public string Name;

    [Export]
    public string Description;

    [Export]
    public MapDifficulty Difficulty;

    [Export]
    public int BattleCount = 1;

    [Export]
    public int GoldReward = 100;

    [Export]
    public string[] EnemyWaveIds = new string[0];

    [Export]
    public bool IsUnlocked = false;

    [Export]
    public bool IsCompleted = false;

    public static MapDefinition CreateMap01()
    {
        return new MapDefinition
        {
            MapId = "map_01",
            Name = "森林深处",
            Description = "一片幽暗的森林，隐藏着未知的危险",
            Difficulty = MapDifficulty.Easy,
            BattleCount = 1,
            GoldReward = 50,
            EnemyWaveIds = new string[] { "wave_01" }
        };
    }

    public static MapDefinition CreateMap02()
    {
        return new MapDefinition
        {
            MapId = "map_02",
            Name = "沼泽地带",
            Description = "泥泞的沼泽中潜伏着各种毒物",
            Difficulty = MapDifficulty.Normal,
            BattleCount = 2,
            GoldReward = 100,
            EnemyWaveIds = new string[] { "wave_02", "wave_03" }
        };
    }

    public static MapDefinition CreateMap03()
    {
        return new MapDefinition
        {
            MapId = "map_03",
            Name = "龙之巢穴",
            Description = "传说中最危险的区域，巨龙栖息之地",
            Difficulty = MapDifficulty.Hard,
            BattleCount = 3,
            GoldReward = 200,
            EnemyWaveIds = new string[] { "wave_04", "wave_05", "wave_06" }
        };
    }

    public static MapDefinition[] GetAllMaps()
    {
        return new MapDefinition[]
        {
            CreateMap01(),
            CreateMap02(),
            CreateMap03()
        };
    }
}
