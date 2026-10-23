using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// **角色一览**：列出所有可玩角色，点单个角色进详情（属性 / 卡组 / 大招）。
	///
	/// 为什么要有这个 —— 之前**全项目没有任何查看角色信息的入口**：
	/// 选人界面点角色是「选中」，战斗/地图界面只看得到头像 + 怒气环。
	/// 主人 2026-10-23：「没有就做个按钮，点击后进入角色列表，然后点击单个角色能进入查看角色信息」。
	///
	/// 版式对照参考游戏《终末前夜 Morimens》的「人物列表」+「详细属性」两页。
	/// </summary>
	public partial class CharacterGallery : Control
	{
		private const string ItemScenePath = "res://Scenes/UI/CharacterGalleryItem.tscn";
		private const string CardEntryScenePath = "res://Scenes/UI/CharacterCardEntry.tscn";

		/// <summary>大招怒气上限（与 <c>RageManager.MaxRage</c> 一致）</summary>
		private const int UltimateRageCost = 100;

		private Control _listView;
		private Control _detailView;
		private GridContainer _grid;
		private Label _countLabel;
		private TextureRect _portraitRect;
		private Label _nameLabel;
		private Label _typeLabel;
		private Label _statsLabel;
		private Label _ultimateLabel;
		private HBoxContainer _cardsRow;
		private Button _backButton;
		private Button _backToListButton;

		public override void _Ready()
		{
			GD.Print("[CharacterGallery] _Ready called");

			_listView = GetNodeOrNull<Control>("ListView");
			_detailView = GetNodeOrNull<Control>("DetailView");
			_grid = GetNodeOrNull<GridContainer>("ListView/VBox/Scroll/Grid");
			_countLabel = GetNodeOrNull<Label>("ListView/VBox/Header/CountLabel");
			_portraitRect = GetNodeOrNull<TextureRect>("DetailView/Panel/Margin/VBox/Header/PortraitRect");
			_nameLabel = GetNodeOrNull<Label>("DetailView/Panel/Margin/VBox/Header/InfoBox/NameLabel");
			_typeLabel = GetNodeOrNull<Label>("DetailView/Panel/Margin/VBox/Header/InfoBox/TypeLabel");
			_statsLabel = GetNodeOrNull<Label>("DetailView/Panel/Margin/VBox/Header/InfoBox/StatsLabel");
			_ultimateLabel = GetNodeOrNull<Label>("DetailView/Panel/Margin/VBox/Header/InfoBox/UltimateLabel");
			_cardsRow = GetNodeOrNull<HBoxContainer>("DetailView/Panel/Margin/VBox/CardsRow");
			_backButton = GetNodeOrNull<Button>("ListView/VBox/Header/BackButton");
			_backToListButton = GetNodeOrNull<Button>("DetailView/Panel/Margin/VBox/Footer/BackToListButton");

			if (_grid == null) GD.PrintErr("[CharacterGallery] 未找到 ListView/VBox/Scroll/Grid");
			if (_cardsRow == null) GD.PrintErr("[CharacterGallery] 未找到 CardsRow");
			if (_backButton == null) GD.PrintErr("[CharacterGallery] 未找到 BackButton");
			if (_backToListButton == null) GD.PrintErr("[CharacterGallery] 未找到 BackToListButton");

			if (_backButton != null) _backButton.Pressed += OnBackToMenuPressed;
			if (_backToListButton != null) _backToListButton.Pressed += OnBackToListPressed;

			BuildList();

			GD.Print("[CharacterGallery] _Ready completed");
		}

		/// <summary>列出所有可玩角色</summary>
		private void BuildList()
		{
			if (_grid == null) return;

			foreach (Node child in _grid.GetChildren())
			{
				child.QueueFree();
			}

			List<CharacterDefinition> characters = CharacterDefinition.GetPlayableCharacters();
			if (_countLabel != null) _countLabel.Text = $"共 {characters.Count} 名";

			PackedScene itemScene = ResourceLoader.Load<PackedScene>(ItemScenePath);
			if (itemScene == null)
			{
				GD.PrintErr($"[CharacterGallery] 列表项场景不存在: {ItemScenePath}");
				return;
			}

			foreach (CharacterDefinition character in characters)
			{
				CharacterGalleryItem item = itemScene.Instantiate<CharacterGalleryItem>();
				// ⚠️ **必须先 AddChild 再 Setup**：节点没进场景树时 _Ready 还没跑，
				//    里面的 Label 引用全是 null → Setup 写不进去（表现就是列表里一片默认文字）。
				_grid.AddChild(item);
				item.Setup(character);
				item.OnSelected = ShowDetail;
			}

			GD.Print($"[CharacterGallery] 列出 {characters.Count} 名可玩角色");
		}

		/// <summary>点某个角色 → 显示详情</summary>
		private void ShowDetail(CharacterDefinition character)
		{
			if (character == null) return;

			GD.Print($"[CharacterGallery] ShowDetail called: {character.CharacterId}（{character.Name}）");

			if (_listView != null) _listView.Visible = false;
			if (_detailView != null) _detailView.Visible = true;

			if (_nameLabel != null) _nameLabel.Text = character.Name;

			// 类型：先显示代码里的 Archetype 中文名。
			// ⚠️ 主人 2026-10-24 说正式定位以后给（参考图截图上有类型）—— 截图到手后替换。
			if (_typeLabel != null) _typeLabel.Text = character.Archetype.GetDisplayName();

			if (_statsLabel != null)
			{
				_statsLabel.Text =
					$"攻击　{character.BaseAttack}\n" +
					$"防御　{character.BaseDefense}\n" +
					$"体质　{character.BaseConstitution}\n" +
					$"暴击率　{character.BaseCritRate}%\n" +
					$"暴击伤害　{character.BaseCritDamage}%\n" +
					$"基础狂气　{UltimateRageCost}";
			}

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

			FillUltimate(character);
			FillCards(character);
		}

		private void FillUltimate(CharacterDefinition character)
		{
			if (_ultimateLabel == null) return;

			UltimateSkill ultimate = UltimateSkill.GetUltimateById(character.UltimateSkillId);
			if (ultimate == null)
			{
				_ultimateLabel.Text = "（没有配置大招）";
				return;
			}

			_ultimateLabel.Text = $"大招：{ultimate.Name}（怒气 {ultimate.RageCost}）\n{ultimate.Description}";
		}

		/// <summary>卡组：打击 / 防御 / 3 张专属</summary>
		private void FillCards(CharacterDefinition character)
		{
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

			CharacterAttributes attributes = character.GetAttributes();
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

			foreach (string cardId in cardIds)
			{
				Card card = CharacterCardLoader.CreateInstance(cardId, attributes);
				if (card == null)
				{
					GD.PrintErr($"[CharacterGallery] 卡牌取不到: {cardId}");
					continue;
				}

				CharacterCardEntry entry = entryScene.Instantiate<CharacterCardEntry>();
				_cardsRow.AddChild(entry);
				entry.Setup(card);
			}
		}

		private void OnBackToMenuPressed()
		{
			GD.Print("[CharacterGallery] OnBackToMenuPressed called");
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
			GD.Print("[CharacterGallery] OnBackToMenuPressed completed");
		}

		private void OnBackToListPressed()
		{
			GD.Print("[CharacterGallery] OnBackToListPressed called");
			if (_detailView != null) _detailView.Visible = false;
			if (_listView != null) _listView.Visible = true;
			GD.Print("[CharacterGallery] OnBackToListPressed completed");
		}
	}
}
