using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// 角色一览「卡牌总览」里的**一张卡牌**。
	///
	/// 版式照参考游戏《终末前夜 Morimens》的卡牌总览：
	/// **等级数字在卡牌外面上方** → 卡内顶部（费用 + 名称）→ 插画（占满剩余空间）
	/// → **正文说明（从底部往上排）** → 底部类型标签（指令 / 衍生 / 灵知觉醒）。
	/// </summary>
	public partial class CharacterCardEntry : VBoxContainer
	{
		private Label _levelLabel;
		private PanelContainer _cardPanel;
		private Label _costLabel;
		private Label _nameLabel;
		private TextureRect _artRect;
		private Label _descLabel;
		private Label _typeLabel;

		public override void _Ready()
		{
			_levelLabel = GetNodeOrNull<Label>("LevelLabel");
			_cardPanel = GetNodeOrNull<PanelContainer>("CardPanel");
			_costLabel = GetNodeOrNull<Label>("CardPanel/Margin/VBox/TopRow/CostBadge/CostLabel");
			_nameLabel = GetNodeOrNull<Label>("CardPanel/Margin/VBox/TopRow/NameLabel");
			_artRect = GetNodeOrNull<TextureRect>("CardPanel/Margin/VBox/ArtRect");
			_descLabel = GetNodeOrNull<Label>("CardPanel/Margin/VBox/DescLabel");
			_typeLabel = GetNodeOrNull<Label>("CardPanel/Margin/VBox/TypeLabel");

			if (_levelLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 LevelLabel");
			if (_cardPanel == null) GD.PrintErr("[CharacterCardEntry] 未找到 CardPanel");
			if (_costLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 CostLabel");
			if (_nameLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 NameLabel");
			if (_artRect == null) GD.PrintErr("[CharacterCardEntry] 未找到 ArtRect");
			if (_descLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 DescLabel");
			if (_typeLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 TypeLabel");
		}

		/// <summary>填入卡牌数据（数值走百分比口径，与战斗同一个函数）</summary>
		/// <param name="card">卡牌</param>
		/// <param name="ownerName">所属角色名（文案里要写「XX获得 x 点怒气」）</param>
		public void Setup(Card card, string ownerName = "")
		{
			if (card == null) return;

			if (ownerName != null && !string.IsNullOrEmpty(ownerName))
			{
				card.OwnerName = ownerName;
			}

			if (_nameLabel != null) _nameLabel.Text = card.Name;
			if (_costLabel != null) _costLabel.Text = card.Cost.ToString();

			// 等级：目前统一按**满级**显示（对照参考图卡牌总览里的 6/6、5/6…）。
			// ⚠️ 卡牌升级系统还没做（主人 2026-10-24：「卡牌升级先不用管」）→ 先显示假值。
			if (_levelLabel != null) _levelLabel.Text = $"{card.MaxLevel}/{card.MaxLevel}";

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

			if (_artRect != null)
			{
				Texture2D art = CardArtResolver.Resolve(card);
				if (art != null)
				{
					_artRect.Texture = art;
				}
				else
				{
					GD.PrintErr($"[CharacterCardEntry] 卡面插画找不到：{card.CardId}");
					_artRect.Texture = null;
				}
			}
		}

		/// <summary>选中态（金色边框，照参考图当前选中的卡）</summary>
		public void SetSelected(bool selected)
		{
			if (_cardPanel == null) return;

			var style = new StyleBoxFlat();
			style.BgColor = new Color(0.05f, 0.05f, 0.08f, 1f);
			style.BorderColor = selected
				? new Color(0.95f, 0.82f, 0.35f)
				: new Color(0.78f, 0.68f, 0.42f);
			style.SetBorderWidthAll(selected ? 3 : 2);
			style.SetCornerRadiusAll(6);
			_cardPanel.AddThemeStyleboxOverride("panel", style);
		}
	}
}
