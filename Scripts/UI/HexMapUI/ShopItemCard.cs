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

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
            _iconRect = GetNode<TextureRect>("VBoxContainer/IconRect");
            _descLabel = GetNode<Label>("VBoxContainer/DescLabel");
            _priceLabel = GetNode<Label>("VBoxContainer/PriceLabel");
            _buyButton = GetNode<Button>("VBoxContainer/BuyButton");

            _buyButton.Pressed += OnBuyPressed;
        }

        public void SetItem(ShopItem item, bool canAfford)
        {
            Item = item;
            _nameLabel.Text = item.Name;
            _descLabel.Text = item.Description;
            _priceLabel.Text = $"💰 {item.Price}";

            if (item.Icon != null)
            {
                _iconRect.Texture = GD.Load<Texture2D>(item.Icon);
            }

            _buyButton.Disabled = !canAfford;
            _priceLabel.Modulate = canAfford ? new Color(1, 0.8f, 0) : new Color(0.5f, 0.5f, 0.5f);
        }

        private void OnBuyPressed()
        {
            OnBuyClicked?.Invoke(Item);
        }
    }
}
