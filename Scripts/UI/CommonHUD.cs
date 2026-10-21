using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Scenes;
using FishEatFish.Shop;

namespace FishEatFish.UI
{
	/// <summary>
	/// 公共 HUD 外壳（对照参考图 R-04 / R-08 / R-17 的顶部条）
	///
	/// 结构：4 个角色圆环 ｜ 当前钥令 ｜ 8 格收集品（造物）｜ 右上 4 个统计位
	///
	/// 设计约束：
	///   - 纯展示组件，**不持有任何游戏状态**，数据全部来自既有 autoload / GlobalData
	///   - 角色圆环复用 AttackerDisplayUI（4 个 CharacterSlotUI，已含头像/名字/怒气圈）
	///   - 收集品槽复用 CommonHUDSlot（图标 / 空槽 ✕）
	///   - 外部改完数据后调 Refresh() 刷新
	/// </summary>
	public partial class CommonHUD : Control
	{
		/// <summary>收集品槽位场景（CommonHUDSlot.tscn）</summary>
		[Export]
		public PackedScene SlotScene;

		/// <summary>
		/// 是否显示角色圆环区。
		/// 地图场景左上角已有自己的怒气圈显示（项目自绘的 RageCircle 控件），
		/// 所以地图接入时设为 false，只显示钥令 + 收集品 + 统计位。
		/// </summary>
		[Export]
		public bool ShowTeamSection = true;

		/// <summary>
		/// 是否显示右上角统计位。
		/// 战斗场景的 TopBar 里已有 PlayerStats（血量/护盾/回合/银钥），所以战斗接入时设为 false。
		/// </summary>
		[Export]
		public bool ShowStatsSection = true;

		/// <summary>
		/// 是否显示收集品（造物）槽区。
		/// 地图场景的 HexMapUI 已有既有的 BackpackUI 造物栏（常驻顶部居中，支持点击查看详情），
		/// 两套同时显示会互相重叠，所以地图接入时设为 false，只保留 BackpackUI 那一套。
		/// 战斗 / 商店场景没有 BackpackUI，用这一套。
		/// </summary>
		[Export]
		public bool ShowCollectionSection = true;

		private const int CollectionSlotCount = 8;

		private AttackerDisplayUI _teamDisplay;
		private HBoxContainer _collectionBox;
		private readonly List<CommonHUDSlot> _collectionSlots = new List<CommonHUDSlot>();

		private readonly Label[] _statTitles = new Label[4];
		private readonly Label[] _statValues = new Label[4];

		public override void _Ready()
		{
			GD.Print("[CommonHUD] _Ready called");

			_teamDisplay = GetNodeOrNull<AttackerDisplayUI>("TeamDisplay");
			_collectionBox = GetNodeOrNull<HBoxContainer>("CollectionBox");

			for (int i = 0; i < 4; i++)
			{
				_statTitles[i] = GetNodeOrNull<Label>($"StatsBox/Stat{i}/TitleLabel");
				_statValues[i] = GetNodeOrNull<Label>($"StatsBox/Stat{i}/ValueLabel");
			}

			if (SlotScene == null)
			{
				SlotScene = GD.Load<PackedScene>("res://Scenes/UI/CommonHUDSlot.tscn");
			}

			BuildCollectionSlots();

			if (_teamDisplay != null)
			{
				_teamDisplay.Visible = ShowTeamSection;
			}

			HBoxContainer statsBox = GetNodeOrNull<HBoxContainer>("StatsBox");
			if (statsBox != null)
			{
				statsBox.Visible = ShowStatsSection;
			}

			if (_collectionBox != null)
			{
				_collectionBox.Visible = ShowCollectionSection;
			}

			Refresh();

			GD.Print("[CommonHUD] _Ready completed");
		}

		/// <summary>
		/// 生成 8 个收集品槽位
		/// </summary>
		private void BuildCollectionSlots()
		{
			if (_collectionBox == null || SlotScene == null) return;

			_collectionSlots.Clear();
			for (int i = 0; i < CollectionSlotCount; i++)
			{
				CommonHUDSlot slot = SlotScene.Instantiate<CommonHUDSlot>();
				if (slot == null) continue;
				_collectionBox.AddChild(slot);
				_collectionSlots.Add(slot);
			}
		}

		/// <summary>
		/// 刷新全部内容（数据变了就调它）
		/// </summary>
		public void Refresh()
		{
			RefreshTeam();
			RefreshCollection();
			RefreshStats();
		}

		/// <summary>
		/// 角色圆环：4 个已上阵角色
		/// </summary>
		private void RefreshTeam()
		{
			if (_teamDisplay == null || !ShowTeamSection) return;

			CharacterDefinition[] team = GlobalData.SelectedCharacters;
			if (team == null || team.Length == 0)
			{
				GD.Print("[CommonHUD] 没有已选角色，队伍区留空");
				return;
			}

			Attacker attacker = new Attacker();
			attacker.SetCharacters(team);
			_teamDisplay.SetAttacker(attacker);
			// attacker 是游离 Node（没加进场景树），用完必须显式释放，否则退出时 ObjectDB 报泄漏
			attacker.Dispose();
		}

		/// <summary>
		/// 8 格收集品：已拥有的造物，空的显示 ✕
		/// </summary>
		private void RefreshCollection()
		{
			if (_collectionSlots.Count == 0) return;

			List<ArtifactData> owned = BlackMarkShopManager.Instance?.GetOwnedArtifacts();

			for (int i = 0; i < _collectionSlots.Count; i++)
			{
				if (owned != null && i < owned.Count)
				{
					_collectionSlots[i].SetIcon(owned[i].icon);
				}
				else
				{
					_collectionSlots[i].SetEmpty();
				}
			}
		}

		/// <summary>
		/// 右上 4 个统计位
		/// </summary>
		private void RefreshStats()
		{
			// 1. 银钥
			int silverKey = MapManager.Instance?.GetSilverKey() ?? 0;
			SetStat(0, "银钥", silverKey.ToString());

			// 2. 黑印
			int blackMark = BlackMarkShopManager.Instance?.BlackMarkCount ?? 0;
			SetStat(1, "黑印", blackMark.ToString());

			// 3. 队伍总血量
			CharacterDefinition[] team = GlobalData.SelectedCharacters;
			int totalHealth = 0;
			if (team != null)
			{
				foreach (CharacterDefinition c in team)
				{
					if (c != null) totalHealth += c.BaseHealth;
				}
			}
			SetStat(2, "队伍血量", totalHealth.ToString());

			// 4. 当前关卡
			string mapName = GlobalData.CurrentMap?.Name ?? "—";
			SetStat(3, "关卡", mapName);
		}

		private void SetStat(int index, string title, string value)
		{
			if (index < 0 || index >= 4) return;
			if (_statTitles[index] != null) _statTitles[index].Text = title;
			if (_statValues[index] != null) _statValues[index].Text = value;
		}
	}
}
