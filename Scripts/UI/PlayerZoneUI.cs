using Godot;

public partial class PlayerZoneUI : Control
{
    private ProgressBar healthBar;
    private Label healthLabel;
    private Label shieldLabel;
    private Label energyLabel;

    private Player playerData;

    public override void _Ready()
    {
        healthBar = GetNode<ProgressBar>("../VBoxContainer/HealthBar");
        healthLabel = GetNode<Label>("../VBoxContainer/HealthLabel");
        shieldLabel = GetNode<Label>("../VBoxContainer/ShieldLabel");
        energyLabel = GetNode<Label>("../VBoxContainer/EnergyLabel");

        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        shieldLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 0.5f));
        energyLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1f));
    }

    public void Setup(Player player)
    {
        playerData = player;
        UpdateStats();
    }

    public void UpdateStats()
    {
        if (playerData == null) return;

        healthBar.MaxValue = playerData.MaxHealth;
        healthBar.Value = playerData.CurrentHealth;
        healthLabel.Text = $"{playerData.CurrentHealth}/{playerData.MaxHealth}";
        shieldLabel.Text = $"护盾: {playerData.Shield}";
        energyLabel.Text = $"能量: {playerData.CurrentEnergy}/{playerData.MaxEnergy}";
    }

    public void UpdateHealth()
    {
        if (playerData == null) return;

        healthBar.MaxValue = playerData.MaxHealth;
        healthBar.Value = playerData.CurrentHealth;
        healthLabel.Text = $"{playerData.CurrentHealth}/{playerData.MaxHealth}";
    }

    public void UpdateShield()
    {
        if (playerData == null) return;

        shieldLabel.Text = $"护盾: {playerData.Shield}";
    }

    public void UpdateEnergy()
    {
        if (playerData == null) return;

        energyLabel.Text = $"能量: {playerData.CurrentEnergy}/{playerData.MaxEnergy}";
    }
}