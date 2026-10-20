using Godot;
using FishEatFish.Battle;

/// <summary>
/// 结算界面里的「唤醒体同调率提升」槽位：角色名 + 提升数值
/// </summary>
public partial class BattleSyncSlot : VBoxContainer
{
    private Label _nameLabel;
    private Label _valueLabel;

    public override void _Ready()
    {
        GD.Print("[BattleSyncSlot] _Ready called");

        _nameLabel = GetNodeOrNull<Label>("NameLabel");
        _valueLabel = GetNodeOrNull<Label>("ValueLabel");

        if (_nameLabel == null)
        {
            GD.PrintErr("[BattleSyncSlot] 未找到 NameLabel 节点");
        }

        if (_valueLabel == null)
        {
            GD.PrintErr("[BattleSyncSlot] 未找到 ValueLabel 节点");
        }

        GD.Print("[BattleSyncSlot] _Ready completed");
    }

    /// <summary>
    /// 设置该槽位显示的角色同调率数据
    /// </summary>
    public void SetEntry(CharacterSyncEntry entry)
    {
        if (entry == null)
        {
            GD.PrintErr("[BattleSyncSlot] SetEntry called with null entry");
            return;
        }

        if (_nameLabel != null)
        {
            _nameLabel.Text = entry.CharacterName;
        }

        if (_valueLabel != null)
        {
            _valueLabel.Text = $"+{entry.SyncGain}";
        }

        GD.Print($"[BattleSyncSlot] SetEntry called: {entry.CharacterName} +{entry.SyncGain}");
        GD.Print($"[BattleSyncSlot] SetEntry completed");
    }
}
