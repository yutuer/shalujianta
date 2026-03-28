using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.ArtifactDescriptionUI
{
    public partial class ArtifactDescriptionUI : Control
    {
        private Label _titleLabel;
        private Label _effectLabel;
        private Label _costLabel;
        private TextureRect _iconRect;
        private Label _emojiLabel;
        private Button _buyButton;
        private Button _cancelButton;

        private ShopItem _currentItem;

        public System.Action OnPurchaseCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            _titleLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _effectLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/EffectLabel");
            _costLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/CostLabel");
            _iconRect = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/IconContainer/IconRect");
            _emojiLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/IconContainer/EmojiLabel");
            _buyButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/BuyButton");
            _cancelButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/CancelButton");

            if (_buyButton != null)
            {
                _buyButton.Pressed += OnBuyPressed;
            }

            if (_cancelButton != null)
            {
                _cancelButton.Pressed += OnCancelPressed;
            }
        }

        public void ShowArtifact(ShopItem item)
        {
            if (item == null) return;

            _currentItem = item;

            if (_titleLabel != null)
                _titleLabel.Text = item.Name;
            if (_effectLabel != null)
                _effectLabel.Text = item.Description;
            if (_costLabel != null)
                _costLabel.Text = $"💰 {item.Price}";

            bool hasTexture = false;
            if (_iconRect != null && !string.IsNullOrEmpty(item.Icon))
            {
                var texture = GD.Load<Texture2D>(item.Icon);
                if (texture != null)
                {
                    _iconRect.Texture = texture;
                    if (_emojiLabel != null) _emojiLabel.Visible = false;
                    hasTexture = true;
                }
            }

            if (!hasTexture && _emojiLabel != null)
            {
                _emojiLabel.Text = "💎";
                _emojiLabel.Visible = true;
            }

            bool canAfford = BlackMarkShopManager.Instance?.CanAfford(item) ?? false;
            if (_buyButton != null)
            {
                _buyButton.Disabled = !canAfford || item.Purchased;

                if (item.Purchased)
                {
                    _buyButton.Text = "已购买";
                }
                else if (!canAfford)
                {
                    _buyButton.Text = "黑印不足";
                }
                else
                {
                    _buyButton.Text = "确定";
                }
            }

            Visible = true;
        }

        public void HideArtifact()
        {
            Visible = false;
            _currentItem = null;
        }

        private void OnBuyPressed()
        {
            if (_currentItem == null || _currentItem.Purchased)
            {
                return;
            }

            if (!BlackMarkShopManager.Instance.CanAfford(_currentItem))
            {
                return;
            }

            bool success = BlackMarkShopManager.Instance.PurchaseArtifact(_currentItem);
            if (success)
            {
                Visible = false;
                OnPurchaseCompleted?.Invoke();
            }
        }

        private void OnCancelPressed()
        {
            Visible = false;
            OnCancel?.Invoke();
        }
    }
}
