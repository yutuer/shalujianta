using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class RageCircle : Control
    {
        [Export]
        private string _characterName = "";

        [Export]
        private int _maxRage = 100;

        [Export]
        private int _currentRage = 0;

        [Export]
        private Color _rageColor = new Color(0.9f, 0.4f, 0.1f);

        [Export]
        private Color _emptyColor = new Color(0.3f, 0.3f, 0.3f);

        private TextureRect _circleBackground;
        private TextureRect _circleFill;
        private Label _nameLabel;
        private Label _rageLabel;

        private float _displayRagePercent = 0f;
        private float _targetRagePercent = 0f;

        [Export]
        private float _smoothSpeed = 3f;

        public override void _Ready()
        {
            InitializeComponents();
            UpdateRagePercent();
            UpdateVisuals();
        }

        private void InitializeComponents()
        {
            CustomMinimumSize = new Vector2(50, 70);

            _circleBackground = new TextureRect();
            AddChild(_circleBackground);
            _circleBackground.Size = new Vector2(50, 50);
            _circleBackground.Position = new Vector2(0, 0);
            _circleBackground.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _circleBackground.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _circleBackground.Modulate = _emptyColor;

            var bgTexture = GD.Load<Texture2D>("res://Assets/UI/rage_circle_bg.png");
            if (bgTexture == null)
            {
                _circleBackground.Modulate = _emptyColor;
            }
            else
            {
                _circleBackground.Texture = bgTexture;
            }

            _circleFill = new TextureRect();
            _circleBackground.AddChild(_circleFill);
            _circleFill.SizeFlagsHorizontal = SizeFlags.Fill;
            _circleFill.SizeFlagsVertical = SizeFlags.Fill;
            _circleFill.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _circleFill.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            _circleFill.Modulate = _rageColor;

            var fillTexture = GD.Load<Texture2D>("res://Assets/UI/rage_circle_fill.png");
            if (fillTexture != null)
            {
                _circleFill.Texture = fillTexture;
            }
            else
            {
                _circleFill.Modulate = _rageColor;
            }

            _nameLabel = new Label();
            AddChild(_nameLabel);
            _nameLabel.Text = _characterName;
            _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _nameLabel.VerticalAlignment = VerticalAlignment.Center;
            _nameLabel.Position = new Vector2(0, 50);
            _nameLabel.Size = new Vector2(50, 20);

            _rageLabel = new Label();
            AddChild(_rageLabel);
            _rageLabel.Text = $"{_currentRage}";
            _rageLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _rageLabel.VerticalAlignment = VerticalAlignment.Center;
            _rageLabel.Position = new Vector2(0, 15);
            _rageLabel.Size = new Vector2(50, 20);
            _rageLabel.AddThemeColorOverride("font_color", Colors.White);
        }

        public override void _Process(double delta)
        {
            UpdateSmoothFill((float)delta);
        }

        private void UpdateSmoothFill(float delta)
        {
            if (Mathf.Abs(_displayRagePercent - _targetRagePercent) > 0.01f)
            {
                _displayRagePercent = Mathf.MoveToward(_displayRagePercent, _targetRagePercent, _smoothSpeed * (float)delta);
                UpdateFillVisuals();
            }
        }

        private void UpdateRagePercent()
        {
            _targetRagePercent = _maxRage > 0 ? (float)_currentRage / _maxRage : 0f;
            _displayRagePercent = _targetRagePercent;
        }

        private void UpdateVisuals()
        {
            UpdateFillVisuals();
            _nameLabel.Text = _characterName;
            _rageLabel.Text = $"{_currentRage}";
        }

        private void UpdateFillVisuals()
        {
            float fillScale = _displayRagePercent;
            _circleFill.Scale = new Vector2(1f, fillScale);

            if (fillScale >= 1f)
            {
                _circleFill.Modulate = new Color(1f, 0.8f, 0.1f);
                PlayFullAnimation();
            }
            else
            {
                _circleFill.Modulate = _rageColor;
            }
        }

        public void SetRage(int current, int max)
        {
            _maxRage = max;
            _currentRage = Mathf.Clamp(current, 0, max);
            UpdateRagePercent();
            UpdateVisuals();
        }

        public void AddRage(int amount)
        {
            int oldRage = _currentRage;
            _currentRage = Mathf.Min(_maxRage, _currentRage + amount);
            UpdateRagePercent();

            if (_currentRage > oldRage)
            {
                PlayRageGainAnimation();
                GD.Print($"[RageCircle] {_characterName} 怒气增加: {amount}, 当前: {_currentRage}/{_maxRage}");
            }
        }

        public void SetRage(int amount)
        {
            int oldRage = _currentRage;
            _currentRage = Mathf.Clamp(amount, 0, _maxRage);
            UpdateRagePercent();

            if (_currentRage != oldRage)
            {
                UpdateVisuals();
            }
        }

        public void SpendRage(int amount)
        {
            if (_currentRage >= amount)
            {
                _currentRage -= amount;
                UpdateRagePercent();
                GD.Print($"[RageCircle] {_characterName} 消耗怒气: {amount}, 剩余: {_currentRage}/{_maxRage}");
            }
        }

        private void PlayRageGainAnimation()
        {
            var tween = CreateTween();
            tween.TweenProperty(_circleFill, "modulate:r", 1.5f, 0.1f);
            tween.TweenProperty(_circleFill, "modulate:r", 1f, 0.1f);

            var pulseTween = CreateTween();
            pulseTween.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.1f);
            pulseTween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.1f);
        }

        private void PlayFullAnimation()
        {
            var pulseTween = CreateTween();
            pulseTween.TweenProperty(_circleBackground, "modulate:g", 1.2f, 0.3f);
            pulseTween.TweenProperty(_circleBackground, "modulate:g", 1f, 0.3f);
            pulseTween.SetLoops(3);
        }

        public void SetCharacterName(string name)
        {
            _characterName = name;
            _nameLabel.Text = name;
        }

        public void SetCircleColor(Color color)
        {
            _rageColor = color;
            _circleFill.Modulate = color;
        }

        public int CurrentRage => _currentRage;
        public int MaxRage => _maxRage;
        public bool IsFull => _currentRage >= _maxRage;

        public float RagePercent => _maxRage > 0 ? (float)_currentRage / _maxRage : 0f;
    }
}
