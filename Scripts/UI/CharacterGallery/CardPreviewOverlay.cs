using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// **卡牌放大预览层** —— 长按卡牌时弹出来。
	///
	/// 版式照参考游戏《终末前夜 Morimens》的长按卡牌：
	/// 整屏压暗 → **卡牌放大浮在屏幕中间偏左** → **右边一个「词缀说明」面板**
	/// （每条词缀：符号 + 名称 + 一段说明文字）。
	///
	/// ⚠️ **没有词缀的卡（打击 / 防御这类）不显示右侧面板**，卡牌回到**正中** ——
	/// 对照参考图「卡牌1_波吕克斯」（打击）长按后就没有说明面板。
	///
	/// 词缀说明的来源：
	/// - 卡牌自带的 <c>affixNotes</c>（灵知觉醒 / 罪印…）
	/// - 卡牌的 <c>affixes</c>（保留 / 消耗…）→ 去 <c>Data/affixes.json</c> 查说明
	/// </summary>
	public partial class CardPreviewOverlay : Control
	{
		/// <summary>有词缀时卡牌中心所在的横向位置（0.45 = 偏左，给右侧面板让位）</summary>
		private const float CardCenterWithAffix = 0.45f;

		/// <summary>没有词缀时卡牌中心所在的横向位置（正中）</summary>
		private const float CardCenterWithoutAffix = 0.5f;

		/// <summary>卡牌半宽（改锚点时要保持左右对称）</summary>
		private const float CardHalfWidth = 200f;

		private PanelContainer _cardBox;
		private Label _costLabel;
		private Label _nameLabel;
		private TextureRect _artRect;
		private Label _descLabel;
		private Label _typeLabel;
		private Control _affixBox;
		private VBoxContainer _affixList;

		public override void _Ready()
		{
			GD.Print("[CardPreviewOverlay] _Ready called");

			_cardBox = GetNodeOrNull<PanelContainer>("CardBox");
			_costLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TopRow/CostLabel");
			_nameLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TopRow/NameLabel");
			_artRect = GetNodeOrNull<TextureRect>("CardBox/Margin/VBox/ArtRect");
			_descLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/DescLabel");
			_typeLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TypeLabel");
			_affixBox = GetNodeOrNull<Control>("AffixBox");
			_affixList = GetNodeOrNull<VBoxContainer>("AffixBox/AffixPanel/Margin/VBox/AffixList");

			if (_cardBox == null) GD.PrintErr("[CardPreviewOverlay] 未找到 CardBox");
			if (_affixList == null) GD.PrintErr("[CardPreviewOverlay] 未找到 AffixList");

			Visible = false;

			GD.Print("[CardPreviewOverlay] _Ready completed");
		}

		/// <summary>显示某张卡的放大预览</summary>
		/// <param name="card">要预览的卡</param>
		/// <param name="art">卡面插画</param>
		public void ShowCard(Card card, Texture2D art = null)
		{
			if (card == null)
			{
				GD.PrintErr("[CardPreviewOverlay] ShowCard 收到空卡牌");
				return;
			}

			GD.Print($"[CardPreviewOverlay] ShowCard called: {card.CardId}（{card.Name}）");

			if (_costLabel != null) _costLabel.Text = card.Cost.ToString();
			if (_nameLabel != null) _nameLabel.Text = card.Name;
			if (_artRect != null) _artRect.Texture = art;
			if (_descLabel != null)
			{
				_descLabel.Text = DescriptionGenerator.GenerateCardFlavorText(card, card.LinkedAttributes);
			}
			if (_typeLabel != null)
			{
				_typeLabel.Text = card.CardType switch
				{
					CardType.Derived => "衍生",
					CardType.Awakening => "灵知觉醒",
					_ => "指令"
				};
			}

			int affixCount = FillAffixList(card);

			// 没有词缀 → 不显示右侧面板，卡牌回到正中（对照参考图的打击卡）
			bool hasAffix = affixCount > 0;
			if (_affixBox != null) _affixBox.Visible = hasAffix;
			SetCardCenter(hasAffix ? CardCenterWithAffix : CardCenterWithoutAffix);

			Visible = true;
		}

		public void HideCard()
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
				if (IsPointInsideCard())
				{
					return;
				}
				HideCard();
				AcceptEvent();
			}
		}

		private bool IsPointInsideCard()
		{
			if (_cardBox == null) return false;
			return _cardBox.GetGlobalRect().HasPoint(GetGlobalMousePosition());
		}

		/// <summary>移动卡牌：改锚点让它的中心落在指定的横向位置</summary>
		private void SetCardCenter(float centerX)
		{
			if (_cardBox == null) return;

			_cardBox.AnchorLeft = centerX;
			_cardBox.AnchorRight = centerX;
			_cardBox.OffsetLeft = -CardHalfWidth;
			_cardBox.OffsetRight = CardHalfWidth;
		}

		/// <summary>填「词缀说明」面板：卡牌特有词缀 + 通用词缀</summary>
		/// <returns>词缀条目数量（0 = 这张卡没有词缀，右侧面板不显示）</returns>
		private int FillAffixList(Card card)
		{
			if (_affixList == null) return 0;

			// 清空 + 填词缀说明 → 走共用构建器（与「衍生卡说明」面板同一份逻辑）
			// ⚠️ 构建器内部**先 RemoveChild 再 QueueFree** —— QueueFree 是帧末才删，
			// 不摘除的话旧条目会和新条目叠在一起（面板越切越长）。
			return AffixListBuilder.Fill(_affixList, card);
		}
	}
}
