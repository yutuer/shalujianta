using Godot;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.UI.CardDetailPanel
{
	/// <summary>
	/// **卡牌详情面板** —— 点手牌打开。
	///
	/// 版式对照参考游戏《终末前夜 Morimens》的卡牌详情面板：
	/// 左边大卡面（插画 / 卡名 / 费用 / 类型 / 等级），右边「属性」区
	/// —— 关键是显示**系数口径**（「伤害 攻击力×20%」），再给当前值与下一级预览。
	///
	/// ⚠️ **卡牌升级本身不做**（主人 2026-10-24：「卡牌升级先不用管」）——
	/// 这里只把升级入口摆出来，点了给一句提示，不改任何数值。
	/// </summary>
	public partial class CardDetailUI : Control
	{
		private PanelContainer _panel;
		private TextureRect _cardImage;
		private Label _nameLabel;
		private Label _typeLabel;
		private Label _costLabel;
		private Label _levelLabel;
		private Label _statsLabel;
		private Label _affixLabel;
		private Label _descLabel;
		private Label _hintLabel;
		private Button _upgradeButton;
		private Button _closeButton;

		private Card _currentCard;
		private ulong _showTimeMsec;

		public override void _Ready()
		{
			_panel = GetNodeOrNull<PanelContainer>("Panel");
			_cardImage = GetNodeOrNull<TextureRect>("Panel/Margin/VBox/Header/CardImage");
			_nameLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Header/TitleBox/NameLabel");
			_typeLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Header/TitleBox/TypeLabel");
			_costLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Header/TitleBox/CostLabel");
			_levelLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Header/TitleBox/LevelLabel");
			_statsLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/StatsPanel/StatsLabel");
			_affixLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/AffixLabel");
			_descLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/DescLabel");
			_hintLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Footer/HintLabel");
			_upgradeButton = GetNodeOrNull<Button>("Panel/Margin/VBox/Footer/UpgradeButton");
			_closeButton = GetNodeOrNull<Button>("Panel/Margin/VBox/Footer/CloseButton");

			if (_panel == null) GD.PrintErr("[CardDetailUI] 未找到 Panel 节点");
			if (_upgradeButton == null) GD.PrintErr("[CardDetailUI] 未找到 UpgradeButton 节点");
			if (_closeButton == null) GD.PrintErr("[CardDetailUI] 未找到 CloseButton 节点");

			if (_upgradeButton != null)
			{
				_upgradeButton.Pressed += OnUpgradePressed;
			}
			if (_closeButton != null)
			{
				_closeButton.Pressed += OnClosePressed;
			}

			Visible = false;
		}

		/// <summary>
		/// 打开详情面板。
		/// </summary>
		/// <param name="card">要展示的卡牌</param>
		/// <param name="art">卡面插画（没有就留空）</param>
		public void ShowCard(Card card, Texture2D art = null)
		{
			if (card == null)
			{
				GD.PrintErr("[CardDetailUI] ShowCard 收到空卡牌");
				return;
			}

			_currentCard = card;
			CharacterAttributes attributes = card.LinkedAttributes;

			if (_cardImage != null)
			{
				_cardImage.Texture = art;
				_cardImage.Visible = art != null;
			}

			if (_nameLabel != null) _nameLabel.Text = card.Name;
			if (_typeLabel != null) _typeLabel.Text = GetCardTypeText(card);
			if (_costLabel != null) _costLabel.Text = $"费用 {card.Cost}";
			if (_levelLabel != null) _levelLabel.Text = DescriptionGenerator.GenerateLevelInfo(card);

			if (_statsLabel != null)
			{
				_statsLabel.Text = BuildStatsText(card, attributes);
			}

			if (_descLabel != null)
			{
				_descLabel.Text = DescriptionGenerator.GenerateCardDescription(card, attributes);
			}

			if (_affixLabel != null)
			{
				string affix = BuildAffixText(card);
				_affixLabel.Text = affix;
				_affixLabel.Visible = !string.IsNullOrEmpty(affix);
			}

			if (_hintLabel != null)
			{
				_hintLabel.Text = "卡牌升级暂未开放（界面已就位，升级逻辑等主人排期）";
			}
			if (_upgradeButton != null)
			{
				_upgradeButton.Disabled = card.Level >= card.MaxLevel;
				_upgradeButton.Text = card.Level >= card.MaxLevel ? "已达等级上限" : "升级";
			}

			_showTimeMsec = Time.GetTicksMsec();
			Visible = true;
			GD.Print($"[CardDetailUI] ShowCard called: {card.CardId} ({card.Name})");
		}

		public void HideCard()
		{
			Visible = false;
			_currentCard = null;
		}

		/// <summary>点面板外（空白处）→ 关掉</summary>
		public override void _GuiInput(InputEvent @event)
		{
			if (!Visible) return;
			if (@event is not InputEventMouseButton mouseEvent) return;
			if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;

			// 刚打开的 120ms 内不响应（避免「点卡牌松手」立刻又把面板关了）
			if (Time.GetTicksMsec() - _showTimeMsec < 120) return;

			if (_panel != null && _panel.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
			{
				return;
			}

			HideCard();
			AcceptEvent();
		}

		private void OnUpgradePressed()
		{
			GD.Print("[CardDetailUI] OnUpgradePressed called");
			if (_currentCard != null)
			{
				GD.Print($"[CardDetailUI] 卡牌升级暂未开放：{_currentCard.CardId} Lv.{_currentCard.Level}/{_currentCard.MaxLevel}");
			}
			GD.Print("[CardDetailUI] OnUpgradePressed completed");
		}

		private void OnClosePressed()
		{
			GD.Print("[CardDetailUI] OnClosePressed called");
			HideCard();
			GD.Print("[CardDetailUI] OnClosePressed completed");
		}

		/// <summary>
		/// 属性区文本：**系数口径 + 当前值 + 下一级预览**。
		/// 系数口径是参考图右侧「属性」面板的写法（「伤害 攻击力×20%」）。
		/// </summary>
		private static string BuildStatsText(Card card, CharacterAttributes attributes)
		{
			var lines = new System.Collections.Generic.List<string>();

			if (card.DamagePercent > 0)
			{
				lines.Add($"伤害　攻击力×{card.DamagePercent}%　→　{card.GetFinalDamage(attributes)} 点");
			}
			else
			{
				int damage = card.GetFinalDamage(attributes);
				if (damage > 0) lines.Add($"伤害　{damage} 点");
			}

			if (card.ShieldPercent > 0)
			{
				lines.Add($"护盾　防御力×{card.ShieldPercent}%　→　{card.GetFinalShield(attributes)} 点");
			}
			else
			{
				int shield = card.GetFinalShield(attributes);
				if (shield > 0) lines.Add($"护盾　{shield} 点");
			}

			int rage = RageManager.GetRageForCard(card);
			if (rage > 0) lines.Add($"狂气　{rage}");

			// 下一级预览：等级系数 1+0.2×(级-1)，所以每升一级数值都涨
			if (card.Level < card.MaxLevel)
			{
				int nextDamage = card.GetFinalDamage(attributes, card.Level + 1);
				int nextShield = card.GetFinalShield(attributes, card.Level + 1);
				string next = $"下一级（Lv.{card.Level + 1}）";
				if (nextDamage > 0) next += $"　伤害 {nextDamage}";
				if (nextShield > 0) next += $"　护盾 {nextShield}";
				lines.Add(next);
			}
			else
			{
				lines.Add("已达等级上限");
			}

			return string.Join("\n", lines);
		}

		/// <summary>词缀行：保留 / 刻印 等不在卡面描述里的标记</summary>
		private static string BuildAffixText(Card card)
		{
			var parts = new System.Collections.Generic.List<string>();

			if (card.IsRetain) parts.Add("保留");
			if (card.IsEngraved && !string.IsNullOrEmpty(card.EngravingEffect?.Name))
			{
				parts.Add($"刻印：{card.EngravingEffect.Name}");
			}

			return parts.Count > 0 ? "词缀：" + string.Join("　", parts) : "";
		}

		private static string GetCardTypeText(Card card)
		{
			if (card.IsAttack) return "攻击";
			if (card.IsDefenseCard()) return "防御";
			return "效果";
		}
	}
}
