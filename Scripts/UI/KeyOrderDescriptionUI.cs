using Godot;
using System.Collections.Generic;
using FishEatFish.Scenes;
using FishEatFish.Shop;

public partial class KeyOrderDescriptionUI : Control
{
    private PanelContainer _backgroundPanel;
    private VBoxContainer _mainContainer;
    private Label _titleLabel;
    private Label _nameLabel;
    private Label _typeLabel;
    private Label _descLabel;
    private Label _costLabel;
    private CenterContainer _iconContainer;
    private Label _emojiLabel;
    private Button _useButton;
    private Button _cancelButton;

    private bool _isVisible = false;
    private System.Action<KeyOrder> _onUseConfirmed;
    private System.Action<ShopItem> _onEngravingConfirm;
    private System.Action _onCancelled;
    private ShopItem _currentEngravingItem;

    public override void _Ready()
    {
        SetupUI();
        Visible = false;
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
        _iconContainer = GetNode<CenterContainer>("BackgroundPanel/VBoxContainer/IconContainer");
        _emojiLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/IconContainer/EmojiLabel");
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

    public void ShowKeyOrderDescription(KeyOrder keyOrder, bool useButtonEnabled, System.Action<KeyOrder> onUseConfirmed, System.Action onCancelled = null)
    {
        if (keyOrder == null) return;

        _onUseConfirmed = onUseConfirmed;
        _onCancelled = onCancelled;
        _currentEngravingItem = null;
        _isVisible = true;
        Visible = true;

        if (_titleLabel != null)
            _titleLabel.Text = "钥令详情";
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
            _useButton.Text = "使用";
        }
    }

    public void ShowEngravingDescription(ShopItem engraving, System.Action<ShopItem> onConfirm, System.Action onCancelled = null)
    {
        if (engraving == null) return;

        _onEngravingConfirm = onConfirm;
        _onCancelled = onCancelled;
        _currentEngravingItem = engraving;
        _isVisible = true;
        Visible = true;

        if (_titleLabel != null)
            _titleLabel.Text = engraving.Name;
        if (_emojiLabel != null)
            _emojiLabel.Text = "⚔️";

        if (_descLabel != null)
            _descLabel.Text = engraving.Description;
        if (_costLabel != null)
            _costLabel.Text = $"💰 {engraving.Price}";

        if (_useButton != null)
        {
            if (engraving.Purchased)
            {
                _useButton.Text = "已购买";
                _useButton.Disabled = true;
            }
            else
            {
                bool canAfford = BlackMarkShopManager.Instance?.CanAfford(engraving) ?? false;
                if (!canAfford)
                {
                    _useButton.Text = "黑印不足";
                    _useButton.Disabled = true;
                }
                else
                {
                    _useButton.Text = "确定";
                    _useButton.Disabled = false;
                }
            }
        }
    }

    public void HideDescription()
    {
        _isVisible = false;
        Visible = false;

        if (_useButton != null)
            _useButton.Text = "使用";
        if (_cancelButton != null)
            _cancelButton.Text = "取消";

        _onUseConfirmed = null;
        _onEngravingConfirm = null;
        _onCancelled = null;
        _currentEngravingItem = null;
    }

    private void OnUsePressed()
    {
        if (!_isVisible) return;

        if (_currentEngravingItem != null)
        {
            var confirmCallback = _onEngravingConfirm;
            var itemToPass = _currentEngravingItem;
            HideDescription();
            confirmCallback?.Invoke(itemToPass);
        }
        else
        {
            KeyOrder equippedOrder = GlobalData.EquippedKeyOrder;
            var useCallback = _onUseConfirmed;
            HideDescription();
            useCallback?.Invoke(equippedOrder);
        }
    }

    private void OnCancelPressed()
    {
        if (!_isVisible) return;

        var cancelledCallback = _onCancelled;
        HideDescription();
        cancelledCallback?.Invoke();
    }

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
