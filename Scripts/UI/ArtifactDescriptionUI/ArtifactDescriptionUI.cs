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

        public System.Action<ShopItem> OnPurchaseCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            GD.Print($"[ArtifactDescriptionUI] _Ready called");

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

            GD.Print($"[ArtifactDescriptionUI] _Ready completed");
        }

        public void ShowArtifact(ShopItem item)
        {
            GD.Print($"[ArtifactDescriptionUI] ShowArtifact called: item={item?.Name}");

            if (item == null)
            {
                GD.PrintErr("[ArtifactDescriptionUI] ShowArtifact: item is null!");
                return;
            }

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

            GD.Print($"[ArtifactDescriptionUI] ShowArtifact completed");
        }

        public void HideArtifact()
        {
            GD.Print($"[ArtifactDescriptionUI] HideArtifact called");

            Visible = false;
            _currentItem = null;

            GD.Print($"[ArtifactDescriptionUI] HideArtifact completed");
        }

        private void OnBuyPressed()
        {
            GD.Print($"[ArtifactDescriptionUI] OnBuyPressed called");

            if (_currentItem == null || _currentItem.Purchased)
            {
                GD.Print("[ArtifactDescriptionUI] OnBuyPressed: item is null or already purchased");
                return;
            }

            if (!BlackMarkShopManager.Instance.CanAfford(_currentItem))
            {
                GD.Print("[ArtifactDescriptionUI] OnBuyPressed: cannot afford item");
                return;
            }

            bool success = BlackMarkShopManager.Instance.PurchaseArtifact(_currentItem);
            if (success)
            {
                Visible = false;
                OnPurchaseCompleted?.Invoke(_currentItem);
                GD.Print($"[ArtifactDescriptionUI] OnBuyPressed: purchase successful for {_currentItem?.Name}");
            }
            else
            {
                GD.Print("[ArtifactDescriptionUI] OnBuyPressed: purchase failed");
            }

            GD.Print($"[ArtifactDescriptionUI] OnBuyPressed completed");
        }

        private void OnCancelPressed()
        {
            GD.Print($"[ArtifactDescriptionUI] OnCancelPressed called");

            Visible = false;
            OnCancel?.Invoke();

            GD.Print($"[ArtifactDescriptionUI] OnCancelPressed completed");
        }
    }
}
