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

			// ⚠️ **必须先 RemoveChild 再 QueueFree**：
			// QueueFree 是**帧末**才删，同帧又 AddChild 新条目 → 旧条目还挂着，
			// 面板就会越叠越长（主人 2026-10-24 报的现象）。
			// 立即摘除 = 这一帧面板就是干净的。
			foreach (Node child in _affixList.GetChildren())
			{
				_affixList.RemoveChild(child);
				child.QueueFree();
			}

			int count = 0;

			// ① 卡牌特有的词缀（灵知觉醒 / 罪印…）
			if (card.AffixNotes != null)
			{
				foreach (CardAffixNote note in card.AffixNotes)
				{
					if (note == null) continue;
					AffixInfo info = AffixLibrary.Get(note.Id);
					AddAffixEntry(
						note.Name,
						note.Description,
						info?.Symbol ?? "⚠",
						info?.Color ?? "#e8a34a");
					count++;
				}
			}

			// ② 通用词缀（保留 / 消耗…）→ 说明去 affixes.json 查
			if (card.Affixes != null)
			{
				foreach (string affixId in card.Affixes)
				{
					AffixInfo info = AffixLibrary.Get(affixId);
					if (info == null) continue;
					AddAffixEntry(info.Name, info.Description, info.Symbol, info.Color);
					count++;
				}
			}

			return count;
		}

		private void AddAffixEntry(string name, string description, string symbol, string colorHex)
		{
			Color color = ParseColor(colorHex);

			var entry = new VBoxContainer();
			entry.AddThemeConstantOverride("separation", 4);

			var titleRow = new HBoxContainer();
			titleRow.AddThemeConstantOverride("separation", 6);

			Label symbolLabel = new Label();
			symbolLabel.Text = string.IsNullOrEmpty(symbol) ? "⚠" : symbol;
			symbolLabel.AddThemeColorOverride("font_color", color);
			symbolLabel.AddThemeFontSizeOverride("font_size", 22);

			Label nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.AddThemeColorOverride("font_color", color);
			nameLabel.AddThemeFontSizeOverride("font_size", 22);

			titleRow.AddChild(symbolLabel);
			titleRow.AddChild(nameLabel);

			Label descLabel = new Label();
			descLabel.Text = description;
			descLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.93f));
			descLabel.AddThemeFontSizeOverride("font_size", 17);
			descLabel.AutowrapMode = TextServer.AutowrapMode.Word;
			descLabel.CustomMinimumSize = new Vector2(480, 0);

			entry.AddChild(titleRow);
			entry.AddChild(descLabel);

			_affixList.AddChild(entry);
		}

		private static Color ParseColor(string hex)
		{
			if (string.IsNullOrEmpty(hex)) return new Color(0.91f, 0.64f, 0.29f);
			if (!hex.StartsWith("#")) hex = "#" + hex;
			return new Color(hex);
		}
	}
}
