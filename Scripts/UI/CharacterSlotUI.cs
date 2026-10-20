using Godot;
using System;
using FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 简化版角色槽位UI组件
/// 显示内容：圆形头像 + 角色名称 + 圆形怒气进度条 + 大招就绪徽标
/// </summary>
public partial class CharacterSlotUI : Panel
{
    /// <summary>「可释放」徽标候选节点名（由场景中添加的 Label）</summary>
    private static readonly string[] UltReadyLabelNames =
    {
        "UltReadyLabel",
        "UltReadyBadge",
        "ReadyBadge",
        "UltimateReadyLabel"
    };

    private TextureRect _avatarRect;
    private Label _nameLabel;
    private TextureRect _rageCircle;
    private Label _ultReadyLabel;

    private CharacterDefinition _character;
    private int _slotIndex;
    private int _currentRage = 0;
    private bool _isHighlighted = false;
    private bool _ultReadyLabelMissingReported = false;

    private System.Action _clickCallback;
    private Tween _ultReadyTween;

    private const int MaxRage = 100;

    /// <summary>
    /// 检查当前怒气是否已达到大招释放条件
    /// </summary>
    public bool CanUseUltimate() => _currentRage >= MaxRage;

    public override void _Ready()
    {
        GD.Print("[CharacterSlotUI] _Ready called");

        SetupUI();

        GD.Print("[CharacterSlotUI] _Ready completed");
    }

    private void SetupUI()
    {
        _avatarRect = GetNodeOrNull<TextureRect>("HBoxContainer/AvatarRect");
        _nameLabel = GetNodeOrNull<Label>("HBoxContainer/VBoxContainer/NameLabel");
        _rageCircle = GetNodeOrNull<TextureRect>("HBoxContainer/VBoxContainer/RageCircle");
        _ultReadyLabel = FindUltReadyLabel();

        if (_avatarRect == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：HBoxContainer/AvatarRect");
        }
        else
        {
            // 让点击落到槽位面板自身，保证 _GuiInput 能收到点击
            _avatarRect.MouseFilter = MouseFilterEnum.Ignore;
        }

        if (_nameLabel == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：HBoxContainer/VBoxContainer/NameLabel");
        }

        if (_rageCircle == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：HBoxContainer/VBoxContainer/RageCircle");
        }
        else
        {
            _rageCircle.MouseFilter = MouseFilterEnum.Ignore;
        }

        if (_ultReadyLabel == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到「可释放」徽标节点（建议命名 UltReadyLabel），无法显示大招就绪提示");
            _ultReadyLabelMissingReported = true;
        }
    }

    /// <summary>
    /// 查找「可释放」徽标 Label（优先槽位直接子节点，其次 HBoxContainer 下）
    /// </summary>
    private Label FindUltReadyLabel()
    {
        foreach (string nodeName in UltReadyLabelNames)
        {
            Label label = GetNodeOrNull<Label>(nodeName);
            if (label != null)
            {
                return label;
            }
        }

        foreach (string nodeName in UltReadyLabelNames)
        {
            Label label = GetNodeOrNull<Label>($"HBoxContainer/{nodeName}");
            if (label != null)
            {
                return label;
            }
        }

        return null;
    }

    /// <summary>
    /// 设置槽位数据
    /// </summary>
    /// <param name="character">角色定义数据</param>
    /// <param name="index">槽位索引</param>
    public void SetCharacter(CharacterDefinition character, int index)
    {
        _character = character;
        _slotIndex = index;

        if (character != null)
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = character.Name;
            }
            LoadCharacterAvatar(character);
        }
        else
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = "空";
            }
        }

        UpdateAppearance();
    }

    private void LoadCharacterAvatar(CharacterDefinition character)
    {
        if (_avatarRect == null)
        {
            return;
        }

        string avatarPath = $"res://Assets/Icons/enemy_elite.svg";
        Texture2D avatar = ResourceLoader.Load<Texture2D>(avatarPath);
        if (avatar != null)
        {
            _avatarRect.Texture = avatar;
        }
    }

    /// <summary>
    /// 更新怒气状态显示 - 圆形进度条样式
    /// 怒气条颜色渐变：灰(0-30%) → 蓝(30-70%) → 黄(70-100%) → 橙(满)
    /// </summary>
    /// <param name="rageValue">怒气值 (0-MaxRage)</param>
    /// <param name="canUseUltimate">是否可释放大招</param>
    public void UpdateRageStatus(int rageValue, bool canUseUltimate)
    {
        _currentRage = Mathf.Clamp(rageValue, 0, MaxRage);

        Color barColor;
        if (_currentRage >= MaxRage)
        {
            barColor = new Color(1f, 0.5f, 0f);
        }
        else if ((float)_currentRage / MaxRage >= 0.7f)
        {
            barColor = new Color(1f, 0.8f, 0.2f);
        }
        else if ((float)_currentRage / MaxRage >= 0.3f)
        {
            barColor = new Color(0.3f, 0.6f, 1f);
        }
        else
        {
            barColor = new Color(0.5f, 0.5f, 0.5f);
        }

        if (_rageCircle != null)
        {
            var rageStyle = new StyleBoxFlat();
            rageStyle.BgColor = barColor;
            rageStyle.SetCornerRadiusAll(15);
            _rageCircle.AddThemeStyleboxOverride("panel", rageStyle);
        }

        // 「可释放」徽标 + 可点击状态 + 脉冲高亮
        SetUltReadyIndicator(canUseUltimate);
        SetClickable(canUseUltimate);
        SetHighlight(canUseUltimate);
    }

    /// <summary>
    /// 显示/隐藏「可释放」徽标
    /// </summary>
    private void SetUltReadyIndicator(bool ready)
    {
        if (_ultReadyLabel == null)
        {
            _ultReadyLabel = FindUltReadyLabel();
        }

        if (_ultReadyLabel == null)
        {
            if (!_ultReadyLabelMissingReported)
            {
                _ultReadyLabelMissingReported = true;
                GD.PrintErr("[CharacterSlotUI] 未找到「可释放」徽标节点（建议命名 UltReadyLabel），无法显示大招就绪提示");
            }
            return;
        }

        _ultReadyLabel.Visible = ready;
        _ultReadyLabel.Text = ready ? "可释放" : "";
    }

    /// <summary>
    /// 大招就绪时的脉冲高亮动画
    /// </summary>
    private void PlayUltReadyAnimation()
    {
        if (_ultReadyTween != null && _ultReadyTween.IsValid())
        {
            _ultReadyTween.Kill();
        }

        _ultReadyTween = CreateTween();
        _ultReadyTween.SetLoops();

        var origColor = Modulate;
        _ultReadyTween.TweenProperty(this, "modulate", new Color(1.2f, 1f, 0.8f, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _ultReadyTween.TweenProperty(this, "modulate", origColor, 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        _ultReadyTween.Play();
    }

    /// <summary>
    /// 停止大招就绪脉冲动画
    /// </summary>
    private void StopUltReadyAnimation()
    {
        if (_ultReadyTween != null && _ultReadyTween.IsValid())
        {
            _ultReadyTween.Kill();
        }
        _ultReadyTween = null;

        Modulate = new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// 设置槽位是否可点击（只有怒气满可释放大招时才可点）
    /// </summary>
    public void SetClickable(bool clickable)
    {
        MouseFilter = clickable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// 设置点击回调（左键点击槽位时触发）
    /// </summary>
    /// <param name="onClicked">点击回调，参数为空表示清除回调</param>
    public void SetClickCallback(System.Action onClicked)
    {
        GD.Print("[CharacterSlotUI] SetClickCallback called");

        _clickCallback = onClicked;

        GD.Print("[CharacterSlotUI] SetClickCallback completed");
    }

    /// <summary>
    /// 鼠标输入：左键按下时触发点击回调
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        // 只处理鼠标按键事件，忽略鼠标移动等，避免刷屏
        if (@event is not InputEventMouseButton mouseButton)
        {
            return;
        }

        GD.Print("[CharacterSlotUI] _GuiInput called");

        if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
        {
            if (_clickCallback != null)
            {
                _clickCallback.Invoke();
            }
            else
            {
                GD.Print("[CharacterSlotUI] 槽位被点击，但没有设置点击回调");
            }

            AcceptEvent();
        }

        GD.Print("[CharacterSlotUI] _GuiInput completed");
    }

    /// <summary>
    /// 设置高亮状态（怒气满时的视觉反馈）
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        _isHighlighted = highlight;
        UpdateAppearance();

        if (highlight)
        {
            PlayUltReadyAnimation();
        }
        else
        {
            StopUltReadyAnimation();
        }
    }

    /// <summary>
    /// 更新槽位外观样式（背景色、边框颜色）
    /// </summary>
    private void UpdateAppearance()
    {
        var styleBox = new StyleBoxFlat();

        if (_character != null)
        {
            styleBox.BgColor = new Color(0.15f, 0.18f, 0.25f);
            styleBox.BorderColor = _isHighlighted ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 0.4f, 0.5f);
        }
        else
        {
            styleBox.BgColor = new Color(0.1f, 0.1f, 0.15f);
            styleBox.BorderColor = new Color(0.2f, 0.2f, 0.3f);
        }

        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(5);

        AddThemeStyleboxOverride("panel", styleBox);
    }
}
