using Godot;
using FishEatFish.Battle.Core;
using FishEatFish.AI;

public partial class EnemyUI : Panel
{
    private Label nameLabel;
    private TextureRect enemySprite;
    private ProgressBar healthBar;
    private Label healthLabel;
    private Label intentLabel;
    private TextureRect intentIcon;

    private Enemy enemyData;
    private Enemy pendingEnemy;
    private Texture2D pendingTexture;

    private Texture2D attackIcon;
    private Texture2D defendIcon;
    private Texture2D buffIcon;
    private Texture2D healIcon;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        enemySprite = GetNode<TextureRect>("VBoxContainer/EnemySprite");
        healthBar = GetNode<ProgressBar>("VBoxContainer/HealthBar");
        healthLabel = GetNode<Label>("VBoxContainer/HealthLabel");
        intentLabel = GetNode<Label>("VBoxContainer/IntentLabel");

        if (HasNode("VBoxContainer/IntentIcon"))
        {
            intentIcon = GetNode<TextureRect>("VBoxContainer/IntentIcon");
        }

        LoadIcons();

        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));

        ApplyPendingSetup();
    }

    private void LoadIcons()
    {
        attackIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_attack.svg");
        defendIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_defend.svg");
        buffIcon = ResourceLoader.Load<Texture2D>("res://Assets/Icons/intent_buff.svg");
    }

    private void ApplyPendingSetup()
    {
        if (pendingEnemy != null)
        {
            ApplySetup(pendingEnemy);
            if (pendingTexture != null && enemySprite != null)
            {
                enemySprite.Texture = pendingTexture;
            }
        }
    }

    public void Setup(Enemy enemy)
    {
        enemyData = enemy;
        pendingEnemy = enemy;

        if (IsInsideTree())
        {
            ApplySetup(enemy);
        }
    }

    private void ApplySetup(Enemy enemy)
    {
        Name = $"Enemy_{enemy.EnemyName}";
        nameLabel.Text = enemy.EnemyName;
        healthBar.MaxValue = enemy.MaxHealth;
        healthBar.Value = enemy.CurrentHealth;
        healthLabel.Text = $"{enemy.CurrentHealth}/{enemy.MaxHealth}";
        UpdateIntentDisplay(AIActionType.Attack, enemy.Attack);
    }

    public void SetSprite(Texture2D texture)
    {
        pendingTexture = texture;
        if (enemySprite != null && texture != null)
        {
            enemySprite.Texture = texture;
        }
    }

    public void UpdateHealth()
    {
        if (enemyData == null) return;

        healthBar.MaxValue = enemyData.MaxHealth;
        healthBar.Value = enemyData.CurrentHealth;
        healthLabel.Text = $"{enemyData.CurrentHealth}/{enemyData.MaxHealth}";

        if (enemyData.CurrentHealth <= 0)
        {
            healthLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    public void UpdateIntent(AIActionType actionType, int value)
    {
        UpdateIntentDisplay(actionType, value);
    }

    private void UpdateIntentDisplay(AIActionType actionType, int value)
    {
        if (intentLabel == null) return;

        Texture2D icon = GetIconForAction(actionType);
        if (intentIcon != null && icon != null)
        {
            intentIcon.Texture = icon;
        }

        Color intentColor = GetColorForAction(actionType);
        intentLabel.AddThemeColorOverride("font_color", intentColor);

        string intentText = actionType switch
        {
            AIActionType.Attack => $"攻击 {value}",
            AIActionType.Defend => $"防御 +{value}",
            AIActionType.Buff => $"强化 +{value}",
            AIActionType.Heal => $"治疗 +{value}",
            _ => $"攻击 {value}"
        };

        intentLabel.Text = intentText;
    }

    private Texture2D GetIconForAction(AIActionType actionType)
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

    private Color GetColorForAction(AIActionType actionType)
    {
        return actionType switch
        {
            AIActionType.Attack => new Color(1f, 0.3f, 0.3f),
            AIActionType.Defend => new Color(0.3f, 0.6f, 1f),
            AIActionType.Buff => new Color(1f, 0.8f, 0.2f),
            AIActionType.Heal => new Color(0.3f, 1f, 0.5f),
            _ => new Color(1f, 0.5f, 0.5f)
        };
    }

    public Enemy GetEnemyData()
    {
        return enemyData;
    }
}
