using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

public class BalancedAI : IEnemyAI
{
    public AIAction ChooseAction(Enemy enemy, Player player, List<Enemy> allEnemies)
    {
        float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
        float playerHealthPercent = (float)player.CurrentHealth / Mathf.Max(player.MaxHealth, 1);

        if (healthPercent < 0.3f)
        {
            float healPriority = CalculateActionPriority(enemy, player, AIActionType.Heal);
            if (healPriority > 60f)
            {
                return new AIAction(AIActionType.Heal, Mathf.Min(12, enemy.MaxHealth - enemy.CurrentHealth), -1, healPriority);
            }
        }

        if (healthPercent < 0.5f && enemy.Defense < enemy.Attack)
        {
            float defendPriority = CalculateActionPriority(enemy, player, AIActionType.Defend);
            float attackPriority = CalculateActionPriority(enemy, player, AIActionType.Attack);

            if (defendPriority > attackPriority * 0.8f)
            {
                return new AIAction(AIActionType.Defend, Mathf.Min(7, enemy.MaxHealth / 6), -1, defendPriority);
            }
        }

        if (player.Shield > 0 && enemy.Attack >= player.Shield && healthPercent > 0.4f)
        {
            float attackPriority = CalculateActionPriority(enemy, player, AIActionType.Attack);
            return new AIAction(AIActionType.Attack, enemy.Attack, -1, attackPriority + 15f);
        }

        if (enemy.Defense < enemy.Attack && healthPercent > 0.6f)
        {
            float buffPriority = CalculateActionPriority(enemy, player, AIActionType.Buff);
            if (buffPriority > 55f)
            {
                return new AIAction(AIActionType.Buff, 2, -1, buffPriority);
            }
        }

        float finalAttackPriority = CalculateActionPriority(enemy, player, AIActionType.Attack);
        float finalDefendPriority = CalculateActionPriority(enemy, player, AIActionType.Defend);

        if (finalDefendPriority > finalAttackPriority * 1.2f && enemy.Defense < enemy.Attack * 1.5f)
        {
            return new AIAction(AIActionType.Defend, Mathf.Min(5, enemy.MaxHealth / 8), -1, finalDefendPriority);
        }

        return new AIAction(AIActionType.Attack, enemy.Attack, -1, finalAttackPriority);
    }

    public float CalculateActionPriority(Enemy enemy, Player player, AIActionType actionType)
    {
        float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
        float playerHealthPercent = (float)player.CurrentHealth / Mathf.Max(player.MaxHealth, 1);

        switch (actionType)
        {
            case AIActionType.Attack:
                float baseAttackPriority = 50f;

                if (player.Shield > 0 && enemy.Attack >= player.Shield)
                {
                    baseAttackPriority += 25f;
                }
                else if (player.Shield > 0)
                {
                    baseAttackPriority -= 10f;
                }

                if (playerHealthPercent < 0.3f)
                {
                    baseAttackPriority += 30f;
                }
                else if (playerHealthPercent < 0.5f)
                {
                    baseAttackPriority += 15f;
                }

                if (healthPercent < 0.3f)
                {
                    baseAttackPriority -= 25f;
                }

                return baseAttackPriority;

            case AIActionType.Defend:
                float baseDefendPriority = 40f;

                if (enemy.Defense >= enemy.Attack * 1.5f)
                {
                    return 20f;
                }

                if (healthPercent < 0.5f)
                {
                    baseDefendPriority += 20f;
                }

                if (enemy.Defense < enemy.Attack)
                {
                    baseDefendPriority += 15f;
                }

                return baseDefendPriority;

            case AIActionType.Buff:
                if (enemy.Defense >= enemy.Attack)
                {
                    return 35f;
                }
                if (healthPercent > 0.6f && enemy.Defense < enemy.Attack)
                {
                    return 50f;
                }
                return 25f;

            case AIActionType.Heal:
                if (healthPercent < 0.3f)
                {
                    return 80f;
                }
                if (healthPercent < 0.5f)
                {
                    return 50f;
                }
                return 0f;

            default:
                return 0f;
        }
    }
}
