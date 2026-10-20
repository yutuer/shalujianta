using Godot;
using FishEatFish.Battle;

/// <summary>
/// 战斗胜利后的「调查完成」结算界面（对照参考图 R-09）
///
/// 流程：BattleManager 胜利 → 组装 BattleResultData → BattleTransitionManager 切到本场景
///       玩家点「完成调查」→ BattleTransitionManager.CompleteBattleResult() → 回地图
/// </summary>
public partial class BattleResultUI : Control
{
    /// <summary>同调率槽位场景（BattleSyncSlot.tscn）</summary>
    [Export]
    public PackedScene SyncSlotScene;

    /// <summary>奖励槽位场景（BattleRewardSlot.tscn）</summary>
    [Export]
    public PackedScene RewardSlotScene;

    private Label _mapLabel;
    private Label _titleLabel;
    private Label _starLabel;
    private Label _starDescLabel;
    private HBoxContainer _syncContainer;
    private HBoxContainer _rewardContainer;
    private Button _confirmButton;

    public override void _Ready()
    {
        GD.Print("[BattleResultUI] _Ready called");

        _mapLabel = GetNodeOrNull<Label>("Margin/VBox/MapLabel");
        _titleLabel = GetNodeOrNull<Label>("Margin/VBox/TitleLabel");
        _starLabel = GetNodeOrNull<Label>("Margin/VBox/StarLabel");
        _starDescLabel = GetNodeOrNull<Label>("Margin/VBox/StarDescLabel");
        _syncContainer = GetNodeOrNull<HBoxContainer>("Margin/VBox/SyncContainer");
        _rewardContainer = GetNodeOrNull<HBoxContainer>("Margin/VBox/RewardContainer");
        _confirmButton = GetNodeOrNull<Button>("Margin/VBox/ButtonRow/ConfirmButton");

        if (_confirmButton != null)
        {
            _confirmButton.Pressed += OnConfirmPressed;
        }
        else
        {
            GD.PrintErr("[BattleResultUI] 未找到 Margin/VBox/ButtonRow/ConfirmButton 节点");
        }

        BindData();

        GD.Print("[BattleResultUI] _Ready completed");
    }

    /// <summary>
    /// 从 BattleTransitionManager 取结算数据并刷新界面
    /// </summary>
    private void BindData()
    {
        BattleTransitionManager manager = BattleTransitionManager.Instance;
        BattleResultData data = manager?.PendingResult;

        if (data == null)
        {
            GD.PrintErr("[BattleResultUI] 没有结算数据，显示空白结算界面");
            return;
        }

        GD.Print($"[BattleResultUI] BindData called: map={data.MapId}, turns={data.TurnCount}, stars={data.StarCount}");

        if (_mapLabel != null)
        {
            _mapLabel.Text = string.IsNullOrEmpty(data.MapName)
                ? data.MapId
                : $"{data.MapId} {data.MapName}";
        }

        if (_titleLabel != null)
        {
            _titleLabel.Text = "调查完成";
        }

        if (_starLabel != null)
        {
            _starLabel.Text = BattleResultRules.GetStarText(data.StarCount);
        }

        if (_starDescLabel != null)
        {
            _starDescLabel.Text = data.StarDescription;
        }

        FillSyncSlots(data);
        FillRewardSlots(data);

        GD.Print($"[BattleResultUI] BindData completed: 同调率 {data.SyncEntries.Count} 项，奖励 {data.Rewards.Count} 项");
    }

    /// <summary>
    /// 填充同调率槽位
    /// </summary>
    private void FillSyncSlots(BattleResultData data)
    {
        if (_syncContainer == null || SyncSlotScene == null) return;

        foreach (Node child in _syncContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (CharacterSyncEntry entry in data.SyncEntries)
        {
            BattleSyncSlot slot = SyncSlotScene.Instantiate<BattleSyncSlot>();
            if (slot == null) continue;
            _syncContainer.AddChild(slot);
            slot.SetEntry(entry);
        }
    }

    /// <summary>
    /// 填充奖励槽位
    /// </summary>
    private void FillRewardSlots(BattleResultData data)
    {
        if (_rewardContainer == null || RewardSlotScene == null) return;

        foreach (Node child in _rewardContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (BattleRewardEntry entry in data.Rewards)
        {
            BattleRewardSlot slot = RewardSlotScene.Instantiate<BattleRewardSlot>();
            if (slot == null) continue;
            _rewardContainer.AddChild(slot);
            slot.SetEntry(entry);
        }
    }

    /// <summary>
    /// 点击「完成调查」：回地图
    /// </summary>
    private void OnConfirmPressed()
    {
        GD.Print("[BattleResultUI] OnConfirmPressed called");
        BattleTransitionManager.Instance?.CompleteBattleResult();
        GD.Print("[BattleResultUI] OnConfirmPressed completed");
    }
}
