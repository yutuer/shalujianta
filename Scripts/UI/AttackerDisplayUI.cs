using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 攻击体角色显示UI组件
/// 功能：展示4个角色的头像和名称，配合圆形怒气进度条
/// 位置：战斗场景顶部中央
/// </summary>
public partial class AttackerDisplayUI : Control
{
    private HBoxContainer _characterContainer;
    private List<CharacterSlotUI> _characterSlots = new List<CharacterSlotUI>();
    private Label _teamStatsLabel;

    public override void _Ready()
    {
        SetupLayout();
        GD.Print("[AttackerDisplayUI] 攻击体角色显示UI初始化完成");
    }

    private void SetupLayout()
    {
        _teamStatsLabel = GetNode<Label>("VBoxContainer/TeamStatsLabel");
        _characterContainer = GetNode<HBoxContainer>("VBoxContainer/CharacterContainer");

        if (_characterContainer != null)
        {
            for (int i = 0; i < 4; i++)
            {
                string slotPath = $"VBoxContainer/CharacterContainer/CharacterSlot{i}";
                if (HasNode(slotPath))
                {
                    CharacterSlotUI slot = GetNode<Panel>(slotPath) as CharacterSlotUI;
                    if (slot != null)
                    {
                        _characterSlots.Add(slot);
                    }
                }
            }
        }

        if (_characterSlots.Count == 0)
        {
            GD.PrintErr("[AttackerDisplayUI] 未找到角色槽位节点");
        }
    }

    /// <summary>
    /// 设置攻击体数据并更新UI
    /// </summary>
    /// <param name="attacker">攻击体实例</param>
    public void SetAttacker(Attacker attacker)
    {
        if (attacker == null || attacker.Characters == null)
        {
            GD.PrintErr("[AttackerDisplayUI] Attacker无效");
            return;
        }

        for (int i = 0; i < 4 && i < attacker.Characters.Length; i++)
        {
            CharacterDefinition character = attacker.Characters[i];
            if (i < _characterSlots.Count)
            {
                _characterSlots[i].SetCharacter(character, i);
            }
        }

        UpdateTeamStats(attacker);
        GD.Print($"[AttackerDisplayUI] 已设置攻击体，共{attacker.GetCharacterCount()}个角色");
    }

    private void UpdateTeamStats(Attacker attacker)
    {
        if (_teamStatsLabel != null)
        {
            _teamStatsLabel.Text = $"总属性: 生命 {attacker.TotalHealth} | 攻击 {attacker.TotalAttack} | 防御 {attacker.TotalDefense} | 能量 {attacker.TotalEnergy}";
        }
    }

    /// <summary>
    /// 更新指定角色的怒气状态显示
    /// </summary>
    /// <param name="slotIndex">角色槽位索引 (0-3)</param>
    /// <param name="rageValue">当前怒气值 (0-100)</param>
    /// <param name="canUseUltimate">是否可以使用大招</param>
    public void UpdateCharacterStatus(int slotIndex, int rageValue, bool canUseUltimate)
    {
        if (slotIndex >= 0 && slotIndex < _characterSlots.Count)
        {
            _characterSlots[slotIndex].UpdateRageStatus(rageValue, canUseUltimate);
        }
    }

    public void SetCharacterSlotClickable(int slotIndex, bool clickable)
    {
        if (slotIndex >= 0 && slotIndex < _characterSlots.Count)
        {
            _characterSlots[slotIndex].SetClickable(clickable);
        }
    }

    /// <summary>
    /// 高亮显示怒气已满的角色（可用于大招释放）
    /// </summary>
    public void HighlightCharacterWithFullRage()
    {
        for (int i = 0; i < _characterSlots.Count; i++)
        {
            if (_characterSlots[i].CanUseUltimate())
            {
                _characterSlots[i].SetHighlight(true);
            }
        }
    }
}

/// <summary>
/// 简化版角色槽位UI组件
/// 显示内容：圆形头像 + 角色名称 + 圆形怒气进度条
/// </summary>
public partial class CharacterSlotUI : Panel
{
    private TextureRect _avatarRect;
    private Label _nameLabel;
    private TextureRect _rageCircle;

    private CharacterDefinition _character;
    private int _slotIndex;
    private int _currentRage = 0;
    private bool _isHighlighted = false;

    private const int MaxRage = 100;

    /// <summary>
    /// 检查当前怒气是否已达到大招释放条件
    /// </summary>
    public bool CanUseUltimate() => _currentRage >= MaxRage;

    public override void _Ready()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        _avatarRect = GetNodeOrNull<TextureRect>("HBoxContainer/AvatarRect");
        _nameLabel = GetNodeOrNull<Label>("HBoxContainer/VBoxContainer/NameLabel");
        _rageCircle = GetNodeOrNull<TextureRect>("HBoxContainer/VBoxContainer/RageCircle");

        var hbox = GetNodeOrNull<HBoxContainer>(".");
        if (hbox != null)
        {
            hbox.Name = $"CharacterSlot{_slotIndex}";
        }
    }

    /// <summary>
    /// 设置角色数据
    /// </summary>
    /// <param name="character">角色定义数据</param>
    /// <param name="index">槽位索引</param>
    public void SetCharacter(CharacterDefinition character, int index)
    {
        _character = character;
        _slotIndex = index;

        if (character != null)
        {
            _nameLabel.Text = character.Name;
            LoadCharacterAvatar(character);
        }
        else
        {
            _nameLabel.Text = "空";
        }

        UpdateAppearance();
    }

    private void LoadCharacterAvatar(CharacterDefinition character)
    {
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

        var rageStyle = new StyleBoxFlat();
        rageStyle.BgColor = barColor;
        rageStyle.SetCornerRadiusAll(15);
        _rageCircle.AddThemeStyleboxOverride("panel", rageStyle);

        if (canUseUltimate)
        {
            PlayUltReadyAnimation();
        }
        else
        {
            var tween = CreateTween();
            tween.Kill();
            Modulate = new Color(1f, 1f, 1f, 1f);
        }
    }

    /// <summary>
    /// 大招就绪时的脉冲高亮动画
    /// </summary>
    private void PlayUltReadyAnimation()
    {
        Tween tween = CreateTween();
        tween.SetLoops();

        var origColor = Modulate;
        tween.TweenProperty(this, "modulate", new Color(1.2f, 1f, 0.8f, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "modulate", origColor, 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        tween.Play();
    }

    public void SetClickable(bool clickable)
    {
        MouseFilter = clickable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
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
            var tween = CreateTween();
            tween.Kill();
            Modulate = new Color(1f, 1f, 1f, 1f);
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
