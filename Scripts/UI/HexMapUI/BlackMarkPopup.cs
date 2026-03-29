using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class BlackMarkPopup : PanelContainer
    {
        private Label _titleLabel;
        private Label _amountLabel;

        public override void _Ready()
        {
            GD.Print($"[BlackMarkPopup] _Ready called");

            _titleLabel = GetNodeOrNull<Label>("VBoxContainer/TitleLabel");
            _amountLabel = GetNodeOrNull<Label>("VBoxContainer/AmountLabel");

            if (_titleLabel == null || _amountLabel == null)
            {
                GD.PrintErr($"[BlackMarkPopup] Failed to get child nodes! TitleLabel:{_titleLabel}, AmountLabel:{_amountLabel}");
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

            GD.Print($"[BlackMarkPopup] ShowPopup: before tween, Visible={Visible}, Modulate={Modulate}, ZIndex={ZIndex}");

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(this, "modulate:a", 0f, 1.5f);
            tween.TweenProperty(this, "position:y", Position.Y - 30, 1.5f);
            tween.Chain();
            tween.TweenCallback(Callable.From(() =>
            {
                GD.Print($"[BlackMarkPopup] ShowPopup animation completed, about to free");
                QueueFree();
            }));

            GD.Print($"[BlackMarkPopup] ShowPopup completed");
        }
    }
}
