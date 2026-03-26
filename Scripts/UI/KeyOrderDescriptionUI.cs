using Godot;
using System.Collections.Generic;
using FishEatFish.Scenes;

/// <summary>
/// 钥令说明界面组件
/// 功能：展示钥令详情，支持使用/取消操作
/// </summary>
public partial class KeyOrderDescriptionUI : Control
{
    private PanelContainer _backgroundPanel;
    private VBoxContainer _mainContainer;
    private Label _titleLabel;
    private Label _nameLabel;
    private Label _typeLabel;
    private Label _descLabel;
    private Label _costLabel;
    private Button _useButton;
    private Button _cancelButton;

    private bool _isVisible = false;
    private System.Action<KeyOrder> _onUseConfirmed;
    private System.Action _onCancelled;

    public override void _Ready()
    {
        SetupUI();
        Visible = false;
        GD.Print("[KeyOrderDescriptionUI] 钥令说明UI初始化完成");
    }

    private void SetupUI()
    {
        _backgroundPanel = GetNode<PanelContainer>("BackgroundPanel");
        _mainContainer = GetNode<VBoxContainer>("BackgroundPanel/VBoxContainer");
        _titleLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
        _nameLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/NameLabel");
        _typeLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TypeLabel");
        _descLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/DescLabel");
        _costLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/CostLabel");
        _useButton = GetNode<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/UseButton");
        _cancelButton = GetNode<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/CancelButton");

        if (_useButton != null)
        {
            _useButton.Pressed += OnUsePressed;
        }

        if (_cancelButton != null)
        {
            _cancelButton.Pressed += OnCancelPressed;
        }
    }

    /// <summary>
    /// 显示钥令说明界面
    /// </summary>
    /// <param name="keyOrder">钥令数据</param>
    /// <param name="useButtonEnabled">使用按钮是否启用</param>
    /// <param name="onUseConfirmed">确认使用回调</param>
    /// <param name="onCancelled">取消回调</param>
    public void ShowDescription(KeyOrder keyOrder, bool useButtonEnabled, System.Action<KeyOrder> onUseConfirmed, System.Action onCancelled = null)
    {
        if (keyOrder == null)
        {
            GD.PrintErr("[KeyOrderDescriptionUI] 钥令数据为空");
            return;
        }

        _onUseConfirmed = onUseConfirmed;
        _onCancelled = onCancelled;
        _isVisible = true;
        Visible = true;

        if (_nameLabel != null)
            _nameLabel.Text = keyOrder.Name;
        if (_typeLabel != null)
            _typeLabel.Text = GetEffectTypeName(keyOrder.EffectType);
        if (_descLabel != null)
            _descLabel.Text = GetEffectDescription(keyOrder);
        if (_costLabel != null)
            _costLabel.Text = $"消耗: {keyOrder.SilverKeyCost} 银钥";

        if (_useButton != null)
        {
            _useButton.Disabled = !useButtonEnabled;

            if (!useButtonEnabled)
            {
                _useButton.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            }
            else
            {
                _useButton.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.6f));
            }
        }

        PlayShowAnimation();
        GD.Print($"[KeyOrderDescriptionUI] 显示钥令说明: {keyOrder.Name}, 按钮启用: {useButtonEnabled}");
    }

    /// <summary>
    /// 显示提示消息
    /// </summary>
    public void ShowPrompt(string message, System.Action onConfirmed, System.Action onCancelled = null)
    {
        _onUseConfirmed = (_) => onConfirmed?.Invoke();
        _onCancelled = onCancelled;
        _isVisible = true;
        Visible = true;

        if (_nameLabel != null)
            _nameLabel.Text = message;
        if (_typeLabel != null)
            _typeLabel.Text = "";
        if (_descLabel != null)
            _descLabel.Text = "";
        if (_costLabel != null)
            _costLabel.Text = "";

        if (_useButton != null)
        {
            _useButton.Disabled = false;
            _useButton.Text = "确定";
        }

        if (_cancelButton != null)
        {
            _cancelButton.Text = "取消";
        }

        PlayShowAnimation();
        GD.Print($"[KeyOrderDescriptionUI] 显示提示: {message}");
    }

    /// <summary>
    /// 隐藏界面
    /// </summary>
    public void HideDescription()
    {
        _isVisible = false;
        Visible = false;

        if (_useButton != null)
            _useButton.Text = "使用";
        if (_cancelButton != null)
            _cancelButton.Text = "取消";

        _onUseConfirmed = null;
        _onCancelled = null;
    }

    private void OnUsePressed()
    {
        if (!_isVisible) return;

        KeyOrder equippedOrder = GlobalData.EquippedKeyOrder;
        HideDescription();
        _onUseConfirmed?.Invoke(equippedOrder);
    }

    private void OnCancelPressed()
    {
        if (!_isVisible) return;

        HideDescription();
        _onCancelled?.Invoke();
    }

    private void PlayShowAnimation()
    {
        var tween = CreateTween();

        var origPos = Position;
        Position = new Vector2(origPos.X, origPos.Y - 30);
        Modulate = new Color(1f, 1f, 1f, 0f);

        tween.TweenProperty(this, "position", origPos, 0.25f)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(this, "modulate:a", 1f, 0.15f);

        tween.Play();
    }

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

    private string GetEffectDescription(KeyOrder keyOrder)
    {
        string effectDesc = keyOrder.EffectType switch
        {
            KeyOrderEffectType.Damage => $"造成 {keyOrder.EffectValue} 点伤害",
            KeyOrderEffectType.Heal => $"恢复 {keyOrder.EffectValue}% 最大生命值",
            KeyOrderEffectType.Buff => $"获得 {keyOrder.EffectValue} 点护盾",
            KeyOrderEffectType.Debuff => $"降低敌人 {keyOrder.EffectValue} 点防御",
            KeyOrderEffectType.Special => $"获得 {keyOrder.EffectValue} 点能量",
            _ => "未知效果"
        };

        return effectDesc;
    }

    public bool IsDescriptionVisible() => _isVisible;
}
