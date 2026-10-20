using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.MapSystem;
using FishEatFish.Scenes;

namespace FishEatFish.UI
{
	/// <summary>
	/// 地图左侧的评分项面板（参考图 R-04）。
	///
	/// - 评分项的**文案与判定条件全部来自 Data/score_items.json**，改评分项不用改代码
	/// - 每条评分项有三态：✓ 已达成 / ✗ 已失效 / • 未触发
	/// - 文案里的占位符（{remainingBattles} 等）会用**实时数据**填充
	///
	/// 组件**不持有游戏状态**，数据全部现读自 MapManager / GlobalData。
	/// </summary>
	public partial class ScorePanel : Control
	{
		/// <summary>评分项单行的场景</summary>
		[Export]
		public PackedScene RowScene;

		private VBoxContainer _itemsBox;
		private readonly List<ScoreItemRow> _rows = new List<ScoreItemRow>();
		private readonly List<ScoreItemData> _items = new List<ScoreItemData>();

		/// <summary>评分项行（供测试与外部查询）</summary>
		public IReadOnlyList<ScoreItemRow> Rows => _rows;

		public override void _Ready()
		{
			GD.Print("[ScorePanel] _Ready called");

			_itemsBox = GetNodeOrNull<VBoxContainer>("ItemsBox");

			if (RowScene == null)
			{
				RowScene = GD.Load<PackedScene>("res://Scenes/UI/ScoreItemRow.tscn");
			}

			BuildRows();
			Refresh();

			GD.Print("[ScorePanel] _Ready completed");
		}

		/// <summary>按配置生成评分项行</summary>
		private void BuildRows()
		{
			if (_itemsBox == null || RowScene == null) return;

			_items.Clear();
			_items.AddRange(ScoreItemData.LoadAll());

			foreach (Node child in _itemsBox.GetChildren())
			{
				child.QueueFree();
			}
			_rows.Clear();

			foreach (ScoreItemData item in _items)
			{
				ScoreItemRow row = RowScene.Instantiate<ScoreItemRow>();
				if (row == null) continue;
				_itemsBox.AddChild(row);
				_rows.Add(row);
			}

			GD.Print($"[ScorePanel] 生成 {_rows.Count} 行评分项");
		}

		/// <summary>刷新所有评分项（状态 + 文案）。数据变了就调它。</summary>
		public void Refresh()
		{
			for (int i = 0; i < _rows.Count && i < _items.Count; i++)
			{
				ScoreItemData item = _items[i];
				ScoreItemState state = EvaluateState(item);
				string text = BuildText(item);
				_rows[i].ApplyState(state, text);
				GD.Print($"[ScorePanel] {item.Id}: {state} | {text}");
			}
		}

		// ===== 数据源 =====

		private int TotalBattles => MapManager.Instance?.GetTotalBattleCount() ?? 0;

		private int BattleIndex => MapManager.Instance?.GetCurrentBattleIndex() ?? 0;

		private int RemainingBattles => Mathf.Max(0, TotalBattles - BattleIndex);

		private int TeamSize => GlobalData.SelectedCharacters?.Length ?? 0;

		private int SilverKey => MapManager.Instance?.GetSilverKey() ?? 0;

		private bool IsFirstPlayToday => MapManager.Instance?.IsFirstPlayToday ?? true;

		private string MapName => GlobalData.CurrentMap?.Name ?? "—";

		// ===== 判定与文案 =====

		/// <summary>按条件算出这条评分项当前处于哪一态</summary>
		private ScoreItemState EvaluateState(ScoreItemData item)
		{
			switch (item.Condition)
			{
				case "remaining_battles":
					if (RemainingBattles <= 0) return ScoreItemState.Expired;
					return RemainingBattles >= item.Param ? ScoreItemState.Active : ScoreItemState.Pending;

				case "silver_key":
					return SilverKey > 0 ? ScoreItemState.Active : ScoreItemState.Pending;

				case "team_size":
					return TeamSize >= item.Param ? ScoreItemState.Active : ScoreItemState.Pending;

				case "daily_first":
					return IsFirstPlayToday ? ScoreItemState.Active : ScoreItemState.Expired;

				default:
					return ScoreItemState.Active;
			}
		}

		/// <summary>把文案里的占位符替换成实时数据</summary>
		private string BuildText(ScoreItemData item)
		{
			string text = item.Text ?? "";
			text = text.Replace("{remainingBattles}", RemainingBattles.ToString());
			text = text.Replace("{totalBattles}", TotalBattles.ToString());
			text = text.Replace("{battleIndex}", BattleIndex.ToString());
			text = text.Replace("{teamSize}", TeamSize.ToString());
			text = text.Replace("{silverKey}", SilverKey.ToString());
			text = text.Replace("{mapName}", MapName);
			return text;
		}
	}
}
