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
        private TextureRect _engravingIcon;
        private Label _engravingNameLabel;
        private Label _engravingEffectLabel;

        private ShopItem _engravingItem;
        private Card _selectedCard;
        private List<Card> _availableCards = new List<Card>();
        private List<FishEatFish.UI.CardSelectionItem.CardSelectionItem> _cardItems = new List<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();

        public System.Action OnEngravingCompleted;
        public System.Action OnCancel;

        public override void _Ready()
        {
            GD.Print($"[EngravingCardSelectionUI] _Ready called");

            var bottomContainer = GetNodeOrNull<HBoxContainer>("BackgroundPanel/VBoxContainer/BottomContainer");
            GD.Print($"[EngravingCardSelectionUI] _Ready: bottomContainer={bottomContainer != null}");

            if (bottomContainer != null)
            {
                GD.Print($"[EngravingCardSelectionUI] _Ready: bottomContainer.GetChildCount={bottomContainer.GetChildCount()}");
                for (int i = 0; i < bottomContainer.GetChildCount(); i++)
                {
                    var child = bottomContainer.GetChild(i);
                    GD.Print($"[EngravingCardSelectionUI] _Ready: bottomContainer child[{i}] name={child.Name}, type={child.GetType().Name}");
                }
            }

            _titleLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
            _cardGrid = GetNodeOrNull<GridContainer>("BackgroundPanel/VBoxContainer/CardScrollMargin/CardScrollContainer/CardGrid");
            _confirmButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ConfirmButton");
            _cancelButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/CancelButton");
            _engravingIcon = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingIcon");
            _engravingNameLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingNameLabel");
            _engravingEffectLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingEffectLabel");

            GD.Print($"[EngravingCardSelectionUI] _Ready: _engravingIcon={_engravingIcon != null}");
            GD.Print($"[EngravingCardSelectionUI] _Ready: _engravingNameLabel={_engravingNameLabel != null}");
            GD.Print($"[EngravingCardSelectionUI] _Ready: _engravingEffectLabel={_engravingEffectLabel != null}");

            if (_confirmButton != null)
            {
                _confirmButton.Pressed += OnConfirmPressed;
            }

            if (_cancelButton != null)
            {
                _cancelButton.Pressed += OnCancelPressed;
            }

            Visible = false;

            GD.Print($"[EngravingCardSelectionUI] _Ready completed");
        }

        public void ShowCardSelection(ShopItem engravingItem, List<Card> availableCards)
        {
            GD.Print($"[EngravingCardSelectionUI] ShowCardSelection called: item={engravingItem?.Name}, cardCount={availableCards?.Count ?? 0}");

            _engravingItem = engravingItem;
            _selectedCard = null;
            _availableCards = availableCards ?? new List<Card>();

            foreach (var item in _cardItems)
            {
                item.QueueFree();
            }
            _cardItems.Clear();

            GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: _cardGrid={_cardGrid != null}");

            if (_cardGrid != null)
            {
                GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: clearing existing children, count={_cardGrid.GetChildCount()}");
                foreach (var child in _cardGrid.GetChildren())
                {
                    child.QueueFree();
                }
            }

            foreach (var card in _availableCards)
            {
                GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: creating card item for {card?.Name}");
                var cardItem = CreateCardItem(card);
                if (cardItem != null && _cardGrid != null)
                {
                    _cardGrid.AddChild(cardItem);
                    cardItem.SetCardData(card);
                    _cardItems.Add(cardItem);
                    GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: card item added to grid");
                }
                else
                {
                    GD.PrintErr($"[EngravingCardSelectionUI] ShowCardSelection: failed to add card item (cardItem={cardItem != null}, _cardGrid={_cardGrid != null})");
                }
            }

            GD.Print($"[EngravingCardSelectionUI] ShowCardSelection: total card items added={_cardItems.Count}");

            if (_confirmButton != null)
            {
                _confirmButton.Disabled = true;
            }
            if (_titleLabel != null)
            {
                _titleLabel.Text = $"选择要刻印的卡牌 - {_engravingItem?.Name ?? "刻印"}";
            }

            UpdateEngravingInfo();

            Visible = true;

            GD.Print($"[EngravingCardSelectionUI] ShowCardSelection completed");
        }

        private void UpdateEngravingInfo()
        {
            GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo called: item={_engravingItem?.Name}");
            GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingNameLabel={_engravingNameLabel != null}");
            GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingEffectLabel={_engravingEffectLabel != null}");
            GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingItem.EngravingEffect={_engravingItem?.EngravingEffect != null}");

            if (_engravingItem == null)
            {
                GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingItem is null!");
                if (_engravingNameLabel != null)
                    _engravingNameLabel.Text = "";
                if (_engravingEffectLabel != null)
                    _engravingEffectLabel.Text = "";
                return;
            }

            if (_engravingNameLabel != null)
            {
                _engravingNameLabel.Text = _engravingItem.Name ?? "未知刻印";
                GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: set name to '{_engravingNameLabel.Text}'");
            }
            else
            {
                GD.PrintErr($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingNameLabel is null!");
            }

            if (_engravingIcon != null && !string.IsNullOrEmpty(_engravingItem.Icon))
            {
                var texture = GD.Load<Texture2D>(_engravingItem.Icon);
                if (texture != null)
                {
                    _engravingIcon.Texture = texture;
                    GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: set icon texture");
                }
            }

            if (_engravingEffectLabel != null)
            {
                if (_engravingItem.EngravingEffect != null)
                {
                    string effectText = GetEffectDescription(_engravingItem.EngravingEffect);
                    _engravingEffectLabel.Text = effectText;
                    GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: set effect to '{effectText}'");
                }
                else
                {
                    _engravingEffectLabel.Text = "无效果";
                    GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo: EngravingEffect is null, set to '无效果'");
                }
            }
            else
            {
                GD.PrintErr($"[EngravingCardSelectionUI] UpdateEngravingInfo: _engravingEffectLabel is null!");
            }

            GD.Print($"[EngravingCardSelectionUI] UpdateEngravingInfo completed");
        }

        private string GetEffectDescription(EngravingEffect effect)
        {
            if (effect == null)
                return "";

            string valueStr = effect.value > 0 ? $"+{effect.value}" : effect.value.ToString();
            string percentStr = effect.value < 1 ? $"{(int)(effect.value * 100)}%" : valueStr;

            return effect.GetEffectType() switch
            {
                EngravingEffectType.AttackBuff => $"攻击力 {percentStr}",
                EngravingEffectType.DefenseBuff => $"防御力 {percentStr}",
                EngravingEffectType.HealthBuff => $"生命值 {percentStr}",
                EngravingEffectType.SpeedBuff => $"速度 {percentStr}",
                EngravingEffectType.CritBuff => $"暴击率 {percentStr}",
                EngravingEffectType.HealBuff => $"治疗效果 {percentStr}",
                EngravingEffectType.ShieldBuff => $"护盾 {percentStr}",
                EngravingEffectType.SlowOnAttack => $"攻击附带减速 {percentStr}",
                _ => $"效果: {valueStr}"
            };
        }

        private FishEatFish.UI.CardSelectionItem.CardSelectionItem CreateCardItem(Card card)
        {
            GD.Print($"[EngravingCardSelectionUI] CreateCardItem called: {card?.Name}");

            var cardItem = CardSelectionItemScene.Instantiate<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();
            if (cardItem == null)
            {
                GD.PrintErr("[EngravingCardSelectionUI] CreateCardItem: failed to instantiate card item!");
                return null;
            }

            cardItem.Card = card;
            cardItem.OnSelected += OnCardItemSelected;

            GD.Print($"[EngravingCardSelectionUI] CreateCardItem completed");
            return cardItem;
        }

        private void OnCardItemSelected(FishEatFish.UI.CardSelectionItem.CardSelectionItem item)
        {
            GD.Print($"[EngravingCardSelectionUI] OnCardItemSelected called: {item?.Card?.Name}");

            foreach (var cardItem in _cardItems)
            {
                cardItem.SetSelected(cardItem == item);
            }

            _selectedCard = item.Card;
            if (_confirmButton != null)
                _confirmButton.Disabled = false;

            GD.Print($"[EngravingCardSelectionUI] OnCardItemSelected completed");
        }

        private void OnConfirmPressed()
        {
            GD.Print($"[EngravingCardSelectionUI] OnConfirmPressed called");

            if (_selectedCard == null || _engravingItem == null)
            {
                GD.Print("[EngravingCardSelectionUI] OnConfirmPressed: no card selected or no engraving item");
                return;
            }

            if (!BlackMarkShopManager.Instance.CanAfford(_engravingItem))
            {
                GD.Print("[EngravingCardSelectionUI] OnConfirmPressed: cannot afford engraving");
                return;
            }

            GD.Print($"[EngravingCardSelectionUI] OnConfirmPressed: confirming engraving for card {_selectedCard.Name}");
            bool purchaseSuccess = BlackMarkShopManager.Instance.ConfirmEngraving(_selectedCard.CardId);
            if (purchaseSuccess)
            {
                Visible = false;
                OnEngravingCompleted?.Invoke();
                GD.Print("[EngravingCardSelectionUI] OnConfirmPressed: engraving completed successfully");
            }
            else
            {
                GD.Print("[EngravingCardSelectionUI] OnConfirmPressed: engraving failed");
            }

            GD.Print($"[EngravingCardSelectionUI] OnConfirmPressed completed");
        }

        private void OnCancelPressed()
        {
            GD.Print($"[EngravingCardSelectionUI] OnCancelPressed called");

            Visible = false;
            OnCancel?.Invoke();

            GD.Print($"[EngravingCardSelectionUI] OnCancelPressed completed");
        }
    }
}
