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

        public event Action<ShopItem> OnEngravingItemConfirmed;
        public event Action<ShopItem> OnArtifactItemConfirmed;
        public event Action OnCloseClicked;
        public event Action<int, int> OnRefreshClicked;
        public event Action OnEngravingCompleted;

        public override void _Ready()
        {
            GD.Print($"[ShopUI] _Ready called");

            if (_shopItemCardScene == null)
            {
                _shopItemCardScene = GD.Load<PackedScene>("res://Scenes/UI/ShopItemCard.tscn");
            }

            InitializeNodes();
            InitializeSubComponents();

            GD.Print($"[ShopUI] _Ready completed");
        }

        private void InitializeNodes()
        {
            GD.Print($"[ShopUI] InitializeNodes called");
            GD.Print($"[ShopUI] InitializeNodes: this.Name={Name}");
            GD.Print($"[ShopUI] InitializeNodes: this.GetChildCount={GetChildCount()}");

            for (int i = 0; i < GetChildCount(); i++)
            {
                var child = GetChild(i);
                GD.Print($"[ShopUI] InitializeNodes: child[{i}] name={child.Name}, type={child.GetType().Name}");
            }

            _shopContainer = GetNodeOrNull<Control>("ShopContainer");
            GD.Print($"[ShopUI] InitializeNodes: _shopContainer={_shopContainer}");

            if (_shopContainer != null)
            {
                var vbox = GetNodeOrNull<Control>("ShopContainer/VBoxContainer");
                GD.Print($"[ShopUI] InitializeNodes: VBoxContainer={vbox}");

                var margin = GetNodeOrNull<Control>("ShopContainer/VBoxContainer/ShopItemsMargin");
                GD.Print($"[ShopUI] InitializeNodes: ShopItemsMargin={margin}");

                _shopItemsContainer = GetNodeOrNull<Control>("ShopContainer/VBoxContainer/ShopItemsMargin/ShopItems");
                GD.Print($"[ShopUI] InitializeNodes: _shopItemsContainer={_shopItemsContainer}");

                if (_shopItemsContainer == null)
                {
                    GD.PrintErr("[ShopUI] InitializeNodes: Failed to find ShopItems! Trying alternative paths...");
                    var shopItemsDirect = GetNodeOrNull<Control>("ShopItems");
                    GD.Print($"[ShopUI] InitializeNodes: ShopItems (direct)={shopItemsDirect}");
                }

                _shopCloseButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/HeaderBox/CloseButton");
                GD.Print($"[ShopUI] InitializeNodes: _shopCloseButton={_shopCloseButton}");

                _refreshButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/RefreshBar/RefreshButton");
                GD.Print($"[ShopUI] InitializeNodes: _refreshButton={_refreshButton}");

                _costLabel = GetNodeOrNull<Label>("ShopContainer/VBoxContainer/RefreshBar/CostLabel");
                GD.Print($"[ShopUI] InitializeNodes: _costLabel={_costLabel}");

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
            else
            {
                GD.PrintErr("[ShopUI] InitializeNodes: _shopContainer is null!");
            }

            GD.Print($"[ShopUI] InitializeNodes completed");
        }

        private void InitializeSubComponents()
        {
            GD.Print($"[ShopUI] InitializeSubComponents called");

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

            GD.Print($"[ShopUI] InitializeSubComponents completed");
        }

        public void SetRefreshCount(int count)
        {
            GD.Print($"[ShopUI] SetRefreshCount called: count={count}");
            _currentRefreshCount = count;
            UpdateRefreshUI();
            GD.Print($"[ShopUI] SetRefreshCount completed");
        }

        public int GetRefreshCount() => _currentRefreshCount;

        private void OnShopClosePressed()
        {
            GD.Print($"[ShopUI] OnShopClosePressed called");
            OnCloseClicked?.Invoke();
            GD.Print($"[ShopUI] OnShopClosePressed completed");
        }

        private void OnRefreshPressed()
        {
            GD.Print($"[ShopUI] OnRefreshPressed called");

            if (_currentRefreshCount <= 0)
            {
                GD.Print($"[ShopUI] OnRefreshPressed: no refreshes left");
                return;
            }

            if (BlackMarkShopManager.Instance.BlackMarkCount < RefreshCost)
            {
                GD.Print($"[ShopUI] OnRefreshPressed: not enough black marks");
                return;
            }

            OnRefreshClicked?.Invoke(_currentRefreshCount, MaxRefreshCount);
            GD.Print($"[ShopUI] OnRefreshPressed completed");
        }

        public bool TrySpendRefresh()
        {
            GD.Print($"[ShopUI] TrySpendRefresh called");

            if (_currentRefreshCount <= 0)
            {
                GD.Print($"[ShopUI] TrySpendRefresh: no refreshes left, returning false");
                return false;
            }

            if (BlackMarkShopManager.Instance.BlackMarkCount < RefreshCost)
            {
                GD.Print($"[ShopUI] TrySpendRefresh: not enough black marks, returning false");
                return false;
            }

            bool spent = BlackMarkShopManager.Instance.SpendBlackMark(RefreshCost);
            if (spent)
            {
                _currentRefreshCount--;
                UpdateRefreshUI();
            }

            GD.Print($"[ShopUI] TrySpendRefresh completed: spent={spent}");
            return spent;
        }

        private void UpdateRefreshUI()
        {
            GD.Print($"[ShopUI] UpdateRefreshUI called");

            if (_refreshButton != null)
            {
                _refreshButton.Text = $"刷新 {_currentRefreshCount}/{MaxRefreshCount}";
                _refreshButton.Disabled = _currentRefreshCount <= 0;
            }

            if (_costLabel != null)
            {
                _costLabel.Text = $"花费 {RefreshCost}点";
            }

            GD.Print($"[ShopUI] UpdateRefreshUI completed");
        }

        public void RefreshShopItems(List<ShopItem> items)
        {
            GD.Print($"[ShopUI] RefreshShopItems called: itemCount={items?.Count ?? 0}");

            if (_shopItemsContainer == null)
            {
                GD.PrintErr("[ShopUI] RefreshShopItems: _shopItemsContainer is null!");
                return;
            }

            var childrenToRemove = _shopItemsContainer.GetChildren().ToList();
            GD.Print($"[ShopUI] RefreshShopItems: removing {childrenToRemove.Count} existing children");
            foreach (var child in childrenToRemove)
            {
                _shopItemsContainer.RemoveChild(child);
            }

            if (_shopItemCardScene == null)
            {
                GD.PrintErr("[ShopUI] RefreshShopItems: _shopItemCardScene is null!");
                return;
            }

            foreach (var item in items)
            {
                GD.Print($"[ShopUI] RefreshShopItems: creating card for {item.Name}");
                var itemCard = CreateShopItemCard(item);
                if (itemCard != null)
                {
                    _shopItemsContainer.AddChild(itemCard);
                    itemCard.SetItem(item);
                    itemCard.OnCardClicked += OnShopItemCardClicked;
                }
            }

            GD.Print($"[ShopUI] RefreshShopItems completed");
        }

        private FishEatFish.UI.ShopItemCard.ShopItemCard CreateShopItemCard(ShopItem item)
        {
            GD.Print($"[ShopUI] CreateShopItemCard called: {item.Name}");
            var card = (FishEatFish.UI.ShopItemCard.ShopItemCard)_shopItemCardScene.Instantiate();
            GD.Print($"[ShopUI] CreateShopItemCard completed");
            return card;
        }

        private void OnShopItemCardClicked(ShopItem item)
        {
            GD.Print($"[ShopUI] OnShopItemCardClicked called: {item.Name}, Purchased={item.Purchased}");

            if (item.Purchased)
            {
                GD.Print("[ShopUI] OnShopItemCardClicked: item already purchased, ignoring");
                return;
            }

            if (item.ItemType == ShopItemType.Artifact)
            {
                GD.Print("[ShopUI] OnShopItemCardClicked: showing artifact description");
                ShowArtifactDescription(item);
            }
            else if (item.ItemType == ShopItemType.Engraving)
            {
                GD.Print("[ShopUI] OnShopItemCardClicked: showing engraving description");
                ShowEngravingDescription(item);
            }

            GD.Print($"[ShopUI] OnShopItemCardClicked completed");
        }

        public void ShowArtifactDescription(ShopItem item)
        {
            GD.Print($"[ShopUI] ShowArtifactDescription called: {item.Name}");

            if (_artifactDescriptionUI != null)
            {
                SetInteractionEnabled(false);
                _artifactDescriptionUI.ShowArtifact(item);
            }

            GD.Print($"[ShopUI] ShowArtifactDescription completed");
        }

        public void ShowEngravingDescription(ShopItem item)
        {
            GD.Print($"[ShopUI] ShowEngravingDescription called: {item.Name}");

            if (_engravingDescriptionUI != null)
            {
                SetInteractionEnabled(false);
                _engravingDescriptionUI.ShowEngravingDescription(item, OnEngravingItemConfirmed, OnEngravingDescriptionCancelled);
            }

            GD.Print($"[ShopUI] ShowEngravingDescription completed");
        }

        public void ShowEngravingCardSelection(ShopItem engravingItem, List<FishEatFish.Battle.Card.Card> availableCards)
        {
            GD.Print($"[ShopUI] ShowEngravingCardSelection called: item={engravingItem?.Name}, cardCount={availableCards?.Count ?? 0}");

            if (_engravingCardSelectionUI != null)
            {
                _engravingCardSelectionUI.ShowCardSelection(engravingItem, availableCards);
            }

            GD.Print($"[ShopUI] ShowEngravingCardSelection completed");
        }

        private void OnEngravingDescriptionCancelled()
        {
            GD.Print($"[ShopUI] OnEngravingDescriptionCancelled called");
            SetInteractionEnabled(true);
            GD.Print($"[ShopUI] OnEngravingDescriptionCancelled completed");
        }

        private void OnArtifactPurchased()
        {
            GD.Print($"[ShopUI] OnArtifactPurchased called");
            OnArtifactItemConfirmed?.Invoke(null);
            SetInteractionEnabled(true);
            GD.Print($"[ShopUI] OnArtifactPurchased completed");
        }

        private void OnArtifactCancelled()
        {
            GD.Print($"[ShopUI] OnArtifactCancelled called");
            SetInteractionEnabled(true);
            GD.Print($"[ShopUI] OnArtifactCancelled completed");
        }

        private void OnEngravingSelectionCompleted()
        {
            GD.Print($"[ShopUI] OnEngravingSelectionCompleted called");
            HideEngravingCardSelection();
            OnEngravingCompleted?.Invoke();
            GD.Print($"[ShopUI] OnEngravingSelectionCompleted completed");
        }

        private void OnEngravingSelectionCancelled()
        {
            GD.Print($"[ShopUI] OnEngravingSelectionCancelled called");
            HideEngravingCardSelection();
            GD.Print($"[ShopUI] OnEngravingSelectionCancelled completed");
        }

        public void HideArtifactDescription()
        {
            GD.Print($"[ShopUI] HideArtifactDescription called");

            if (_artifactDescriptionUI != null)
            {
                _artifactDescriptionUI.Visible = false;
            }

            GD.Print($"[ShopUI] HideArtifactDescription completed");
        }

        public void HideEngravingDescription()
        {
            GD.Print($"[ShopUI] HideEngravingDescription called");

            if (_engravingDescriptionUI != null)
            {
                _engravingDescriptionUI.HideDescription();
            }

            GD.Print($"[ShopUI] HideEngravingDescription completed");
        }

        public void HideEngravingCardSelection()
        {
            GD.Print($"[ShopUI] HideEngravingCardSelection called");

            if (_engravingCardSelectionUI != null)
            {
                _engravingCardSelectionUI.Visible = false;
            }

            GD.Print($"[ShopUI] HideEngravingCardSelection completed");
        }

        public void ShowShop()
        {
            GD.Print($"[ShopUI] ShowShop called");

            if (_shopContainer != null)
            {
                _shopContainer.Visible = true;
            }

            GD.Print($"[ShopUI] ShowShop completed");
        }

        public void HideShop()
        {
            GD.Print($"[ShopUI] HideShop called");

            if (_shopContainer != null)
            {
                _shopContainer.Visible = false;
            }
            HideArtifactDescription();
            HideEngravingDescription();
            HideEngravingCardSelection();

            GD.Print($"[ShopUI] HideShop completed");
        }

        public void SetInteractionEnabled(bool enabled)
        {
            GD.Print($"[ShopUI] SetInteractionEnabled called: enabled={enabled}");

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

            GD.Print($"[ShopUI] SetInteractionEnabled completed");
        }
    }
}
