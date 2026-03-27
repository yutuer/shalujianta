using System;
using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.HexMap
{
    public partial class ShopItemCard : PanelContainer
    {
        private Label _nameLabel;
        private TextureRect _iconRect;
        private Label _descLabel;
        private Label _priceLabel;
        private Button _buyButton;

        public ShopItem Item { get; private set; }
        public System.Action<ShopItem> OnBuyClicked;

        private bool _isReady = false;

        public override void _Ready()
        {
            GD.Print($"[ShopItemCard] _Ready called, Name={Name}, Type={GetType()}");

            try
            {
                var vbox = GetNodeOrNull("VBoxContainer");
                GD.Print($"[ShopItemCard] _Ready: VBoxContainer={vbox}");

                _nameLabel = GetNodeOrNull<Label>("VBoxContainer/NameLabel");
                _iconRect = GetNodeOrNull<TextureRect>("VBoxContainer/IconRect");
                _descLabel = GetNodeOrNull<Label>("VBoxContainer/DescLabel");
                _priceLabel = GetNodeOrNull<Label>("VBoxContainer/PriceLabel");
                _buyButton = GetNodeOrNull<Button>("VBoxContainer/BuyButton");

                GD.Print($"[ShopItemCard] _Ready: labels found, name={_nameLabel}, icon={_iconRect}, desc={_descLabel}, price={_priceLabel}, button={_buyButton}");

                if (_buyButton != null)
                {
                    _buyButton.Pressed += OnBuyPressed;
                }

                _isReady = true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ShopItemCard] _Ready exception: {ex.Message}\n{ex.StackTrace}");
            }

            GD.Print($"[ShopItemCard] _Ready completed");
        }

        public void SetItem(ShopItem item, bool canAfford)
        {
            GD.Print($"[ShopItemCard] SetItem called: item={item.Name}, canAfford={canAfford}, _isReady={_isReady}");
            Item = item;

            if (!_isReady)
            {
                GD.PrintErr("[ShopItemCard] SetItem called before _Ready! This should not happen.");
                return;
            }

            GD.Print($"[ShopItemCard] SetItem: _nameLabel={_nameLabel}");
            _nameLabel.Text = item.Name;
            GD.Print($"[ShopItemCard] SetItem: name set to '{item.Name}'");

            GD.Print($"[ShopItemCard] SetItem: _descLabel={_descLabel}");
            _descLabel.Text = item.Description;
            GD.Print($"[ShopItemCard] SetItem: desc set");

            GD.Print($"[ShopItemCard] SetItem: _priceLabel={_priceLabel}");
            _priceLabel.Text = $"💰 {item.Price}";
            GD.Print($"[ShopItemCard] SetItem: price set to {item.Price}");

            GD.Print($"[ShopItemCard] SetItem: icon path = {item.Icon}");
            if (!string.IsNullOrEmpty(item.Icon) && GD.Load<Texture2D>(item.Icon) != null)
            {
                GD.Print($"[ShopItemCard] SetItem: loading icon from {item.Icon}");
                _iconRect.Texture = GD.Load<Texture2D>(item.Icon);
                GD.Print($"[ShopItemCard] SetItem: icon loaded");
            }
            else
            {
                GD.Print($"[ShopItemCard] SetItem: icon is null or invalid, skipping");
            }

            GD.Print($"[ShopItemCard] SetItem: _buyButton={_buyButton}");
            _buyButton.Disabled = !canAfford;
            _priceLabel.Modulate = canAfford ? new Color(1, 0.8f, 0) : new Color(0.5f, 0.5f, 0.5f);
            GD.Print($"[ShopItemCard] SetItem completed, button disabled={!canAfford}");
        }

        private void OnBuyPressed()
        {
            GD.Print($"[ShopItemCard] OnBuyPressed, item={Item?.Name}");
            OnBuyClicked?.Invoke(Item);
        }
    }
}
