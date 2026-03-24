using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

public class DefensiveAI : IEnemyAI
{
    public AIAction ChooseAction(Enemy enemy, Player player, List<Enemy> allEnemies)
    {
        if (enemy.Defense < enemy.Attack)
        {
            float defendPriority = CalculateActionPriority(enemy, player, AIActionType.Defend);
            float buffPriority = CalculateActionPriority(enemy, player, AIActionType.Buff);

            if (defendPriority >= buffPriority && enemy.Defense < enemy.MaxHealth / 3)
            {
                return new AIAction(AIActionType.Defend, Mathf.Min(8, enemy.MaxHealth / 5), -1, defendPriority);
            }

            if (buffPriority > defendPriority)
            {
                return new AIAction(AIActionType.Buff, 3, -1, buffPriority);
            }
        }

        if (enemy.CurrentHealth < enemy.MaxHealth * 0.4f)
        {
            float healPriority = CalculateActionPriority(enemy, player, AIActionType.Heal);
            return new AIAction(AIActionType.Heal, Mathf.Min(15, enemy.MaxHealth - enemy.CurrentHealth), -1, healPriority);
        }

        float attackPriority = CalculateActionPriority(enemy, player, AIActionType.Attack);
        float currentDefendPriority = CalculateActionPriority(enemy, player, AIActionType.Defend);

        if (currentDefendPriority > attackPriority && enemy.Defense < enemy.Attack * 2)
        {
            return new AIAction(AIActionType.Defend, Mathf.Min(6, enemy.MaxHealth / 6), -1, currentDefendPriority);
        }

        return new AIAction(AIActionType.Attack, enemy.Attack, -1, attackPriority);
    }

    public float CalculateActionPriority(Enemy enemy, Player player, AIActionType actionType)
    {
        switch (actionType)
        {
            case AIActionType.Attack:
                if (enemy.Defense < enemy.Attack * 1.5f)
                {
                    return 30f;
                }
                if (CombatCalculator.ShouldAttackShield(player.Shield, enemy.Attack))
                {
                    return 70f;
                }
                return 50f;

            case AIActionType.Defend:
                float baseDefendPriority = 60f;
                if (enemy.Defense >= enemy.Attack)
                {
                    return 30f;
                }
                float defenseGap = (enemy.Attack * 1.5f) - enemy.Defense;
                return baseDefendPriority + defenseGap;

            case AIActionType.Buff:
                if (enemy.Defense >= enemy.Attack)
                {
                    return 80f;
                }
                return 40f;

            case AIActionType.Heal:
                if (enemy.CurrentHealth < enemy.MaxHealth * 0.4f)
                {
                    return 90f;
                }
                if (enemy.CurrentHealth < enemy.MaxHealth * 0.6f)
                {
                    return 50f;
                }
                return 0f;

            default:
                return 0f;
        }
    }
}
