using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Shop;
using FishEatFish.Battle.Card;
using FishEatFish.UI.CardSelectionItem;

namespace FishEatFish.UI.EngravingCardSelectionUI
{
    public partial class EngravingCardSelectionUI : Control
    {
        private static readonly PackedScene CardSelectionItemScene = GD.Load<PackedScene>("res://Scenes/UI/CardSelectionItem.tscn");

        private Label _titleLabel;
        private GridContainer _cardGrid;
        private Button _confirmButton;
        private Button _cancelButton;

        private ShopItem _engravingItem;
        private Card _selectedCard;
        private List<Card> _availableCards = new List<Card>();
        private List<FishEatFish.UI.CardSelectionItem.CardSelectionItem> _cardItems = new List<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();

        public System.Action OnEngravingCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            _titleLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _cardGrid = GetNodeOrNull<GridContainer>("BackgroundPanel/VBoxContainer/CardScrollMargin/CardScrollContainer/CardGridContainer/CardGrid");
            _confirmButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ConfirmButton");
            _cancelButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/CancelButton");

            if (_confirmButton != null)
            {
                _confirmButton.Pressed += OnConfirmPressed;
            }

            if (_cancelButton != null)
            {
                _cancelButton.Pressed += OnCancelPressed;
            }

            Visible = false;
        }

        public void ShowCardSelection(ShopItem engravingItem, List<Card> availableCards)
        {
            _engravingItem = engravingItem;
            _selectedCard = null;
            _availableCards = availableCards ?? new List<Card>();

            foreach (var item in _cardItems)
            {
                item.QueueFree();
            }
            _cardItems.Clear();

            if (_cardGrid != null)
            {
                foreach (var child in _cardGrid.GetChildren())
                {
                    child.QueueFree();
                }
            }

            foreach (var card in _availableCards)
            {
                var cardItem = CreateCardItem(card);
                if (cardItem != null && _cardGrid != null)
                {
                    _cardGrid.AddChild(cardItem);
                    cardItem.SetCardData(card);
                    _cardItems.Add(cardItem);
                }
            }

            if (_confirmButton != null)
                _confirmButton.Disabled = true;
            if (_titleLabel != null)
                _titleLabel.Text = $"选择要刻印的卡牌 - {_engravingItem?.Name ?? "刻印"}";
            Visible = true;
        }

        private FishEatFish.UI.CardSelectionItem.CardSelectionItem CreateCardItem(Card card)
        {
            var cardItem = CardSelectionItemScene.Instantiate<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();
            if (cardItem == null) return null;

            cardItem.Card = card;
            cardItem.OnSelected += OnCardItemSelected;
            return cardItem;
        }

        private void OnCardItemSelected(FishEatFish.UI.CardSelectionItem.CardSelectionItem item)
        {
            foreach (var cardItem in _cardItems)
            {
                cardItem.SetSelected(cardItem == item);
            }

            _selectedCard = item.Card;
            if (_confirmButton != null)
                _confirmButton.Disabled = false;
        }

        private void OnConfirmPressed()
        {
            if (_selectedCard == null || _engravingItem == null) return;

            if (!BlackMarkShopManager.Instance.CanAfford(_engravingItem)) return;

            bool purchaseSuccess = BlackMarkShopManager.Instance.ConfirmEngraving(_selectedCard.CardId);
            if (purchaseSuccess)
            {
                Visible = false;
                OnEngravingCompleted?.Invoke();
            }
        }

        private void OnCancelPressed()
        {
            Visible = false;
            OnCancel?.Invoke();
        }
    }
}
