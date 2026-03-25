using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class PlayerIcon : Control
    {
        [Export]
        private float _moveSpeed = 5.0f;

        [Export]
        private float _bobAmplitude = 3.0f;

        [Export]
        private float _bobFrequency = 2.0f;

        private TextureRect _balloonIcon;
        private TextureRect _playerAvatar;
        private ColorRect _shadow;

        private Vector2 _targetPosition;
        private Vector2 _currentPosition;
        private bool _isMoving;

        private float _bobTimer;
        private float _baseY;

        public override void _Ready()
        {
            InitializeComponents();
            _currentPosition = Position;
            _targetPosition = Position;
        }

        private void InitializeComponents()
        {
            _shadow = new ColorRect();
            AddChild(_shadow);
            _shadow.Color = new Color(0, 0, 0, 0.3f);
            _shadow.Size = new Vector2(40, 10);
            _shadow.Position = new Vector2(-20, 35);
            _shadow.ZIndex = 0;

            _balloonIcon = new TextureRect();
            AddChild(_balloonIcon);
            _balloonIcon.Size = new Vector2(50, 50);
            _balloonIcon.Position = new Vector2(-25, -15);
            _balloonIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _balloonIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _balloonIcon.ZIndex = 1;

            var defaultTexture = GD.Load<Texture2D>("res://Assets/Icons/player_balloon.png");
            if (defaultTexture != null)
            {
                _balloonIcon.Texture = defaultTexture;
            }
            else
            {
                _balloonIcon.Modulate = new Color(0.2f, 0.8f, 1f);
            }

            _playerAvatar = new TextureRect();
            AddChild(_playerAvatar);
            _playerAvatar.Size = new Vector2(30, 30);
            _playerAvatar.Position = new Vector2(-15, -10);
            _playerAvatar.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _playerAvatar.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _playerAvatar.ZIndex = 2;

            var avatarTexture = GD.Load<Texture2D>("res://Assets/UI/avatar_default.png");
            if (avatarTexture != null)
            {
                _playerAvatar.Texture = avatarTexture;
            }
            else
            {
                _playerAvatar.Modulate = new Color(1f, 0.9f, 0.7f);
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
                _balloonIcon.Position = new Vector2(-25, -15 + offset);
                _playerAvatar.Position = new Vector2(-15, -10 + offset * 0.8f);
            }
            else
            {
                _bobTimer = 0;
            }
        }

        public void MoveTo(Vector2 target)
        {
            _targetPosition = target - Size / 2;
            _isMoving = true;
            _baseY = _targetPosition.Y;
        }

        public void TeleportTo(Vector2 target)
        {
            _targetPosition = target - Size / 2;
            _currentPosition = _targetPosition;
            Position = _currentPosition;
            _isMoving = false;
        }

        public bool IsMoving => _isMoving;

        public void SetBalloonTexture(Texture2D texture)
        {
            if (texture != null)
            {
                _balloonIcon.Texture = texture;
            }
        }

        public void SetAvatarTexture(Texture2D texture)
        {
            if (texture != null)
            {
                _playerAvatar.Texture = texture;
            }
        }

        public void PlayArriveAnimation()
        {
            var tween = CreateTween();
            tween.TweenProperty(_balloonIcon, "scale", new Vector2(1.2f, 1.2f), 0.1f);
            tween.TweenProperty(_balloonIcon, "scale", new Vector2(1f, 1f), 0.1f);
        }

        public void PlayDisappearAnimation(System.Action onComplete = null)
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
            if (onComplete != null)
            {
                tween.TweenCallback(Callable.From(() => onComplete()));
            }
        }

        public void PlayAppearAnimation()
        {
            Modulate = new Color(1, 1, 1, 0);
            Scale = new Vector2(0.5f, 0.5f);

            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 1f, 0.3f);
            tween.Parallel().TweenProperty(this, "scale", new Vector2(1f, 1f), 0.3f);
        }
    }
}
