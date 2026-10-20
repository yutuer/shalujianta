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

    /// <summary>意图图标与颜色的单一数据源（避免与 EnemyIntentDisplay 重复实现）</summary>
    private readonly EnemyIntentDisplay _intentDisplay = new EnemyIntentDisplay();

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


        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));

        ApplyPendingSetup();
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
        // 初始不写死「攻击」——真实意图由 BattleManager.UpdateEnemyUI 用 AI 预测填入
        if (intentLabel != null) intentLabel.Text = "?";
        if (intentIcon != null) intentIcon.Texture = null;
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
        return _intentDisplay.GetIconForAction(actionType);
    }

    private Color GetColorForAction(AIActionType actionType)
    {
        return _intentDisplay.GetColorForAction(actionType);
    }

    public Enemy GetEnemyData()
    {
        return enemyData;
    }
}
