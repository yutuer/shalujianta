using System;
using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.ShopItemCard
{
    public partial class ShopItemCard : PanelContainer
    {
        private Label _nameLabel;
        private TextureRect _iconRect;
        private Label _emojiLabel;
        private Label _priceLabel;

        public ShopItem Item { get; private set; }
        public System.Action<ShopItem> OnCardClicked;

        private bool _isReady = false;
        private bool _isHovered = false;
        private bool _clickEnabled = true;

        public override void _Ready()
        {
            var vbox = GetNodeOrNull("VBoxContainer");
            _nameLabel = GetNodeOrNull<Label>("VBoxContainer/NameLabel");
            var iconContainer = GetNodeOrNull("VBoxContainer/IconContainer");

            if (iconContainer != null)
            {
                _iconRect = iconContainer.GetNodeOrNull<TextureRect>("IconRect");
                _emojiLabel = iconContainer.GetNodeOrNull<Label>("EmojiLabel");
            }

            _priceLabel = GetNodeOrNull<Label>("VBoxContainer/PriceContainer/PriceLabel");

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;

            _isReady = true;
        }

        public void SetItem(ShopItem item)
        {
            GD.Print($"[ShopItemCard] SetItem called: {item?.Name}, Purchased={item?.Purchased}");
            
            if (item == null || _nameLabel == null) return;

            Item = item;
            _nameLabel.Text = item.Name;

            bool hasTexture = false;
            if (!string.IsNullOrEmpty(item.Icon))
            {
                var texture = GD.Load<Texture2D>(item.Icon);
                if (texture != null && _iconRect != null)
                {
                    _iconRect.Texture = texture;
                    if (_emojiLabel != null) _emojiLabel.Visible = false;
                    hasTexture = true;
                }
            }

            if (!hasTexture && _emojiLabel != null)
            {
                _emojiLabel.Text = GetItemEmoji(item);
                _emojiLabel.Visible = true;
            }

            if (_priceLabel != null)
            {
                if (item.Purchased)
                {
                    GD.Print($"[ShopItemCard] SetItem: setting purchased state for {item.Name}");
                    _priceLabel.Text = "已售出";
                    _priceLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                    _nameLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
                }
                else
                {
                    GD.Print($"[ShopItemCard] SetItem: setting available state for {item.Name}");
                    _priceLabel.Text = $"💰 {item.Price}";
                    _priceLabel.Modulate = new Color(1, 0.8f, 0);
                    _nameLabel.Modulate = new Color(1, 0.95f, 0.9f);
                }
            }
        }

        private string GetItemEmoji(ShopItem item)
        {
            return item.ItemType switch
            {
                ShopItemType.Artifact => "💎",
                ShopItemType.Engraving => "✨",
                _ => "🎁"
            };
        }

        private void OnMouseEntered()
        {
            if (Item?.Purchased == true) return;

            _isHovered = true;
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate", new Color(1.1f, 1.1f, 1.1f), 0.15f);
        }

        private void OnMouseExited()
        {
            _isHovered = false;
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate", Colors.White, 0.15f);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && _clickEnabled)
                {
                    OnCardClicked?.Invoke(Item);
                }
            }
        }

        public void SetClickEnabled(bool enabled)
        {
            _clickEnabled = enabled;
        }
    }
}
