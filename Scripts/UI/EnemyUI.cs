using Godot;

public partial class EnemyUI : Panel
{
    private Label nameLabel;
    private TextureRect enemySprite;
    private ProgressBar healthBar;
    private Label healthLabel;
    private Label intentLabel;

    private Enemy enemyData;
    private Enemy pendingEnemy;
    private Texture2D pendingTexture;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        enemySprite = GetNode<TextureRect>("VBoxContainer/EnemySprite");
        healthBar = GetNode<ProgressBar>("VBoxContainer/HealthBar");
        healthLabel = GetNode<Label>("VBoxContainer/HealthLabel");
        intentLabel = GetNode<Label>("VBoxContainer/IntentLabel");

        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        intentLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));

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
        intentLabel.Text = $"意图: 攻击 {enemy.Attack}";
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

    public void UpdateIntent(string intent, int value)
    {
        if (intentLabel != null)
        {
            intentLabel.Text = $"意图: {intent} {value}";
        }
    }

    public Enemy GetEnemyData()
    {
        return enemyData;
    }
}
