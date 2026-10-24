using Godot;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// 「卡牌总览」视图左边那一竖列里的**一个头像**（点击切换右侧的卡牌组）。
	///
	/// 版式照参考图《终末前夜 Morimens》的卡牌总览页：左边一列角色立绘头像，
	/// 当前选中的那个高亮（金框）。
	/// </summary>
	public partial class CharacterPortraitItem : Panel
	{
		private TextureRect _avatarRect;
		private Label _nameLabel;

		private CharacterDefinition _character;
		private bool _hovered;
		private bool _selected;

		/// <summary>点击这一项（参数 = 该角色定义）</summary>
		public System.Action<CharacterDefinition> OnSelected;

		public override void _Ready()
		{
			_avatarRect = GetNodeOrNull<TextureRect>("VBox/AvatarRect");
			_nameLabel = GetNodeOrNull<Label>("VBox/NameLabel");

			if (_avatarRect == null) GD.PrintErr("[CharacterPortraitItem] 未找到 VBox/AvatarRect");
			if (_nameLabel == null) GD.PrintErr("[CharacterPortraitItem] 未找到 VBox/NameLabel");

			MouseEntered += OnMouseEntered;
			MouseExited += OnMouseExited;

			ApplyStyle();
		}

		public void Setup(CharacterDefinition character, bool selected)
		{
			_character = character;
			_selected = selected;
			if (character == null) return;

			if (_nameLabel != null) _nameLabel.Text = character.Name;

			if (_avatarRect != null)
			{
				string avatarPath = $"res://Assets/Portraits/avatar_{character.CharacterId}.svg";
				if (ResourceLoader.Exists(avatarPath))
				{
					_avatarRect.Texture = ResourceLoader.Load<Texture2D>(avatarPath);
				}
				else
				{
					GD.PrintErr($"[CharacterPortraitItem] 头像不存在: {avatarPath}");
					_avatarRect.Texture = null;
				}
			}

			ApplyStyle();
		}

		public override void _GuiInput(InputEvent @event)
		{
			if (@event is not InputEventMouseButton mouseEvent) return;
			if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;

			GD.Print($"[CharacterPortraitItem] 点击：{_character?.Name}");
			OnSelected?.Invoke(_character);
			AcceptEvent();
		}

		private void OnMouseEntered()
		{
			_hovered = true;
			ApplyStyle();
		}

		private void OnMouseExited()
		{
			_hovered = false;
			ApplyStyle();
		}

		private void ApplyStyle()
		{
			var style = new StyleBoxFlat();
			style.BgColor = _selected ? new Color(0.22f, 0.2f, 0.14f) : new Color(0.13f, 0.14f, 0.2f);
			style.BorderColor = _selected
				? new Color(0.95f, 0.8f, 0.35f)
				: (_hovered ? new Color(0.7f, 0.8f, 1f) : new Color(0.3f, 0.34f, 0.45f));
			style.SetBorderWidthAll(_selected ? 3 : 2);
			style.SetCornerRadiusAll(8);
			AddThemeStyleboxOverride("panel", style);
		}
	}
}
