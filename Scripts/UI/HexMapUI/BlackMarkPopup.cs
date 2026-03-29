using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class BlackMarkPopup : PanelContainer
    {
        private Label _titleLabel;
        private Label _amountLabel;
        private Button _confirmButton;

        public System.Action OnClosed { get; set; }

        public override void _Ready()
        {
            GD.Print($"[BlackMarkPopup] _Ready called");

            _titleLabel = GetNodeOrNull<Label>("MainVBox/TitleLabel");
            _amountLabel = GetNodeOrNull<Label>("MainVBox/AmountLabel");
            _confirmButton = GetNodeOrNull<Button>("MainVBox/ConfirmButton");

            if (_titleLabel == null || _amountLabel == null || _confirmButton == null)
            {
                GD.PrintErr($"[BlackMarkPopup] Failed to get child nodes! TitleLabel:{_titleLabel}, AmountLabel:{_amountLabel}, ConfirmButton:{_confirmButton}");
            }
            else
            {
                _confirmButton.Pressed += OnConfirmPressed;
                GD.Print($"[BlackMarkPopup] ConfirmButton event bound successfully");
            }

            GD.Print($"[BlackMarkPopup] _Ready completed: Size={Size}, CustomMinimumSize={CustomMinimumSize}");
        }

        public void ShowPopup(int amount)
        {
            GD.Print($"[BlackMarkPopup] ShowPopup called: amount={amount}, Position={Position}, Size={Size}");

            if (_titleLabel != null)
            {
                _titleLabel.Text = "获得黑印！";
            }

            if (_amountLabel != null)
            {
                _amountLabel.Text = $"+{amount} 💎";
            }

            ZIndex = 1000;
            Visible = true;
            Modulate = Colors.White;

            GD.Print($"[BlackMarkPopup] ShowPopup: Visible={Visible}, Modulate={Modulate}, ZIndex={ZIndex}");
            GD.Print($"[BlackMarkPopup] ShowPopup completed");
        }

        private void OnConfirmPressed()
        {
            GD.Print($"[BlackMarkPopup] OnConfirmPressed called");
            OnClosed?.Invoke();
            QueueFree();
            GD.Print($"[BlackMarkPopup] OnConfirmPressed completed");
        }
    }
}
