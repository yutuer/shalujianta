using Godot;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// 「角色一览」列表里的**一项**：头像 + 名字 + 类型。点一下进该角色的详情。
	///
	/// 版式照参考图的「人物列表」：一列/几列圆头像 + 名字 + 类型标签。
	/// </summary>
	public partial class CharacterGalleryItem : Panel
	{
		private TextureRect _avatarRect;
		private Label _nameLabel;
		private Label _typeLabel;

		private CharacterDefinition _character;
		private bool _hovered;

		/// <summary>点击这一项（参数 = 该角色定义）</summary>
		public System.Action<CharacterDefinition> OnSelected;

		public override void _Ready()
		{
			GD.Print("[CharacterGalleryItem] _Ready called");

			_avatarRect = GetNodeOrNull<TextureRect>("VBox/AvatarRect");
			_nameLabel = GetNodeOrNull<Label>("VBox/NameLabel");
			_typeLabel = GetNodeOrNull<Label>("VBox/TypeLabel");

			if (_avatarRect == null) GD.PrintErr("[CharacterGalleryItem] 未找到 VBox/AvatarRect");
			if (_nameLabel == null) GD.PrintErr("[CharacterGalleryItem] 未找到 VBox/NameLabel");
			if (_typeLabel == null) GD.PrintErr("[CharacterGalleryItem] 未找到 VBox/TypeLabel");

			MouseEntered += OnMouseEntered;
			MouseExited += OnMouseExited;

			ApplyStyle();

			GD.Print("[CharacterGalleryItem] _Ready completed");
		}

		/// <summary>填入角色数据</summary>
		public void Setup(CharacterDefinition character)
		{
			_character = character;
			if (character == null) return;

			if (_nameLabel != null) _nameLabel.Text = character.Name;

			// 类型：先显示代码里的 Archetype 中文名。
			// ⚠️ 主人 2026-10-24 说「定位以后给正式名称，参考图截图上有类型」——
			//    这里先按现有 Archetype 显示，截图到手后换成正式定位。
			if (_typeLabel != null) _typeLabel.Text = character.Archetype.GetShortName();

			if (_avatarRect != null)
			{
				string avatarPath = $"res://Assets/Portraits/avatar_{character.CharacterId}.svg";
				if (ResourceLoader.Exists(avatarPath))
				{
					_avatarRect.Texture = ResourceLoader.Load<Texture2D>(avatarPath);
				}
				else
				{
					GD.PrintErr($"[CharacterGalleryItem] 头像不存在: {avatarPath}");
					_avatarRect.Texture = null;
				}
			}
		}

		public override void _GuiInput(InputEvent @event)
		{
			if (@event is not InputEventMouseButton mouseEvent) return;
			if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;

			GD.Print($"[CharacterGalleryItem] 点击：{_character?.Name}");
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
			style.BgColor = _hovered ? new Color(0.2f, 0.22f, 0.32f) : new Color(0.15f, 0.16f, 0.23f);
			style.BorderColor = _hovered ? new Color(0.7f, 0.8f, 1f) : new Color(0.38f, 0.42f, 0.55f);
			style.SetBorderWidthAll(2);
			style.SetCornerRadiusAll(10);
			AddThemeStyleboxOverride("panel", style);
		}
	}
}
