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
                GD.PrintErr("[BattleTransitionManager] Already in battle!");
                return;
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
