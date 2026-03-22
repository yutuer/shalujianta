using Godot;
using System.Collections.Generic;

public enum AIActionType
{
    Attack,
    Defend,
    Buff,
    Heal
}

public class AIAction
{
    public AIActionType Type { get; set; }
    public int TargetPosition { get; set; } = -1;
    public int Value { get; set; }
    public float Priority { get; set; }

    public AIAction(AIActionType type, int value = 0, int targetPosition = -1, float priority = 0f)
    {
        Type = type;
        Value = value;
        TargetPosition = targetPosition;
        Priority = priority;
    }
}

public interface IEnemyAI
{
    AIAction ChooseAction(Enemy enemy, Player player, List<Enemy> allEnemies);
    float CalculateActionPriority(Enemy enemy, Player player, AIActionType actionType);
}
