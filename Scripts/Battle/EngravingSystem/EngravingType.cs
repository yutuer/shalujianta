using Godot;

public enum EngravingType
{
    Shield,
    Poison,
    Copy,
    TeamRage,
    SelfRage,
    Vulnerable
}

public class EngravingEffect
{
    public EngravingType Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int PrimaryValue { get; set; }
    public int SecondaryValue { get; set; }
    public int Priority { get; set; }
    public string Rarity { get; set; }

    public static EngravingEffect CreateShieldEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.Shield,
            Id = "E1",
            Name = "护盾刻印",
            Description = "使用此卡后获得 +5 护盾",
            PrimaryValue = 5,
            Priority = 1,
            Rarity = "common"
        };
    }

    public static EngravingEffect CreatePoisonEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.Poison,
            Id = "E2",
            Name = "毒伤刻印",
            Description = "目标中毒 3 回合，每回合受 3 点伤害",
            PrimaryValue = 3,
            SecondaryValue = 3,
            Priority = 2,
            Rarity = "common"
        };
    }

    public static EngravingEffect CreateCopyEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.Copy,
            Id = "E3",
            Name = "复制刻印",
            Description = "获得一张此卡牌的复制",
            Priority = 3,
            Rarity = "rare"
        };
    }

    public static EngravingEffect CreateTeamRageEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.TeamRage,
            Id = "E4",
            Name = "团队怒气刻印",
            Description = "其他队友各获得 +15 怒气值",
            PrimaryValue = 15,
            Priority = 4,
            Rarity = "rare"
        };
    }

    public static EngravingEffect CreateSelfRageEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.SelfRage,
            Id = "E5",
            Name = "自身怒气刻印",
            Description = "出牌角色获得 +30 怒气值",
            PrimaryValue = 30,
            Priority = 5,
            Rarity = "common"
        };
    }

    public static EngravingEffect CreateVulnerableEngraving()
    {
        return new EngravingEffect
        {
            Type = EngravingType.Vulnerable,
            Id = "E6",
            Name = "易伤刻印",
            Description = "所有敌人受到伤害 +50%，持续1回合",
            PrimaryValue = 50,
            SecondaryValue = 1,
            Priority = 6,
            Rarity = "epic"
        };
    }

    public static EngravingEffect[] GetAllEngravingEffects()
    {
        return new EngravingEffect[]
        {
            CreateShieldEngraving(),
            CreatePoisonEngraving(),
            CreateCopyEngraving(),
            CreateTeamRageEngraving(),
            CreateSelfRageEngraving(),
            CreateVulnerableEngraving()
        };
    }

    public static EngravingEffect GetById(string id)
    {
        return id switch
        {
            "E1" => CreateShieldEngraving(),
            "E2" => CreatePoisonEngraving(),
            "E3" => CreateCopyEngraving(),
            "E4" => CreateTeamRageEngraving(),
            "E5" => CreateSelfRageEngraving(),
            "E6" => CreateVulnerableEngraving(),
            _ => CreateShieldEngraving()
        };
    }
}
