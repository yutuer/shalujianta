using Godot;
using System;

namespace FishEatFish.Battle
{
    public partial class BattleTransitionManager : Node
    {
        private static BattleTransitionManager _instance;
        public static BattleTransitionManager Instance => _instance;

        private HexMap.HexCoord _savedPlayerPosition;
        private HexMap.HexMap _savedMap;
        private int _savedPlayerLevel;
        private float _savedPlayerHealth;
        private float _savedPlayerMaxHealth;
        private int _savedBlackMarkCount;
        private HexMap.HexEventType _currentBattleType;
        private string _currentEnemyConfig;

        public bool IsInBattle { get; private set; }
        public HexMap.HexEventType CurrentBattleType => _currentBattleType;
        public string CurrentEnemyConfig => _currentEnemyConfig;

        /// <summary>
        /// 是否存在可恢复的地图存档（战斗返回地图时用）
        /// </summary>
        public bool HasSavedMap => _savedMap != null;

        /// <summary>
        /// 指定本次战斗使用的敌人波次（供调试场景准备器在切场景前预设）
        /// </summary>
        public void SetEnemyConfig(string enemyConfigKey)
        {
            _currentEnemyConfig = enemyConfigKey;
            GD.Print($"[BattleTransitionManager] 敌人波次设为 {enemyConfigKey}");
        }

        /// <summary>
        /// 打赢 BOSS 战后置位：回到地图时需要结算"地图完成"
        /// </summary>
        public bool PendingMapCompletion { get; private set; } = false;

        /// <summary>
        /// 清除"待结算地图完成"标记（地图结算后调用）
        /// </summary>
        public void ClearPendingMapCompletion()
        {
            PendingMapCompletion = false;
        }

        /// <summary>
        /// 把战斗结束时的剩余血量写回地图存档（血量在地图与战斗之间是同一份）
        /// </summary>
        public void UpdateSavedPlayerHealth(float health)
        {
            _savedPlayerHealth = Mathf.Max(0f, health);
            GD.Print($"[BattleTransitionManager] 地图血量更新为 {_savedPlayerHealth}");
        }

        public event Action OnBattleStart;
        public event Action<bool> OnBattleEnd;

        public override void _Ready()
        {
            GD.Print("[BattleTransitionManager] _Ready called");
            _instance = this;
            IsInBattle = false;
        }

        public void SaveMapState(HexMap.HexMapController controller)
        {
            GD.Print("[BattleTransitionManager] SaveMapState called");

            if (controller == null)
            {
                GD.PrintErr("[BattleTransitionManager] controller is null!");
                return;
            }

            _savedPlayerPosition = controller.CurrentPosition;
            _savedPlayerLevel = controller.PlayerLevel;
            _savedPlayerHealth = controller.PlayerCurrentHealth;
            _savedPlayerMaxHealth = controller.PlayerMaxHealth;
            _savedBlackMarkCount = controller.BlackMarkCount;

            if (controller.CurrentMap != null)
            {
                _savedMap = controller.CurrentMap;
            }

            GD.Print($"[BattleTransitionManager] State saved: Position={_savedPlayerPosition}, Health={_savedPlayerHealth}/{_savedPlayerMaxHealth}, BlackMark={_savedBlackMarkCount}");
        }

        public void StartBattle(HexMap.HexEventType battleType, string enemyConfig, HexMap.HexMapController controller)
        {
            GD.Print($"[BattleTransitionManager] StartBattle called: type={battleType}, config={enemyConfig}");

            if (IsInBattle)
            {
                GD.PrintErr("[BattleTransitionManager] 上一次战斗未正常结束，重置状态后继续");
                ClearSavedState();
                IsInBattle = false;
            }

            SaveMapState(controller);

            _currentBattleType = battleType;
            _currentEnemyConfig = enemyConfig;
            IsInBattle = true;

            OnBattleStart?.Invoke();

            GD.Print($"[BattleTransitionManager] Switching to BattleScene...");
            GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
        }

        public void EndBattle(bool victory)
        {
            GD.Print($"[BattleTransitionManager] EndBattle called: victory={victory}");

            IsInBattle = false;

            // 打赢 BOSS 战 → 回到地图后要结算"地图完成"
            if (victory && _currentBattleType == HexMap.HexEventType.BattleBoss)
            {
                PendingMapCompletion = true;
                GD.Print("[BattleTransitionManager] BOSS 战胜利，标记为待结算地图完成");
            }

            OnBattleEnd?.Invoke(victory);

            GD.Print($"[BattleTransitionManager] Switching back to HexMapScene...");
            GetTree().ChangeSceneToFile("res://Scenes/HexMapScene.tscn");
        }

        public HexMap.HexCoord GetSavedPlayerPosition()
        {
            return _savedPlayerPosition;
        }

        public int GetSavedPlayerLevel()
        {
            return _savedPlayerLevel;
        }

        public float GetSavedPlayerHealth()
        {
            return _savedPlayerHealth;
        }

        public float GetSavedPlayerMaxHealth()
        {
            return _savedPlayerMaxHealth;
        }

        public int GetSavedBlackMarkCount()
        {
            return _savedBlackMarkCount;
        }

        public HexMap.HexMap GetSavedMap()
        {
            return _savedMap;
        }

        public void ClearSavedState()
        {
            _savedPlayerPosition = new HexMap.HexCoord(0, 0);
            _savedMap = null;
            _savedPlayerLevel = 1;
            _savedPlayerHealth = 100f;
            _savedPlayerMaxHealth = 100f;
            _savedBlackMarkCount = 0;
            _currentBattleType = HexMap.HexEventType.Empty;
            _currentEnemyConfig = null;
            GD.Print("[BattleTransitionManager] Saved state cleared");
        }
    }
}
