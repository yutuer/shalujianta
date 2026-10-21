using Godot;
using FishEatFish.Battle.Core;

public partial class PlayerStatsUI : HBoxContainer
{
    private Label healthLabel;
    private Label energyLabel;
    private Label shieldLabel;
    private Label turnLabel;

    private Player playerData;
    private Player pendingPlayer;
    private int pendingTurn;

    public override void _Ready()
    {
        healthLabel = GetNode<Label>("HealthLabel");
        energyLabel = GetNode<Label>("EnergyLabel");
        shieldLabel = GetNode<Label>("ShieldLabel");
        turnLabel = GetNode<Label>("TurnLabel");

        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        energyLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1f));
        shieldLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 0.5f));

        // 能量（当前算力）挪到**左下角玩家区**显示（对照参考图 R-08 的左下角），
        // 顶部这里只留生命 / 护盾 / 回合，避免同一信息出现两遍。
        energyLabel.Visible = false;

        ApplyPendingSetup();
    }

    private void ApplyPendingSetup()
    {
        if (pendingPlayer != null)
        {
            ApplyStats(pendingTurn);
        }
    }

    public void Setup(Player player, int turn)
    {
        playerData = player;
        pendingPlayer = player;
        pendingTurn = turn;

        if (IsInsideTree())
        {
            ApplyStats(turn);
        }
    }

    private void ApplyStats(int turn)
    {
        healthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
        energyLabel.Text = playerData.CurrentEnergy.ToString();
        shieldLabel.Text = $"护盾: {playerData.Shield}";
        turnLabel.Text = $"回合: {turn}";
    }

    public void UpdateStats(int turn)
    {
        if (playerData == null) return;

        healthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
        energyLabel.Text = playerData.CurrentEnergy.ToString();
        shieldLabel.Text = $"护盾: {playerData.Shield}";
        turnLabel.Text = $"回合: {turn}";
    }

    public void SetTurn(int turn)
    {
        if (turnLabel != null)
        {
            turnLabel.Text = $"回合: {turn}";
        }
    }
}
