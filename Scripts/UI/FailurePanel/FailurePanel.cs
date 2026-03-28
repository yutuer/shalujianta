using Godot;

namespace FishEatFish.UI.FailurePanel
{
    public partial class FailurePanel : PanelContainer
    {
        private Label _failureMessage;
        private Button _okButton;

        public System.Action OnOkPressed;

        public override void _Ready()
        {
            _failureMessage = GetNodeOrNull<Label>("VBoxContainer/FailureMessage");
            _okButton = GetNodeOrNull<Button>("VBoxContainer/OkButton");

            if (_okButton != null)
            {
                _okButton.Pressed += OnOkButtonPressed;
            }
        }

        public void ShowFailure(string message)
        {
            if (_failureMessage != null)
            {
                _failureMessage.Text = message;
            }
            Visible = true;
        }

        public void HideFailure()
        {
            Visible = false;
        }

        private void OnOkButtonPressed()
        {
            GD.Print($"[FailurePanel] OnOkButtonPressed called");
            OnOkPressed?.Invoke();
            GD.Print($"[FailurePanel] OnOkButtonPressed completed");
        }
    }
}
