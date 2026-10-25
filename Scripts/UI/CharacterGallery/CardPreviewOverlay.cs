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
		private RichTextLabel _descText;
		private Label _typeLabel;
		private Control _affixBox;
		private VBoxContainer _affixList;

		/// <summary>文字超量时的**自动滚动**速度（像素/秒）</summary>
		private const float AutoScrollSpeed = 8f;

		/// <summary>自动滚动的亚像素累计（ScrollVertical 是整数，小步会被抹平）</summary>
		private float _scrollRemainder;

		/// <summary>本次显示是否已经进入「自动滚动」状态（用于只打一次日志）</summary>
		private bool _autoScrollStarted;

		/// <summary>
		/// 打开后**先等几帧再开始滚** —— RichTextLabel 的换行在头几帧还没算完，
		/// 早滚会按错的尺寸算（实测首次打开时报「可滚 100px」，稳定后其实只有 20px）。
		/// </summary>
		private int _scrollWarmupFrames;

		public override void _Ready()
		{
			GD.Print("[CardPreviewOverlay] _Ready called");

			_cardBox = GetNodeOrNull<PanelContainer>("CardBox");
			_costLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TopRow/CostLabel");
			_nameLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TopRow/NameLabel");
			_artRect = GetNodeOrNull<TextureRect>("CardBox/Margin/VBox/ArtRect");
			_descText = GetNodeOrNull<RichTextLabel>("CardBox/Margin/VBox/DescText");
			_typeLabel = GetNodeOrNull<Label>("CardBox/Margin/VBox/TypeLabel");
			_affixBox = GetNodeOrNull<Control>("AffixBox");
			_affixList = GetNodeOrNull<VBoxContainer>("AffixBox/AffixPanel/Margin/VBox/AffixList");

			if (_cardBox == null) GD.PrintErr("[CardPreviewOverlay] 未找到 CardBox");
			if (_descText == null) GD.PrintErr("[CardPreviewOverlay] 未找到 DescText");
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
			if (_descText != null)
			{
				_descText.Text = DescriptionGenerator.BuildCardFlavorBBCode(card, card.LinkedAttributes);
				// 每次打开都从**第一行**开始（超量时的自动滚动会带着往下走）
				ResetDescScroll();
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
			// 收起后复位 —— 下次长按重新从**第一行**开始
			ResetDescScroll();
		}

		/// <summary>把描述文字的滚动位置复位到**第一行**</summary>
		private void ResetDescScroll()
		{
			_scrollRemainder = 0f;
			_autoScrollStarted = false;
			_scrollWarmupFrames = 3;
			if (_descText == null) return;
			VScrollBar bar = _descText.GetVScrollBar();
			if (bar != null) bar.Value = 0;
		}

		/// <summary>
		/// **文字超量时自动向下滚动**（主人 2026-10-25）：
		/// 长按放大预览时，卡面文字超出显示区就自动往下滚，方便看完整段说明。
		/// 文字没超量 → 停在**第一行**；收起预览 → 同样复位。
		///
		/// ⚠️ Godot 4 的 `RichTextLabel` **没有** `ScrollVertical` 属性（那是 Godot 3 的 API），
		///    滚动要通过 `GetVScrollBar()` 操作；
		///    而且它的 `MaxValue` 是**内容高度**、不是差值 → 真正能滚的距离 = `MaxValue - Page`。
		/// </summary>
		public override void _Process(double delta)
		{
			if (!Visible || _descText == null) return;

			VScrollBar bar = _descText.GetVScrollBar();
			if (bar == null) return;

			// 等布局稳定（换行算完）再开始滚
			if (_scrollWarmupFrames > 0)
			{
				_scrollWarmupFrames--;
				return;
			}

			// 没超量（内容装得下）→ 回到第一行
			int maxScroll = (int)(bar.MaxValue - bar.Page);
			if (maxScroll <= 0)
			{
				_scrollRemainder = 0f;
				if (bar.Value != 0) bar.Value = 0;
				return;
			}

			// 超量 → 自动往下滚（滚到底停住）
			// ⚠️ 滚动条位置是整数，每帧的小步长会被取整抹平 → 亚像素先累计
			if (!_autoScrollStarted)
			{
				_autoScrollStarted = true;
				GD.Print($"[CardPreviewOverlay] 文字超量（可滚 {maxScroll}px），从第一行开始自动向下滚动");
			}
			_scrollRemainder += AutoScrollSpeed * (float)delta;
			int step = (int)_scrollRemainder;
			if (step <= 0) return;
			_scrollRemainder -= step;

			bar.Value = Mathf.Min(bar.Value + step, maxScroll);
		}

		/// <summary>
		/// **点面板外关闭**。
		///
		/// ⚠️ 用 `_Input` 而不是 `_GuiInput`：
		/// `_GuiInput` 只在「本控件是**命中控件的祖先**」时才被调用，
		/// 而本预览层是 `CharacterGallery` 的**兄弟节点** —— 点外面时命中的是
		/// 卡牌总览页里的控件，事件沿祖先链冒泡到不了这里 → 「点空白关不掉」。
		/// `_Input` 在所有 GUI 处理之前执行，能稳定收到点击。
		/// </summary>
		public override void _Input(InputEvent @event)
		{
			if (!Visible) return;
			if (@event is not InputEventMouseButton mouseEvent) return;
			if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;

			// 点在卡牌上 → 不关
			if (IsPointInsideCard(mouseEvent.Position)) return;

			HideCard();
			// 吃掉这次点击，免得落到下层控件上
			GetViewport().SetInputAsHandled();
		}

		/// <summary>该点是否落在卡牌内（视口坐标）</summary>
		private bool IsPointInsideCard(Vector2 viewportPoint)
		{
			if (_cardBox == null) return false;
			return _cardBox.GetGlobalRect().HasPoint(viewportPoint);
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
