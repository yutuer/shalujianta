using Godot;

namespace FishEatFish.UI
{
	/// <summary>
	/// 关卡详情弹窗里的「奖励预览」单格。
	///
	/// 只负责渲染，**不持有游戏状态** —— 数据由 LevelDetailUI 算好后传进来。
	/// </summary>
	public partial class LevelDetailRewardSlot : Control
	{
		private TextureRect _icon;
		private Label _nameLabel;
		private Label _amountLabel;
		private Label _tagLabel;

		public override void _Ready()
		{
			GD.Print("[LevelDetailRewardSlot] _Ready called");

			_icon = GetNodeOrNull<TextureRect>("Frame/Icon");
			_nameLabel = GetNodeOrNull<Label>("NameLabel");
			_amountLabel = GetNodeOrNull<Label>("AmountLabel");
			_tagLabel = GetNodeOrNull<Label>("TagLabel");

			GD.Print("[LevelDetailRewardSlot] _Ready completed");
		}

		/// <summary>
		/// 设置一格奖励预览。
		/// </summary>
		/// <param name="displayName">道具名字</param>
		/// <param name="iconPath">图标路径（可空）</param>
		/// <param name="amount">数量</param>
		/// <param name="tag">品质标签（三星 / 二星 / 普通，空则不显示）</param>
		public void ApplyReward(string displayName, string iconPath, int amount, string tag)
		{
			if (_icon == null) _icon = GetNodeOrNull<TextureRect>("Frame/Icon");
			if (_nameLabel == null) _nameLabel = GetNodeOrNull<Label>("NameLabel");
			if (_amountLabel == null) _amountLabel = GetNodeOrNull<Label>("AmountLabel");
			if (_tagLabel == null) _tagLabel = GetNodeOrNull<Label>("TagLabel");

			if (_icon != null)
			{
				bool loaded = false;
				if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
				{
					Texture2D tex = GD.Load<Texture2D>(iconPath);
					if (tex != null)
					{
						_icon.Texture = tex;
						_icon.Visible = true;
						loaded = true;
					}
				}
				if (!loaded)
				{
					_icon.Visible = false;
				}
			}

			if (_nameLabel != null)
			{
				_nameLabel.Text = displayName ?? "";
			}

			if (_amountLabel != null)
			{
				_amountLabel.Text = amount > 0 ? amount.ToString() : "";
				_amountLabel.Visible = amount > 0;
			}

			if (_tagLabel != null)
			{
				_tagLabel.Text = tag ?? "";
				_tagLabel.Visible = !string.IsNullOrEmpty(tag);
			}
		}
	}
}
