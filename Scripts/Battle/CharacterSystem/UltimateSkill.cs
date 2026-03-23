using Godot;
using System;

public partial class UltimateSkill : Resource
{
    [Export]
    public string SkillId;

    [Export]
    public string Name;

    [Export]
    public string Description;

    [Export]
    public int RageCost = 100;

    [Export]
    public int Damage;

    [Export]
    public int Heal;

    [Export]
    public int Shield;

    [Export]
    public int DrawCount;

    [Export]
    public int BuffValue;

    [Export]
    public int BuffDuration;

    public delegate void SkillExecuteHandler(Player player, Enemy[] enemies);
    public SkillExecuteHandler OnExecute;

    public void Execute(Player player, Enemy[] enemies)
    {
        OnExecute?.Invoke(player, enemies);
    }

    public static UltimateSkill CreateRatUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "rat_ultimate",
            Name = "鼠群来袭",
            Description = "全体敌人受20伤害，抽3张",
            RageCost = 100,
            Damage = 20,
            DrawCount = 3
        };
        return skill;
    }

    public static UltimateSkill CreateOxUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "ox_ultimate",
            Name = "震天蛮牛冲",
            Description = "对最前方敌人造成50伤害",
            RageCost = 100,
            Damage = 50
        };
        return skill;
    }

    public static UltimateSkill CreateTigerUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "tiger_ultimate",
            Name = "猛虎灭世斩",
            Description = "全体敌人受30伤害",
            RageCost = 100,
            Damage = 30
        };
        return skill;
    }

    public static UltimateSkill CreateRabbitUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "rabbit_ultimate",
            Name = "疾风连蹬",
            Description = "随机敌人攻击4次，各10伤害",
            RageCost = 100,
            Damage = 10
        };
        return skill;
    }

    public static UltimateSkill CreateDragonUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "dragon_ultimate",
            Name = "龙王咆哮",
            Description = "全体敌人受25伤害，附加3层中毒",
            RageCost = 100,
            Damage = 25,
            BuffValue = 3,
            BuffDuration = 3
        };
        return skill;
    }

    public static UltimateSkill CreateSnakeUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "snake_ultimate",
            Name = "万蛇噬咬",
            Description = "全体敌人受15伤害，附加5层中毒",
            RageCost = 100,
            Damage = 15,
            BuffValue = 5,
            BuffDuration = 3
        };
        return skill;
    }

    public static UltimateSkill CreateHorseUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "horse_ultimate",
            Name = "踏雪飞驹",
            Description = "对敌人造成20伤害，+2能量",
            RageCost = 100,
            Damage = 20,
            BuffValue = 2
        };
        return skill;
    }

    public static UltimateSkill CreateGoatUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "goat_ultimate",
            Name = "羊灵祝福",
            Description = "全体队友回复15生命，加10护盾",
            RageCost = 100,
            Heal = 15,
            Shield = 10
        };
        return skill;
    }

    public static UltimateSkill CreateMonkeyUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "monkey_ultimate",
            Name = "齐天大圣",
            Description = "随机抽3张牌，本回合能量不限",
            RageCost = 100,
            DrawCount = 3,
            BuffValue = 999
        };
        return skill;
    }

    public static UltimateSkill CreateRoosterUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "rooster_ultimate",
            Name = "雄鸡一唱天下白",
            Description = "敌人下回合无法行动",
            RageCost = 100,
            BuffDuration = 1
        };
        return skill;
    }

    public static UltimateSkill CreateDogUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "dog_ultimate",
            Name = "忠犬护主",
            Description = "全体队友加25护盾",
            RageCost = 100,
            Shield = 25
        };
        return skill;
    }

    public static UltimateSkill CreatePigUltimate()
    {
        var skill = new UltimateSkill
        {
            SkillId = "pig_ultimate",
            Name = "猪刚鬣冲击",
            Description = "对最前方敌人造成35伤害，回10血",
            RageCost = 100,
            Damage = 35,
            Heal = 10
        };
        return skill;
    }

    public static UltimateSkill GetUltimateById(string skillId)
    {
        return skillId switch
        {
            "rat_ultimate" => CreateRatUltimate(),
            "ox_ultimate" => CreateOxUltimate(),
            "tiger_ultimate" => CreateTigerUltimate(),
            "rabbit_ultimate" => CreateRabbitUltimate(),
            "dragon_ultimate" => CreateDragonUltimate(),
            "snake_ultimate" => CreateSnakeUltimate(),
            "horse_ultimate" => CreateHorseUltimate(),
            "goat_ultimate" => CreateGoatUltimate(),
            "monkey_ultimate" => CreateMonkeyUltimate(),
            "rooster_ultimate" => CreateRoosterUltimate(),
            "dog_ultimate" => CreateDogUltimate(),
            "pig_ultimate" => CreatePigUltimate(),
            _ => CreateRatUltimate()
        };
    }
}
