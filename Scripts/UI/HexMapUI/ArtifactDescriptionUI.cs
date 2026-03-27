using System;
using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.HexMap
{
    public partial class ArtifactDescriptionUI : Control
    {
        private Label _titleLabel;
        private Label _nameLabel;
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
            GD.Print($"[ArtifactDescriptionUI] _Ready called");

            _titleLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _nameLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/NameLabel");
            _effectLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/EffectLabel");
            _costLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/CostLabel");
            _iconRect = GetNode<TextureRect>("BackgroundPanel/VBoxContainer/IconContainer/IconRect");
            _emojiLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/IconContainer/EmojiLabel");
            _buyButton = GetNode<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/BuyButton");
            _cancelButton = GetNode<Button>("BackgroundPanel/VBoxContainer/ButtonContainer/CancelButton");

            _buyButton.Pressed += OnBuyPressed;
            _cancelButton.Pressed += OnCancelPressed;

            Visible = false;
            GD.Print($"[ArtifactDescriptionUI] _Ready completed");
        }

        public void ShowArtifact(ShopItem item)
        {
            GD.Print($"[ArtifactDescriptionUI] ShowArtifact: {item.Name}");
            _currentItem = item;

            _titleLabel.Text = "造物详情";
            _nameLabel.Text = item.Name;
            _effectLabel.Text = item.Description;
            _costLabel.Text = $"💰 价格: {item.Price} 黑印";

            bool hasTexture = false;
            if (!string.IsNullOrEmpty(item.Icon))
            {
                var texture = GD.Load<Texture2D>(item.Icon);
                if (texture != null)
                {
                    _iconRect.Texture = texture;
                    _emojiLabel.Visible = false;
                    hasTexture = true;
                }
            }

            if (!hasTexture)
            {
                _emojiLabel.Text = "💎";
                _emojiLabel.Visible = true;
            }

            bool canAfford = BlackMarkShopManager.Instance?.CanAfford(item) ?? false;
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
                _buyButton.Text = "购买";
            }

            Visible = true;
        }

        private void OnBuyPressed()
        {
            if (_currentItem == null || _currentItem.Purchased)
            {
                GD.Print($"[ArtifactDescriptionUI] Cannot purchase: item is null or already purchased");
                return;
            }

            GD.Print($"[ArtifactDescriptionUI] OnBuyPressed: {_currentItem.Name}");

            if (!BlackMarkShopManager.Instance.CanAfford(_currentItem))
            {
                GD.Print($"[ArtifactDescriptionUI] Not enough black marks!");
                return;
            }

            bool success = BlackMarkShopManager.Instance.PurchaseArtifact(_currentItem);
            if (success)
            {
                GD.Print($"[ArtifactDescriptionUI] Purchase successful!");
                Visible = false;
                OnPurchaseCompleted?.Invoke();
            }
            else
            {
                GD.Print($"[ArtifactDescriptionUI] Purchase failed!");
            }
        }

        private void OnCancelPressed()
        {
            GD.Print($"[ArtifactDescriptionUI] OnCancelPressed");
            Visible = false;
            OnCancel?.Invoke();
        }
    }
}
