using System;

namespace FishEatFish.Shop
{
    public enum EngravingEffectType
    {
        AttackBuff,
        DefenseBuff,
        HealthBuff,
        SpeedBuff,
        CritBuff,
        HealBuff,
        ShieldBuff,
        SlowOnAttack
    }

    [Serializable]
    public class EngravingEffect
    {
        public string type;
        public float value;

        public EngravingEffectType GetEffectType()
        {
            return type.ToLower() switch
            {
                "attack_buff" => EngravingEffectType.AttackBuff,
                "defense_buff" => EngravingEffectType.DefenseBuff,
                "health_buff" => EngravingEffectType.HealthBuff,
                "speed_buff" => EngravingEffectType.SpeedBuff,
                "crit_buff" => EngravingEffectType.CritBuff,
                "heal_buff" => EngravingEffectType.HealBuff,
                "shield_buff" => EngravingEffectType.ShieldBuff,
                "slow_on_attack" => EngravingEffectType.SlowOnAttack,
                _ => EngravingEffectType.AttackBuff
            };
        }
    }

    [Serializable]
    public class EngravingData
    {
        public string engravingId;
        public string name;
        public string icon;
        public string description;
        public int price;
        public EngravingEffect effect;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(engravingId) &&
                   !string.IsNullOrEmpty(name) &&
                   price > 0;
        }
    }

    [Serializable]
    public class EngravingsData
    {
        public EngravingData[] buffEngravings;
    }
}
