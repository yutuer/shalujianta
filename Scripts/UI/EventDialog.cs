using Godot;
using System.Collections.Generic;
using FishEatFish.Battle;

namespace FishEatFish.UI
{
	/// <summary>
	/// 通用事件弹窗（对照参考图 R-07 / R-16）
	///
	/// 结构：标题 + 「隐藏事件」标记 ｜ 主图 + 叙事文案 ｜ N 个带描述的选项按钮
	/// 数据来自 Data/events.json（MapEventData），**不硬编码任何文案**。
	/// </summary>
	public partial class EventDialog : PanelContainer
	{
		/// <summary>选中第几个选项（0 起）</summary>
		public System.Action<int> OnOptionSelected;

		/// <summary>弹窗关闭（无论选了什么）</summary>
		public System.Action OnClosed;

		private Label _titleLabel;
		private TextureRect _imageRect;
		private Label _bodyLabel;
		private VBoxContainer _optionBox;
		private HBoxContainer _hiddenEventBox;
		private TextureRect _hiddenIcon;

		public override void _Ready()
		{
			GD.Print("[EventDialog] _Ready called");

			_titleLabel = GetNodeOrNull<Label>("MainVBox/TopRow/TitleLabel");
			_imageRect = GetNodeOrNull<TextureRect>("MainVBox/ContentRow/ImageRect");
			_bodyLabel = GetNodeOrNull<Label>("MainVBox/ContentRow/BodyLabel");
			_optionBox = GetNodeOrNull<VBoxContainer>("MainVBox/OptionBox");
			_hiddenEventBox = GetNodeOrNull<HBoxContainer>("MainVBox/TopRow/HiddenEventBox");
			_hiddenIcon = GetNodeOrNull<TextureRect>("MainVBox/TopRow/HiddenEventBox/HiddenIcon");

			if (_titleLabel == null || _bodyLabel == null || _optionBox == null)
			{
				GD.PrintErr("[EventDialog] 关键节点缺失，检查 EventDialog.tscn 的层级");
			}

			GD.Print("[EventDialog] _Ready completed");
		}

		/// <summary>
		/// 显示一个事件
		/// </summary>
		/// <param name="data">事件表现数据</param>
		public void ShowEvent(MapEventData data)
		{
			GD.Print($"[EventDialog] ShowEvent called: title={data?.Title}");

			if (data == null)
			{
				GD.PrintErr("[EventDialog] ShowEvent called with null data");
				return;
			}

			if (_titleLabel != null)
			{
				_titleLabel.Text = data.Title;
			}

			if (_bodyLabel != null)
			{
				_bodyLabel.Text = data.Body;
			}

			if (_imageRect != null)
			{
				if (!string.IsNullOrEmpty(data.Image) && ResourceLoader.Exists(data.Image))
				{
					Texture2D texture = ResourceLoader.Load<Texture2D>(data.Image);
					_imageRect.Texture = texture;
					_imageRect.Visible = texture != null;
				}
				else
				{
					_imageRect.Visible = false;
				}
			}

			if (_hiddenEventBox != null)
			{
				_hiddenEventBox.Visible = data.Hidden;
			}

			BuildOptions(data);

			Visible = true;
			GD.Print("[EventDialog] ShowEvent completed");
		}

		/// <summary>
		/// 按数据生成选项按钮
		/// </summary>
		private void BuildOptions(MapEventData data)
		{
			if (_optionBox == null) return;

			// ⚠️ 必须先 RemoveChild 再 QueueFree —— 只调 QueueFree 的话，旧按钮在**本帧结束前**
			// 仍然留在容器里（QueueFree 是延迟删除），于是新按钮会叠在旧按钮上面：
			//   第二次打开弹窗 → 容器里有 4 个按钮（旧的 2 个 + 新的 2 个）
			//   点上面那两个旧的 → 它们已被标记删除但还没释放 → **点了没反应**
			//   新按钮被压在下面 → 面板看着"卡住"关不掉
			// 这就是「双向门第一次能跳、第二次点了没反应」的根因。
			// 手法与 ShopUI 一致（见踩坑记录 §27）。
			foreach (Node child in _optionBox.GetChildren())
			{
				_optionBox.RemoveChild(child);
				child.QueueFree();
			}

			for (int i = 0; i < data.Options.Count; i++)
			{
				int index = i;
				MapEventOption option = data.Options[i];

				Button button = new Button
				{
					Text = option.Text,
					CustomMinimumSize = new Vector2(0, 52),
					Alignment = HorizontalAlignment.Center
				};
				button.Pressed += () => OnOptionButtonPressed(index);
				_optionBox.AddChild(button);
			}
		}

		private void OnOptionButtonPressed(int index)
		{
			GD.Print($"[EventDialog] OnOptionButtonPressed called: index={index}");
			Visible = false;
			OnOptionSelected?.Invoke(index);
			OnClosed?.Invoke();
			GD.Print($"[EventDialog] OnOptionButtonPressed completed");
		}

		/// <summary>
		/// 关闭弹窗（不触发选项）
		/// </summary>
		public void Close()
		{
			GD.Print("[EventDialog] Close called");
			Visible = false;
			OnClosed?.Invoke();
			GD.Print("[EventDialog] Close completed");
		}
	}
}
