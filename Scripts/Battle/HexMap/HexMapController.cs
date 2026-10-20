using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.UI.HexMap;
using FishEatFish.Shop;
using FishEatFish.Scenes;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle.HexMap
{
    public enum HexMapState
    {
        Idle,
        Moving,
        Teleporting,
        EventProcessing,
        ShopOpen,
        Paused
    }

    public partial class HexMapController : Node
    {
        private static HexMapController _instance;
        public static HexMapController Instance => _instance;

        [Export]
        private int _playerLevel = 1;
        public int PlayerLevel => _playerLevel;

        [Export]
        private int _mapRadius = 5;

        private HexMap _currentMap;
        public HexMap CurrentMap => _currentMap;

        private HexCoord _currentPosition;
        public HexCoord CurrentPosition => _currentPosition;

        private HexCoord _previousPosition;

        private HexMapState _currentState = HexMapState.Idle;
        public HexMapState CurrentState => _currentState;

        private HexMapGenerator _generator;
        private HexEventManager _eventManager;

        private float _playerMaxHealth = 100f;
        private float _playerCurrentHealth = 100f;
        public float PlayerCurrentHealth => _playerCurrentHealth;
        public float PlayerMaxHealth => _playerMaxHealth;

        public int BlackMarkCount => BlackMarkShopManager.Instance?.BlackMarkCount ?? 0;

        private bool _hasSkipped = false;
        public bool HasSkipped => _hasSkipped;

        private List<HexCoord> _currentPath = new List<HexCoord>();
        private int _pathIndex = 0;

        public System.Action<HexCoord> OnPlayerMoved;
        public System.Action<HexCoord> OnPlayerTeleported;
        public System.Action<HexTile> OnTileTriggered;
        public System.Action<HexCoord> OnTeleportTriggered;
        public System.Action<float, float> OnHealthChanged;
        public System.Action<int> OnBlackMarkChanged;
        public System.Action<int, int> OnSilverKeyChanged;
        public System.Action OnMapCompleted;
        public System.Action OnChallengeFailed;
        public System.Action OnShopOpened;
        public System.Action OnShopClosed;

        /// <summary>可见区域（战争迷雾）发生变化时触发</summary>
        public System.Action OnRevealedChanged;

        private BattleTransitionManager _battleTransitionManager;
        private bool _isRestored = false;

        public override void _Ready()
        {
            _instance = this;

            _generator = new HexMapGenerator();
            _eventManager = new HexEventManager();

            // BattleTransitionManager 是 autoload，跨场景存活，持有地图存档
            _battleTransitionManager = BattleTransitionManager.Instance;

            _eventManager.OnBattleTriggered += OnBattleTriggered;

            // 地图血量上限 = 队伍总血量（与战斗内一致，血量在地图与战斗之间是同一份）
            SyncMaxHealthFromTeam();

            if (_battleTransitionManager != null && _battleTransitionManager.HasSavedMap)
            {
                RestoreMapState();
            }
            else
            {
                GenerateMap();
            }
        }

        /// <summary>
        /// 按已选队伍的总血量设置地图血量上限
        /// </summary>
        private void SyncMaxHealthFromTeam()
        {
            if (GlobalData.SelectedCharacters == null || GlobalData.SelectedCharacters.Length != 4)
            {
                return;
            }

            int total = 0;
            foreach (CharacterDefinition character in GlobalData.SelectedCharacters)
            {
                if (character != null)
                {
                    total += character.BaseHealth;
                }
            }

            if (total <= 0)
            {
                return;
            }

            _playerMaxHealth = total;
            // 新地图从满血开始；恢复存档时随后会被存档值覆盖
            _playerCurrentHealth = total;
            GD.Print($"[HexMapController] 血量上限按队伍设为 {_playerMaxHealth}（当前 {_playerCurrentHealth}）");
        }

        /// <summary>
        /// 从 BattleTransitionManager 的存档恢复地图（战斗结束后返回地图时走这条路径）
        /// </summary>
        private void RestoreMapState()
        {
            HexMap savedMap = _battleTransitionManager.GetSavedMap();
            if (savedMap == null)
            {
                GD.PrintErr("[HexMapController] 地图存档为空，改为重新生成");
                GenerateMap();
                return;
            }

            _currentMap = savedMap;
            _currentPosition = _battleTransitionManager.GetSavedPlayerPosition();
            _playerLevel = _battleTransitionManager.GetSavedPlayerLevel();
            _currentState = HexMapState.Idle;
            _hasSkipped = false;
            _isRestored = true;

            // 用 SetPlayerHealth 走一遍，才能顺带把血条 UI 推上去
            SetPlayerHealth(
                _battleTransitionManager.GetSavedPlayerMaxHealth(),
                _battleTransitionManager.GetSavedPlayerHealth());

            int savedBlackMark = _battleTransitionManager.GetSavedBlackMarkCount();
            BlackMarkShopManager.Instance?.SetBlackMarkCount(savedBlackMark);

            // 战争迷雾：战斗结束回到地图后，把已照亮的格子恢复回来
            RestoreRevealedTiles();

            GD.Print($"[HexMapController] 恢复地图状态: 位置={_currentPosition}, 等级={_playerLevel}, 生命={_playerCurrentHealth}/{_playerMaxHealth}, 黑印={savedBlackMark}");

            // 打赢 BOSS 战回到地图 → 结算地图完成
            if (_battleTransitionManager.PendingMapCompletion)
            {
                _battleTransitionManager.ClearPendingMapCompletion();
                GD.Print("[HexMapController] BOSS 已击败，结算地图完成");
                CompleteMap();
            }
        }

        private void OnBattleTriggered(HexEventType battleType, string enemyConfig)
        {
            GD.Print($"[HexMapController] OnBattleTriggered: type={battleType}, config={enemyConfig}");

            if (_battleTransitionManager == null)
            {
                GD.PrintErr("[HexMapController] BattleTransitionManager 不可用，无法进入战斗");
                return;
            }

            _battleTransitionManager.StartBattle(battleType, enemyConfig, this);
        }

        public void GenerateMap()
        {
            _currentMap = _generator.Generate(_mapRadius, _playerLevel);
            _currentPosition = _currentMap.Start;
            _currentState = HexMapState.Idle;
            _hasSkipped = false;

            // 新的地图 = 新的关卡，战争迷雾从零开始（存档里的旧集合也要清掉，避免跨关卡串味）
            _revealedTiles.Clear();
            _battleTransitionManager?.SaveRevealedTiles(null, CurrentMapId);

            GD.Print($"[HexMapController] 地图已生成，起点: {_currentPosition}, 终点: {_currentMap.End}");
        }

        public void SetPlayerLevel(int level)
        {
            // 从战斗返回的地图使用存档等级，不被外部覆盖
            if (_isRestored)
            {
                GD.Print($"[HexMapController] 地图已从存档恢复，保留存档等级 {_playerLevel}，忽略传入等级 {level}");
                return;
            }

            _playerLevel = level;
            if (level <= 5)
                _mapRadius = 3;
            else if (level <= 10)
                _mapRadius = 4;
            else
                _mapRadius = 5;
        }

        public void SetPlayerHealth(float max, float current)
        {
            _playerMaxHealth = max;
            _playerCurrentHealth = Mathf.Clamp(current, 0, max);
            OnHealthChanged?.Invoke(_playerCurrentHealth, _playerMaxHealth);
        }

        public void DamagePlayer(float amount)
        {
            _playerCurrentHealth = Mathf.Max(0, _playerCurrentHealth - amount);
            OnHealthChanged?.Invoke(_playerCurrentHealth, _playerMaxHealth);

            GD.Print($"[HexMapController] 玩家受到伤害: {amount}, 当前生命: {_playerCurrentHealth}/{_playerMaxHealth}");

            if (_playerCurrentHealth <= 0)
            {
                HandlePlayerDeath();
            }
        }

        public void HealPlayer(float amount)
        {
            float oldHealth = _playerCurrentHealth;
            _playerCurrentHealth = Mathf.Min(_playerMaxHealth, _playerCurrentHealth + amount);
            OnHealthChanged?.Invoke(_playerCurrentHealth, _playerMaxHealth);

            if (_playerCurrentHealth > oldHealth)
            {
                GD.Print($"[HexMapController] 玩家恢复生命: {amount}, 当前生命: {_playerCurrentHealth}/{_playerMaxHealth}");
            }
        }

        private void HandlePlayerDeath()
        {
            GD.Print("[HexMapController] 玩家死亡!");
        }

        public bool MoveTo(HexCoord targetCoord)
        {
            if (_currentState != HexMapState.Idle)
            {
                GD.Print("[HexMapController] 当前状态不允许移动");
                return false;
            }

            var targetTile = _currentMap.GetTile(targetCoord);
            if (targetTile == null || !targetTile.CanEnter)
            {
                GD.Print($"[HexMapController] 目标格子不可进入: {targetCoord}");
                return false;
            }

            var path = _currentMap.FindShortestPath(_currentPosition, targetCoord);
            if (path.Count == 0)
            {
                GD.Print($"[HexMapController] 无法找到路径: {_currentPosition} -> {targetCoord}");
                return false;
            }

            _currentPath = path;
            _pathIndex = 0;
            _currentState = HexMapState.Moving;

            MoveToNextTile();

            return true;
        }

        private async void MoveToNextTile()
        {
            _pathIndex++;

            if (_pathIndex >= _currentPath.Count)
            {
                _currentState = HexMapState.Idle;
                _currentPosition = _currentPath[_currentPath.Count - 1];
                OnPlayerMoved?.Invoke(_currentPosition);

                var tile = _currentMap.GetTile(_currentPosition);
                if (tile != null)
                {
                    ProcessTile(tile);
                }

                return;
            }

            var nextCoord = _currentPath[_pathIndex];
            _previousPosition = _currentPosition;
            _currentPosition = nextCoord;
            OnPlayerMoved?.Invoke(nextCoord);

            var previousTile = _currentMap.GetTile(_previousPosition);
            if (previousTile != null && (previousTile.EventType == HexEventType.TwoWayTeleport || previousTile.EventType == HexEventType.OneDirectionTele))
            {
                previousTile.HasTriggeredThisVisit = false;
            }

            await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);

            if (_currentState == HexMapState.Moving)
            {
                MoveToNextTile();
            }
        }

        public bool CanMoveToAdjacent(HexCoord neighbor)
        {
            if (_currentState != HexMapState.Idle)
                return false;

            var currentTile = _currentMap.GetTile(_currentPosition);
            if (currentTile == null)
                return false;

            var neighbors = _currentPosition.GetNeighbors();
            return neighbors.Contains(neighbor) &&
                   _currentMap.Contains(neighbor) &&
                   _currentMap.GetTile(neighbor).CanEnter;
        }

        // ===== 战争迷雾（可见区域）=====

        private readonly HashSet<HexCoord> _revealedTiles = new HashSet<HexCoord>();

        /// <summary>
        /// 视野计算规则（可插拔）。
        /// 默认纯半径；想加「墙体挡视线」就换成 <see cref="FovCalculator.BlockingRule"/>。
        /// </summary>
        public IFovRule FovRule { get; set; } = FovCalculator.DefaultRule;

        /// <summary>当前已照亮的格子集合</summary>
        public IReadOnlyCollection<HexCoord> RevealedTiles => _revealedTiles;

        /// <summary>该格子是否已被照亮（迷雾之外）</summary>
        public bool IsRevealed(HexCoord coord)
        {
            return _revealedTiles.Contains(coord);
        }

        /// <summary>
        /// 照亮一片区域（半径内的格子 + 中心本身）。
        /// 探照灯打开开关时调用；玩家移动时也会照亮周围一圈。
        /// </summary>
        /// <param name="center">中心坐标</param>
        /// <param name="radius">半径（0 = 只照亮中心）</param>
        /// <returns>本次新照亮的格子数量</returns>
        public int RevealTiles(HexCoord center, int radius)
        {
            if (_currentMap == null) return 0;

            List<HexCoord> newlyRevealed = new List<HexCoord>();

            foreach (HexCoord coord in FovCalculator.Compute(center, radius, _currentMap, FovRule))
            {
                if (_revealedTiles.Contains(coord)) continue;

                HexTile tile = _currentMap.GetTile(coord);
                if (tile == null) continue;

                tile.IsRevealed = true;
                _revealedTiles.Add(coord);
                newlyRevealed.Add(coord);
            }

            if (newlyRevealed.Count > 0)
            {
                SaveRevealedTiles();
                OnRevealedChanged?.Invoke();
                GD.Print($"[HexMapController] 照亮 {newlyRevealed.Count} 个格子（中心 {center}，半径 {radius}），当前共 {_revealedTiles.Count}");
            }

            return newlyRevealed.Count;
        }

        /// <summary>把已照亮集合写进地图存档</summary>
        private void SaveRevealedTiles()
        {
            _battleTransitionManager?.SaveRevealedTiles(_revealedTiles, CurrentMapId);
        }

        /// <summary>当前关卡标识（用于照亮存档跨关卡区分）</summary>
        public string CurrentMapId => GlobalData.CurrentMap?.MapId ?? string.Empty;

        /// <summary>
        /// 从地图存档恢复已照亮集合。
        /// 只在「战斗结束回到地图」时调用（RestoreMapState），
        /// 且存档所属关卡必须与当前关卡一致，否则不恢复（换关卡不能继承上一关的视野）。
        /// </summary>
        private void RestoreRevealedTiles()
        {
            HashSet<HexCoord> saved = _battleTransitionManager?.GetSavedRevealedTiles();
            if (saved == null || saved.Count == 0) return;

            string savedMapId = _battleTransitionManager?.GetSavedRevealedMapId() ?? string.Empty;
            if (!string.IsNullOrEmpty(savedMapId) && savedMapId != CurrentMapId)
            {
                GD.Print($"[HexMapController] 照亮存档属于关卡 {savedMapId}，当前关卡 {CurrentMapId}，不恢复");
                return;
            }

            _revealedTiles.Clear();
            foreach (HexCoord coord in saved)
            {
                _revealedTiles.Add(coord);
                HexTile tile = _currentMap?.GetTile(coord);
                if (tile != null) tile.IsRevealed = true;
            }

            GD.Print($"[HexMapController] 恢复已照亮格子: {_revealedTiles.Count}");
            OnRevealedChanged?.Invoke();
        }

        public void QuickMoveTo(HexCoord targetCoord)
        {
            if (_currentState != HexMapState.Idle)
            {
                GD.Print("[HexMapController] 当前状态不允许快速移动");
                return;
            }

            _currentPosition = targetCoord;
            _currentState = HexMapState.Idle;

            OnPlayerMoved?.Invoke(_currentPosition);

            var tile = _currentMap.GetTile(_currentPosition);
            if (tile != null)
            {
                ProcessTile(tile);
            }
        }

        private void ProcessTile(HexTile tile)
        {
            if (tile.IsStart)
            {
                return;
            }

            // 终点格子是 BOSS：先进 BOSS 战，打赢了才算这张地图完成
            if (tile.IsEnd && tile.EventType == HexEventType.BattleBoss)
            {
                TriggerTileEvent(tile);
                return;
            }

            if (tile.ShouldShowTeleportPrompt)
            {
                _currentState = HexMapState.EventProcessing;
                OnTeleportTriggered?.Invoke(_currentPosition);
                return;
            }

            TriggerTileEvent(tile);
        }

        public void ConfirmTeleport()
        {
            if (_currentState != HexMapState.EventProcessing)
                return;

            var tile = _currentMap.GetTile(_currentPosition);
            if (tile == null || !tile.ShouldShowTeleportPrompt)
                return;

            if (tile.EventType == HexEventType.OneDirectionTele && tile.TeleportRole != TeleportRole.Entry)
            {
                GD.Print($"[HexMapController] 单向传送门出口不能传送");
                _currentState = HexMapState.Idle;
                return;
            }

            var targetCoord = _currentMap.GetPairedTeleport(_currentPosition, tile.TeleportPairId);

            if (targetCoord != _currentPosition)
            {
                _currentState = HexMapState.Teleporting;
                TeleportPlayer(targetCoord);
            }
            else
            {
                _currentState = HexMapState.Idle;
            }
        }

        public void CancelTeleport()
        {
            if (_currentState != HexMapState.EventProcessing)
                return;

            _currentState = HexMapState.Idle;
            GD.Print("[HexMapController] 取消传送");
        }

        private void TeleportPlayer(HexCoord targetCoord)
        {
            _currentPosition = targetCoord;
            OnPlayerTeleported?.Invoke(_currentPosition);

            GD.Print($"[HexMapController] 传送到: {targetCoord}");

            _currentState = HexMapState.Idle;

            var tile = _currentMap.GetTile(_currentPosition);
            if (tile != null)
            {
                if (tile.EventType == HexEventType.TwoWayTeleport)
                {
                    tile.HasTriggeredThisVisit = true;
                }
                ProcessTile(tile);
            }
        }

        public void TeleportPlayerTo(HexTile targetTile)
        {
            if (targetTile == null || !targetTile.CanEnter)
            {
                GD.PrintErr("[HexMapController] TeleportPlayerTo: target tile is invalid!");
                return;
            }

            GD.Print($"[HexMapController] TeleportPlayerTo: 从 {_currentPosition} 传送到 {targetTile.Coord}");

            _currentPosition = targetTile.Coord;
            OnPlayerMoved?.Invoke(_currentPosition);

            _currentState = HexMapState.Idle;

            targetTile.OnPlayerEnter();
            OnTileTriggered?.Invoke(targetTile);
        }

        private void TriggerTileEvent(HexTile tile)
        {
            if (!tile.CanTrigger)
            {
                return;
            }

            if (tile.EventType == HexEventType.Hole)
            {
                tile.TriggerHole();
                OnTileTriggered?.Invoke(tile);
                return;
            }

            _eventManager.ProcessEvent(tile, this);

            tile.Trigger();
            tile.OnPlayerEnter();

            OnTileTriggered?.Invoke(tile);
        }

        public void SkipMap()
        {
            if (_currentState != HexMapState.Idle)
            {
                GD.Print("[HexMapController] 当前状态不允许跳过");
                return;
            }

            _hasSkipped = true;
            QuickMoveTo(_currentMap.End);

            CompleteMap();
        }

        public void ReturnToStart()
        {
            if (_currentState != HexMapState.Idle)
            {
                GD.Print("[HexMapController] 当前状态不允许返回");
                return;
            }

            if (!_hasSkipped)
            {
                FailChallenge();
            }
            else
            {
                GD.Print("[HexMapController] 已跳过地图，返回起点");
                QuickMoveTo(_currentMap.Start);
            }
        }

        private void CompleteMap()
        {
            GD.Print("[HexMapController] 地图探索完成!");
            _battleTransitionManager?.ClearSavedState();
            OnMapCompleted?.Invoke();
        }

        private void FailChallenge()
        {
            GD.Print("[HexMapController] 挑战失败!");
            _battleTransitionManager?.ClearSavedState();
            OnChallengeFailed?.Invoke();
        }

        public void OpenShop()
        {
            if (_currentState == HexMapState.ShopOpen) return;

            _currentState = HexMapState.ShopOpen;

            if (BlackMarkShopManager.Instance != null)
            {
                var currentTile = CurrentMap?.GetTile(CurrentPosition);
                if (currentTile != null && currentTile.EventType == HexEventType.Shop)
                {
                    BlackMarkShopManager.Instance.OpenShop();
                    BlackMarkShopManager.Instance.LoadShopItems(currentTile.ShopItems);
                }
            }

            OnShopOpened?.Invoke();
        }

        public void CloseShop()
        {
            if (_currentState != HexMapState.ShopOpen) return;

            if (BlackMarkShopManager.Instance != null)
            {
                var currentTile = CurrentMap?.GetTile(CurrentPosition);
                if (currentTile != null && currentTile.EventType == HexEventType.Shop)
                {
                    currentTile.ShopItems = BlackMarkShopManager.Instance.SaveShopItems();
                }
                BlackMarkShopManager.Instance.CloseShop();
            }

            _currentState = HexMapState.Idle;
            OnShopClosed?.Invoke();
        }

        public void AddBlackMark(int amount)
        {
            BlackMarkShopManager.Instance?.AddBlackMark(amount);
        }

        public bool SpendBlackMark(int amount)
        {
            if (BlackMarkShopManager.Instance == null) return false;
            return BlackMarkShopManager.Instance.SpendBlackMark(amount);
        }

        public List<HexCoord> GetReachableNeighbors()
        {
            var neighbors = _currentPosition.GetNeighbors();
            return neighbors.Where(c =>
                _currentMap.Contains(c) &&
                _currentMap.GetTile(c).CanEnter)
                .ToList();
        }

        public List<HexCoord> GetPathTo(HexCoord target)
        {
            return _currentMap.FindShortestPath(_currentPosition, target);
        }

        public float GetDistanceTo(HexCoord target)
        {
            return _currentPosition.DistanceTo(target);
        }

        public void Reset()
        {
            if (_currentMap != null)
            {
                _currentMap.ResetAllTiles();
            }

            _currentPosition = _currentMap?.Start ?? new HexCoord(0, 0);
            _currentState = HexMapState.Idle;
            _currentPath.Clear();
            _pathIndex = 0;
            _hasSkipped = false;

            GD.Print("[HexMapController] 地图控制器已重置");
        }
    }
}
