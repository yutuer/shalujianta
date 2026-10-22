using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI
{
	/// <summary>
	/// **抽牌池查看器**：点右下角的抽牌堆图标后弹出的面板，列出抽牌池里**还剩哪些牌**。
	///
	/// 对照参考图 R-08 右下角的牌堆图标（点开能看剩余卡牌）。
	/// 纯展示组件：数据由 <c>BattleManager</c> 在点击时推给它。
	/// </summary>
	public partial class DrawPileViewer : PanelContainer
	{
		private Label _titleLabel;
		private VBoxContainer _list;
		private Label _emptyLabel;
		private Button _closeButton;

		public override void _Ready()
		{
			GD.Print("[DrawPileViewer] _Ready called");

			_titleLabel = GetNodeOrNull<Label>("VBox/TitleLabel");
			_list = GetNodeOrNull<VBoxContainer>("VBox/Scroll/List");
			_emptyLabel = GetNodeOrNull<Label>("VBox/Scroll/List/EmptyLabel");
			_closeButton = GetNodeOrNull<Button>("VBox/CloseButton");

			if (_closeButton != null)
			{
				_closeButton.Pressed += OnClosePressed;
			}
			else
			{
				GD.PrintErr("[DrawPileViewer] 未找到 CloseButton 节点");
			}

			Visible = false;
			GD.Print("[DrawPileViewer] _Ready completed");
		}

		/// <summary>
		/// 显示抽牌池里剩余的卡牌（同名卡牌聚合成「名字 ×N」）
		/// </summary>
		/// <param name="cards">抽牌堆里的卡牌</param>
		public void ShowCards(List<Card> cards)
		{
			GD.Print("[DrawPileViewer] ShowCards called");

			if (_list == null) return;

			// 清空旧的行（先摘出再释放，避免同一帧内新旧行重叠）
			foreach (Node child in _list.GetChildren())
			{
				if (child == _emptyLabel) continue;
				_list.RemoveChild(child);
				child.QueueFree();
			}

			// 同名卡牌聚合计数
			Dictionary<string, int> counts = new Dictionary<string, int>();
			int total = 0;
			if (cards != null)
			{
				foreach (Card card in cards)
				{
					if (card == null) continue;
					total++;
					string name = string.IsNullOrEmpty(card.Name) ? card.CardId : card.Name;
					if (counts.ContainsKey(name)) counts[name]++;
					else counts[name] = 1;
				}
			}

			if (_titleLabel != null)
			{
				_titleLabel.Text = $"抽牌池（剩余 {total} 张）";
			}

			if (_emptyLabel != null)
			{
				_emptyLabel.Visible = total == 0;
			}

			// 字典遍历顺序不固定，按数量降序排一下更好读
			List<string> names = new List<string>(counts.Keys);
			names.Sort((a, b) => counts[b].CompareTo(counts[a]));
			foreach (string name in names)
			{
				Label row = new Label
				{
					Text = counts[name] > 1 ? $"{name}  ×{counts[name]}" : name
				};
				row.AddThemeFontSizeOverride("font_size", 16);
				row.AddThemeColorOverride("font_color", new Color(0.9f, 0.92f, 0.96f));
				_list.AddChild(row);
			}

			Visible = true;
			GD.Print($"[DrawPileViewer] ShowCards completed: {total} 张 / {names.Count} 种");
		}

		private void OnClosePressed()
		{
			GD.Print("[DrawPileViewer] OnClosePressed called");
			Visible = false;
			GD.Print("[DrawPileViewer] OnClosePressed completed");
		}
	}
}
