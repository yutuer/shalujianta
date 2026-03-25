using System.Text;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle.Card;

public static class DescriptionGenerator
{
    public static string GenerateCardDescription(Card card, CharacterAttributes attributes = null)
    {
        if (attributes == null)
        {
            attributes = card.LinkedAttributes;
        }

        var parts = new List<string>();

        if (card.IsAttack && card.Damage > 0)
        {
            int damage = attributes != null
                ? card.CalculatedDamage
                : card.Damage;
            parts.Add($"造成 {damage} 点伤害");
        }

        if (card.ShieldGain > 0)
        {
            int shield = attributes != null
                ? card.CalculatedShield
                : card.ShieldGain;
            parts.Add($"获得 {shield} 点护盾");
        }

        if (card.HealAmount > 0)
        {
            int heal = attributes != null
                ? card.CalculatedHeal
                : card.HealAmount;
            parts.Add($"回复 {heal} 点生命");
        }

        if (card.EnergyGain > 0 || card.EnergyGainBase > 0)
        {
            int energy = attributes != null
                ? card.CalculatedEnergyGain
                : (card.EnergyGain > 0 ? card.EnergyGain : card.EnergyGainBase);
            parts.Add($"获得 {energy} 点能量");
        }

        if (card.DrawCount > 0 || card.DrawCountBase > 0)
        {
            int draw = attributes != null
                ? card.CalculatedDrawCount
                : (card.DrawCount > 0 ? card.DrawCount : card.DrawCountBase);
            parts.Add($"抽 {draw} 张牌");
        }

        if (!string.IsNullOrEmpty(card.ApplyBuffName) && card.ApplyBuffDuration > 0)
        {
            int buffValue = attributes != null ? card.CalculatedBuffValue : card.BuffValueBase;
            string buffName = GetBuffDisplayName(card.ApplyBuffName);
            parts.Add($"获得 {buffName} +{buffValue}，持续 {card.ApplyBuffDuration} 回合");
        }

        if (!string.IsNullOrEmpty(card.ApplyDebuffName) && card.ApplyDebuffDuration > 0)
        {
            int debuffValue = attributes != null ? card.CalculatedDebuffValue : card.DebuffValueBase;
            string debuffName = GetDebuffDisplayName(card.ApplyDebuffName);
            parts.Add($"目标获得 {debuffName} -{debuffValue}，持续 {card.ApplyDebuffDuration} 回合");
        }

        if (card.RageGainPerLevel > 0 || card.Level > 1)
        {
            int rage = card.CalculatedRageGain;
            parts.Add($"获得 {rage} 怒气");
        }

        if (card.IsRetain)
        {
            parts.Add("保留");
        }

        if (parts.Count == 0)
        {
            return card.Description;
        }

        return string.Join("，", parts);
    }

    public static string GenerateUltimateDescription(UltimateSkill ultimate, CharacterAttributes attributes = null)
    {
        if (attributes == null)
        {
            attributes = ultimate.LinkedAttributes;
        }

        var parts = new List<string>();

        if (ultimate.Damage > 0)
        {
            int damage = attributes != null
                ? ultimate.CalculatedDamage
                : ultimate.Damage;
            parts.Add($"造成 {damage} 点伤害");
        }

        if (ultimate.Heal > 0)
        {
            int heal = attributes != null
                ? ultimate.CalculatedHeal
                : ultimate.Heal;
            parts.Add($"回复 {heal} 点生命");
        }

        if (ultimate.Shield > 0)
        {
            int shield = attributes != null
                ? ultimate.CalculatedShield
                : ultimate.Shield;
            parts.Add($"获得 {shield} 点护盾");
        }

        if (ultimate.DrawCount > 0 || ultimate.DrawCountPerLevel > 0)
        {
            int draw = attributes != null
                ? ultimate.CalculatedDrawCount
                : ultimate.DrawCount;
            parts.Add($"抽 {draw} 张牌");
        }

        if (ultimate.BuffValue > 0 || ultimate.BuffValuePerLevel > 0)
        {
            int buffVal = attributes != null
                ? ultimate.CalculatedBuffValue
                : ultimate.BuffValue;
            if (ultimate.BuffDuration > 0)
            {
                parts.Add($"附加 {buffVal} 层效果，持续 {ultimate.BuffDuration} 回合");
            }
            else
            {
                parts.Add($"附加 {buffVal} 层效果");
            }
        }

        if (ultimate.BuffDuration == 1 && ultimate.Damage == 0 && ultimate.Heal == 0 && ultimate.Shield == 0)
        {
            parts.Add("目标下回合无法行动");
        }

        if (parts.Count == 0)
        {
            return ultimate.Description;
        }

        return string.Join("，", parts);
    }

    public static string GetBuffDisplayName(string buffName)
    {
        return buffName.ToLower() switch
        {
            "strength" => "攻击力",
            "defense" => "防御力",
            "regeneration" => "回复",
            "thorns" => "荆棘",
            "fury" => "狂暴",
            _ => buffName
        };
    }

    public static string GetDebuffDisplayName(string debuffName)
    {
        return debuffName.ToLower() switch
        {
            "weak" => "虚弱",
            "vulnerable" => "易伤",
            "poison" => "中毒",
            "slow" => "减速",
            "silence" => "沉默",
            _ => debuffName
        };
    }

    public static string GenerateLevelInfo(Card card)
    {
        if (card.Level >= card.MaxLevel)
        {
            return $"等级: {card.Level} (MAX)";
        }
        return $"等级: {card.Level}/{card.MaxLevel}";
    }

    public static string GenerateUpgradePreview(Card card, CharacterAttributes attributes = null)
    {
        if (card.Level >= card.MaxLevel)
        {
            return "已达到最大等级";
        }

        if (attributes == null)
        {
            attributes = card.LinkedAttributes;
        }

        var preview = new StringBuilder();
        preview.AppendLine("升级后效果:");

        if (card.IsAttack && card.Damage > 0)
        {
            int current = attributes != null ? card.CalculatedDamage : card.Damage;
            int next = CardLevelSystem.CalculateAttributeLinkedValue(
                card.Level + 1,
                card.Damage,
                card.DamageBaseCoefficient);
            preview.AppendLine($"  伤害: {current} → {next}");
        }

        if (card.ShieldGain > 0)
        {
            int current = attributes != null ? card.CalculatedShield : card.ShieldGain;
            int next = CardLevelSystem.CalculateAttributeLinkedValue(
                card.Level + 1,
                card.ShieldGain,
                card.ShieldBaseCoefficient);
            preview.AppendLine($"  护盾: {current} → {next}");
        }

        if (card.HealAmount > 0)
        {
            int current = attributes != null ? card.CalculatedHeal : card.HealAmount;
            int next = CardLevelSystem.CalculateAttributeLinkedValue(
                card.Level + 1,
                card.HealAmount,
                card.HealBaseCoefficient);
            preview.AppendLine($"  治疗: {current} → {next}");
        }

        if (card.RageGainPerLevel > 0)
        {
            int current = card.CalculatedRageGain;
            int next = CardLevelSystem.CalculateRageGain(card.Level + 1, card.RageGainPerLevel);
            preview.AppendLine($"  怒气: {current} → {next}");
        }

        return preview.ToString();
    }
}
