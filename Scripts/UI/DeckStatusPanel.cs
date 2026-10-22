using Godot;
using System;

namespace FishEatFish.UI
{
	/// <summary>
	/// 战斗界面**右下角的牌堆状态面板**（对照参考图 R-08 右下角）。
	///
	/// 三组「数字 + 图标」：
	///   - 手牌：图标 + **当前张数/上限**（如 9/10）
	///   - 抽牌堆：数字 + **可点击的图标**（点开看抽牌池里还剩哪些牌）
	///   - 弃牌堆：数字 + 图标
	///
	/// 设计约束：纯展示组件，数据由 <c>BattleManager</c> 推给它；不持有游戏状态。
	/// </summary>
	public partial class DeckStatusPanel : HBoxContainer
	{
		private Label _handCount;
		private Label _drawCount;
		private Label _discardCount;
		private Button _drawButton;

		private Action _drawPileClickCallback;

		public override void _Ready()
		{
			GD.Print("[DeckStatusPanel] _Ready called");

			_handCount = GetNodeOrNull<Label>("HandGroup/HandCount");
			_drawCount = GetNodeOrNull<Label>("DrawGroup/DrawCount");
			_discardCount = GetNodeOrNull<Label>("DiscardGroup/DiscardCount");
			_drawButton = GetNodeOrNull<Button>("DrawGroup/DrawButton");

			if (_drawButton != null)
			{
				_drawButton.Pressed += OnDrawPilePressed;
			}
			else
			{
				GD.PrintErr("[DeckStatusPanel] 未找到 DrawButton 节点，抽牌池图标无法点击");
			}

			GD.Print("[DeckStatusPanel] _Ready completed");
		}

		/// <summary>
		/// 刷新三组数字
		/// </summary>
		/// <param name="handCount">当前手牌张数</param>
		/// <param name="handMax">手牌上限</param>
		/// <param name="drawCount">抽牌堆剩余张数</param>
		/// <param name="discardCount">弃牌堆张数</param>
		public void Setup(int handCount, int handMax, int drawCount, int discardCount)
		{
			if (_handCount != null)
			{
				_handCount.Text = $"{handCount}/{handMax}";
			}
			if (_drawCount != null)
			{
				_drawCount.Text = drawCount.ToString();
			}
			if (_discardCount != null)
			{
				_discardCount.Text = discardCount.ToString();
			}
		}

		/// <summary>设置「点抽牌堆图标」的回调（由 BattleManager 接到抽牌池查看器）</summary>
		/// <param name="callback">点击时执行的回调</param>
		public void SetDrawPileClickCallback(Action callback)
		{
			_drawPileClickCallback = callback;
		}

		private void OnDrawPilePressed()
		{
			GD.Print("[DeckStatusPanel] OnDrawPilePressed called");
			_drawPileClickCallback?.Invoke();
			GD.Print("[DeckStatusPanel] OnDrawPilePressed completed");
		}
	}
}
