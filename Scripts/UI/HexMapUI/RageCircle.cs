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
		private Label _rageValueLabel;
		private float _displayRagePercent = 0f;
		private float _targetRagePercent = 0f;
		private float _smoothSpeed = 3f;

		/// <summary>直径（像素）</summary>
		[Export]
		public int Diameter = 120;

		/// <summary>
		/// 是否画底色圆。
		/// 战斗场景把它**叠在头像外面**当「怒气环」用 → 关掉底色，只留弧。
		/// </summary>
		[Export]
		public bool DrawBackground = true;

		/// <summary>是否画外圈灰环</summary>
		[Export]
		public bool DrawOuterRing = true;

		public override void _Ready()
		{
			GD.Print($"[RageCircle] _Ready - Name: {Name}");
			_rageLabel = GetNodeOrNull<Label>("RageLabel");
			_rageValueLabel = GetNodeOrNull<Label>("RageValueLabel");
			if (_rageValueLabel == null)
			{
				GD.PrintErr("[RageCircle] 未找到节点：RageValueLabel");
			}
			CustomMinimumSize = new Vector2(Diameter, Diameter);
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			SizeFlagsVertical = SizeFlags.ShrinkBegin;

			// 环模式（叠在头像外面）→ 中间是头像，中心大数字用不了，
			// 改成在头像底部显示怒气数值（小字 + 描边，压在立绘上也读得清）
			if (_rageLabel != null)
			{
				_rageLabel.Visible = DrawBackground;
			}
			if (_rageValueLabel != null)
			{
				_rageValueLabel.Visible = !DrawBackground;
			}

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

			if (DrawBackground)
			{
				DrawCircle(center, radius, _backgroundColor);
			}

			if (DrawOuterRing)
			{
				// 环要比背景亮一档，否则暗底上完全看不见（战斗场景的四个头像就是这么"消失"的）
				DrawArc(center, radius, 0, (float)(System.Math.PI * 2), 32, new Color(0.85f, 0.8f, 0.62f, 1f), 3, true);
			}

			if (!DrawBackground)
			{
				// 环模式：内圈补一圈**进度槽底**。
				// 没有槽底时，怒气 0 和怒气满都只剩同一条装饰圆环，
				// 玩家看不出这是个怒气槽 —— 这正是"没有怒气的显示效果"的由来。
				DrawArc(center, radius - 3, 0, (float)(System.Math.PI * 2), 32, new Color(0.16f, 0.17f, 0.22f, 1f), 4, true);
			}

			if (_displayRagePercent > 0)
			{
				float fillAngle = _displayRagePercent * 360f;
				DrawArc(center, radius - 3, Mathf.DegToRad(-90), Mathf.DegToRad(-90 + fillAngle), 32, _rageColor, 4, true);
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
			if (_rageValueLabel != null)
			{
				_rageValueLabel.Text = $"{_currentRage}";
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
