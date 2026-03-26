using Godot;

namespace FishEatFish.UI.HexMap
{
	public partial class RageCircle : Control
	{
		private string _characterName = "";
		private int _maxRage = 100;
		private int _currentRage = 0;
		private Color _rageColor = new Color(1, 0.5f, 0);
		private Color _backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

		private Label _rageLabel;
		private float _displayRagePercent = 0f;
		private float _targetRagePercent = 0f;
		private float _smoothSpeed = 3f;

		public override void _Ready()
		{
			GD.Print($"[RageCircle] _Ready - Name: {Name}");
			_rageLabel = GetNodeOrNull<Label>("RageLabel");
			CustomMinimumSize = new Vector2(50, 50);
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			SizeFlagsVertical = SizeFlags.ShrinkBegin;
			GD.Print($"[RageCircle] _Ready complete - Name: {Name}, MinSize: {CustomMinimumSize}");
			QueueRedraw();
		}

		public override void _Process(double delta)
		{
			if (Mathf.Abs(_displayRagePercent - _targetRagePercent) > 0.01f)
			{
				_displayRagePercent = Mathf.MoveToward(_displayRagePercent, _targetRagePercent, _smoothSpeed * (float)delta);
				QueueRedraw();
			}
		}

		public override void _Draw()
		{
			if (Size == Vector2.Zero) return;

			float radius = Mathf.Min(Size.X, Size.Y) / 2 - 4;
			Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);

			DrawCircle(center, radius, _backgroundColor);

			DrawArc(center, radius, 0, (float)(System.Math.PI * 2), 32, new Color(0.5f, 0.5f, 0.5f, 1), 2, true);

			if (_displayRagePercent > 0)
			{
				float fillAngle = _displayRagePercent * 360f;
				DrawArc(center, radius - 2, Mathf.DegToRad(-90), Mathf.DegToRad(-90 + fillAngle), 32, _rageColor, 4, true);
			}
		}

		public void SetRage(int current, int max)
		{
			_maxRage = max;
			_currentRage = Mathf.Clamp(current, 0, max);
			_targetRagePercent = _maxRage > 0 ? (float)_currentRage / _maxRage : 0f;
			_displayRagePercent = _targetRagePercent;
			if (_rageLabel != null)
			{
				_rageLabel.Text = $"{_currentRage}";
			}
			QueueRedraw();
		}

		public void SetCharacterName(string name)
		{
			_characterName = name;
		}

		public void AddRage(int amount)
		{
			_currentRage = Mathf.Min(_maxRage, _currentRage + amount);
			SetRage(_currentRage, _maxRage);
		}

		public void SetCircleColor(Color color)
		{
			_rageColor = color;
			QueueRedraw();
		}
	}
}
