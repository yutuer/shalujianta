using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI
{
	/// <summary>
	/// **衍生卡说明面板** —— 点狂气爆发说明里的「圣心」弹出来。
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
		private RichTextLabel _descLabel;
		private VBoxContainer _affixList;

		public override void _Ready()
		{
			GD.Print("[DerivedCardInfoPanel] _Ready called");

			_panel = GetNodeOrNull<PanelContainer>("Panel");
			_nameLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/NameRow/NameLabel");
			_costLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/NameRow/CostLabel");
			_descLabel = GetNodeOrNull<RichTextLabel>("Panel/Margin/VBox/DescLabel");
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
				// ⚠️ 走 BuildCardBodyBBCode —— **正文末尾要带词缀标注**，
				//    而且正文里出现的词缀**按词缀说明区的颜色着色**
				//    （照参考游戏：卡面正文带「⚠保留。」「✕罪印」，同时右侧还有词缀说明区，
				//    两处都有）。之前这里直接用 TemplateDescription → 正文一个标记都没有。
				_descLabel.Text = DescriptionGenerator.BuildCardBodyBBCode(card, null, card.LinkedAttributes);
			}

			// 词缀说明（通用构建器，与长按卡牌的面板同一份逻辑）
			AffixListBuilder.Fill(_affixList, card);

			Visible = true;
		}

		public void HidePanel()
		{
			Visible = false;
		}

		/// <summary>
		/// **点面板外关闭**。
		///
		/// ⚠️ 这里用 `_Input` 而**不是** `_GuiInput`：
		/// `_GuiInput` 只在「本控件是**命中控件的祖先**」时才会被调用。
		/// 而这个面板是 `CharacterGallery` 的**兄弟节点** —— 点面板外时命中的是
		/// 卡牌总览页里的 `CardsSide` / 卡牌，事件沿**祖先链**向上冒泡，
		/// 永远到不了本面板 → 表现为「点了外面面板不消失」（2026-10-25 主人报的 bug）。
		/// `_Input` 在所有 GUI 处理**之前**执行，能稳定收到点击。
		/// </summary>
		public override void _Input(InputEvent @event)
		{
			if (!Visible) return;
			if (@event is not InputEventMouseButton mouseEvent) return;
			if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;

			// 点在面板内部 → 不关
			if (_panel != null && _panel.GetGlobalRect().HasPoint(mouseEvent.Position)) return;

			HidePanel();
			// 吃掉这次点击 —— 否则它还会落到下层的卡牌上，误触发卡牌的长按/点击
			GetViewport().SetInputAsHandled();
		}
	}
}
