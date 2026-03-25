using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class HealthBar : Control
    {
        [Export]
        private float _maxHealth = 100f;

        [Export]
        private float _currentHealth = 100f;

        [Export]
        private float _smoothSpeed = 5f;

        private TextureRect _avatarIcon;
        private TextureRect _backgroundBar;
        private TextureRect _healthBar;
        private TextureRect _damageBar;
        private Label _healthText;

        private float _displayHealth;
        private float _targetHealth;

        private Color _highHealthColor = new Color(0.2f, 0.8f, 0.2f);
        private Color _mediumHealthColor = new Color(0.8f, 0.8f, 0.2f);
        private Color _lowHealthColor = new Color(0.8f, 0.2f, 0.2f);

        public override void _Ready()
        {
            InitializeComponents();
            _displayHealth = _currentHealth;
            _targetHealth = _currentHealth;
            UpdateVisuals();
        }

        private void InitializeComponents()
        {
            var panel = new PanelContainer();
            AddChild(panel);
            panel.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;

            var vBox = new VBoxContainer();
            panel.AddChild(vBox);

            _avatarIcon = new TextureRect();
            _avatarIcon.Size = new Vector2(40, 40);
            _avatarIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _avatarIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _avatarIcon.CustomMinimumSize = new Vector2(40, 40);
            var avatarTexture = GD.Load<Texture2D>("res://Assets/UI/avatar_default.png");
            if (avatarTexture != null)
            {
                _avatarIcon.Texture = avatarTexture;
            }
            else
            {
                _avatarIcon.Modulate = new Color(1f, 0.9f, 0.7f);
            }
            vBox.AddChild(_avatarIcon);

            var barContainer = new HBoxContainer();
            barContainer.CustomMinimumSize = new Vector2(120, 16);
            vBox.AddChild(barContainer);

            _backgroundBar = new TextureRect();
            _backgroundBar.CustomMinimumSize = new Vector2(120, 16);
            _backgroundBar.StretchMode = TextureRect.StretchModeEnum.Tile;
            _backgroundBar.Modulate = new Color(0.2f, 0.2f, 0.2f);

            var bgTexture = GD.Load<Texture2D>("res://Assets/UI/health_bar_bg.png");
            if (bgTexture == null)
            {
                _backgroundBar.Modulate = new Color(0.2f, 0.2f, 0.2f);
            }
            else
            {
                _backgroundBar.Texture = bgTexture;
            }
            barContainer.AddChild(_backgroundBar);

            var healthContainer = new Control();
            healthContainer.CustomMinimumSize = new Vector2(120, 16);
            healthContainer.SizeFlagsHorizontal = SizeFlags.Fill;
            barContainer.AddChild(healthContainer);

            _damageBar = new TextureRect();
            _damageBar.SizeFlagsHorizontal = SizeFlags.Fill;
            _damageBar.SizeFlagsVertical = SizeFlags.Fill;
            _damageBar.Modulate = new Color(0.8f, 0.2f, 0.2f);
            healthContainer.AddChild(_damageBar);

            _healthBar = new TextureRect();
            _healthBar.SizeFlagsHorizontal = SizeFlags.Fill;
            _healthBar.SizeFlagsVertical = SizeFlags.Fill;
            _healthBar.Modulate = _highHealthColor;
            healthContainer.AddChild(_healthBar);

            var healthBarTexture = GD.Load<Texture2D>("res://Assets/UI/health_bar_fill.png");
            if (healthBarTexture != null)
            {
                _healthBar.Texture = healthBarTexture;
                _damageBar.Texture = healthBarTexture;
            }

            _healthText = new Label();
            _healthText.Text = $"{_currentHealth}/{_maxHealth}";
            _healthText.HorizontalAlignment = HorizontalAlignment.Center;
            _healthText.VerticalAlignment = VerticalAlignment.Center;
            _healthText.Position = new Vector2(0, -2);
            healthContainer.AddChild(_healthText);
        }

        public override void _Process(double delta)
        {
            UpdateSmoothHealth((float)delta);
        }

        private void UpdateSmoothHealth(float delta)
        {
            if (Mathf.Abs(_displayHealth - _targetHealth) > 0.1f)
            {
                _displayHealth = Mathf.MoveToward(_displayHealth, _targetHealth, _smoothSpeed * 100 * delta);
                UpdateHealthVisuals();
            }
        }

        private void UpdateVisuals()
        {
            UpdateHealthVisuals();
        }

        private void UpdateHealthVisuals()
        {
            float healthPercent = _maxHealth > 0 ? Mathf.Clamp(_displayHealth / _maxHealth, 0f, 1f) : 0f;

            float damagePercent = _maxHealth > 0 ? Mathf.Clamp(_targetHealth / _maxHealth, 0f, 1f) : 0f;

            float currentPercent = _maxHealth > 0 ? Mathf.Clamp(_currentHealth / _maxHealth, 0f, 1f) : 0f;

            _healthBar.Size = new Vector2(_backgroundBar.Size.X * healthPercent, _backgroundBar.Size.Y);
            _damageBar.Size = new Vector2(_backgroundBar.Size.X * damagePercent, _backgroundBar.Size.Y);
            _damageBar.Position = new Vector2(_healthBar.Size.X, 0);

            _healthText.Text = $"{(int)_currentHealth}/{(int)_maxHealth}";

            Color healthColor;
            if (currentPercent > 0.6f)
            {
                healthColor = _highHealthColor;
            }
            else if (currentPercent > 0.3f)
            {
                healthColor = _mediumHealthColor;
            }
            else
            {
                healthColor = _lowHealthColor;
            }

            _healthBar.Modulate = healthColor;
        }

        public void SetHealth(float current, float max)
        {
            _maxHealth = max;
            _currentHealth = current;
            _targetHealth = current;
            _displayHealth = current;
            UpdateVisuals();
        }

        public void SetCurrentHealth(float current)
        {
            _currentHealth = Mathf.Clamp(current, 0f, _maxHealth);
            _targetHealth = _currentHealth;
        }

        public void Damage(float amount)
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            PlayDamageAnimation();

            if (_currentHealth < oldHealth)
            {
                GD.Print($"[HealthBar] 受到伤害: {amount}, 当前生命: {_currentHealth}/{_maxHealth}");
            }
        }

        public void Heal(float amount)
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            _targetHealth = _currentHealth;

            if (_currentHealth > oldHealth)
            {
                PlayHealAnimation();
                GD.Print($"[HealthBar] 恢复生命: {amount}, 当前生命: {_currentHealth}/{_maxHealth}");
            }
        }

        private void PlayDamageAnimation()
        {
            var tween = CreateTween();
            tween.TweenProperty(_healthBar, "modulate:r", 2f, 0.1f);
            tween.TweenProperty(_healthBar, "modulate:r", 1f, 0.1f);

            var shakeTween = CreateTween();
            var originalPos = _healthBar.Position;
            for (int i = 0; i < 3; i++)
            {
                shakeTween.TweenProperty(_healthBar, "position:x", originalPos.X + 5, 0.05f);
                shakeTween.TweenProperty(_healthBar, "position:x", originalPos.X - 5, 0.05f);
            }
            shakeTween.TweenProperty(_healthBar, "position:x", originalPos.X, 0.05f);
        }

        private void PlayHealAnimation()
        {
            var tween = CreateTween();
            tween.TweenProperty(_healthBar, "modulate:g", 1.5f, 0.2f);
            tween.TweenProperty(_healthBar, "modulate:g", 1f, 0.2f);
        }

        public float GetHealthPercent()
        {
            return _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        }

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        public void SetAvatarTexture(Texture2D texture)
        {
            if (texture != null)
            {
                _avatarIcon.Texture = texture;
            }
        }
    }
}
