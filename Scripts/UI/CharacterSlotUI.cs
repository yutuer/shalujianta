using Godot;
using System;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.UI.HexMap;

/// <summary>
/// 战斗场景顶部左侧的角色槽位。
///
/// 版式（对照参考图 R-08 顶部左）：**立绘头像 + 环形怒气 + 角色名 + 可释放徽标**。
///
/// 怒气环复用 <see cref="RageCircle"/>（地图场景同款，自绘弧），
/// 以「环模式」叠在头像外面 —— 原来这里是一个失效的 TextureRect
/// （给 TextureRect 加 panel 样式框不生效，怒气其实一直没画出来）。
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
    private RageCircle _rageRing;
    private Label _ultReadyLabel;

    private CharacterDefinition _character;
    private int _slotIndex;
    private int _currentRage = 0;
    private bool _isHighlighted = false;
    private bool _ultReadyLabelMissingReported = false;

    private System.Action _clickCallback;
    private Tween _ultReadyTween;
    private Tween _rageGainTween;

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
        _avatarRect = GetNodeOrNull<TextureRect>("VBoxContainer/AvatarBox/AvatarRect");
        _nameLabel = GetNodeOrNull<Label>("VBoxContainer/NameLabel");
        _rageRing = GetNodeOrNull<RageCircle>("VBoxContainer/AvatarBox/RageRing");
        _ultReadyLabel = FindUltReadyLabel();

        if (_avatarRect == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：VBoxContainer/AvatarBox/AvatarRect");
        }
        else
        {
            // 让点击落到槽位面板自身，保证 _GuiInput 能收到点击
            _avatarRect.MouseFilter = MouseFilterEnum.Ignore;
        }

        if (_nameLabel == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：VBoxContainer/NameLabel");
        }

        if (_rageRing == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到节点：VBoxContainer/AvatarBox/RageRing");
        }
        else
        {
            _rageRing.MouseFilter = MouseFilterEnum.Ignore;
        }

        if (_ultReadyLabel == null)
        {
            GD.PrintErr("[CharacterSlotUI] 未找到「可释放」徽标节点（建议命名 UltReadyLabel），无法显示大招就绪提示");
            _ultReadyLabelMissingReported = true;
        }
    }

    /// <summary>
    /// 查找「可释放」徽标 Label（优先槽位直接子节点，其次 VBoxContainer 下）
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
            Label label = GetNodeOrNull<Label>($"VBoxContainer/{nodeName}");
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
            if (_avatarRect != null)
            {
                _avatarRect.Texture = null;
            }
        }

        UpdateAppearance();
    }

    /// <summary>
    /// 载入角色头像：优先用**圆形头像图**（Assets/Portraits/avatar_*.svg），
    /// 没有就退回立绘裁头部，最后退回通用图标。
    /// </summary>
    private void LoadCharacterAvatar(CharacterDefinition character)
    {
        if (_avatarRect == null) return;

        if (!string.IsNullOrEmpty(character.CharacterId))
        {
            string avatarPath = $"res://Assets/Portraits/avatar_{character.CharacterId}.svg";
            if (ResourceLoader.Exists(avatarPath))
            {
                Texture2D avatar = ResourceLoader.Load<Texture2D>(avatarPath);
                if (avatar != null)
                {
                    _avatarRect.Texture = avatar;
                    return;
                }
            }
        }

        // 没有圆形头像 → 用立绘裁出头部
        // ⚠️ Godot 4 的 TextureRect **没有** region_enabled / region_rect（那是 Godot 3 的 Sprite 属性），
        //    裁剪要用 AtlasTexture。
        string path = character.GetPortraitPath();
        if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
        {
            Texture2D portrait = ResourceLoader.Load<Texture2D>(path);
            if (portrait != null)
            {
                _avatarRect.Texture = new AtlasTexture
                {
                    Atlas = portrait,
                    Region = new Rect2(46, 40, 164, 130)
                };
                return;
            }
        }

        // 都没有 → 退回通用图标
        Texture2D fallback = ResourceLoader.Load<Texture2D>("res://Assets/Icons/enemy_elite.svg");
        if (fallback != null)
        {
            _avatarRect.Texture = fallback;
        }
    }

    /// <summary>
    /// 更新怒气状态显示 —— 画在头像外的**圆环**上。
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

        if (_rageRing != null)
        {
            _rageRing.SetCircleColor(barColor);
            _rageRing.SetRage(_currentRage, MaxRage);
        }

        // 「可释放」徽标 + 可点击状态 + 脉冲高亮
        SetUltReadyIndicator(canUseUltimate);
        SetClickable(canUseUltimate);
        SetHighlight(canUseUltimate);
    }

    /// <summary>
    /// 怒气增长时的反馈动画（头像弹一下）
    /// </summary>
    public void PlayRageGainAnimation()
    {
        if (_rageGainTween != null && _rageGainTween.IsValid())
        {
            _rageGainTween.Kill();
        }

        var avatarBox = GetNodeOrNull<Control>("VBoxContainer/AvatarBox");
        if (avatarBox == null) return;

        _rageGainTween = CreateTween();
        _rageGainTween.TweenProperty(avatarBox, "scale", new Vector2(1.25f, 1.25f), 0.12f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _rageGainTween.TweenProperty(avatarBox, "scale", Vector2.One, 0.18f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
    }

    /// <summary>
    /// 显示/隐藏「可释放」徽标
    /// </summary>
    private void SetUltReadyIndicator(bool ready)
    {
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

        // 头像槽**不画底框**（对照参考图 R-08：头像是浮在场景里的）
        styleBox.BgColor = new Color(0f, 0f, 0f, 0f);

        if (_character != null)
        {
            styleBox.BorderColor = _isHighlighted ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 0.4f, 0.5f);
            styleBox.SetBorderWidthAll(_isHighlighted ? 2 : 0);
        }
        else
        {
            styleBox.BorderColor = new Color(0.2f, 0.2f, 0.3f);
            styleBox.SetBorderWidthAll(0);
        }

        styleBox.SetCornerRadiusAll(8);
        styleBox.SetContentMarginAll(2);

        AddThemeStyleboxOverride("panel", styleBox);
    }
}