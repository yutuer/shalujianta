using Godot;

namespace FishEatFish.UI.TeleportDialog
{
    public partial class TeleportDialog : PanelContainer
    {
        private Button _confirmButton;
        private Button _cancelButton;

        public System.Action OnConfirmPressed;
        public System.Action OnCancelPressed;

        public override void _Ready()
        {
            _confirmButton = GetNodeOrNull<Button>("VBoxContainer/ButtonBox/ConfirmButton");
            _cancelButton = GetNodeOrNull<Button>("VBoxContainer/ButtonBox/CancelButton");

            if (_confirmButton != null)
            {
                _confirmButton.Pressed += OnConfirmButtonPressed;
            }

            if (_cancelButton != null)
            {
                _cancelButton.Pressed += OnCancelButtonPressed;
            }
        }

        public new void Show()
        {
            Visible = true;
        }

        public new void Hide()
        {
            Visible = false;
        }

        private void OnConfirmButtonPressed()
        {
            GD.Print($"[TeleportDialog] OnConfirmButtonPressed called");
            OnConfirmPressed?.Invoke();
            GD.Print($"[TeleportDialog] OnConfirmButtonPressed completed");
        }

        private void OnCancelButtonPressed()
        {
            GD.Print($"[TeleportDialog] OnCancelButtonPressed called");
            OnCancelPressed?.Invoke();
            GD.Print($"[TeleportDialog] OnCancelButtonPressed completed");
        }
    }
}
