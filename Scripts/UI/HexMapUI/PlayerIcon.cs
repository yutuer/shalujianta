using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class PlayerIcon : Control
    {
        private float _moveSpeed = 5.0f;
        private float _bobAmplitude = 3.0f;
        private float _bobFrequency = 2.0f;

        private ColorRect _background;
        private Label _iconLabel;
        private Vector2 _targetPosition;
        private Vector2 _currentPosition;
        private bool _isMoving;
        private float _bobTimer;

        public override void _Ready()
        {
            try
            {
                GD.Print("[PlayerIcon] _Ready started");

                if (Size == Vector2.Zero)
                {
                    Size = CustomMinimumSize;
                }

                _background = GetNodeOrNull<ColorRect>("Background");
                _iconLabel = GetNodeOrNull<Label>("IconLabel");

                if (_background == null)
                {
                    GD.PrintErr("[PlayerIcon] Background not found!");
                }
                if (_iconLabel == null)
                {
                    GD.PrintErr("[PlayerIcon] IconLabel not found!");
                }

                _currentPosition = Position;
                _targetPosition = Position;
                GD.Print($"[PlayerIcon] _Ready complete: Position={Position}, Size={Size}, CustomMinimumSize={CustomMinimumSize}");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[PlayerIcon] _Ready exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void _Process(double delta)
        {
            UpdateMovement((float)delta);
            UpdateBobbing((float)delta);
        }

        private void UpdateMovement(float delta)
        {
            if (_isMoving)
            {
                var direction = _targetPosition - _currentPosition;
                var distance = direction.Length();

                if (distance < 1f)
                {
                    _currentPosition = _targetPosition;
                    _isMoving = false;
                    Position = _currentPosition;
                }
                else
                {
                    var moveAmount = direction.Normalized() * _moveSpeed * 100 * delta;
                    if (moveAmount.Length() > distance)
                    {
                        _currentPosition = _targetPosition;
                    }
                    else
                    {
                        _currentPosition += moveAmount;
                    }
                    Position = _currentPosition;
                }
            }
        }

        private void UpdateBobbing(float delta)
        {
            if (!_isMoving)
            {
                _bobTimer += delta * _bobFrequency;
                float offset = Mathf.Sin(_bobTimer * Mathf.Pi * 2) * _bobAmplitude;
                _iconLabel.Position = new Vector2(0, offset);
            }
        }

        public void MoveTo(Vector2 target)
        {
            _targetPosition = target;
            _isMoving = true;
        }

        public void TeleportTo(Vector2 target)
        {
            GD.Print($"[PlayerIcon] TeleportTo: target={target}");
            _targetPosition = target;
            _currentPosition = target;
            Position = _currentPosition;
            GD.Print($"[PlayerIcon] Position after teleport: {Position}");
            _isMoving = false;
        }
    }
}
