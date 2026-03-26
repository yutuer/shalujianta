using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.AI;

public class EnemyIntentDisplay
{
    private Texture2D attackIcon;
    private Texture2D defendIcon;
    private Texture2D buffIcon;
    private Texture2D healIcon;

    public EnemyIntentDisplay()
    {
        attackIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_attack.svg");
        defendIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_defend.svg");
        buffIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_buff.svg");
    }

    public AIAction PredictEnemyAction(Enemy enemy, Player player, List<Enemy> allEnemies)
    {
        return enemy.GetQueuedAction(player, allEnemies);
    }

    public Texture2D GetIconForAction(AIActionType actionType)
    {
        return actionType switch
        {
            AIActionType.Attack => attackIcon,
            AIActionType.Defend => defendIcon,
            AIActionType.Buff => buffIcon,
            AIActionType.Heal => healIcon,
            _ => attackIcon
        };
    }

    public Color GetColorForAction(AIActionType actionType)
    {
        return actionType switch
        {
            AIActionType.Attack => new Color(1f, 0.3f, 0.3f),
            AIActionType.Defend => new Color(0.3f, 0.6f, 1f),
            AIActionType.Buff => new Color(1f, 0.8f, 0.2f),
            AIActionType.Heal => new Color(0.3f, 1f, 0.5f),
            _ => Colors.White
        };
    }

    public string GetIntentDescription(AIAction action)
    {
        if (action == null) return "";

        return action.Type switch
        {
            AIActionType.Attack => $"{action.Value}",
            AIActionType.Defend => $"{action.Value}",
            AIActionType.Buff => $"+{action.Value}",
            AIActionType.Heal => $"+{action.Value}",
            _ => ""
        };
    }
}
