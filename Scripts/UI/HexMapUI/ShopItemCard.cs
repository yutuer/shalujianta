using System;
using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.HexMap
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
        private StyleBoxFlat _normalStyle;
        private StyleBoxFlat _hoverStyle;

        public override void _Ready()
        {
            GD.Print($"[ShopItemCard] _Ready called, Name={Name}, Type={GetType()}");

            try
            {
                var vbox = GetNodeOrNull("VBoxContainer");
                GD.Print($"[ShopItemCard] _Ready: VBoxContainer={vbox}");

                _nameLabel = GetNodeOrNull<Label>("VBoxContainer/NameLabel");
                GD.Print($"[ShopItemCard] _Ready: trying IconContainer...");
                var iconContainer = GetNodeOrNull("VBoxContainer/IconContainer");
                GD.Print($"[ShopItemCard] _Ready: IconContainer={iconContainer}");

                if (iconContainer != null)
                {
                    _iconRect = iconContainer.GetNodeOrNull<TextureRect>("IconRect");
                    _emojiLabel = iconContainer.GetNodeOrNull<Label>("EmojiLabel");
                }

                _priceLabel = GetNodeOrNull<Label>("VBoxContainer/PriceContainer/PriceLabel");

                GD.Print($"[ShopItemCard] _Ready: name={_nameLabel}, icon={_iconRect}, emoji={_emojiLabel}, price={_priceLabel}");

                if (_nameLabel != null)
                {
                    _nameLabel.Text = "Loading...";
                    GD.Print($"[ShopItemCard] _Ready: test text set");
                }

                MouseEntered += OnMouseEntered;
                MouseExited += OnMouseExited;

                _isReady = true;
                GD.Print($"[ShopItemCard] _Ready completed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ShopItemCard] _Ready exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SetItem(ShopItem item)
        {
            GD.Print($"[ShopItemCard] SetItem: item={item?.Name}, _isReady={_isReady}");

            if (item == null)
            {
                GD.PrintErr("[ShopItemCard] SetItem: item is null!");
                return;
            }

            Item = item;

            if (!_isReady)
            {
                GD.PrintErr("[ShopItemCard] SetItem called before _Ready!");
                return;
            }

            GD.Print($"[ShopItemCard] SetItem: _nameLabel={_nameLabel}");

            if (_nameLabel == null)
            {
                GD.PrintErr("[ShopItemCard] SetItem: _nameLabel is null!");
                return;
            }

            _nameLabel.Text = item.Name;
            GD.Print($"[ShopItemCard] SetItem: name set to '{item.Name}'");

            bool hasTexture = false;
            if (!string.IsNullOrEmpty(item.Icon))
            {
                var texture = GD.Load<Texture2D>(item.Icon);
                if (texture != null)
                {
                    if (_iconRect != null) _iconRect.Texture = texture;
                    if (_emojiLabel != null) _emojiLabel.Visible = false;
                    hasTexture = true;
                    GD.Print($"[ShopItemCard] SetItem: icon loaded");
                }
                else
                {
                    GD.Print($"[ShopItemCard] SetItem: icon failed to load from {item.Icon}");
                }
            }

            if (!hasTexture && _emojiLabel != null)
            {
                string emoji = GetItemEmoji(item);
                _emojiLabel.Text = emoji;
                _emojiLabel.Visible = true;
                GD.Print($"[ShopItemCard] SetItem: showing emoji '{emoji}'");
            }

            if (_priceLabel != null)
            {
                if (item.Purchased)
                {
                    _priceLabel.Text = "已售出";
                    _priceLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                    _nameLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
                    GD.Print($"[ShopItemCard] SetItem: item is purchased");
                }
                else
                {
                    _priceLabel.Text = $"💰 {item.Price}";
                    _priceLabel.Modulate = new Color(1, 0.8f, 0);
                    _nameLabel.Modulate = new Color(1, 0.95f, 0.9f);
                    GD.Print($"[ShopItemCard] SetItem: item price set to {item.Price}");
                }
            }

            GD.Print($"[ShopItemCard] SetItem completed");
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
            GD.Print($"[ShopItemCard] OnMouseEntered: {Item?.Name}");
        }

        private void OnMouseExited()
        {
            _isHovered = false;
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate", Colors.White, 0.15f);
            GD.Print($"[ShopItemCard] OnMouseExited: {Item?.Name}");
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (!_clickEnabled)
                    {
                        GD.Print($"[ShopItemCard] Card click ignored: _clickEnabled={_clickEnabled}");
                        return;
                    }
                    GD.Print($"[ShopItemCard] Card clicked: {Item?.Name}");
                    OnCardClicked?.Invoke(Item);
                }
            }
        }

        public void SetClickEnabled(bool enabled)
        {
            _clickEnabled = enabled;
            GD.Print($"[ShopItemCard] SetClickEnabled: {enabled}, Name={Name}");
        }
    }
}
