using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Shop;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.HexMap
{
    public partial class EngravingCardSelectionUI : Control
    {
        private Label _titleLabel;
        private GridContainer _cardGrid;
        private Button _confirmButton;
        private Button _cancelButton;

        private ShopItem _engravingItem;
        private CardData _selectedCardData;
        private List<CardData> _availableCards = new List<CardData>();
        private List<CardSelectionItem> _cardItems = new List<CardSelectionItem>();

        public System.Action OnEngravingCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            GD.Print($"[EngravingCardSelectionUI] _Ready called");

            _titleLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _cardGrid = GetNode<GridContainer>("BackgroundPanel/VBoxContainer/CardScrollContainer/CardGridContainer");
            _confirmButton = GetNode<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ConfirmButton");
            _cancelButton = GetNode<Button>("BackgroundPanel/VBoxContainer/BottomContainer/CancelButton");

            _confirmButton.Pressed += OnConfirmPressed;
            _cancelButton.Pressed += OnCancelPressed;

            Visible = false;
            GD.Print($"[EngravingCardSelectionUI] _Ready completed");
        }

        public void ShowCardSelection(ShopItem engravingItem, List<CardData> availableCards)
        {
            GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: engraving={engravingItem?.Name}, cardCount={availableCards?.Count ?? 0}");

            _engravingItem = engravingItem;
            _selectedCardData = null;
            _availableCards = availableCards ?? new List<CardData>();

            foreach (var item in _cardItems)
            {
                item.QueueFree();
            }
            _cardItems.Clear();

            foreach (var child in _cardGrid.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var cardData in _availableCards)
            {
                var cardItem = CreateCardItem(cardData);
                _cardGrid.AddChild(cardItem);
                _cardItems.Add(cardItem);
            }

            _confirmButton.Disabled = true;

            _titleLabel.Text = $"选择要刻印的卡牌 - {_engravingItem?.Name ?? "刻印"}";

            Visible = true;
            GD.Print($"[EngravingCardSelectionUI] Showing {availableCards?.Count ?? 0} cards");
        }

        private CardSelectionItem CreateCardItem(CardData cardData)
        {
            var container = new PanelContainer();
            container.CustomMinimumSize = new Vector2(120, 160);

            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.2f, 0.2f, 0.3f);
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = new Color(0.4f, 0.4f, 0.5f);
            styleBox.CornerRadiusTopLeft = 8;
            styleBox.CornerRadiusTopRight = 8;
            styleBox.CornerRadiusBottomRight = 8;
            styleBox.CornerRadiusBottomLeft = 8;
            container.AddThemeStyleboxOverride("panel", styleBox);

            var vbox = new VBoxContainer();
            vbox.LayoutMode = 2;
            vbox.Alignment = VBoxContainer.AlignmentMode.Center;
            container.AddChild(vbox);

            var nameLabel = new Label();
            nameLabel.Text = cardData?.Name ?? "Unknown";
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            nameLabel.CustomMinimumSize = new Vector2(110, 30);
            vbox.AddChild(nameLabel);

            var costLabel = new Label();
            costLabel.Text = $"费用: {cardData?.Cost ?? 0}";
            costLabel.HorizontalAlignment = HorizontalAlignment.Center;
            costLabel.AddThemeFontSizeOverride("font_size", 10);
            costLabel.Modulate = new Color(0.7f, 0.7f, 0.9f);
            vbox.AddChild(costLabel);

            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 10);
            vbox.AddChild(spacer);

            var effectLabel = new Label();
            effectLabel.Text = GetCardEffectText(cardData);
            effectLabel.HorizontalAlignment = HorizontalAlignment.Center;
            effectLabel.AddThemeFontSizeOverride("font_size", 10);
            effectLabel.Modulate = new Color(0.9f, 0.9f, 0.8f);
            effectLabel.CustomMinimumSize = new Vector2(110, 80);
            effectLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            vbox.AddChild(effectLabel);

            var cardItem = new CardSelectionItem();
            cardItem.CardData = cardData;
            cardItem.AddChild(container);
            cardItem.OnSelected += OnCardItemSelected;

            return cardItem;
        }

        private string GetCardEffectText(CardData cardData)
        {
            if (cardData == null) return "";

            var parts = new List<string>();
            if (cardData.Damage > 0) parts.Add($"伤害: {cardData.Damage}");
            if (cardData.ShieldGain > 0) parts.Add($"护盾: {cardData.ShieldGain}");
            if (cardData.HealAmount > 0) parts.Add($"治疗: {cardData.HealAmount}");
            if (cardData.EnergyGain > 0) parts.Add($"能量: +{cardData.EnergyGain}");

            return string.Join("\n", parts);
        }

        private void OnCardItemSelected(CardSelectionItem item)
        {
            GD.Print($"[EngravingCardSelectionUI] Card selected: {item.CardData?.Name}");

            foreach (var cardItem in _cardItems)
            {
                cardItem.SetSelected(cardItem == item);
            }

            _selectedCardData = item.CardData;
            _confirmButton.Disabled = false;
        }

        private void OnConfirmPressed()
        {
            if (_selectedCardData == null || _engravingItem == null)
            {
                GD.Print($"[EngravingCardSelectionUI] Cannot confirm: selectedCard={_selectedCardData}, engraving={_engravingItem}");
                return;
            }

            GD.Print($"[EngravingCardSelectionUI] OnConfirmPressed: card={_selectedCardData.Name}, engraving={_engravingItem.Name}");

            if (!BlackMarkShopManager.Instance.CanAfford(_engravingItem))
            {
                GD.Print($"[EngravingCardSelectionUI] Not enough black marks!");
                return;
            }

            bool purchaseSuccess = BlackMarkShopManager.Instance.ConfirmEngraving(_selectedCardData.CardId);
            if (purchaseSuccess)
            {
                GD.Print($"[EngravingCardSelectionUI] Engraving successful!");
                Visible = false;
                OnEngravingCompleted?.Invoke();
            }
            else
            {
                GD.Print($"[EngravingCardSelectionUI] Engraving failed!");
            }
        }

        private void OnCancelPressed()
        {
            GD.Print($"[EngravingCardSelectionUI] OnCancelPressed");
            Visible = false;
            OnCancel?.Invoke();
        }
    }

    public partial class CardSelectionItem : Control
    {
        public CardData CardData { get; set; }
        public System.Action<CardSelectionItem> OnSelected;
        private bool _isSelected = false;
        private PanelContainer _container;

        public override void _Ready()
        {
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    OnSelected?.Invoke(this);
                }
            }
        }

        private void OnMouseEntered()
        {
            if (_container == null)
            {
                _container = GetChild<PanelContainer>(0);
            }

            var style = (StyleBoxFlat)_container.GetThemeStylebox("panel");
            if (style != null && !_isSelected)
            {
                style.BorderColor = new Color(0.8f, 0.7f, 0.3f);
            }
        }

        private void OnMouseExited()
        {
            if (_container == null)
            {
                _container = GetChild<PanelContainer>(0);
            }

            var style = (StyleBoxFlat)_container.GetThemeStylebox("panel");
            if (style != null && !_isSelected)
            {
                style.BorderColor = new Color(0.4f, 0.4f, 0.5f);
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_container == null)
            {
                _container = GetChild<PanelContainer>(0);
            }

            var style = (StyleBoxFlat)_container.GetThemeStylebox("panel");
            if (style != null)
            {
                if (selected)
                {
                    style.BorderColor = new Color(0.3f, 0.9f, 0.3f);
                    style.BorderWidthLeft = 3;
                    style.BorderWidthTop = 3;
                    style.BorderWidthRight = 3;
                    style.BorderWidthBottom = 3;
                }
                else
                {
                    style.BorderColor = new Color(0.4f, 0.4f, 0.5f);
                    style.BorderWidthLeft = 2;
                    style.BorderWidthTop = 2;
                    style.BorderWidthRight = 2;
                    style.BorderWidthBottom = 2;
                }
            }
        }
    }
}
