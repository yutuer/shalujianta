using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class EngravingCardSlot : PanelContainer
    {
        private Label _nameLabel;
        private Button _selectButton;

        public string CardId { get; private set; }
        public System.Action<EngravingCardSlot, string> OnSelected;

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
            _selectButton = GetNode<Button>("VBoxContainer/SelectButton");

            _selectButton.Pressed += OnSelectPressed;
        }

        public void SetCard(string cardId, string cardName)
        {
            CardId = cardId;
            _nameLabel.Text = cardName;
        }

        public void SetSelected(bool selected)
        {
            Modulate = selected ? new Color(1, 1, 0) : Colors.White;
        }

        private void OnSelectPressed()
        {
            OnSelected?.Invoke(this, CardId);
        }
    }
}
