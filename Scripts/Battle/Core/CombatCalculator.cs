using Godot;

namespace FishEatFish.Battle.Core;

public static class CombatCalculator
{
	public static int CalculateDamage(int baseDamage, int defense)
	{
		int damageAfterDefense = baseDamage - defense;
		return Mathf.Max(1, damageAfterDefense);
	}

	public static int CalculateDamageWithVariance(int baseDamage, float variancePercent = 0.1f)
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Randomize();

		float variance = baseDamage * variancePercent;
		int minDamage = Mathf.RoundToInt(baseDamage - variance);
		int maxDamage = Mathf.RoundToInt(baseDamage + variance);

		return rng.RandiRange(minDamage, maxDamage);
	}

	public static bool ShouldAttackShield(int damage, int shield)
	{
		return shield > 0 && damage > shield * 0.5f;
	}

	public static int CalculateThreatLevel(int health, int attack, int shield, int damageTaken)
	{
		int threat = 0;

		if (health < 30)
			threat += 30;
		else if (health < 60)
			threat += 15;

		if (attack > 20)
			threat += 25;
		else if (attack > 10)
			threat += 15;

		if (shield > 10)
			threat += 10;

		if (damageTaken > 20)
			threat += 15;

		return threat;
	}
}
