using System;
using System.Collections.Generic;
using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.HexMap
{
    public partial class CardSelectionItem : Control
    {
        private Label _nameLabel;
        private Label _costLabel;
        private Label _effectLabel;
        private PanelContainer _panelContainer;
        private StyleBoxFlat _normalStyle;
        private StyleBoxFlat _selectedStyle;
        private StyleBoxFlat _hoverStyle;

        public Card Card { get; set; }
        public System.Action<CardSelectionItem> OnSelected;

        private bool _isSelected = false;

        public override void _Ready()
        {
            _panelContainer = GetNode<PanelContainer>("PanelContainer");
            if (_panelContainer == null) return;

            _nameLabel = _panelContainer.GetNode<Label>("VBoxContainer/NameLabel");
            _costLabel = _panelContainer.GetNode<Label>("VBoxContainer/CostLabel");
            _effectLabel = _panelContainer.GetNode<Label>("VBoxContainer/EffectLabel");

            _normalStyle = new StyleBoxFlat();
            _normalStyle.BgColor = new Color(0.2f, 0.2f, 0.3f);
            _normalStyle.BorderWidthLeft = 2;
            _normalStyle.BorderWidthTop = 2;
            _normalStyle.BorderWidthRight = 2;
            _normalStyle.BorderWidthBottom = 2;
            _normalStyle.BorderColor = new Color(0.4f, 0.4f, 0.5f);
            _normalStyle.CornerRadiusTopLeft = 8;
            _normalStyle.CornerRadiusTopRight = 8;
            _normalStyle.CornerRadiusBottomRight = 8;
            _normalStyle.CornerRadiusBottomLeft = 8;

            _selectedStyle = new StyleBoxFlat();
            _selectedStyle.BgColor = _normalStyle.BgColor;
            _selectedStyle.BorderWidthLeft = 3;
            _selectedStyle.BorderWidthTop = 3;
            _selectedStyle.BorderWidthRight = 3;
            _selectedStyle.BorderWidthBottom = 3;
            _selectedStyle.BorderColor = new Color(0.3f, 0.9f, 0.3f);
            _selectedStyle.CornerRadiusTopLeft = 8;
            _selectedStyle.CornerRadiusTopRight = 8;
            _selectedStyle.CornerRadiusBottomRight = 8;
            _selectedStyle.CornerRadiusBottomLeft = 8;

            _hoverStyle = new StyleBoxFlat();
            _hoverStyle.BgColor = _normalStyle.BgColor;
            _hoverStyle.BorderWidthLeft = 2;
            _hoverStyle.BorderWidthTop = 2;
            _hoverStyle.BorderWidthRight = 2;
            _hoverStyle.BorderWidthBottom = 2;
            _hoverStyle.BorderColor = new Color(0.8f, 0.7f, 0.3f);
            _hoverStyle.CornerRadiusTopLeft = 8;
            _hoverStyle.CornerRadiusTopRight = 8;
            _hoverStyle.CornerRadiusBottomRight = 8;
            _hoverStyle.CornerRadiusBottomLeft = 8;

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        public void SetCardData(Card card)
        {
            EnsureNodeReferences();

            if (_nameLabel != null)
                _nameLabel.Text = card?.Name ?? "Unknown";

            if (_costLabel != null)
                _costLabel.Text = $"费用: {card?.Cost ?? 0}";

            if (_effectLabel != null)
                _effectLabel.Text = GetCardEffectText(card);
        }

        private void EnsureNodeReferences()
        {
            if (_panelContainer == null)
            {
                _panelContainer = GetNode<PanelContainer>("PanelContainer");
                if (_panelContainer != null)
                {
                    _nameLabel = _panelContainer.GetNode<Label>("VBoxContainer/NameLabel");
                    _costLabel = _panelContainer.GetNode<Label>("VBoxContainer/CostLabel");
                    _effectLabel = _panelContainer.GetNode<Label>("VBoxContainer/EffectLabel");
                }
            }
        }

        private string GetCardEffectText(Card card)
        {
            if (card == null) return "";

            var parts = new List<string>();
            if (card.Damage > 0) parts.Add($"伤害: {card.Damage}");
            if (card.ShieldGain > 0) parts.Add($"护盾: {card.ShieldGain}");
            if (card.HealAmount > 0) parts.Add($"治疗: {card.HealAmount}");
            if (card.EnergyGain > 0) parts.Add($"能量: +{card.EnergyGain}");

            return string.Join("\n", parts);
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
            if (!_isSelected && _hoverStyle != null && _panelContainer != null)
            {
                _panelContainer.AddThemeStyleboxOverride("panel", _hoverStyle);
            }
        }

        private void OnMouseExited()
        {
            if (!_isSelected && _normalStyle != null && _panelContainer != null)
            {
                _panelContainer.AddThemeStyleboxOverride("panel", _normalStyle);
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_panelContainer == null) return;

            if (selected && _selectedStyle != null)
            {
                _panelContainer.AddThemeStyleboxOverride("panel", _selectedStyle);
            }
            else if (_normalStyle != null)
            {
                _panelContainer.AddThemeStyleboxOverride("panel", _normalStyle);
            }
        }
    }
}
