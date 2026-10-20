using Godot;
using System;
using System.Collections.Generic;

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
        private HashSet<HexMap.HexCoord> _savedRevealedTiles = new HashSet<HexMap.HexCoord>();

        /// <summary>已照亮集合所属的关卡标识（空 = 无存档）</summary>
        private string _savedRevealedMapId = string.Empty;

        /// <summary>已消耗的洞（一次性格子，用掉就从地图上删除）</summary>
        private HashSet<HexMap.HexCoord> _savedConsumedHoles = new HashSet<HexMap.HexCoord>();

        /// <summary>保存已消耗的洞集合（D12：战斗返回后不能丢）</summary>
        public void SaveConsumedHoles(HashSet<HexMap.HexCoord> holes)
        {
            _savedConsumedHoles = holes != null
                ? new HashSet<HexMap.HexCoord>(holes)
                : new HashSet<HexMap.HexCoord>();
        }

        /// <summary>读取已消耗的洞集合</summary>
        public HashSet<HexMap.HexCoord> GetSavedConsumedHoles()
        {
            return _savedConsumedHoles;
        }

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

        /// <summary>
        /// 保存已照亮（战争迷雾）的格子集合
        /// </summary>
        /// <param name="revealed">已照亮格子集合</param>
        /// <param name="mapId">关卡标识（GlobalData.CurrentMap.MapId），用于跨关卡时区分，避免串味</param>
        public void SaveRevealedTiles(HashSet<HexMap.HexCoord> revealed, string mapId = null)
        {
            _savedRevealedTiles = revealed != null
                ? new HashSet<HexMap.HexCoord>(revealed)
                : new HashSet<HexMap.HexCoord>();
            _savedRevealedMapId = mapId ?? string.Empty;
        }

        /// <summary>
        /// 读取已照亮（战争迷雾）的格子集合
        /// </summary>
        public HashSet<HexMap.HexCoord> GetSavedRevealedTiles()
        {
            return _savedRevealedTiles;
        }

        /// <summary>已照亮集合所属的关卡标识</summary>
        public string GetSavedRevealedMapId()
        {
            return _savedRevealedMapId;
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

        /// <summary>
        /// 待显示的结算数据（战斗胜利后传给结算界面）
        /// </summary>
        public BattleResultData PendingResult { get; private set; } = null;

        /// <summary>先暂存结算数据（奖励选择之后还要用）</summary>
        public void SetPendingResult(BattleResultData data)
        {
            PendingResult = data;
        }

        /// <summary>待选的战斗奖励候选（战斗胜利后传给奖励选择界面）</summary>
        public List<BattleRewardChoice> PendingRewardChoices { get; private set; } = null;

        /// <summary>待镶嵌的刻印（奖励选择界面选了刻印后带过来）</summary>
        public BattleRewardChoice PendingRewardEngraving { get; private set; } = null;

        /// <summary>待镶嵌刻印的目标卡牌候选</summary>
        public List<FishEatFish.Battle.Card.Card> PendingRewardTargetCards { get; private set; } = null;

        /// <summary>设置待镶嵌的刻印</summary>
        public void SetPendingRewardEngraving(BattleRewardChoice choice)
        {
            PendingRewardEngraving = choice;
        }

        /// <summary>设置待镶嵌刻印的目标卡牌候选</summary>
        public void SetPendingRewardTargetCards(List<FishEatFish.Battle.Card.Card> cards)
        {
            PendingRewardTargetCards = cards;
        }

        /// <summary>
        /// 显示战斗奖励选择界面：保存候选并切场景。
        /// </summary>
        public void ShowBattleReward(List<BattleRewardChoice> choices)
        {
            GD.Print($"[BattleTransitionManager] ShowBattleReward called: {choices?.Count ?? 0} 个候选");

            if (choices == null || choices.Count == 0)
            {
                GD.PrintErr("[BattleTransitionManager] 没有奖励候选，直接结束战斗");
                EndBattle(true);
                return;
            }

            PendingRewardChoices = choices;
            PendingRewardEngraving = null;
            PendingRewardTargetCards = null;

            GetTree().ChangeSceneToFile("res://Scenes/UI/BattleRewardUI.tscn");
            GD.Print("[BattleTransitionManager] ShowBattleReward completed");
        }

        /// <summary>
        /// 奖励选择完成：清掉待选数据，接着显示结算界面（结算之后才回地图）。
        /// </summary>
        public void CompleteBattleReward()
        {
            GD.Print("[BattleTransitionManager] CompleteBattleReward called");

            PendingRewardChoices = null;
            PendingRewardEngraving = null;
            PendingRewardTargetCards = null;

            if (PendingResult != null)
            {
                ShowBattleResult(PendingResult);
            }
            else
            {
                EndBattle(true);
            }
            GD.Print("[BattleTransitionManager] CompleteBattleReward completed");
        }

        /// <summary>
        /// 显示战斗结算界面：保存结算数据并切场景
        /// </summary>
        public void ShowBattleResult(BattleResultData data)
        {
            GD.Print($"[BattleTransitionManager] ShowBattleResult called: map={data?.MapId}, turns={data?.TurnCount}, stars={data?.StarCount}");

            if (data == null)
            {
                GD.PrintErr("[BattleTransitionManager] 结算数据为 null，直接回地图");
                EndBattle(true);
                return;
            }

            PendingResult = data;
            GetTree().ChangeSceneToFile("res://Scenes/UI/BattleResultUI.tscn");
            GD.Print("[BattleTransitionManager] ShowBattleResult completed");
        }

        /// <summary>
        /// 结算界面点「完成调查」：回六边形地图；没有地图存档则回关卡地图
        /// </summary>
        public void CompleteBattleResult()
        {
            GD.Print("[BattleTransitionManager] CompleteBattleResult called");

            PendingResult = null;

            if (HasSavedMap)
            {
                EndBattle(true);
            }
            else
            {
                GD.Print("[BattleTransitionManager] 无地图存档，返回关卡地图");
                ClearSavedState();
                GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
            }

            GD.Print("[BattleTransitionManager] CompleteBattleResult completed");
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
            _savedRevealedTiles = new HashSet<HexMap.HexCoord>();
            _savedRevealedMapId = string.Empty;
            _savedConsumedHoles = new HashSet<HexMap.HexCoord>();
            GD.Print("[BattleTransitionManager] Saved state cleared");
        }
    }
}
