using Godot;
using System;
using System.Collections.Generic;
using FishEatFish.Battle.Effects;

public enum ArtifactType
{
    Attacker,
    Character,
    Condition
}

public enum ArtifactRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public partial class Artifact : Resource
{
    [Export]
    public string ArtifactId;

    [Export]
    public string Name;

    [Export]
    public string Description;

    [Export]
    public ArtifactType Type;

    [Export]
    public ArtifactRarity Rarity;

    [Export]
    public string TargetCharacterId;

    [Export]
    public string TriggerCondition;

    [Export]
    public int HealthBonus = 0;

    [Export]
    public int AttackBonus = 0;

    [Export]
    public int DefenseBonus = 0;

    [Export]
    public int EnergyBonus = 0;

    [Export]
    public int HealthPenalty = 0;

    [Export]
    public int AttackPenalty = 0;

    [Export]
    public int DefensePenalty = 0;

    [Export]
    public int EnergyPenalty = 0;

    public List<string> EffectIds { get; set; } = new List<string>();

    public List<Effect> CachedEffects { get; set; } = new List<Effect>();

    public void LoadEffects()
    {
        CachedEffects.Clear();
        foreach (var effectId in EffectIds)
        {
            var effect = EffectRegistry.CreateEffect(effectId);
            if (effect != null)
            {
                CachedEffects.Add(effect);
            }
        }
    }

    public List<Effect> GetEffects()
    {
        if (CachedEffects.Count == 0 && EffectIds.Count > 0)
        {
            LoadEffects();
        }
        return CachedEffects;
    }

    public static Artifact GenerateRandom()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        int roll = rng.RandiRange(1, 100);
        ArtifactRarity rarity;
        if (roll <= 60)
            rarity = ArtifactRarity.Common;
        else if (roll <= 85)
            rarity = ArtifactRarity.Rare;
        else if (roll <= 95)
            rarity = ArtifactRarity.Epic;
        else
            rarity = ArtifactRarity.Legendary;

        return CreateRandomArtifactOfRarity(rarity);
    }

    private static Artifact CreateRandomArtifactOfRarity(ArtifactRarity rarity)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int typeRoll = rng.RandiRange(1, 3);

        Artifact artifact = new Artifact
        {
            Rarity = rarity
        };

        switch (typeRoll)
        {
            case 1:
                return CreateAttackerArtifact(artifact, rarity);
            case 2:
                return CreateCharacterArtifact(artifact, rarity);
            case 3:
                return CreateConditionArtifact(artifact, rarity);
            default:
                return CreateAttackerArtifact(artifact, rarity);
        }
    }

    private static Artifact CreateAttackerArtifact(Artifact artifact, ArtifactRarity rarity)
    {
        artifact.Type = ArtifactType.Attacker;
        artifact.ArtifactId = $"attacker_{Guid.NewGuid().ToString().Substring(0, 8)}";

        switch (rarity)
        {
            case ArtifactRarity.Common:
                artifact.Name = "战士之心";
                artifact.Description = "所有角色攻击+5";
                artifact.AttackBonus = 5;
                artifact.HealthPenalty = 2;
                break;
            case ArtifactRarity.Rare:
                artifact.Name = "魔力戒指";
                artifact.Description = "所有角色能量+2";
                artifact.EnergyBonus = 2;
                artifact.DefensePenalty = 1;
                break;
            case ArtifactRarity.Epic:
                artifact.Name = "生命护符";
                artifact.Description = "所有角色生命+20";
                artifact.HealthBonus = 20;
                break;
            case ArtifactRarity.Legendary:
                artifact.Name = "龙之力量";
                artifact.Description = "所有角色攻击+10，防御+5";
                artifact.AttackBonus = 10;
                artifact.DefenseBonus = 5;
                break;
        }

        return artifact;
    }

    private static Artifact CreateCharacterArtifact(Artifact artifact, ArtifactRarity rarity)
    {
        artifact.Type = ArtifactType.Character;

        string[] characterIds = { "rat", "ox", "tiger", "rabbit", "dragon", "snake", "horse", "goat", "monkey", "rooster", "dog", "pig" };
        string[] characterNames = { "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" };

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int index = rng.RandiRange(0, characterIds.Length - 1);

        artifact.TargetCharacterId = characterIds[index];

        switch (rarity)
        {
            case ArtifactRarity.Common:
                artifact.Name = $"{characterNames[index]}的利爪";
                artifact.Description = $"{characterNames[index]}的攻击+8";
                artifact.AttackBonus = 8;
                artifact.DefensePenalty = 2;
                break;
            case ArtifactRarity.Rare:
                artifact.Name = $"{characterNames[index]}的坚韧";
                artifact.Description = $"{characterNames[index]}的生命+15";
                artifact.HealthBonus = 15;
                artifact.EnergyPenalty = 1;
                break;
            case ArtifactRarity.Epic:
                artifact.Name = $"{characterNames[index]}之鳞片";
                artifact.Description = $"{characterNames[index]}的防御+10";
                artifact.DefenseBonus = 10;
                break;
            case ArtifactRarity.Legendary:
                artifact.Name = $"{characterNames[index]}的传承";
                artifact.Description = $"{characterNames[index]}全属性+5";
                artifact.AttackBonus = 5;
                artifact.DefenseBonus = 5;
                artifact.HealthBonus = 5;
                break;
        }

        artifact.ArtifactId = $"{characterIds[index]}_artifact_{Guid.NewGuid().ToString().Substring(0, 8)}";
        return artifact;
    }

    private static Artifact CreateConditionArtifact(Artifact artifact, ArtifactRarity rarity)
    {
        artifact.Type = ArtifactType.Condition;

        switch (rarity)
        {
            case ArtifactRarity.Common:
                artifact.Name = "怒气之魂";
                artifact.Description = "怒气满100时伤害+20%";
                artifact.AttackBonus = 3;
                artifact.DefensePenalty = 3;
                artifact.TriggerCondition = "rage_full";
                break;
            case ArtifactRarity.Rare:
                artifact.Name = "连击之刃";
                artifact.Description = "连续攻击3次伤害+15";
                artifact.AttackBonus = 5;
                artifact.TriggerCondition = "combo_3";
                break;
            case ArtifactRarity.Epic:
                artifact.Name = "濒死之力";
                artifact.Description = "生命<30%时攻击+10";
                artifact.AttackBonus = 10;
                artifact.HealthPenalty = 5;
                artifact.TriggerCondition = "health_low";
                break;
            case ArtifactRarity.Legendary:
                artifact.Name = "凤凰之羽";
                artifact.Description = "每场战斗首次濒死时回复50%生命";
                artifact.TriggerCondition = "near_death";
                break;
        }

        artifact.ArtifactId = $"condition_{Guid.NewGuid().ToString().Substring(0, 8)}";
        return artifact;
    }

    public int GetTotalAttackBonus()
    {
        return AttackBonus;
    }

    public int GetTotalDefenseBonus()
    {
        return DefenseBonus;
    }

    public int GetTotalHealthBonus()
    {
        return HealthBonus;
    }

    public int GetTotalEnergyBonus()
    {
        return EnergyBonus;
    }

    public int GetTotalAttackPenalty()
    {
        return AttackPenalty;
    }

    public int GetTotalDefensePenalty()
    {
        return DefensePenalty;
    }
}
