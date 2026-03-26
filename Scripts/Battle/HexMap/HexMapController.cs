using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.UI.HexMap;
using FishEatFish.Shop;

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

        [Export]
        private int _mapRadius = 3;

        private HexMap _currentMap;
        public HexMap CurrentMap => _currentMap;

        private HexCoord _currentPosition;
        public HexCoord CurrentPosition => _currentPosition;

        private HexMapState _currentState = HexMapState.Idle;
        public HexMapState CurrentState => _currentState;

        private HexMapGenerator _generator;
        private HexEventManager _eventManager;

        private float _playerMaxHealth = 100f;
        private float _playerCurrentHealth = 100f;
        public float PlayerCurrentHealth => _playerCurrentHealth;
        public float PlayerMaxHealth => _playerMaxHealth;

        private int _blackMarkCount = 0;
        public int BlackMarkCount => _blackMarkCount;

        private bool _hasSkipped = false;
        public bool HasSkipped => _hasSkipped;

        private List<HexCoord> _currentPath = new List<HexCoord>();
        private int _pathIndex = 0;

        public System.Action<HexCoord> OnPlayerMoved;
        public System.Action<HexTile> OnTileTriggered;
        public System.Action<HexCoord> OnTeleportTriggered;
        public System.Action<float, float> OnHealthChanged;
        public System.Action<int> OnBlackMarkChanged;
        public System.Action OnMapCompleted;
        public System.Action OnChallengeFailed;
        public System.Action OnShopOpened;
        public System.Action OnShopClosed;

        public override void _Ready()
        {
            _instance = this;

            _generator = new HexMapGenerator();
            _eventManager = new HexEventManager();

            GenerateMap();
        }

        public void GenerateMap()
        {
            _currentMap = _generator.Generate(_mapRadius, _playerLevel);
            _currentPosition = _currentMap.Start;
            _currentState = HexMapState.Idle;
            _hasSkipped = false;

            GD.Print($"[HexMapController] 地图已生成，起点: {_currentPosition}, 终点: {_currentMap.End}");
        }

        public void SetPlayerLevel(int level)
        {
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
            _currentPosition = nextCoord;
            OnPlayerMoved?.Invoke(nextCoord);

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
            if (tile.IsStart || tile.IsEnd && tile.EventType == HexEventType.BattleBoss)
            {
                if (tile.IsEnd && tile.EventType == HexEventType.BattleBoss)
                {
                    CompleteMap();
                }
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
            OnPlayerMoved?.Invoke(_currentPosition);

            GD.Print($"[HexMapController] 传送到: {targetCoord}");

            _currentState = HexMapState.Idle;

            var tile = _currentMap.GetTile(_currentPosition);
            if (tile != null)
            {
                ProcessTile(tile);
            }
        }

        private void TriggerTileEvent(HexTile tile)
        {
            if (tile.IsVisited && tile.TriggerType == EventTriggerType.OneTime)
            {
                return;
            }

            _eventManager.ProcessEvent(tile, this);

            tile.OnPlayerEnter();

            OnTileTriggered?.Invoke(tile);

            if (tile.EventType == HexEventType.Hole)
            {
                tile.TriggerHole();
            }
            else
            {
                tile.Trigger();
            }
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
            OnMapCompleted?.Invoke();
        }

        private void FailChallenge()
        {
            GD.Print("[HexMapController] 挑战失败!");
            OnChallengeFailed?.Invoke();
        }

        public void OpenShop()
        {
            if (_currentState == HexMapState.ShopOpen)
                return;

            _currentState = HexMapState.ShopOpen;

            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.OpenShop();
            }

            OnShopOpened?.Invoke();
            GD.Print("[HexMapController] 商店已打开");
        }

        public void CloseShop()
        {
            if (_currentState != HexMapState.ShopOpen)
                return;

            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.CloseShop();
            }

            _currentState = HexMapState.Idle;
            OnShopClosed?.Invoke();
            GD.Print("[HexMapController] 商店已关闭");
        }

        public void AddBlackMark(int amount)
        {
            _blackMarkCount += amount;
            OnBlackMarkChanged?.Invoke(_blackMarkCount);
            GD.Print($"[HexMapController] 获得 {amount} 黑印，当前: {_blackMarkCount}");
        }

        public bool SpendBlackMark(int amount)
        {
            if (_blackMarkCount < amount)
            {
                GD.Print($"[HexMapController] 黑印不足: 需要 {amount}，现有 {_blackMarkCount}");
                return false;
            }

            _blackMarkCount -= amount;
            OnBlackMarkChanged?.Invoke(_blackMarkCount);
            return true;
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
