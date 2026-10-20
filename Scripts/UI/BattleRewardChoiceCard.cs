using Godot;
using FishEatFish.Battle;
using FishEatFish.Battle.EngravingSystem;

namespace FishEatFish.UI;

/// <summary>
/// 战斗奖励三选一的**单个选项卡**（对照参考图 R-10 / R-11）。
///
/// 两种奖励共用这一个卡：
///   - 造物（Artifact）：图标 + 名称 + 描述
///   - 刻印（Engraving）：图标 + 名称 + 描述
///
/// 布局在 <c>Scenes/UI/BattleRewardChoiceCard.tscn</c>，本类只管数据与交互。
/// </summary>
public partial class BattleRewardChoiceCard : Panel
{
    private TextureRect _iconRect;
    private Label _nameLabel;
    private Label _descLabel;
    private PanelContainer _frame;

    private bool _selected = false;

    /// <summary>点击回调（参数是本卡对应的奖励数据）</summary>
    public System.Action<BattleRewardChoiceCard> OnClicked;

    public override void _Ready()
    {
        GD.Print("[BattleRewardChoiceCard] _Ready called");

        _frame = GetNodeOrNull<PanelContainer>("Frame");
        _iconRect = GetNodeOrNull<TextureRect>("Frame/IconRect");
        _nameLabel = GetNodeOrNull<Label>("Frame/NameLabel");
        _descLabel = GetNodeOrNull<Label>("Frame/DescLabel");

        if (_iconRect == null) GD.PrintErr("[BattleRewardChoiceCard] 未找到 Frame/IconRect 节点");
        if (_nameLabel == null) GD.PrintErr("[BattleRewardChoiceCard] 未找到 Frame/NameLabel 节点");
        if (_descLabel == null) GD.PrintErr("[BattleRewardChoiceCard] 未找到 Frame/DescLabel 节点");

        GD.Print("[BattleRewardChoiceCard] _Ready completed");
    }

    /// <summary>
    /// 设置奖励数据并刷新显示。
    /// </summary>
    public void SetChoice(BattleRewardChoice choice)
    {
        if (choice == null)
        {
            GD.PrintErr("[BattleRewardChoiceCard] SetChoice 收到空奖励");
            return;
        }

        Choice = choice;

        if (_nameLabel != null)
        {
            _nameLabel.Text = choice.Name;
        }

        if (_descLabel != null)
        {
            _descLabel.Text = choice.Description;
        }

        if (_iconRect != null)
        {
            if (!string.IsNullOrEmpty(choice.IconPath) && ResourceLoader.Exists(choice.IconPath))
            {
                Texture2D texture = ResourceLoader.Load<Texture2D>(choice.IconPath);
                _iconRect.Texture = texture;
                _iconRect.Visible = texture != null;
            }
            else
            {
                // 图标资源不存在时只记警告，不显示占位图（避免误导玩家）
                if (!string.IsNullOrEmpty(choice.IconPath))
                {
                    GD.PrintErr($"[BattleRewardChoiceCard] 奖励图标不存在: {choice.IconPath}");
                }
                _iconRect.Visible = false;
            }
        }

        SetSelected(false);
        GD.Print($"[BattleRewardChoiceCard] SetChoice called: {choice.Name}（{choice.Kind}）");
    }

    /// <summary>本卡对应的奖励数据</summary>
    public BattleRewardChoice Choice { get; private set; }

    /// <summary>设置选中状态（选中时边框高亮）</summary>
    public void SetSelected(bool selected)
    {
        _selected = selected;

        if (_frame == null) return;

        StyleBoxFlat style = new StyleBoxFlat
        {
            BgColor = selected ? new Color(0.22f, 0.26f, 0.34f) : new Color(0.12f, 0.13f, 0.18f),
            BorderColor = selected ? new Color(1f, 0.85f, 0.35f) : new Color(0.35f, 0.37f, 0.45f),
            BorderWidthLeft = selected ? 3 : 2,
            BorderWidthTop = selected ? 3 : 2,
            BorderWidthRight = selected ? 3 : 2,
            BorderWidthBottom = selected ? 3 : 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10
        };

        _frame.AddThemeStyleboxOverride("panel", style);
    }

    public bool IsSelected() => _selected;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                GD.Print("[BattleRewardChoiceCard] _GuiInput called: 点击选项卡");
                OnClicked?.Invoke(this);
            }
        }
    }
}
