using Godot;

namespace FishEatFish.UI
{
	/// <summary>
	/// 评分项的一行：一个状态标记（✓ / ✗ / •）+ 一段可换行的文案。
	///
	/// 只负责渲染，**不持有任何游戏状态** —— 状态与文案由 ScorePanel 算好后传进来。
	/// </summary>
	public partial class ScoreItemRow : HBoxContainer
	{
		private Label _marker;
		private Label _text;

		/// <summary>当前状态</summary>
		public ScoreItemState CurrentState { get; private set; } = ScoreItemState.Pending;

		/// <summary>当前显示的文案</summary>
		public string DisplayText => _text?.Text ?? "";

		/// <summary>当前状态标记（✓ / ✗ / •）</summary>
		public string MarkerText => _marker?.Text ?? "";

		public override void _Ready()
		{
			GD.Print("[ScoreItemRow] _Ready called");

			_marker = GetNodeOrNull<Label>("Marker");
			_text = GetNodeOrNull<Label>("Text");

			ApplyState(ScoreItemState.Pending, "");
			GD.Print("[ScoreItemRow] _Ready completed");
		}

		/// <summary>设置这一行的状态与文案</summary>
		public void ApplyState(ScoreItemState state, string text)
		{
			CurrentState = state;

			if (_marker == null) _marker = GetNodeOrNull<Label>("Marker");
			if (_text == null) _text = GetNodeOrNull<Label>("Text");

			if (_marker != null)
			{
				switch (state)
				{
					case ScoreItemState.Active:
						_marker.Text = "✓";
						_marker.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 0.45f));
						break;
					case ScoreItemState.Expired:
						_marker.Text = "✗";
						_marker.AddThemeColorOverride("font_color", new Color(0.85f, 0.35f, 0.35f));
						break;
					default:
						_marker.Text = "•";
						_marker.AddThemeColorOverride("font_color", new Color(0.88f, 0.9f, 0.95f));
						break;
				}
			}

			if (_text != null)
			{
				_text.Text = text ?? "";
				switch (state)
				{
					case ScoreItemState.Active:
						_text.AddThemeColorOverride("font_color", new Color(0.56f, 0.88f, 0.56f));
						break;
					case ScoreItemState.Expired:
						_text.AddThemeColorOverride("font_color", new Color(0.55f, 0.57f, 0.62f));
						break;
					default:
						_text.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 0.98f));
						break;
				}
			}
		}
	}
}
