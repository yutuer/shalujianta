using Godot;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI.CharacterGallery
{
	/// <summary>
	/// 角色详情里的**一张卡牌小项**：插画 + 卡名 + 费用 + 伤害/护盾。
	/// </summary>
	public partial class CharacterCardEntry : Panel
	{
		private TextureRect _artRect;
		private Label _nameLabel;
		private Label _costLabel;
		private Label _valueLabel;

		public override void _Ready()
		{
			_artRect = GetNodeOrNull<TextureRect>("VBox/ArtRect");
			_nameLabel = GetNodeOrNull<Label>("VBox/NameLabel");
			_costLabel = GetNodeOrNull<Label>("VBox/CostLabel");
			_valueLabel = GetNodeOrNull<Label>("VBox/ValueLabel");

			if (_artRect == null) GD.PrintErr("[CharacterCardEntry] 未找到 VBox/ArtRect");
			if (_nameLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 VBox/NameLabel");
			if (_costLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 VBox/CostLabel");
			if (_valueLabel == null) GD.PrintErr("[CharacterCardEntry] 未找到 VBox/ValueLabel");
		}

		/// <summary>填入卡牌数据（数值走百分比口径，与战斗一致）</summary>
		public void Setup(Card card)
		{
			if (card == null) return;

			if (_nameLabel != null) _nameLabel.Text = card.Name;
			if (_costLabel != null) _costLabel.Text = $"费用 {card.Cost}";

			if (_valueLabel != null)
			{
				var parts = new System.Collections.Generic.List<string>();
				int damage = card.GetFinalDamage(card.LinkedAttributes);
				int shield = card.GetFinalShield(card.LinkedAttributes);
				if (damage > 0) parts.Add($"伤害 {damage}");
				if (shield > 0) parts.Add($"护盾 {shield}");
				if (parts.Count == 0) parts.Add(card.IsAttack ? "效果卡" : "辅助卡");
				_valueLabel.Text = string.Join(" / ", parts);
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
	}
}
