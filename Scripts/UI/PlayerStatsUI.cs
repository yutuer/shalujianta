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
        // 顶部这里只留生命 / 回合，避免同一信息出现两遍。
        energyLabel.Visible = false;

        // 护盾（盾量）也挪到**玩家血条末端**的盾牌徽标上了（对照参考图 R-08），
        // 顶部不再重复显示。
        shieldLabel.Visible = false;

        // 血量（生命）也挪到**左下角玩家血条上**了（D30 起血量数字写在条里），
        // 顶部再显示一份就是重复信息 → 去掉。
        healthLabel.Visible = false;

        // 顶栏现在只剩「回合」了 —— 那几个空格分隔符（原来用来隔开各项）就都成了多余的留白，
        // 会把「回合」往左推 → 一起隐藏。
        foreach (string spacerPath in new[] { "Spacer1", "Spacer2", "Spacer3" })
        {
            Label spacer = GetNodeOrNull<Label>(spacerPath);
            if (spacer != null) spacer.Visible = false;
        }

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
