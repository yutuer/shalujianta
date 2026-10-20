using Godot;
using FishEatFish.Battle;

/// <summary>
/// 结算界面里的「调查奖励」槽位：道具图标 + 数量
/// </summary>
public partial class BattleRewardSlot : VBoxContainer
{
    private TextureRect _iconRect;
    private Label _amountLabel;
    private Label _nameLabel;

    public override void _Ready()
    {
        GD.Print("[BattleRewardSlot] _Ready called");

        _iconRect = GetNodeOrNull<TextureRect>("IconRect");
        _amountLabel = GetNodeOrNull<Label>("AmountLabel");
        _nameLabel = GetNodeOrNull<Label>("NameLabel");

        if (_iconRect == null)
        {
            GD.PrintErr("[BattleRewardSlot] 未找到 IconRect 节点");
        }

        if (_amountLabel == null)
        {
            GD.PrintErr("[BattleRewardSlot] 未找到 AmountLabel 节点");
        }

        GD.Print("[BattleRewardSlot] _Ready completed");
    }

    /// <summary>
    /// 设置该槽位显示的奖励数据
    /// </summary>
    public void SetEntry(BattleRewardEntry entry)
    {
        if (entry == null)
        {
            GD.PrintErr("[BattleRewardSlot] SetEntry called with null entry");
            return;
        }

        if (_nameLabel != null)
        {
            _nameLabel.Text = entry.RewardName;
        }

        if (_amountLabel != null)
        {
            _amountLabel.Text = entry.Amount.ToString();
        }

        if (_iconRect != null)
        {
            if (!string.IsNullOrEmpty(entry.IconPath) && ResourceLoader.Exists(entry.IconPath))
            {
                Texture2D texture = ResourceLoader.Load<Texture2D>(entry.IconPath);
                if (texture != null)
                {
                    _iconRect.Texture = texture;
                    _iconRect.Visible = true;
                }
            }
            else
            {
                // 没有图标的奖励（比如金币）就留空，不显示占位图
                _iconRect.Visible = false;
            }
        }

        GD.Print($"[BattleRewardSlot] SetEntry called: {entry.RewardName} x{entry.Amount}");
        GD.Print($"[BattleRewardSlot] SetEntry completed");
    }
}
