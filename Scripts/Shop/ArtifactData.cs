using System;

namespace FishEatFish.Shop
{
    public enum ArtifactEffectType
    {
        AttackBuff,
        DefenseBuff,
        HealthBuff,
        SpeedBuff,
        CritChance,
        CritBuff,
        HealPercent,
        DamageReduction,
        StunChance,
        DodgeChance,
        HealBuff,
        ShieldBuff,
        RageGain,
        InitialShield,
        AoeOnKill,
        SlowOnAttack
    }

    [Serializable]
    public class ArtifactEffect
    {
        public string type;
        public float value;

        public ArtifactEffectType GetEffectType()
        {
            return type.ToLower() switch
            {
                "attack_buff" => ArtifactEffectType.AttackBuff,
                "defense_buff" => ArtifactEffectType.DefenseBuff,
                "health_buff" => ArtifactEffectType.HealthBuff,
                "speed_buff" => ArtifactEffectType.SpeedBuff,
                "crit_chance" => ArtifactEffectType.CritChance,
                "crit_buff" => ArtifactEffectType.CritBuff,
                "heal_percent" => ArtifactEffectType.HealPercent,
                "damage_reduction" => ArtifactEffectType.DamageReduction,
                "stun_chance" => ArtifactEffectType.StunChance,
                "dodge_chance" => ArtifactEffectType.DodgeChance,
                "heal_buff" => ArtifactEffectType.HealBuff,
                "shield_buff" => ArtifactEffectType.ShieldBuff,
                "rage_gain" => ArtifactEffectType.RageGain,
                "initial_shield" => ArtifactEffectType.InitialShield,
                "aoe_on_kill" => ArtifactEffectType.AoeOnKill,
                "slow_on_attack" => ArtifactEffectType.SlowOnAttack,
                _ => ArtifactEffectType.AttackBuff
            };
        }
    }

    [Serializable]
    public class ArtifactData
    {
        public string artifactId;
        public string name;
        public string icon;
        public string description;
        public int price;
        public ArtifactEffect effect;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(artifactId) &&
                   !string.IsNullOrEmpty(name) &&
                   price > 0;
        }
    }

    [Serializable]
    public class ArtifactsData
    {
        public ArtifactData[] artifacts;
    }
}
