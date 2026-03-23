using Godot;
using System.Collections.Generic;

/// <summary>
/// 怒气值显示UI组件
/// 功能：集中展示4个角色的怒气状态，提供怒气获取动画反馈
/// 位置：战斗场景左侧
/// </summary>
public partial class RageDisplayUI : Control
{
    private Dictionary<int, RageSlotUI> _rageSlots = new Dictionary<int, RageSlotUI>();
    private Label _totalRageLabel;
    private VBoxContainer _mainContainer;

    public override void _Ready()
    {
        SetupUI();
        GD.Print("[RageDisplayUI] 怒气显示UI初始化完成");
    }

    private void SetupUI()
    {
        _mainContainer = GetNodeOrNull<VBoxContainer>("VBoxContainer");
        _totalRageLabel = GetNodeOrNull<Label>("VBoxContainer/TotalRageLabel");

        var slotsContainer = GetNodeOrNull<HBoxContainer>("VBoxContainer/RageSlotsContainer");
        if (slotsContainer != null)
        {
            for (int i = 0; i < 4; i++)
            {
                string slotPath = $"RageSlotsContainer/RageSlot{i}";
                if (HasNode(slotPath))
                {
                    RageSlotUI slot = GetNodeOrNull<Panel>(slotPath) as RageSlotUI;
                    if (slot != null)
                    {
                        _rageSlots[i] = slot;
                    }
                }
            }
        }

        if (_rageSlots.Count == 0)
        {
            GD.PrintErr("[RageDisplayUI] 未找到怒气槽位节点");
        }
    }

    /// <summary>
    /// 设置单个角色的怒气值
    /// </summary>
    /// <param name="slotIndex">角色槽位索引 (0-3)</param>
    /// <param name="currentRage">当前怒气值</param>
    /// <param name="maxRage">最大怒气值，默认为100</param>
    public void SetCharacterRage(int slotIndex, int currentRage, int maxRage = 100)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].SetRage(currentRage, maxRage);
        }
    }

    /// <summary>
    /// 设置角色名称显示
    /// </summary>
    /// <param name="slotIndex">角色槽位索引</param>
    /// <param name="characterName">角色名称</param>
    /// <param name="nameColor">角色名称颜色</param>
    public void SetCharacterInfo(int slotIndex, string characterName, Color nameColor)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].SetCharacterName(characterName, nameColor);
        }
    }

    /// <summary>
    /// 批量更新所有角色的怒气值
    /// </summary>
    /// <param name="rageValues">怒气值字典 (key:槽位索引, value:怒气值)</param>
    public void UpdateAllRage(Dictionary<int, int> rageValues)
    {
        int totalRage = 0;
        foreach (var kvp in rageValues)
        {
            if (_rageSlots.ContainsKey(kvp.Key))
            {
                _rageSlots[kvp.Key].SetRage(kvp.Value);
                totalRage += kvp.Value;
            }
        }

        if (_totalRageLabel != null)
        {
            _totalRageLabel.Text = $"总怒气: {totalRage}";
        }
    }

    /// <summary>
    /// 高亮显示可释放大招的角色
    /// </summary>
    /// <param name="readyIndices">怒气已满的角色索引列表</param>
    public void HighlightReadyCharacters(List<int> readyIndices)
    {
        foreach (var kvp in _rageSlots)
        {
            if (readyIndices.Contains(kvp.Key))
            {
                kvp.Value.SetHighlight(true);
            }
            else
            {
                kvp.Value.SetHighlight(false);
            }
        }
    }

    /// <summary>
    /// 显示怒气获取动画
    /// </summary>
    /// <param name="slotIndex">角色槽位索引</param>
    /// <param name="gainedRage">获得的怒气值</param>
    public void ShowRageGainAnimation(int slotIndex, int gainedRage)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].PlayGainAnimation(gainedRage);
        }
    }
}

/// <summary>
/// 单个怒气槽位UI组件
/// 显示内容：角色名称、怒气进度条、怒气数值、大招就绪提示
/// </summary>
public partial class RageSlotUI : Panel
{
    private Label _characterNameLabel;
    private Label _rageValueLabel;
    private ProgressBar _rageBar;
    private Label _ultReadyLabel;
    private int _currentRage = 0;
    private int _maxRage = 100;

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
                    if (_characterNameLabel == null && nodeName.Contains("Character"))
                        _characterNameLabel = label;
                    else if (_rageValueLabel == null && nodeName.Contains("Value"))
                        _rageValueLabel = label;
                    else if (nodeName.Contains("Ready"))
                        _ultReadyLabel = label;
                }
                else if (child is ProgressBar progressBar)
                {
                    if (_rageBar == null)
                        _rageBar = progressBar;
                }
            }
        }
    }

    /// <summary>
    /// 设置怒气值并更新显示
    /// </summary>
    /// <param name="currentRage">当前怒气值</param>
    /// <param name="maxRage">最大怒气值</param>
    public void SetRage(int currentRage, int maxRage = 100)
    {
        _currentRage = Mathf.Clamp(currentRage, 0, maxRage);
        _maxRage = maxRage;

        if (_rageBar != null)
        {
            _rageBar.MaxValue = _maxRage;
            _rageBar.Value = _currentRage;
        }

        if (_rageValueLabel != null)
        {
            _rageValueLabel.Text = $"{_currentRage}/{_maxRage}";
        }

        bool isReady = _currentRage >= _maxRage;
        if (_ultReadyLabel != null)
        {
            _ultReadyLabel.Visible = isReady;
        }

        UpdateRageColor();
        UpdateAppearance();
    }

    /// <summary>
    /// 设置角色名称显示
    /// </summary>
    public void SetCharacterName(string name, Color color)
    {
        if (_characterNameLabel != null)
        {
            _characterNameLabel.Text = name;
            _characterNameLabel.AddThemeColorOverride("font_color", color);
        }
    }

    /// <summary>
    /// 根据怒气百分比更新进度条颜色
    /// 颜色规则：100%橙色 | 70%-99%黄色 | 30%-69%蓝色 | 0%-29%灰色
    /// </summary>
    private void UpdateRageColor()
    {
        float percent = _maxRage > 0 ? (float)_currentRage / _maxRage : 0f;
        Color barColor;

        if (percent >= 1.0f)
        {
            barColor = new Color(1f, 0.5f, 0f);
        }
        else if (percent >= 0.7f)
        {
            barColor = new Color(1f, 0.8f, 0.2f);
        }
        else if (percent >= 0.3f)
        {
            barColor = new Color(0.3f, 0.6f, 1f);
        }
        else
        {
            barColor = new Color(0.5f, 0.5f, 0.5f);
        }

        if (_rageBar != null)
        {
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = barColor;
            styleBox.SetCornerRadiusAll(3);
            _rageBar.AddThemeStyleboxOverride("fill", styleBox);
        }
    }

    /// <summary>
    /// 设置高亮状态（怒气满时）
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        UpdateAppearance();

        if (highlight)
        {
            PlayHighlightAnimation();
        }
    }

    /// <summary>
    /// 大招就绪时的脉冲动画
    /// </summary>
    private void PlayHighlightAnimation()
    {
        Tween tween = CreateTween();
        tween.SetLoops();

        var origColor = Modulate;
        tween.TweenProperty(this, "modulate", new Color(1.3f, 1f, 0.7f, 1f), 0.4f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(this, "modulate", origColor, 0.4f)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Play();
    }

    /// <summary>
    /// 怒气获取动画：显示"+N"绿色文字后恢复
    /// </summary>
    /// <param name="gainedRage">获得的怒气值</param>
    public void PlayGainAnimation(int gainedRage)
    {
        Tween tween = CreateTween();

        var origColor = _rageValueLabel.GetThemeColor("font_color");
        var gainColor = new Color(0.3f, 1f, 0.3f);

        _rageValueLabel.AddThemeColorOverride("font_color", gainColor);
        _rageValueLabel.Text = $"+{gainedRage}";

        tween.TweenCallback(new Callable(this, nameof(ResetGainDisplay)));

        tween.Play();
    }

    /// <summary>
    /// 重置怒气显示为正常状态
    /// </summary>
    private void ResetGainDisplay()
    {
        _rageValueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0.3f));
        _rageValueLabel.Text = $"{_currentRage}/{_maxRage}";
    }

    /// <summary>
    /// 更新槽位外观样式
    /// </summary>
    private void UpdateAppearance()
    {
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.12f, 0.14f, 0.2f);
        styleBox.BorderColor = _currentRage >= _maxRage ? new Color(1f, 0.7f, 0.2f) : new Color(0.3f, 0.3f, 0.4f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(6);
        styleBox.SetContentMarginAll(3);

        AddThemeStyleboxOverride("panel", styleBox);
    }
}
