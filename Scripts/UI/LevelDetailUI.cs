using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Shop;

namespace FishEatFish.UI
{
	/// <summary>
	/// 关卡详情弹窗（参考图 R-02）。
	///
	/// 点关卡卡片 → 弹这个窗 → 看关卡信息（关卡号 / 描述 / 星级 / 推荐等级 /
	/// 难度档 / 敌方预览 / 奖励预览）→ 点「调查」才真正进关卡。
	///
	/// 设计约束：
	///   - **纯展示 + 数据装配**，不持有游戏状态；所有数据现读自 MapDefinition /
	///     MapManager / EnemyConfigLoader / BlackMarkShopManager
	///   - 敌方预览、奖励预览用的是**真实配置数据**（Data/enemies.json、Data/artifacts.json），
	///     不是占位图
	/// </summary>
	public partial class LevelDetailUI : Control
	{
		/// <summary>敌方预览单格场景</summary>
		[Export]
		public PackedScene EnemySlotScene;

		/// <summary>奖励预览单格场景</summary>
		[Export]
		public PackedScene RewardSlotScene;

		/// <summary>玩家点了「调查」——由外部决定进关卡的后续流程</summary>
		public event System.Action<MapDefinition> OnInvestigate;

		/// <summary>章节名（项目目前只有一章）</summary>
		public const string ChapterName = "星辰篇";

		/// <summary>困难档相对普通档的推荐等级加成</summary>
		private const int HardLevelBonus = 5;

		private MapDefinition _map;
		private MapDifficulty _difficulty = MapDifficulty.Normal;

		private Label _nameLabel;
		private Label _descLabel;
		private Label _recLabel;
		private Label _noticeLabel;
		private Label[] _starLabels = new Label[3];
		private HBoxContainer _enemyBox;
		private HBoxContainer _rewardBox;
		private Button _normalButton;
		private Button _hardButton;

		public override void _Ready()
		{
			GD.Print("[LevelDetailUI] _Ready called");

			_nameLabel = GetNodeOrNull<Label>("Panel/MainVBox/NameLabel");
			_descLabel = GetNodeOrNull<Label>("Panel/MainVBox/DescLabel");
			_recLabel = GetNodeOrNull<Label>("Panel/MainVBox/InfoRow/RecLabel");
			_noticeLabel = GetNodeOrNull<Label>("Panel/MainVBox/NoticeLabel");
			_enemyBox = GetNodeOrNull<HBoxContainer>("Panel/MainVBox/PreviewRow/EnemySection/EnemyBox");
			_rewardBox = GetNodeOrNull<HBoxContainer>("Panel/MainVBox/PreviewRow/RewardSection/RewardBox");
			_normalButton = GetNodeOrNull<Button>("Panel/MainVBox/InfoRow/DifficultyBox/NormalButton");
			_hardButton = GetNodeOrNull<Button>("Panel/MainVBox/InfoRow/DifficultyBox/HardButton");

			for (int i = 0; i < 3; i++)
			{
				_starLabels[i] = GetNodeOrNull<Label>($"Panel/MainVBox/InfoRow/StarsRow/Star{i}");
			}

			ConnectButton("Panel/MainVBox/TopRow/CloseButton", Close);
			ConnectButton("Panel/MainVBox/BottomRow/CancelButton", Close);
			ConnectButton("Panel/MainVBox/BottomRow/InvestigateButton", OnInvestigatePressed);
			ConnectButton("Panel/MainVBox/BottomRow/RecordButton", ShowRecordNotice);
			ConnectButton("Panel/MainVBox/InfoRow/DifficultyBox/NormalButton", () => SetDifficulty(MapDifficulty.Normal));
			ConnectButton("Panel/MainVBox/InfoRow/DifficultyBox/HardButton", () => SetDifficulty(MapDifficulty.Hard));

			Hide();
			GD.Print("[LevelDetailUI] _Ready completed");
		}

		private void ConnectButton(string path, System.Action handler)
		{
			Button btn = GetNodeOrNull<Button>(path);
			if (btn != null)
			{
				btn.Pressed += handler;
			}
			else
			{
				GD.PrintErr($"[LevelDetailUI] 找不到按钮: {path}");
			}
		}

		/// <summary>打开弹窗并显示这个关卡</summary>
		public void ShowDetail(MapDefinition map)
		{
			if (map == null) return;

			_map = map;
			_difficulty = map.Difficulty == MapDifficulty.Hard ? MapDifficulty.Hard : MapDifficulty.Normal;

			Refresh();

			Show();
			GD.Print($"[LevelDetailUI] ShowDetail: {map.MapId} {map.Name}（难度 {_difficulty}）");
		}

		/// <summary>关闭弹窗</summary>
		public void Close()
		{
			Hide();
			GD.Print("[LevelDetailUI] Close called");
		}

		/// <summary>切换难度档（普通 / 困难）</summary>
		public void SetDifficulty(MapDifficulty difficulty)
		{
			if (_map == null) return;

			_difficulty = difficulty;
			// 难度写回关卡定义，进关卡后生效
			_map.Difficulty = difficulty;

			Refresh();
			GD.Print($"[LevelDetailUI] 切换难度档: {_difficulty}");
		}

		/// <summary>当前显示的关卡（供测试查询）</summary>
		public MapDefinition CurrentMap => _map;

		/// <summary>当前难度档（供测试查询）</summary>
		public MapDifficulty CurrentDifficulty => _difficulty;

		/// <summary>当前敌方预览格数（供测试查询）</summary>
		public int EnemyPreviewCount => _enemyBox?.GetChildCount() ?? 0;

		/// <summary>当前奖励预览格数（供测试查询）</summary>
		public int RewardPreviewCount => _rewardBox?.GetChildCount() ?? 0;

		// ===== 刷新 =====

		private void Refresh()
		{
			if (_map == null) return;

			if (_nameLabel != null)
			{
				_nameLabel.Text = $"{ChapterName.Split('篇')[0]}篇 第{_map.LevelNumber}关  {_map.Name}";
			}

			if (_descLabel != null)
			{
				_descLabel.Text = _map.Description ?? "";
			}

			// 星级：已通关点亮，未通关空心
			for (int i = 0; i < _starLabels.Length; i++)
			{
				if (_starLabels[i] == null) continue;
				_starLabels[i].Text = _map.IsCompleted ? "★" : "☆";
			}

			if (_recLabel != null)
			{
				int level = _map.RecommendedLevel + (_difficulty == MapDifficulty.Hard ? HardLevelBonus : 0);
				_recLabel.Text = $"推荐等级  {level}";
			}

			if (_normalButton != null) _normalButton.Disabled = _difficulty == MapDifficulty.Normal;
			if (_hardButton != null) _hardButton.Disabled = _difficulty == MapDifficulty.Hard;

			RefreshEnemyPreview();
			RefreshRewardPreview();

			if (_noticeLabel != null) _noticeLabel.Text = "";
		}

		/// <summary>敌方预览：用 Data/enemies.json 里关卡实际会用到的波次</summary>
		private void RefreshEnemyPreview()
		{
			if (_enemyBox == null) return;

			foreach (Node child in _enemyBox.GetChildren())
			{
				child.QueueFree();
			}

			foreach (string waveKey in GetPreviewWaveKeys())
			{
				EnemyWaveConfig wave = EnemyConfigLoader.GetWave(waveKey);
				if (wave == null) continue;

				foreach (EnemyDefinition enemy in wave.Enemies)
				{
					AddEnemySlot(enemy, waveKey);
				}
			}
		}

		/// <summary>本档位下要预览哪些波次</summary>
		private List<string> GetPreviewWaveKeys()
		{
			List<string> keys = new List<string>();

			// 关卡配置里的波次（已修正为 enemies.json 里真实存在的 key）
			if (_map != null && _map.EnemyWaveIds != null)
			{
				foreach (string id in _map.EnemyWaveIds)
				{
					if (!string.IsNullOrEmpty(id) && !keys.Contains(id))
					{
						keys.Add(id);
					}
				}
			}

			// 困难档额外加入 BOSS 遭遇
			if (_difficulty == MapDifficulty.Hard && !keys.Contains("boss_wave"))
			{
				keys.Add("boss_wave");
			}

			return keys;
		}

		private void AddEnemySlot(EnemyDefinition enemy, string waveKey)
		{
			if (EnemySlotScene == null) return;

			LevelDetailEnemySlot slot = EnemySlotScene.Instantiate<LevelDetailEnemySlot>();
			if (slot == null) return;

			string tag = enemy.IsBoss ? "首领" : enemy.IsElite ? "精英" : "";
			string icon = GetEnemyIconPath(enemy);

			_enemyBox.AddChild(slot);
			slot.ApplyEnemy(enemy.Name, icon, tag);

			GD.Print($"[LevelDetailUI] 敌方预览: {enemy.Name}（{waveKey}）");
		}

		/// <summary>敌人图标（Assets/Icons 下实际存在的资源）</summary>
		private static string GetEnemyIconPath(EnemyDefinition enemy)
		{
			if (enemy.IsBoss) return "res://Assets/Icons/skull.svg";
			if (enemy.IsElite) return "res://Assets/Icons/enemy_elite.svg";
			return "res://Assets/Icons/enemy_cultist.svg";
		}

		/// <summary>奖励预览：金币 + 造物（真实配置）</summary>
		private void RefreshRewardPreview()
		{
			if (_rewardBox == null) return;

			foreach (Node child in _rewardBox.GetChildren())
			{
				child.QueueFree();
			}

			if (RewardSlotScene == null) return;

			// 金币
			int gold = _map.GoldReward * (_difficulty == MapDifficulty.Hard ? 2 : 1);
			LevelDetailRewardSlot goldSlot = RewardSlotScene.Instantiate<LevelDetailRewardSlot>();
			if (goldSlot != null)
			{
				_rewardBox.AddChild(goldSlot);
				goldSlot.ApplyReward("金币", "", gold, "货币");
			}

			// 造物：取配置里的前 3 件
			List<ArtifactData> artifacts = BlackMarkShopManager.Instance?.GetAllArtifacts();
			if (artifacts == null) return;

			int count = Mathf.Min(3, artifacts.Count);
			for (int i = 0; i < count; i++)
			{
				ArtifactData art = artifacts[i];
				if (art == null) continue;

				LevelDetailRewardSlot slot = RewardSlotScene.Instantiate<LevelDetailRewardSlot>();
				if (slot == null) continue;

				_rewardBox.AddChild(slot);
				slot.ApplyReward(art.name, art.icon, 1, GetQualityTag(art.price));

				GD.Print($"[LevelDetailUI] 奖励预览: {art.name} ×1（{GetQualityTag(art.price)}）");
			}
		}

		/// <summary>由价格推导品质标签</summary>
		private static string GetQualityTag(int price)
		{
			if (price >= 50) return "三星";
			if (price >= 30) return "二星";
			return "普通";
		}

		// ===== 按钮 =====

		private void OnInvestigatePressed()
		{
			if (_map == null) return;

			GD.Print($"[LevelDetailUI] 调查: {_map.MapId}（难度 {_difficulty}）");
			Close();
			OnInvestigate?.Invoke(_map);
		}

		private void ShowRecordNotice()
		{
			if (_noticeLabel == null) return;

			_noticeLabel.Text = "暂无通关记录（本项目还没有记录系统）";
			GD.Print("[LevelDetailUI] 记录：暂无记录");
		}
	}
}
