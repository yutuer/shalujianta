using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.EngravingDescriptionUI
{
    public partial class EngravingDescriptionUI : Control
    {
        private Label _titleLabel;
        private Label _descLabel;
        private Label _costLabel;
        private Label _emojiLabel;
        private Button _useButton;
        private Button _cancelButton;

        private bool _isVisible = false;
        private System.Action<ShopItem> _onEngravingConfirm;
        private System.Action _onCancelled;
        private ShopItem _currentEngravingItem;

        public override void _Ready()
        {
            _titleLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _descLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/DescLabel");
            _costLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/CostLabel");
            _emojiLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/IconContainer/EmojiLabel");
            _useButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/UseButton");
            _cancelButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/CancelButton");

            if (_useButton != null)
            {
                _useButton.Pressed += OnUsePressed;
            }

            if (_cancelButton != null)
            {
                _cancelButton.Pressed += OnCancelPressed;
            }
        }

        public void ShowEngravingDescription(ShopItem engraving, System.Action<ShopItem> onConfirm, System.Action onCancelled = null)
        {
            if (engraving == null) return;

            _onEngravingConfirm = onConfirm;
            _onCancelled = onCancelled;
            _currentEngravingItem = engraving;
            _isVisible = true;

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

            Visible = true;
        }

        public void HideDescription()
        {
            _isVisible = false;
            Visible = false;

            if (_useButton != null)
                _useButton.Text = "确定";
            if (_cancelButton != null)
                _cancelButton.Text = "取消";

            _onEngravingConfirm = null;
            _onCancelled = null;
            _currentEngravingItem = null;
        }

        private void OnUsePressed()
        {
            if (!_isVisible) return;

            var confirmCallback = _onEngravingConfirm;
            var itemToPass = _currentEngravingItem;
            HideDescription();
            confirmCallback?.Invoke(itemToPass);
        }

        private void OnCancelPressed()
        {
            if (!_isVisible) return;

            var cancelledCallback = _onCancelled;
            HideDescription();
            cancelledCallback?.Invoke();
        }

        public bool IsDescriptionVisible() => _isVisible;
    }
}
