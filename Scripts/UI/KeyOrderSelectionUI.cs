using Godot;
using System.Collections.Generic;

/// <summary>
/// 钥令选择UI组件
/// 功能：展示随机钥令选项供玩家选择，支持点击选择和取消操作
/// 触发条件：银钥满1000且钥令次数未用完时
/// </summary>
public partial class KeyOrderSelectionUI : Control
{
    private PanelContainer _backgroundPanel;
    private VBoxContainer _mainContainer;
    private Label _titleLabel;
    private HBoxContainer _keyOrderSlotsContainer;
    private List<KeyOrderSlotUI> _keyOrderSlots = new List<KeyOrderSlotUI>();
    private Button _cancelButton;
    private Label _instructionsLabel;

    private bool _isVisible = false;
    private System.Action<KeyOrder> _onKeyOrderSelected;
    private System.Action _onCancelled;

    public override void _Ready()
    {
        SetupUI();
        Visible = false;
        GD.Print("[KeyOrderSelectionUI] 钥令选择UI初始化完成");
    }

    private void SetupUI()
    {
        _backgroundPanel = GetNode<PanelContainer>("BackgroundPanel");
        _mainContainer = GetNode<VBoxContainer>("BackgroundPanel/VBoxContainer");
        _titleLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
        _instructionsLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/InstructionsLabel");
        _cancelButton = GetNode<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/CancelButton");

        if (_cancelButton != null)
        {
            _cancelButton.Pressed += OnCancelPressed;
        }

        _keyOrderSlotsContainer = GetNode<HBoxContainer>("BackgroundPanel/VBoxContainer/KeyOrderSlotsContainer");

        if (_keyOrderSlotsContainer != null)
        {
            foreach (Node child in _keyOrderSlotsContainer.GetChildren())
            {
                KeyOrderSlotUI slot = child as KeyOrderSlotUI;
                if (slot != null)
                {
                    _keyOrderSlots.Add(slot);
                }
            }
        }
    }

    /// <summary>
    /// 显示钥令选择界面
    /// </summary>
    /// <param name="keyOrders">可选择的钥令列表</param>
    /// <param name="onSelected">选择回调</param>
    /// <param name="onCancelled">取消回调（可选）</param>
    public void ShowSelection(List<KeyOrder> keyOrders, System.Action<KeyOrder> onSelected, System.Action onCancelled = null)
    {
        if (keyOrders == null || keyOrders.Count == 0)
        {
            GD.PrintErr("[KeyOrderSelectionUI] 没有可用的钥令");
            return;
        }

        _onKeyOrderSelected = onSelected;
        _onCancelled = onCancelled;
        _isVisible = true;
        Visible = true;

        int displayCount = Mathf.Min(keyOrders.Count, _keyOrderSlots.Count);
        for (int i = 0; i < _keyOrderSlots.Count; i++)
        {
            if (i < displayCount)
            {
                _keyOrderSlots[i].SetKeyOrder(keyOrders[i], i);
                _keyOrderSlots[i].Visible = true;
                int capturedIndex = i;
                _keyOrderSlots[i].SetClickCallback(() => OnSlotClicked(keyOrders[capturedIndex]));
            }
            else
            {
                _keyOrderSlots[i].Visible = false;
            }
        }

        PlayShowAnimation();
        GD.Print($"[KeyOrderSelectionUI] 显示{keyOrders.Count}个钥令选择");
    }

    /// <summary>
    /// 处理钥令槽位点击事件
    /// </summary>
    private void OnSlotClicked(KeyOrder keyOrder)
    {
        if (!_isVisible) return;

        HideSelection();
        _onKeyOrderSelected?.Invoke(keyOrder);
    }

    /// <summary>
    /// 处理取消按钮点击事件
    /// </summary>
    private void OnCancelPressed()
    {
        if (!_isVisible) return;

        HideSelection();
        _onCancelled?.Invoke();
    }

    /// <summary>
    /// 隐藏选择界面
    /// </summary>
    public void HideSelection()
    {
        _isVisible = false;
        Visible = false;
        _onKeyOrderSelected = null;
        _onCancelled = null;
    }

    /// <summary>
    /// 显示动画：从上方滑入并淡入
    /// </summary>
    private void PlayShowAnimation()
    {
        var tween = CreateTween();

        var origPos = Position;
        Position = new Vector2(origPos.X, origPos.Y - 50);
        Modulate = new Color(1f, 1f, 1f, 0f);

        tween.TweenProperty(this, "position", origPos, 0.3f)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(this, "modulate:a", 1f, 0.2f);

        tween.Play();
    }

    public bool IsSelectionVisible() => _isVisible;
}

/// <summary>
/// 单个钥令槽位UI组件
/// 显示内容：图标、名称、类型、消耗、效果描述、快捷键提示
/// </summary>
public partial class KeyOrderSlotUI : Panel
{
    private Label _nameLabel;
    private Label _costLabel;
    private Label _descLabel;
    private Label _effectTypeLabel;
    private TextureRect _iconRect;
    private Label _hotkeyLabel;

    private KeyOrder _keyOrder;
    private System.Action _onClicked;
    private bool _isHovered = false;

    public override void _Ready()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var vbox = GetNodeOrNull<VBoxContainer>("VBoxContainer");
        if (vbox == null)
        {
            vbox = GetNodeOrNull<VBoxContainer>(".");
        }

        if (vbox != null)
        {
            foreach (Node child in vbox.GetChildren())
            {
                string nodeName = child.Name.ToString();
                if (child is Label label)
                {
                    if (_nameLabel == null && nodeName.Contains("Name"))
                        _nameLabel = label;
                    else if (_costLabel == null && nodeName.Contains("Cost"))
                        _costLabel = label;
                    else if (_descLabel == null && nodeName.Contains("Desc"))
                        _descLabel = label;
                    else if (_effectTypeLabel == null && nodeName.Contains("Type"))
                        _effectTypeLabel = label;
                    else if (_hotkeyLabel == null && nodeName.Contains("Hotkey"))
                        _hotkeyLabel = label;
                }
                else if (child is TextureRect textureRect && nodeName.Contains("Icon"))
                {
                    _iconRect = textureRect;
                }
            }
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    /// <summary>
    /// 设置钥令数据并更新显示
    /// </summary>
    /// <param name="keyOrder">钥令数据</param>
    /// <param name="index">槽位索引（用于快捷键显示）</param>
    public void SetKeyOrder(KeyOrder keyOrder, int index)
    {
        _keyOrder = keyOrder;

        if (keyOrder != null)
        {
            if (_nameLabel != null)
                _nameLabel.Text = keyOrder.Name;
            if (_effectTypeLabel != null)
                _effectTypeLabel.Text = GetEffectTypeName(keyOrder.EffectType);
            if (_costLabel != null)
                _costLabel.Text = $"消耗: {keyOrder.SilverKeyCost} 银钥";
            if (_descLabel != null)
                _descLabel.Text = GetEffectDescription(keyOrder);
            if (_hotkeyLabel != null)
                _hotkeyLabel.Text = $"[ {index + 1} ] 点击选择";

            string iconPath = GetIconForEffectType(keyOrder.EffectType);
            Texture2D icon = ResourceLoader.Load<Texture2D>(iconPath);
            if (icon != null && _iconRect != null)
            {
                _iconRect.Texture = icon;
            }
        }

        UpdateAppearance();
    }

    /// <summary>
    /// 获取效果类型显示名称
    /// </summary>
    private string GetEffectTypeName(KeyOrderEffectType effectType)
    {
        return effectType switch
        {
            KeyOrderEffectType.Damage => "⚔️ 伤害型",
            KeyOrderEffectType.Heal => "❤️ 治疗型",
            KeyOrderEffectType.Buff => "🛡️ 增益型",
            KeyOrderEffectType.Debuff => "💀 减益型",
            KeyOrderEffectType.Special => "✨ 特殊型",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 获取效果描述文本
    /// </summary>
    private string GetEffectDescription(KeyOrder keyOrder)
    {
        string effectDesc = keyOrder.EffectType switch
        {
            KeyOrderEffectType.Damage => $"造成 {keyOrder.EffectValue} 伤害",
            KeyOrderEffectType.Heal => $"恢复 {keyOrder.EffectValue}% 最大生命",
            KeyOrderEffectType.Buff => $"获得 {keyOrder.EffectValue} 护盾",
            KeyOrderEffectType.Debuff => $"降低敌人 {keyOrder.EffectValue} 防御",
            KeyOrderEffectType.Special => $"获得 {keyOrder.EffectValue} 能量",
            _ => "未知效果"
        };

        return effectDesc;
    }

    /// <summary>
    /// 根据效果类型获取图标路径
    /// </summary>
    private string GetIconForEffectType(KeyOrderEffectType effectType)
    {
        return effectType switch
        {
            KeyOrderEffectType.Damage => "res://Assets/Icons/sword.png",
            KeyOrderEffectType.Heal => "res://Assets/Icons/star.png",
            KeyOrderEffectType.Buff => "res://Assets/Icons/shield.png",
            KeyOrderEffectType.Debuff => "res://Assets/Icons/enemy_elite.svg",
            KeyOrderEffectType.Special => "res://Assets/Icons/star.png",
            _ => "res://Assets/Icons/enemy_elite.svg"
        };
    }

    /// <summary>
    /// 设置点击回调
    /// </summary>
    public void SetClickCallback(System.Action callback)
    {
        _onClicked = callback;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        UpdateAppearance();
        PlayHoverAnimation();
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        UpdateAppearance();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                _onClicked?.Invoke();
            }
        }
    }

    /// <summary>
    /// 悬停放大动画
    /// </summary>
    private void PlayHoverAnimation()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1.05f, 1.05f), 0.15f);
        tween.Play();
    }

    /// <summary>
    /// 更新槽位外观（悬停时边框变亮、背景变深）
    /// </summary>
    private void UpdateAppearance()
    {
        var styleBox = new StyleBoxFlat();

        if (_keyOrder != null)
        {
            Color borderColor = _isHovered ? new Color(1f, 0.8f, 0.4f) : new Color(0.5f, 0.4f, 0.3f);
            Color bgColor = _isHovered ? new Color(0.2f, 0.18f, 0.25f) : new Color(0.15f, 0.15f, 0.2f);

            styleBox.BgColor = bgColor;
            styleBox.BorderColor = borderColor;
        }
        else
        {
            styleBox.BgColor = new Color(0.1f, 0.1f, 0.15f);
            styleBox.BorderColor = new Color(0.2f, 0.2f, 0.3f);
        }

        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(8);

        AddThemeStyleboxOverride("panel", styleBox);
    }
}
