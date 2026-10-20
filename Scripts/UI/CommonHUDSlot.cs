using Godot;

namespace FishEatFish.UI
{
	/// <summary>
	/// 公共 HUD 里的收集品槽位（造物/刻印）：有内容显示图标，空的显示 ✕
	/// </summary>
	public partial class CommonHUDSlot : Control
	{
		private TextureRect _iconRect;
		private Label _emptyLabel;

		public override void _Ready()
		{
			GD.Print("[CommonHUDSlot] _Ready called");

			_iconRect = GetNodeOrNull<TextureRect>("Panel/IconRect");
			_emptyLabel = GetNodeOrNull<Label>("Panel/EmptyLabel");

			if (_iconRect == null)
			{
				GD.PrintErr("[CommonHUDSlot] 未找到 IconRect 节点");
			}

			if (_emptyLabel == null)
			{
				GD.PrintErr("[CommonHUDSlot] 未找到 EmptyLabel 节点");
			}

			GD.Print("[CommonHUDSlot] _Ready completed");
		}

		/// <summary>
		/// 设为空槽（显示 ✕）
		/// </summary>
		public void SetEmpty()
		{
			if (_iconRect != null)
			{
				_iconRect.Texture = null;
				_iconRect.Visible = false;
			}

			if (_emptyLabel != null)
			{
				_emptyLabel.Visible = true;
			}
		}

		/// <summary>
		/// 设置槽位显示的图标
		/// </summary>
		/// <param name="iconPath">图标资源路径，空则按空槽处理</param>
		public void SetIcon(string iconPath)
		{
			if (_iconRect != null)
			{
				if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
				{
					Texture2D texture = ResourceLoader.Load<Texture2D>(iconPath);
					if (texture != null)
					{
						_iconRect.Texture = texture;
						_iconRect.Visible = true;
					}
					else
					{
						SetEmpty();
					}
				}
				else
				{
					SetEmpty();
				}
			}

			if (_emptyLabel != null)
			{
				_emptyLabel.Visible = _iconRect == null || !_iconRect.Visible;
			}
		}
	}
}
