using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

public class AggressiveAI : IEnemyAI
{
    public AIAction ChooseAction(Enemy enemy, Player player, List<Enemy> allEnemies)
    {
        if (player.CurrentHealth <= enemy.Attack && CombatCalculator.ShouldAttackShield(player.Shield, enemy.Attack))
        {
            return new AIAction(AIActionType.Attack, enemy.Attack, -1, 100f);
        }

        if (enemy.CurrentHealth < enemy.MaxHealth * 0.3f && enemy.Attack < player.CurrentHealth)
        {
            float healPriority = CalculateActionPriority(enemy, player, AIActionType.Heal);
            return new AIAction(AIActionType.Heal, Mathf.Min(10, enemy.MaxHealth - enemy.CurrentHealth), -1, healPriority);
        }

        float attackPriority = CalculateActionPriority(enemy, player, AIActionType.Attack);
        float defendPriority = CalculateActionPriority(enemy, player, AIActionType.Defend);

        if (attackPriority >= defendPriority)
        {
            return new AIAction(AIActionType.Attack, enemy.Attack, -1, attackPriority);
        }

        return new AIAction(AIActionType.Defend, Mathf.Min(5, enemy.MaxHealth / 4), -1, defendPriority);
    }

    public float CalculateActionPriority(Enemy enemy, Player player, AIActionType actionType)
    {
        switch (actionType)
        {
            case AIActionType.Attack:
                float baseAttackPriority = 80f;
                float healthThreat = CombatCalculator.CalculateThreatLevel(enemy.Attack, enemy.CurrentHealth, player.CurrentHealth, player.Shield);
                if (player.Shield > 0 && enemy.Attack >= player.Shield)
                {
                    baseAttackPriority += 20f;
                }
                if (enemy.CurrentHealth < enemy.MaxHealth * 0.3f)
                {
                    baseAttackPriority -= 30f;
                }
                return baseAttackPriority + healthThreat;

            case AIActionType.Defend:
                if (enemy.Defense > enemy.Attack)
                {
                    return 20f;
                }
                return 40f + (enemy.MaxHealth - enemy.CurrentHealth) / 10f;

            case AIActionType.Heal:
                if (enemy.CurrentHealth < enemy.MaxHealth * 0.5f)
                {
                    return 60f + (enemy.MaxHealth - enemy.CurrentHealth) / 5f;
                }
                return 0f;

            default:
                return 0f;
        }
    }
}
