using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Shop;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.HexMap
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
        private List<CardSelectionItem> _cardItems = new List<CardSelectionItem>();

        public System.Action OnEngravingCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            _titleLabel = GetNode<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _cardGrid = GetNode<GridContainer>("BackgroundPanel/VBoxContainer/CardScrollContainer/CardGridContainer");
            _confirmButton = GetNode<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ConfirmButton");
            _cancelButton = GetNode<Button>("BackgroundPanel/VBoxContainer/BottomContainer/CancelButton");

            _confirmButton.Pressed += OnConfirmPressed;
            _cancelButton.Pressed += OnCancelPressed;

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

            foreach (var child in _cardGrid.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var card in _availableCards)
            {
                var cardItem = CreateCardItem(card);
                if (cardItem != null)
                {
                    _cardGrid.AddChild(cardItem);
                    cardItem.SetCardData(card);
                    _cardItems.Add(cardItem);
                }
            }

            _confirmButton.Disabled = true;
            _titleLabel.Text = $"选择要刻印的卡牌 - {_engravingItem?.Name ?? "刻印"}";
            Visible = true;
        }

        private CardSelectionItem CreateCardItem(Card card)
        {
            var cardItem = CardSelectionItemScene.Instantiate<CardSelectionItem>();
            if (cardItem == null) return null;

            cardItem.Card = card;
            cardItem.OnSelected += OnCardItemSelected;
            return cardItem;
        }

        private void OnCardItemSelected(CardSelectionItem item)
        {
            foreach (var cardItem in _cardItems)
            {
                cardItem.SetSelected(cardItem == item);
            }

            _selectedCard = item.Card;
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
