using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class HealthBar : Control
    {
        private float _maxHealth = 100f;
        private float _currentHealth = 100f;

        private PanelContainer _background;
        private ColorRect _damageBar;
        private ColorRect _healthBarFill;
        private Label _healthLabel;

        private float _displayHealth = 100f;
        private float _smoothSpeed = 3f;

        public override void _Ready()
        {
            _background = GetNodeOrNull<PanelContainer>("Background");
            _damageBar = GetNodeOrNull<ColorRect>("Background/DamageBar");
            _healthBarFill = GetNodeOrNull<ColorRect>("Background/HealthBarFill");
            _healthLabel = GetNodeOrNull<Label>("Background/HealthLabel");

            if (_background == null || _healthBarFill == null || _healthLabel == null)
            {
                GD.PrintErr($"[HealthBar] Failed to get child nodes! Background:{_background}, Fill:{_healthBarFill}, Label:{_healthLabel}");
                return;
            }

            CustomMinimumSize = new Vector2(200, 30);
            UpdateVisuals();
        }

        public override void _Process(double delta)
        {
            if (Mathf.Abs(_displayHealth - _currentHealth) > 0.1f)
            {
                _displayHealth = Mathf.MoveToward(_displayHealth, _currentHealth, _smoothSpeed * (float)delta);
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (_healthBarFill == null || _healthLabel == null) return;

            float healthPercent = _maxHealth > 0 ? _displayHealth / _maxHealth : 0f;
            float fillWidth = 200 * healthPercent;
            fillWidth = Mathf.Max(0, fillWidth);

            _healthBarFill.Size = new Vector2(fillWidth, 25);
            _healthLabel.Text = $"{Mathf.FloorToInt(_displayHealth)}/{Mathf.FloorToInt(_maxHealth)}";
        }

        public void SetHealth(float current, float max)
        {
            _maxHealth = max;
            _currentHealth = Mathf.Clamp(current, 0, max);
            UpdateVisuals();
        }

        public void TakeDamage(float damage)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
        }

        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        }
    }
}
