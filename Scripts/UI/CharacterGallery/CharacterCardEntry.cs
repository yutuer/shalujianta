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
		/// <summary>按住多久算「长按」（秒）</summary>
		private const double LongPressDelay = 0.4;

		/// <summary>长按期间允许的抖动（像素）—— 超过就当普通点击</summary>
		private const float LongPressMoveTolerance = 8f;

		private Label _levelLabel;
		private PanelContainer _cardPanel;
		private Label _costLabel;
		private Label _nameLabel;
		private TextureRect _artRect;
		private Label _descLabel;
		private Label _typeLabel;
		private Card _card;

		// ---- 长按检测（长按 → 弹出放大预览 + 词缀说明）----
		private bool _pressed;
		private bool _longPressFired;
		private double _pressElapsed;
		private Vector2 _pressPosition;

		/// <summary>长按这张卡（按住不动超过 0.4 秒）时触发</summary>
		public System.Action<Card> OnLongPress;

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

			_card = card;

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

	/// <summary>
	/// 鼠标输入：记录按下 / 抖动（超过容差就取消长按）
	/// </summary>
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				_pressed = true;
				_longPressFired = false;
				_pressElapsed = 0;
				_pressPosition = mouseEvent.Position;
			}
			else
			{
				_pressed = false;
			}
		}
		else if (@event is InputEventMouseMotion motion && _pressed)
		{
			if (motion.Position.DistanceTo(_pressPosition) > LongPressMoveTolerance)
			{
				_pressed = false;
			}
		}
	}

	/// <summary>每帧检查长按时长</summary>
	public override void _Process(double delta)
	{
		if (!_pressed || _longPressFired) return;

		_pressElapsed += delta;
		if (_pressElapsed < LongPressDelay) return;

		_longPressFired = true;
		GD.Print($"[CharacterCardEntry] 长按触发：{_card?.Name}（{_card?.CardId}）");
		OnLongPress?.Invoke(_card);
	}
	}
}
