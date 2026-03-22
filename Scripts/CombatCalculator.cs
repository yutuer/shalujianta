using Godot;

public static class CombatCalculator
{
    public static int CalculateDamage(int baseDamage, int targetDefense, float defensePenetration = 0f)
    {
        float effectiveDefense = targetDefense * (1f - defensePenetration);
        int actualDamage = Mathf.Max(0, baseDamage - Mathf.RoundToInt(effectiveDefense));
        return actualDamage;
    }

    public static int CalculateShieldBreak(int shieldAmount, int damage)
    {
        int remainingDamage = damage;
        if (shieldAmount >= damage)
        {
            return 0;
        }
        return damage - shieldAmount;
    }

    public static float CalculateCritChance(int baseCritChance, float critBonus = 0f)
    {
        return Mathf.Clamp((baseCritChance + critBonus) / 100f, 0f, 1f);
    }

    public static int CalculateCriticalDamage(int baseDamage, float critMultiplier = 1.5f)
    {
        return Mathf.RoundToInt(baseDamage * critMultiplier);
    }

    public static int CalculateDamageWithVariance(int baseDamage, float variancePercent = 0.1f)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        float variance = 1f + rng.RandfRange(-variancePercent, variancePercent);
        return Mathf.RoundToInt(baseDamage * variance);
    }

    public static float CalculateThreatLevel(int enemyAttack, int enemyHealth, int playerHealth, int playerShield)
    {
        float healthRatio = (float)enemyHealth / Mathf.Max(playerHealth, 1);
        float shieldRatio = playerShield > 0 ? 0.5f : 1f;
        return enemyAttack * healthRatio * shieldRatio;
    }

    public static bool ShouldAttackShield(int playerShield, int enemyAttack, int breakThreshold = 5)
    {
        if (playerShield <= 0) return true;
        return enemyAttack >= playerShield - breakThreshold;
    }
}
