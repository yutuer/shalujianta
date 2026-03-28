using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Shop;
using FishEatFish.UI.ShopItemCard;

namespace FishEatFish.UI.ShopUI
{
    public partial class ShopUI : Control
    {
        private const int RefreshCost = 5;

        private Control _shopContainer;
        private Control _shopItemsContainer;
        private Button _shopCloseButton;
        private Button _refreshButton;
        private Label _costLabel;

        private PackedScene _shopItemCardScene;

        private int _currentRefreshCount = 2;
        private const int MaxRefreshCount = 2;

        private FishEatFish.UI.EngravingDescriptionUI.EngravingDescriptionUI _engravingDescriptionUI;
        private FishEatFish.UI.ArtifactDescriptionUI.ArtifactDescriptionUI _artifactDescriptionUI;
        private FishEatFish.UI.EngravingCardSelectionUI.EngravingCardSelectionUI _engravingCardSelectionUI;

        public event Action<ShopItem> OnShopItemClicked;
        public event Action<ShopItem> OnEngravingItemConfirmed;
        public event Action<ShopItem> OnArtifactItemConfirmed;
        public event Action OnCloseClicked;
        public event Action<int, int> OnRefreshClicked;
        public event Action OnEngravingCompleted;

        public override void _Ready()
        {
            if (_shopItemCardScene == null)
            {
                _shopItemCardScene = GD.Load<PackedScene>("res://Scenes/UI/ShopItemCard.tscn");
            }

            InitializeNodes();
            InitializeSubComponents();
        }

        private void InitializeNodes()
        {
            _shopContainer = GetNodeOrNull<Control>("ShopContainer");
            if (_shopContainer != null)
            {
                _shopItemsContainer = GetNodeOrNull<Control>("ShopContainer/VBoxContainer/ShopItemsMargin/ShopItems");
                _shopCloseButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/HeaderBox/CloseButton");
                _refreshButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/RefreshBar/RefreshButton");
                _costLabel = GetNodeOrNull<Label>("ShopContainer/VBoxContainer/RefreshBar/CostLabel");

                if (_shopCloseButton != null)
                {
                    _shopCloseButton.Pressed += OnShopClosePressed;
                }

                if (_refreshButton != null)
                {
                    _refreshButton.Pressed += OnRefreshPressed;
                }

                UpdateRefreshUI();
            }
        }

        private void InitializeSubComponents()
        {
            _engravingDescriptionUI = GetNodeOrNull<FishEatFish.UI.EngravingDescriptionUI.EngravingDescriptionUI>("EngravingDescriptionUI");
            if (_engravingDescriptionUI != null)
            {
                _engravingDescriptionUI.Visible = false;
                GD.Print("[ShopUI] EngravingDescriptionUI initialized");
            }

            _artifactDescriptionUI = GetNodeOrNull<FishEatFish.UI.ArtifactDescriptionUI.ArtifactDescriptionUI>("ArtifactDescriptionUI");
            if (_artifactDescriptionUI != null)
            {
                _artifactDescriptionUI.Visible = false;
                _artifactDescriptionUI.OnPurchaseCompleted += OnArtifactPurchased;
                _artifactDescriptionUI.OnCancel += OnArtifactCancelled;
                GD.Print("[ShopUI] ArtifactDescriptionUI initialized");
            }

            _engravingCardSelectionUI = GetNodeOrNull<FishEatFish.UI.EngravingCardSelectionUI.EngravingCardSelectionUI>("EngravingCardSelectionUI");
            if (_engravingCardSelectionUI != null)
            {
                _engravingCardSelectionUI.Visible = false;
                _engravingCardSelectionUI.OnEngravingCompleted += OnEngravingSelectionCompleted;
                _engravingCardSelectionUI.OnCancel += OnEngravingSelectionCancelled;
                GD.Print("[ShopUI] EngravingCardSelectionUI initialized");
            }
        }

        public void SetRefreshCount(int count)
        {
            _currentRefreshCount = count;
            UpdateRefreshUI();
        }

        public int GetRefreshCount() => _currentRefreshCount;

        private void OnShopClosePressed()
        {
            OnCloseClicked?.Invoke();
        }

        private void OnRefreshPressed()
        {
            if (_currentRefreshCount <= 0)
            {
                return;
            }

            if (BlackMarkShopManager.Instance.BlackMarkCount < RefreshCost)
            {
                return;
            }

            OnRefreshClicked?.Invoke(_currentRefreshCount, MaxRefreshCount);
        }

        public bool TrySpendRefresh()
        {
            if (_currentRefreshCount <= 0)
            {
                return false;
            }

            if (BlackMarkShopManager.Instance.BlackMarkCount < RefreshCost)
            {
                return false;
            }

            bool spent = BlackMarkShopManager.Instance.SpendBlackMark(RefreshCost);
            if (spent)
            {
                _currentRefreshCount--;
                UpdateRefreshUI();
            }
            return spent;
        }

        private void UpdateRefreshUI()
        {
            if (_refreshButton != null)
            {
                _refreshButton.Text = $"刷新 {_currentRefreshCount}/{MaxRefreshCount}";
                _refreshButton.Disabled = _currentRefreshCount <= 0;
            }

            if (_costLabel != null)
            {
                _costLabel.Text = $"花费 {RefreshCost}点";
            }
        }

        public void RefreshShopItems(List<ShopItem> items)
        {
            if (_shopItemsContainer == null) return;

            var childrenToRemove = _shopItemsContainer.GetChildren().ToList();
            foreach (var child in childrenToRemove)
            {
                _shopItemsContainer.RemoveChild(child);
            }

            if (_shopItemCardScene == null)
            {
                GD.PrintErr("[ShopUI] _shopItemCardScene is null!");
                return;
            }

            foreach (var item in items)
            {
                var itemCard = CreateShopItemCard(item);
                if (itemCard != null)
                {
                    _shopItemsContainer.AddChild(itemCard);
                    itemCard.SetItem(item);
                    itemCard.OnCardClicked += OnShopItemCardClicked;
                }
            }
        }

        private FishEatFish.UI.ShopItemCard.ShopItemCard CreateShopItemCard(ShopItem item)
        {
            var card = (FishEatFish.UI.ShopItemCard.ShopItemCard)_shopItemCardScene.Instantiate();
            return card;
        }

        private void OnShopItemCardClicked(ShopItem item)
        {
            OnShopItemClicked?.Invoke(item);

            if (item.Purchased)
            {
                GD.Print("[ShopUI] Item already purchased, ignoring click");
                return;
            }

            if (item.ItemType == ShopItemType.Artifact)
            {
                ShowArtifactDescription(item);
            }
            else if (item.ItemType == ShopItemType.Engraving)
            {
                ShowEngravingDescription(item);
            }
        }

        public void ShowArtifactDescription(ShopItem item)
        {
            if (_artifactDescriptionUI != null)
            {
                SetInteractionEnabled(false);
                _artifactDescriptionUI.ShowArtifact(item);
            }
        }

        public void ShowEngravingDescription(ShopItem item)
        {
            if (_engravingDescriptionUI != null)
            {
                SetInteractionEnabled(false);
                _engravingDescriptionUI.ShowEngravingDescription(item, OnEngravingItemConfirmed, OnEngravingDescriptionCancelled);
            }
        }

        public void ShowEngravingCardSelection(ShopItem engravingItem, List<FishEatFish.Battle.Card.Card> availableCards)
        {
            if (_engravingCardSelectionUI != null)
            {
                _engravingCardSelectionUI.ShowCardSelection(engravingItem, availableCards);
            }
        }

        private void OnEngravingDescriptionCancelled()
        {
            SetInteractionEnabled(true);
        }

        private void OnArtifactPurchased()
        {
            OnArtifactItemConfirmed?.Invoke(null);
            SetInteractionEnabled(true);
        }

        private void OnArtifactCancelled()
        {
            SetInteractionEnabled(true);
        }

        private void OnEngravingSelectionCompleted()
        {
            HideEngravingCardSelection();
            OnEngravingCompleted?.Invoke();
        }

        private void OnEngravingSelectionCancelled()
        {
            HideEngravingCardSelection();
        }

        public void HideArtifactDescription()
        {
            if (_artifactDescriptionUI != null)
            {
                _artifactDescriptionUI.Visible = false;
            }
        }

        public void HideEngravingDescription()
        {
            if (_engravingDescriptionUI != null)
            {
                _engravingDescriptionUI.HideDescription();
            }
        }

        public void HideEngravingCardSelection()
        {
            if (_engravingCardSelectionUI != null)
            {
                _engravingCardSelectionUI.Visible = false;
            }
        }

        public void ShowShop()
        {
            if (_shopContainer != null)
            {
                _shopContainer.Visible = true;
            }
        }

        public void HideShop()
        {
            if (_shopContainer != null)
            {
                _shopContainer.Visible = false;
            }
            HideArtifactDescription();
            HideEngravingDescription();
            HideEngravingCardSelection();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (_shopCloseButton != null)
            {
                _shopCloseButton.Disabled = !enabled;
            }

            if (_refreshButton != null)
            {
                _refreshButton.Disabled = !enabled || _currentRefreshCount <= 0;
            }

            if (_shopItemsContainer != null)
            {
                foreach (var child in _shopItemsContainer.GetChildren())
                {
                    if (child is FishEatFish.UI.ShopItemCard.ShopItemCard card)
                    {
                        card.SetClickEnabled(enabled);
                    }
                }
            }
        }
    }
}
