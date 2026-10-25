using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// **角色一览**（三个视图）：
	///
	/// ① 列表视图（默认）：左边角色网格 + 中间大立绘 + **右边选中角色的简易信息**
	///    （名称 / 类型 / 等级 / 5 卡 + 必杀技的等级 / 天赋等级占位 / 三个简单属性
	///    + 「详细信息」按钮 + 「详情」按钮）—— 版式照参考图《终末前夜 Morimens》的「人物列表」。
	/// ② 属性详情弹窗：点「详细信息」→ 体质 / 攻击 / 防御 + 暴击率 / 暴击伤害 / 界域精通 /
	///    伤害强效 / 基础狂气 / 狂气回充等级 / 银钥充能等级 / 黑印掉落 / 死亡抵抗。
	/// ③ 卡牌总览：点「详情」→ **左边一竖列角色头像**（点头像快速切角色）
	///    + 右边该角色的必杀技 + 5 张卡 —— 版式照参考图的「卡牌总览」。
	///
	/// 主人 2026-10-24 明确**暂时不管**的：种族天赋图标（4 种，最后做）、
	/// 名座三颗星、同调率（好感度）。
	/// </summary>
	public partial class CharacterGallery : Control
	{
		private const string ItemScenePath = "res://Scenes/UI/CharacterGalleryItem.tscn";
		private const string CardEntryScenePath = "res://Scenes/UI/CharacterCardEntry.tscn";
		private const string PortraitScenePath = "res://Scenes/UI/CharacterPortraitItem.tscn";

		/// <summary>角色等级：目前所有角色固定 60 级（主人 2026-10-24）</summary>
		private const int CharacterLevel = 60;

		/// <summary>卡牌等级上限：参考图「卡牌总览」顶部那个 Lv.6/6 指的是**卡牌等级**</summary>
		private const int CardMaxLevel = 6;

		/// <summary>
		/// 竖列头像项的高度（要和 <c>CharacterPortraitItem.tscn</c> 里的 custom_minimum_size 保持一致）——
		/// 用来把滚动条滚到当前选中的那个头像。
		/// </summary>
		private const float PortraitItemHeight = 132f;

		/// <summary>竖列头像项之间的间距（同上）</summary>
		private const float PortraitItemSeparation = 8f;

		/// <summary>卡牌 + 必杀技的等级：目前统一满级（主人 2026-10-24）</summary>
		private const string CardLevelText = "6/6/6/6/6/6";

		/// <summary>天赋等级：占位（主人 2026-10-24：「你先写成 1/1/1」）</summary>
		private const string TalentLevelText = "1/1/1";

		// ---- 列表视图 ----
		private Control _listView;
		private GridContainer _grid;
		private Label _countLabel;
		private TextureRect _portraitRect;
		private Label _nameLabel;
		private Label _typeLabel;
		private Label _levelLabel;
		private Label _cardLevelsLabel;
		private Label _talentLabel;
		private Label _statsLabel;
		private Button _backButton;
		private Button _attributeButton;
		private Button _detailButton;

		// ---- 属性详情弹窗 ----
		private ColorRect _attributeDim;
		private PanelContainer _attributePanel;
		private GridContainer _statsGrid;
		private Button _attributeCloseButton;

		// ---- 卡牌总览 ----
		private Control _cardsView;
		private CardPreviewOverlay cardPreviewOverlay;
		private DerivedCardInfoPanel derivedCardInfoPanel;
		private ScrollContainer _avatarScroll;
		private VBoxContainer _avatarColumn;
		private Label _ultimateLevelLabel;
		private TextureRect _ultimateAvatarRect;
		private Label _ultimateNameLabel;
		private Label _ultimateBurstLabel;
		private RichTextLabel _ultimateDescText;
		private HBoxContainer _cardsRow;
		private Button _backToListButton;

		private List<CharacterDefinition> _characters = new List<CharacterDefinition>();
		private CharacterDefinition _current;

		/// <summary>
		/// 待办：把竖列滚到当前选中的角色。
		/// ⚠️ **必须延后一帧** —— 刚 AddChild 完，ScrollContainer 的内容还没排版，
		/// 滚动条的 max_value 还是 0，此刻设 value 会被 clamp 成 0（滚不动）。
		/// </summary>
		private bool _pendingAvatarScroll = false;

		public override void _Process(double delta)
		{
			if (!_pendingAvatarScroll) return;
			_pendingAvatarScroll = false;
			ScrollAvatarColumnToCurrent();
		}

		public override void _Ready()
		{
			GD.Print("[CharacterGallery] _Ready called");

			_listView = GetNodeOrNull<Control>("ListView");
			_grid = GetNodeOrNull<GridContainer>("ListView/HBox/LeftColumn/Scroll/Grid");
			_countLabel = GetNodeOrNull<Label>("ListView/HBox/LeftColumn/Header/CountLabel");
			_portraitRect = GetNodeOrNull<TextureRect>("ListView/HBox/PortraitRect");
			_nameLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/NameLabel");
			_typeLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/TypeLabel");
			_levelLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/LevelLabel");
			_cardLevelsLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/CardLevelsLabel");
			_talentLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/TalentLabel");
			_statsLabel = GetNodeOrNull<Label>("ListView/HBox/InfoColumn/StatsLabel");
			_backButton = GetNodeOrNull<Button>("ListView/HBox/LeftColumn/Header/BackButton");
			_attributeButton = GetNodeOrNull<Button>("ListView/HBox/InfoColumn/AttributeButton");
			_detailButton = GetNodeOrNull<Button>("ListView/HBox/InfoColumn/DetailButton");

			_attributeDim = GetNodeOrNull<ColorRect>("AttributeDim");
			_attributePanel = GetNodeOrNull<PanelContainer>("AttributePanel");
			_statsGrid = GetNodeOrNull<GridContainer>("AttributePanel/Margin/VBox/StatsGrid");
			_attributeCloseButton = GetNodeOrNull<Button>("AttributePanel/Margin/VBox/CloseButton");

			_cardsView = GetNodeOrNull<Control>("CardsView");
			cardPreviewOverlay = GetNodeOrNull<CardPreviewOverlay>("CardPreviewOverlay");
			derivedCardInfoPanel = GetNodeOrNull<DerivedCardInfoPanel>("DerivedCardInfoPanel");
			_avatarScroll = GetNodeOrNull<ScrollContainer>("CardsView/HBox/AvatarScroll");
			_avatarColumn = GetNodeOrNull<VBoxContainer>("CardsView/HBox/AvatarScroll/AvatarColumn");
			_ultimateLevelLabel = GetNodeOrNull<Label>("CardsView/HBox/CardsSide/UltimateHeroBox/UltimateLevelLabel");
			_ultimateAvatarRect = GetNodeOrNull<TextureRect>("CardsView/HBox/CardsSide/UltimateHeroBox/AvatarFrame/AvatarRect");
			_ultimateNameLabel = GetNodeOrNull<Label>("CardsView/HBox/CardsSide/UltimatePanel/Margin/VBox/TitleRow/UltimateNameLabel");
			_ultimateBurstLabel = GetNodeOrNull<Label>("CardsView/HBox/CardsSide/UltimatePanel/Margin/VBox/TitleRow/UltimateBurstLabel");
			_ultimateDescText = GetNodeOrNull<RichTextLabel>("CardsView/HBox/CardsSide/UltimatePanel/Margin/VBox/UltimateDescText");
			_cardsRow = GetNodeOrNull<HBoxContainer>("CardsView/HBox/CardsSide/CardsRow");
			_backToListButton = GetNodeOrNull<Button>("CardsView/HBox/CardsSide/Header/CircleBackButton");

			if (_grid == null) GD.PrintErr("[CharacterGallery] 未找到 Grid");
			if (_statsGrid == null) GD.PrintErr("[CharacterGallery] 未找到 StatsGrid");
			if (_cardsRow == null) GD.PrintErr("[CharacterGallery] 未找到 CardsRow");
			if (_avatarColumn == null) GD.PrintErr("[CharacterGallery] 未找到 AvatarColumn");

			if (_backButton != null) _backButton.Pressed += OnBackToMenuPressed;
			if (_attributeButton != null) _attributeButton.Pressed += OnAttributeButtonPressed;
			if (_detailButton != null) _detailButton.Pressed += OnDetailButtonPressed;
			if (_attributeCloseButton != null) _attributeCloseButton.Pressed += OnAttributeClosePressed;
			if (_backToListButton != null) _backToListButton.Pressed += OnBackToListPressed;
			// 必杀技说明里的「圣心」是可点的 → 弹出衍生卡说明面板
			if (_ultimateDescText != null) _ultimateDescText.MetaClicked += OnUltimateDescMetaClicked;

			BuildList();
			ShowListView();

			GD.Print("[CharacterGallery] _Ready completed");
		}

		// ==================== 视图切换 ====================

		private void ShowListView()
		{
			if (_listView != null) _listView.Visible = true;
			if (_cardsView != null) _cardsView.Visible = false;
			if (cardPreviewOverlay != null) cardPreviewOverlay.HideCard();
			if (derivedCardInfoPanel != null) derivedCardInfoPanel.HidePanel();
			HideAttributePanel();
		}

		private void ShowCardsView()
		{
			if (_listView != null) _listView.Visible = false;
			if (_cardsView != null) _cardsView.Visible = true;
			if (cardPreviewOverlay != null) cardPreviewOverlay.HideCard();
			if (derivedCardInfoPanel != null) derivedCardInfoPanel.HidePanel();
			HideAttributePanel();

			BuildAvatarColumn();
			if (_current != null) FillCardsView(_current);
		}

		// ==================== 列表 ====================

		/// <summary>列出所有可玩角色</summary>
		private void BuildList()
		{
			if (_grid == null) return;

			foreach (Node child in _grid.GetChildren())
			{
				child.QueueFree();
			}

			_characters = CharacterDefinition.GetPlayableCharacters();
			if (_countLabel != null) _countLabel.Text = $"共 {_characters.Count} 名";

			PackedScene itemScene = ResourceLoader.Load<PackedScene>(ItemScenePath);
			if (itemScene == null)
			{
				GD.PrintErr($"[CharacterGallery] 列表项场景不存在: {ItemScenePath}");
				return;
			}

			foreach (CharacterDefinition character in _characters)
			{
				CharacterGalleryItem item = itemScene.Instantiate<CharacterGalleryItem>();
				// ⚠️ **必须先 AddChild 再 Setup**：节点没进场景树时 _Ready 还没跑，
				//    里面的 Label 引用全是 null → Setup 写不进去（表现就是列表里一片默认文字）。
				_grid.AddChild(item);
				item.Setup(character, CharacterLevel);
				item.OnSelected = OnCharacterSelected;
			}

			GD.Print($"[CharacterGallery] 列出 {_characters.Count} 名可玩角色");

			if (_characters.Count > 0) OnCharacterSelected(_characters[0]);
		}

		/// <summary>选中某个角色 → 刷新立绘与右侧信息栏</summary>
		private void OnCharacterSelected(CharacterDefinition character)
		{
			if (character == null) return;

			_current = character;
			GD.Print($"[CharacterGallery] 选中：{character.CharacterId}（{character.Name}）");

			if (_portraitRect != null)
			{
				string portraitPath = character.GetPortraitPath();
				if (!string.IsNullOrEmpty(portraitPath) && ResourceLoader.Exists(portraitPath))
				{
					_portraitRect.Texture = ResourceLoader.Load<Texture2D>(portraitPath);
				}
				else
				{
					GD.PrintErr($"[CharacterGallery] 立绘不存在: {portraitPath}");
					_portraitRect.Texture = null;
				}
			}

			if (_nameLabel != null) _nameLabel.Text = character.Name;
			// 类型：伤害 / 辅助 / 防御（暂按 Archetype 映射，等主人给正式定位）
			if (_typeLabel != null) _typeLabel.Text = character.Archetype.GetRoleType();
			if (_levelLabel != null) _levelLabel.Text = $"Lv.{CharacterLevel}";
			if (_cardLevelsLabel != null) _cardLevelsLabel.Text = CardLevelText;
			if (_talentLabel != null) _talentLabel.Text = TalentLevelText;

			if (_statsLabel != null)
			{
				_statsLabel.Text =
					$"体质　{character.BaseConstitution}\n" +
					$"攻击　{character.BaseAttack}\n" +
					$"防御　{character.BaseDefense}";
			}
		}

		// ==================== 属性详情弹窗 ====================

		private void OnAttributeButtonPressed()
		{
			GD.Print("[CharacterGallery] OnAttributeButtonPressed called");
			if (_current == null) return;

			FillAttributePanel(_current);

			if (_attributeDim != null) _attributeDim.Visible = true;
			if (_attributePanel != null) _attributePanel.Visible = true;

			GD.Print("[CharacterGallery] OnAttributeButtonPressed completed");
		}

		/// <summary>
		/// 属性详情的内容。
		/// ⚠️ **只有攻击 / 防御 / 体质 / 暴击率 / 暴击伤害 / 基础狂气 是我们的真值**，
		/// 其余（界域精通 / 伤害强效 / 狂气回充等级 / 银钥充能等级 / 黑印掉落 / 死亡抵抗）
		/// 目前**套用参考图上的数字占位**，等主人给公式后再接数据表。
		/// </summary>
		private void FillAttributePanel(CharacterDefinition character)
		{
			if (_statsGrid == null) return;

			foreach (Node child in _statsGrid.GetChildren())
			{
				child.QueueFree();
			}

			var left = new List<string>
			{
				$"攻击　{character.BaseAttack}",
				$"防御　{character.BaseDefense}",
				$"暴击率　{character.BaseCritRate}%",
				$"暴击伤害　{character.BaseCritDamage}%",
				"界域精通　0",
				"伤害强效　4.8%"
			};

			var right = new List<string>
			{
				$"体质　{character.BaseConstitution}",
				"基础狂气　100",
				"狂气回充等级　0",
				"银钥充能等级　15",
				"黑印掉落　0%",
				"死亡抵抗　0%"
			};

			for (int i = 0; i < left.Count; i++)
			{
				_statsGrid.AddChild(MakeStatLabel(left[i]));
				_statsGrid.AddChild(MakeStatLabel(right[i]));
			}
		}

		private static Label MakeStatLabel(string text)
		{
			var label = new Label();
			label.Text = text;
			label.AddThemeFontSizeOverride("font_size", 19);
			label.AddThemeColorOverride("font_color", new Color(0.88f, 0.9f, 0.95f));
			return label;
		}

		private void OnAttributeClosePressed()
		{
			GD.Print("[CharacterGallery] OnAttributeClosePressed called");
			HideAttributePanel();
			GD.Print("[CharacterGallery] OnAttributeClosePressed completed");
		}

		private void HideAttributePanel()
		{
			if (_attributeDim != null) _attributeDim.Visible = false;
			if (_attributePanel != null) _attributePanel.Visible = false;
		}

		// ==================== 卡牌总览 ====================

		/// <summary>点「详情」→ 进入卡牌总览视图</summary>
		private void OnDetailButtonPressed()
		{
			GD.Print("[CharacterGallery] OnDetailButtonPressed called");
			ShowCardsView();
			GD.Print("[CharacterGallery] OnDetailButtonPressed completed");
		}

		/// <summary>左边那一竖列头像（点头像切右侧卡组）</summary>
		private void BuildAvatarColumn()
		{
			if (_avatarColumn == null) return;

			foreach (Node child in _avatarColumn.GetChildren())
			{
				child.QueueFree();
			}

			PackedScene portraitScene = ResourceLoader.Load<PackedScene>(PortraitScenePath);
			if (portraitScene == null)
			{
				GD.PrintErr($"[CharacterGallery] 头像项场景不存在: {PortraitScenePath}");
				return;
			}

			foreach (CharacterDefinition character in _characters)
			{
				CharacterPortraitItem portrait = portraitScene.Instantiate<CharacterPortraitItem>();
				_avatarColumn.AddChild(portrait);
				portrait.Setup(character, _current != null && character.CharacterId == _current.CharacterId);
				portrait.OnSelected = OnPortraitSelected;
			}

			// 延后一帧再滚（见 _pendingAvatarScroll 的说明）
			_pendingAvatarScroll = true;
		}

		/// <summary>
		/// 把竖列滚到当前选中的角色。
		/// 13 个头像放不下一屏 —— 不滚的话，从列表进「详情」时当前角色在视口外，**看不到高亮**。
		/// </summary>
		private void ScrollAvatarColumnToCurrent()
		{
			if (_avatarScroll == null || _current == null) return;

			int index = _characters.FindIndex(c => c != null && c.CharacterId == _current.CharacterId);
			if (index < 0) return;

			var bar = _avatarScroll.GetVScrollBar();
			if (bar == null) return;

			float itemTop = index * (PortraitItemHeight + PortraitItemSeparation);
			float value = itemTop - _avatarScroll.Size.Y * 0.5f + PortraitItemHeight * 0.5f;
			bar.Value = Mathf.Clamp(value, 0f, bar.MaxValue);

			GD.Print($"[CharacterGallery] 竖列滚到 {index + 1}/{_characters.Count}（{_current.Name}）value={bar.Value:F0}/{bar.MaxValue:F0}");
		}

		/// <summary>点左边竖列的头像 → 换右侧的卡牌组</summary>
		private void OnPortraitSelected(CharacterDefinition character)
		{
			if (character == null) return;

			_current = character;
			GD.Print($"[CharacterGallery] 切到：{character.Name}");

			BuildAvatarColumn();
			FillCardsView(character);
		}

		private void FillCardsView(CharacterDefinition character)
		{
			if (character == null) return;

			CharacterAttributes attributes = character.GetAttributes();

			UltimateSkill ultimate = UltimateSkill.GetUltimateById(character.UltimateSkillId);
			// 把角色属性挂上去 —— 百分比口径的必杀技（攻击力 × 百分比）要靠它算数值
			if (ultimate != null) ultimate.LinkedAttributes = attributes;

			if (_ultimateNameLabel != null)
			{
				// ⚠️ **不再带「必杀技：」前缀**（主人 2026-10-25）——
				//    照参考图《终末前夜 Morimens》：标题行左边只有**技能名**，
				//    右边是黄色的「狂气爆发」（= 怒气满了能释放的技能，与「必杀技 / 大招」同义）。
				_ultimateNameLabel.Text = ultimate != null ? ultimate.Name : "未配置";
			}
			if (_ultimateBurstLabel != null)
			{
				// 「狂气爆发」—— 标题行右上角的黄色标签（参考图同位置同色）
				_ultimateBurstLabel.Text = "狂气爆发";
			}
			if (_ultimateDescText != null)
			{
				_ultimateDescText.Text = BuildUltimateDescription(ultimate, attributes);
			}

			// 必杀技等级（头像上方）—— 卡牌等级系统还没做，先统一按满级显示（同卡牌的做法）
			if (_ultimateLevelLabel != null)
			{
				int maxLevel = ultimate != null ? ultimate.MaxLevel : CardMaxLevel;
				_ultimateLevelLabel.Text = $"Lv.{maxLevel}/{maxLevel}";
			}

			// 头像（必杀技说明的正上方居中）
			if (_ultimateAvatarRect != null)
			{
				string avatarPath = $"res://Assets/Portraits/avatar_{character.CharacterId}.svg";
				if (ResourceLoader.Exists(avatarPath))
				{
					_ultimateAvatarRect.Texture = ResourceLoader.Load<Texture2D>(avatarPath);
				}
				else
				{
					GD.PrintErr($"[CharacterGallery] 头像不存在: {avatarPath}");
					_ultimateAvatarRect.Texture = null;
				}
			}

			if (_cardsRow == null) return;

			foreach (Node child in _cardsRow.GetChildren())
			{
				child.QueueFree();
			}

			PackedScene entryScene = ResourceLoader.Load<PackedScene>(CardEntryScenePath);
			if (entryScene == null)
			{
				GD.PrintErr($"[CharacterGallery] 卡牌项场景不存在: {CardEntryScenePath}");
				return;
			}

			foreach (string cardId in GetDeckCardIds(character))
			{
				Card card = CharacterCardLoader.CreateInstance(cardId, attributes);
				if (card == null)
				{
					GD.PrintErr($"[CharacterGallery] 卡牌取不到: {cardId}");
					continue;
				}

				CharacterCardEntry entry = entryScene.Instantiate<CharacterCardEntry>();
				_cardsRow.AddChild(entry);
				// 长按 → 弹出放大预览 + 词缀说明（照参考游戏）
				entry.OnLongPress = OnCardLongPress;
				// 传角色名 —— 卡面文案要写「波吕克斯获得 10 点怒气」
				entry.Setup(card, character.Name);
			}
		}

		/// <summary>
		/// **必杀技说明文本** = 算出来的数值（造成 546 点伤害）+ 数据表里的补充说明，
		/// 其中「圣心」那个词做成**可点击链接**（点了弹出衍生卡说明）。
		/// </summary>
		/// <param name="ultimate">必杀技</param>
		/// <param name="attributes">角色属性（算数值要用）</param>
		/// <returns>BBCode 文本</returns>
		private static string BuildUltimateDescription(UltimateSkill ultimate, CharacterAttributes attributes)
		{
			if (ultimate == null) return "";

			// ① 数值部分（与战斗同一个函数算出来的，不会漂）
			string generated = DescriptionGenerator.GenerateUltimateDescription(ultimate, attributes);

			// ② 补充说明（数据表里的原文）
			string extra = ultimate.Description ?? "";

			// ③ 「圣心」→ 可点击
			// ⚠️ url 的值必须是**卡牌 id**（点击时传回来的 meta 就是 url 的值），
			//    显示文字才是给人看的名字。
			if (!string.IsNullOrEmpty(ultimate.DerivedCardId) &&
				!string.IsNullOrEmpty(ultimate.DerivedCardName) &&
				!string.IsNullOrEmpty(extra))
			{
				extra = extra.Replace(ultimate.DerivedCardName,
					$"[url={ultimate.DerivedCardId}]{ultimate.DerivedCardName}[/url]");
			}

			if (string.IsNullOrEmpty(generated)) return extra;
			if (string.IsNullOrEmpty(extra)) return generated;
			return generated + "，" + extra;
		}

		/// <summary>
		/// 点了必杀技说明里的链接 → 弹出对应的**衍生卡说明**面板。
		/// </summary>
		/// <param name="meta">链接目标（= 衍生卡 id）</param>
		private void OnUltimateDescMetaClicked(Variant meta)
		{
			string cardId = meta.AsString();
			GD.Print($"[CharacterGallery] 点了必杀技说明里的链接：{cardId}");

			if (derivedCardInfoPanel == null)
			{
				GD.PrintErr("[CharacterGallery] 未找到 DerivedCardInfoPanel 节点");
				return;
			}
			if (_current == null) return;

			Card derived = CharacterCardLoader.CreateInstance(cardId, _current.GetAttributes());
			if (derived == null)
			{
				GD.PrintErr($"[CharacterGallery] 衍生卡取不到: {cardId}");
				return;
			}

			derivedCardInfoPanel.ShowCard(derived, _current.Name);
		}

		/// <summary>卡组顺序：打击 / 防御 / 3 张专属</summary>
		private static List<string> GetDeckCardIds(CharacterDefinition character)
		{
			List<string> cardIds = new List<string>();
			if (!string.IsNullOrEmpty(character.AttackCardId)) cardIds.Add(character.AttackCardId);
			if (!string.IsNullOrEmpty(character.DefenseCardId)) cardIds.Add(character.DefenseCardId);
			if (character.SpecialCardIds != null)
			{
				foreach (string specialId in character.SpecialCardIds)
				{
					if (!string.IsNullOrEmpty(specialId)) cardIds.Add(specialId);
				}
			}
			return cardIds;
		}

		// ==================== 返回 ====================

		/// <summary>
		/// 长按卡牌 → 弹出**放大预览层**（卡牌放大 + 右侧词缀说明）
		/// </summary>
		private void OnCardLongPress(Card card)
		{
			if (card == null) return;

			if (cardPreviewOverlay == null)
			{
				GD.PrintErr("[CharacterGallery] 未找到 CardPreviewOverlay 节点，长按预览不可用");
				return;
			}

			cardPreviewOverlay.ShowCard(card, CardArtResolver.Resolve(card));
		}

		private void OnBackToListPressed()
		{
			GD.Print("[CharacterGallery] OnBackToListPressed called");
			ShowListView();
			GD.Print("[CharacterGallery] OnBackToListPressed completed");
		}

		private void OnBackToMenuPressed()
		{
			GD.Print("[CharacterGallery] OnBackToMenuPressed called");
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
			GD.Print("[CharacterGallery] OnBackToMenuPressed completed");
		}
	}
}
