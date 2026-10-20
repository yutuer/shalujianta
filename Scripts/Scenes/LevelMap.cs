using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.HexMap;
using FishEatFish.Battle.MapSystem;
using FishEatFish.UI;

namespace FishEatFish.Scenes
{
	/// <summary>
	/// 关卡展示页（D10）。
	///
	/// 结构（对照参考图 R-01）：
	///   顶部：章节切换（◀ 第N章 ▶）｜ 难度档（溯梦 / 普通 / 困难）｜ UID
	///   中部：当前章节的关卡卡片（编号 + 标题 + 星级 + 推荐等级 + 难度）
	///   底部：共鸣进度（占位）｜ 星级评分进度（真实，按通关状态算）｜ 关卡进度 ｜ 返回主菜单
    ///
    /// 点关卡卡片 → 弹 D3 的关卡详情弹窗 → 点「调查」才真正进关卡。
    /// </summary>
    public partial class LevelMap : Control
    {
        /// <summary>关卡卡片场景</summary>
        [Export]
        public PackedScene CardScene;

        private VBoxContainer _mapContainer;
        private Button _backButton;
        private Button _battleButton;

        private Label _chapterLabel;
        private Label _uidLabel;
        private Label _resonanceLabel;
        private Label _starScoreLabel;
        private Label _clearLabel;
        private Button _prevButton;
        private Button _nextButton;
        private Button _dreamButton;
        private Button _normalButton;
        private Button _hardButton;

        private LevelDetailUI _levelDetailUI;

        private List<LevelChapterMeta> _chapters = new List<LevelChapterMeta>();
        private List<MapDefinition> _availableMaps = new List<MapDefinition>();
        private MapDefinition _selectedMap;
        private int _chapterIndex = 0;
        private MapDifficulty _difficulty = MapDifficulty.Normal;

        public override void _Ready()
        {
            GD.Print("[LevelMap] _Ready called");

            _mapContainer = GetNode<VBoxContainer>("SafeArea/Content/MapList/MapContainer");
            _backButton = GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/BackButton");
            _battleButton = GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/BattleButton");

            _chapterLabel = GetNodeOrNull<Label>("SafeArea/Content/TopBar/ChapterBox/ChapterLabel");
            _uidLabel = GetNodeOrNull<Label>("SafeArea/Content/TopBar/UidLabel");
            _resonanceLabel = GetNodeOrNull<Label>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ProgressBox/ResonanceLabel");
            _starScoreLabel = GetNodeOrNull<Label>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ProgressBox/StarScoreLabel");
            _clearLabel = GetNodeOrNull<Label>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ProgressBox/ClearLabel");

            _prevButton = GetNodeOrNull<Button>("SafeArea/Content/TopBar/ChapterBox/PrevButton");
            _nextButton = GetNodeOrNull<Button>("SafeArea/Content/TopBar/ChapterBox/NextButton");
            _dreamButton = GetNodeOrNull<Button>("SafeArea/Content/TopBar/DifficultyBox/DreamButton");
            _normalButton = GetNodeOrNull<Button>("SafeArea/Content/TopBar/DifficultyBox/NormalButton");
            _hardButton = GetNodeOrNull<Button>("SafeArea/Content/TopBar/DifficultyBox/HardButton");

            _backButton.Pressed += OnBackPressed;
            _battleButton.Pressed += OnBattlePressed;

            // 注意：不要写 `?.Pressed += ...`（那是 C# 14 的 null 条件赋值，项目是 C# 12）
            if (_prevButton != null) _prevButton.Pressed += () => SwitchChapter(-1);
            if (_nextButton != null) _nextButton.Pressed += () => SwitchChapter(1);
            if (_dreamButton != null) _dreamButton.Pressed += () => SetDifficulty(MapDifficulty.Easy);
            if (_normalButton != null) _normalButton.Pressed += () => SetDifficulty(MapDifficulty.Normal);
            if (_hardButton != null) _hardButton.Pressed += () => SetDifficulty(MapDifficulty.Hard);
            SetupLevelDetail();

            LoadData();
            RefreshAll();

            GD.Print("[LevelMap] _Ready completed");
        }

        /// <summary>关卡详情弹窗（D3）：点关卡卡片先看详情，点「调查」才进关卡</summary>
        private void SetupLevelDetail()
        {
            var scene = GD.Load<PackedScene>("res://Scenes/UI/LevelDetailUI.tscn");
            if (scene == null)
            {
                GD.PrintErr("[LevelMap] 关卡详情弹窗场景加载失败");
                return;
            }

            _levelDetailUI = scene.Instantiate<LevelDetailUI>();
            if (_levelDetailUI == null) return;

            AddChild(_levelDetailUI);
            _levelDetailUI.OnInvestigate += OnInvestigateConfirmed;
        }

        /// <summary>
        /// 载入数据。
        /// 关卡定义用 **MapManager 里那一份**（不是自己 new），
        /// 否则通关状态 / 难度改动在两处不同步。
        /// </summary>
        private void LoadData()
        {
            _chapters = LevelMetaData.LoadChapters();

            if (MapManager.Instance != null)
            {
                _availableMaps = MapManager.Instance.GetAvailableMaps();
            }
            else
            {
                GD.PrintErr("[LevelMap] MapManager 单例不可用");
                _availableMaps = new List<MapDefinition>(MapDefinition.GetAllMaps());
            }
        }

        // ===== 刷新 =====

        private void RefreshAll()
        {
            RefreshChapterBar();
            RefreshDifficultyBar();
            RefreshCards();
            RefreshProgress();
        }

        private void RefreshChapterBar()
        {
 			if (_chapters.Count == 0) return;

			_chapterIndex = Mathf.Clamp(_chapterIndex, 0, _chapters.Count - 1);
			LevelChapterMeta chapter = _chapters[_chapterIndex];

			if (_chapterLabel != null)
			{
				_chapterLabel.Text = $"第 {_chapterIndex + 1} 章　{chapter.Name}";
			}
			if (_prevButton != null) _prevButton.Disabled = _chapters.Count <= 1;
			if (_nextButton != null) _nextButton.Disabled = _chapters.Count <= 1;

			if (_uidLabel != null)
			{
				_uidLabel.Text = $"UID: {LevelMetaData.Uid}";
			}
		}

        private void RefreshDifficultyBar()
        {
			if (_dreamButton != null) _dreamButton.Disabled = true;   // 溯梦档项目还没内容
			if (_normalButton != null) _normalButton.Disabled = _difficulty == MapDifficulty.Normal;
			if (_hardButton != null) _hardButton.Disabled = _difficulty == MapDifficulty.Hard;
        }

        private void RefreshCards()
        {
            if (_mapContainer == null) return;

            foreach (Node child in _mapContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (_chapters.Count == 0 || CardScene == null) return;

            LevelChapterMeta chapter = _chapters[Mathf.Clamp(_chapterIndex, 0, _chapters.Count - 1)];
            Label title = GetNodeOrNull<Label>("SafeArea/Content/Header/Title");
            Label subtitle = GetNodeOrNull<Label>("SafeArea/Content/Header/Subtitle");
            if (title != null) title.Text = chapter.Name;
            if (subtitle != null) subtitle.Text = $"第 {chapter.Levels.Count} 个关卡 · 选择一个关卡开始你的冒险";

            foreach (LevelNodeMeta node in chapter.Levels)
            {
                MapDefinition map = FindMap(node.MapId);
                LevelMapCard card = CardScene.Instantiate<LevelMapCard>();
                if (card == null) continue;

                _mapContainer.AddChild(card);
                card.ApplyNode(node, GetStars(map), node.Unlocked);
                card.OnOpen = OnLevelCardOpened;
            }
        }

        private void RefreshProgress()
        {
            int totalStars = 0;
            int earnedStars = 0;
            int unlocked = 0;
            int totalLevels = 0;

            foreach (LevelChapterMeta chapter in _chapters)
            {
                foreach (LevelNodeMeta node in chapter.Levels)
                {
                    totalLevels++;
                    if (node.Unlocked) unlocked++;

                    MapDefinition map = FindMap(node.MapId);
                    int stars = GetStars(map);
                    earnedStars += stars;
                    totalStars += 3;
                }
            }

            if (_starScoreLabel != null)
            {
                _starScoreLabel.Text = $"星级评分 {earnedStars}/{totalStars}";
            }
            if (_clearLabel != null)
            {
                _clearLabel.Text = $"关卡进度 {unlocked}/{totalLevels}";
            }
            if (_resonanceLabel != null)
            {
                // 占位：项目还没有「共鸣」系统（战斗结算的同调率只是单次增量，不累计）
                _resonanceLabel.Text = $"共鸣 {LevelMetaData.ResonanceCurrent}/{LevelMetaData.ResonanceTotal}";
            }
        }

        /// <summary>该关卡已获得的星数（通关 = 3 星）</summary>
        private static int GetStars(MapDefinition map)
        {
            return map != null && map.IsCompleted ? 3 : 0;
        }

        private MapDefinition FindMap(string mapId)
        {
            foreach (MapDefinition map in _availableMaps)
            {
                if (map.MapId == mapId) return map;
            }
            return null;
        }

        // ===== 交互 =====

        /// <summary>切换章节</summary>
        public void SwitchChapter(int delta)
        {
            if (_chapters.Count == 0) return;

            int next = Mathf.Clamp(_chapterIndex + delta, 0, _chapters.Count - 1);
            if (next == _chapterIndex) return;

            _chapterIndex = next;
            GD.Print($"[LevelMap] 切换到第 {_chapterIndex + 1} 章");
            RefreshAll();
        }

        /// <summary>切换全局难度档</summary>
        public void SetDifficulty(MapDifficulty difficulty)
        {
            if (_difficulty == difficulty) return;

            _difficulty = difficulty;
            GD.Print($"[LevelMap] 切换难度档: {difficulty}");
            RefreshAll();
        }

        /// <summary>点关卡卡片 → 弹详情</summary>
        private void OnLevelCardOpened(LevelNodeMeta node)
        {
            MapDefinition map = FindMap(node.MapId);
            if (map == null)
            {
                GD.PrintErr($"[LevelMap] 找不到关卡定义: {node.MapId}");
                return;
            }

            if (_levelDetailUI == null) return;

            _levelDetailUI.ShowDetail(map);
            // 顶部的全局难度档决定弹窗的初始难度档
            if (_difficulty != MapDifficulty.Easy)
            {
                _levelDetailUI.SetDifficulty(_difficulty);
            }
        }

        /// <summary>弹窗里点了「调查」→ 真正进这个关卡</summary>
        private void OnInvestigateConfirmed(MapDefinition map)
        {
            OnMapSelected(map);
        }

        private void OnMapSelected(MapDefinition map)
        {
            _selectedMap = map;
            GD.Print($"选择了地图: {map.Name}");

            // MapManager 是 autoload，直接使用单例（不要 new，new 出来的实例没有进入场景树）
            MapManager mapManager = MapManager.Instance;
            if (mapManager != null)
            {
                mapManager.StartMap(map);
            }
            else
            {
                GD.PrintErr("MapManager 单例不可用，地图进度不会被记录");
            }

            GlobalData.CurrentMap = map;
            GetTree().ChangeSceneToFile("res://Scenes/CharacterSelection.tscn");
        }

        private void OnBackPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }

        private void OnBattlePressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
        }

        // ===== 供测试查询 =====

        /// <summary>当前章节序号（从 0 开始）</summary>
        public int ChapterIndex => _chapterIndex;

        /// <summary>当前章节的关卡卡片数</summary>
        public int CardCount => _mapContainer?.GetChildCount() ?? 0;

        /// <summary>当前全局难度档</summary>
        public MapDifficulty CurrentDifficulty => _difficulty;

        /// <summary>关卡详情弹窗（供测试查询）</summary>
        public LevelDetailUI DetailPanel => _levelDetailUI;
    }
}
