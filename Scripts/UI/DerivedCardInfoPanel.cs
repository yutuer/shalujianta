using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI
{
	/// <summary>
	/// **衍生卡说明面板** —— 点必杀技说明里的「圣心」弹出来。
	///
	/// 版式照参考游戏《终末前夜 Morimens》卡牌总览页右侧那块「衍生卡说明」：
	/// 整屏压暗 → 右侧面板 → 面板里分两段：
	/// ① **卡牌摘要**（卡名 + 消耗算力 + 卡面正文）
	/// ② **词缀说明**（每条词缀：符号 + 名称 + 说明文字）
	/// </summary>
	public partial class DerivedCardInfoPanel : Control
	{
		private PanelContainer _panel;
		private Label _nameLabel;
		private Label _costLabel;
		private Label _descLabel;
		private VBoxContainer _affixList;

		public override void _Ready()
		{
			GD.Print("[DerivedCardInfoPanel] _Ready called");

			_panel = GetNodeOrNull<PanelContainer>("Panel");
			_nameLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/NameRow/NameLabel");
			_costLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/NameRow/CostLabel");
			_descLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/DescLabel");
			_affixList = GetNodeOrNull<VBoxContainer>("Panel/Margin/VBox/AffixList");

			if (_nameLabel == null) GD.PrintErr("[DerivedCardInfoPanel] 未找到 NameLabel");
			if (_affixList == null) GD.PrintErr("[DerivedCardInfoPanel] 未找到 AffixList");

			Visible = false;

			GD.Print("[DerivedCardInfoPanel] _Ready completed");
		}

		/// <summary>显示某张衍生卡的说明</summary>
		/// <param name="card">衍生卡（数据表里的条目）</param>
		/// <param name="ownerName">所属角色名（可空）</param>
		public void ShowCard(Card card, string ownerName = "")
		{
			if (card == null)
			{
				GD.PrintErr("[DerivedCardInfoPanel] ShowCard 收到空卡牌");
				return;
			}

			GD.Print($"[DerivedCardInfoPanel] ShowCard called: {card.CardId}（{card.Name}）");

			if (_nameLabel != null) _nameLabel.Text = card.Name;
			if (_costLabel != null) _costLabel.Text = $"消耗算力: {card.Cost}";

			if (_descLabel != null)
			{
				// 优先用策划写的完整说明（这张卡的部分机制还没实现，生成器写不全）；
				// 没有就退回生成器（数值与战斗同源）。
				_descLabel.Text = !string.IsNullOrEmpty(card.TemplateDescription)
					? card.TemplateDescription
					: DescriptionGenerator.GenerateCardDescription(card, card.LinkedAttributes);
			}

			// 词缀说明（通用构建器，与长按卡牌的面板同一份逻辑）
			AffixListBuilder.Fill(_affixList, card);

			Visible = true;
		}

		public void HidePanel()
		{
			Visible = false;
		}

		public override void _GuiInput(InputEvent @event)
		{
			// 点空白处关闭
			if (@event is InputEventMouseButton mouseEvent &&
				mouseEvent.Pressed &&
				mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (_panel != null && _panel.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
				{
					return;
				}
				HidePanel();
				AcceptEvent();
			}
		}
	}
}
