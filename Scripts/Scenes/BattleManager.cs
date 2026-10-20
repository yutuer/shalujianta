using Godot;
using System.Collections.Generic;
using FishEatFish.Battle;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.EngravingSystem;
using FishEatFish.Scenes;
using FishEatFish.AI;
using FishEatFish.Shop;

namespace FishEatFish.Scenes
{
    /// <summary>
    /// 战斗管理器 - 核心战斗系统
    /// 功能：管理战斗流程、玩家/敌人状态、卡牌系统、银钥/怒气系统
    /// 初始化顺序：Attacker → Managers → UI → Player → Enemies → Cards → StartBattle
    /// </summary>
    public partial class BattleManager : Node2D
    {
	// ========== 核心战斗数据 ==========
	private Player player;
	private List<Enemy> enemies = new List<Enemy>();
	private int turnCount = 1;

		/// <summary>
		/// 本场战斗已进行的回合数（结算界面要用）
		/// </summary>
		public int TurnCount => turnCount;
	private bool isPlayerTurn = true;

	// ========== UI节点引用 ==========
	private HBoxContainer handContainer;
	private HBoxContainer enemyContainer;
	private HBoxContainer playerStatsContainer;
	private Button endTurnButton;
	private Label handInfoLabel;
	private Button mapButton;
	private Label silverKeyLabel;
	private Button keyOrderButton;

	// ========== UI组件 ==========
	private List<EnemyUI> enemyUIs = new List<EnemyUI>();
	private List<CardUI> cardUIs = new List<CardUI>();
	private PlayerStatsUI playerStatsUI;
	private BuffDebuffDisplay _buffDebuffDisplay;
	private PlayerZoneUI playerZoneUI;

	private BattleLogWindow battleLogWindow;
	private EnemyIntentDisplay intentDisplay;
	private CardLayoutManager cardLayoutManager;
	private EnemyManager enemyManager;
	private ResourceManager resourceManager;

	// ========== CardLayoutManager 节点引用 ==========
	private CardLayoutManager cardLayoutManagerNode;

	// ========== 银钥系统 ==========
	private SilverKeyConfig silverKeyConfig;
	private KeyOrderManager keyOrderManager;
	private int currentSilverKey = 0;
	private bool isKeyOrderSelectionOpen = false;
	public System.Action<int, int> OnSilverKeyChanged;

	// ========== 其他 ==========
	private int hoveredCardIndex = -1;
	private Attacker attacker;

	// ========== 怒气系统 ==========
	private RageManager rageManager;
	private BattleEndRageHandler battleEndRageHandler;
	private List<CharacterPosition> characterPositions = new List<CharacterPosition>();

	// ========== 新增UI组件 ==========
	private SilverKeyProgressIndicator silverKeyProgressIndicator;
	private AttackerDisplayUI attackerDisplayUI;
	private RageDisplayUI rageDisplayUI;
	private KeyOrderSelectionUI keyOrderSelectionUI;
	private KeyOrderDescriptionUI keyOrderDescriptionUI;

	public override void _Ready()
	{
		InitializeAttacker();
		InitializeManagers();
		InitializeUI();
		InitializePlayer();
		InitializeEnemies();
		InitializeCards();
		StartBattle();
	}

	/// <summary>
	/// 初始化攻击体和钥令
	/// 优先级：GlobalData.SelectedCharacters > 默认角色
	/// </summary>
	private void InitializeAttacker()
	{
		if (GlobalData.SelectedCharacters != null && GlobalData.SelectedCharacters.Length == 4)
		{
			attacker = new Attacker();
			AddChild(attacker);
			attacker.SetCharacters(GlobalData.SelectedCharacters);
			GD.Print($"Attacker created with characters: {string.Join(", ", System.Array.ConvertAll(GlobalData.SelectedCharacters, c => c.Name))}");
		}
		else
		{
			GD.PrintErr("No valid character selection found! Using default.");
			CharacterDefinition[] defaultChars = new CharacterDefinition[]
			{
				CharacterDefinition.CreateRat(),
				CharacterDefinition.CreateOx(),
				CharacterDefinition.CreateTiger(),
				CharacterDefinition.CreateRabbit()
			};
			attacker = new Attacker();
			AddChild(attacker);
			attacker.SetCharacters(defaultChars);
		}

		if (GlobalData.EquippedKeyOrder != null)
		{
			keyOrderManager = new KeyOrderManager();
			AddChild(keyOrderManager);
			keyOrderManager.EquipKeyOrder(GlobalData.EquippedKeyOrder);
			GD.Print($"KeyOrder equipped: {GlobalData.EquippedKeyOrder.Name}");
		}
	}

	/// <summary>
	/// 初始化各类管理器
	/// 包含：ResourceManager、EnemyManager、RageManager、BattleEndRageHandler
	/// </summary>
	private void InitializeManagers()
	{
		GD.Print("[BattleManager] 开始初始化Manager...");
		resourceManager = new ResourceManager();
		resourceManager.LoadAll();
		GD.Print("[BattleManager] ResourceManager加载完成");

		enemyManager = new EnemyManager();
		silverKeyConfig = SilverKeyConfig.CreateDefault();

		if (keyOrderManager == null)
		{
			keyOrderManager = new KeyOrderManager();
			AddChild(keyOrderManager);
			KeyOrder defaultOrder = KeyOrder.CreateSilverSlash();
			keyOrderManager.EquipKeyOrder(defaultOrder);
			GD.Print("[BattleManager] 默认钥令装备完成");
		}

		rageManager = new RageManager();
		AddChild(rageManager);
		GD.Print("[BattleManager] RageManager初始化完成");

		battleEndRageHandler = new BattleEndRageHandler();
		AddChild(battleEndRageHandler);
		GD.Print("[BattleManager] BattleEndRageHandler初始化完成");
	}

	public override void _Process(double delta)
	{
		int newHoveredIndex = cardLayoutManager.CheckCardHover(
			cardUIs,
			handContainer,
			GetGlobalMousePosition(),
			hoveredCardIndex);

		if (newHoveredIndex != hoveredCardIndex)
		{
			hoveredCardIndex = newHoveredIndex;
		}

		cardLayoutManager.UpdateCardPositions(cardUIs, handContainer, hoveredCardIndex);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
			{
				TryQuickPlayCard();
			}
		}
	}

	private void TryQuickPlayCard()
	{
		if (hoveredCardIndex >= 0 && hoveredCardIndex < cardUIs.Count)
		{
			CardUI hoveredCard = cardUIs[hoveredCardIndex];
			if (hoveredCard != null && isPlayerTurn && player.CurrentEnergy >= hoveredCard.GetCardData().Cost)
			{
				hoveredCard.GetPlayButton().EmitSignal("pressed");
			}
		}
	}

	/// <summary>
	/// 初始化UI节点引用和事件绑定
	/// 关键节点：HandArea、EnemyZone、TopBar、BottomBar
	/// </summary>
	private void InitializeUI()
	{
		GD.Print("[BattleManager] 开始初始化UI...");

		handContainer = GetNodeOrNull<HBoxContainer>("UI/HandArea/HandContainer");
		enemyContainer = GetNodeOrNull<HBoxContainer>("UI/MainArea/BattleZones/EnemyZone/EnemyContainer");
		playerStatsContainer = GetNodeOrNull<HBoxContainer>("UI/TopBar/PlayerStats");
		endTurnButton = GetNodeOrNull<Button>("UI/BottomBar/EndTurnButton");
		handInfoLabel = GetNodeOrNull<Label>("UI/BottomBar/HandInfo");
		mapButton = GetNodeOrNull<Button>("UI/BottomBar/MapButton");
		silverKeyLabel = GetNodeOrNull<Label>("UI/TopBar/SilverKeyLabel");
		keyOrderButton = GetNodeOrNull<Button>("UI/BottomBar/KeyOrderButton");

		if (handContainer == null || enemyContainer == null || endTurnButton == null)
		{
			GD.PrintErr("[BattleManager] 关键UI节点缺失，初始化失败!");
			return;
		}

		GD.Print("[BattleManager] UI节点获取成功");

		if (HasNode("UI/MainArea/BattleZones/PlayerZone"))
		{
			Control playerZone = GetNode<Control>("UI/MainArea/BattleZones/PlayerZone");
			playerZoneUI = new PlayerZoneUI();
			playerZone.AddChild(playerZoneUI);
			GD.Print("[BattleManager] PlayerZoneUI初始化成功");
		}

		cardLayoutManagerNode = GetNodeOrNull<CardLayoutManager>("CardLayoutManager");
		if (cardLayoutManagerNode != null)
		{
			cardLayoutManager = cardLayoutManagerNode;
			GD.Print("[BattleManager] CardLayoutManager 从场景节点加载成功");
		}
		else
		{
			cardLayoutManager = new CardLayoutManager();
			AddChild(cardLayoutManager);
			GD.Print("[BattleManager] CardLayoutManager 创建新实例");
		}

		endTurnButton.Pressed += OnEndTurnPressed;
		mapButton.Pressed += OnMapPressed;

		if (keyOrderButton != null)
		{
			keyOrderButton.Pressed += OnKeyOrderPressed;
			keyOrderButton.Disabled = true;
		}

		InitializeNewUIComponents();
		InitializeBattleLogWindow();
		GD.Print("[BattleManager] UI初始化完成");
	}

	/// <summary>
	/// 初始化新增的UI组件
	/// 包含：银钥进度指示器、攻击体显示UI、怒气显示UI、钥令选择UI
	/// 支持从.tscn场景文件加载，也支持代码创建fallback
	/// </summary>
	private void InitializeNewUIComponents()
	{
		GD.Print("[BattleManager] 开始初始化新UI组件...");

		var uiCanvas = GetNode<CanvasLayer>("UI");

		silverKeyProgressIndicator = LoadUIComponent<SilverKeyProgressIndicator>(
			"res://Scenes/UI/SilverKeyProgressIndicator.tscn",
			() => new SilverKeyProgressIndicator(),
			uiCanvas
		);
		if (silverKeyProgressIndicator != null)
		{
			silverKeyProgressIndicator.Position = new Vector2(50, 400);
		}
		GD.Print("[BattleManager] 银钥进度指示器初始化成功");

		attackerDisplayUI = LoadUIComponent<AttackerDisplayUI>(
			"res://Scenes/UI/AttackerDisplayUI.tscn",
			() => new AttackerDisplayUI(),
			uiCanvas
		);
		if (attackerDisplayUI != null)
		{
			attackerDisplayUI.Position = new Vector2(200, 10);
			if (attacker != null)
			{
				attackerDisplayUI.SetAttacker(attacker);
			}
			// 点击角色槽位释放大招（怒气满时才可点）
			attackerDisplayUI.SetUltimateClickCallback(OnUltimateSlotClicked);
		}
		GD.Print("[BattleManager] 攻击体角色显示UI初始化成功");

		rageDisplayUI = LoadUIComponent<RageDisplayUI>(
			"res://Scenes/UI/RageDisplayUI.tscn",
			() => new RageDisplayUI(),
			uiCanvas
		);
		if (rageDisplayUI != null)
		{
			rageDisplayUI.Position = new Vector2(20, 150);
		}
		InitializeCharacterPositions();
		GD.Print("[BattleManager] 怒气显示UI初始化成功");

		keyOrderSelectionUI = LoadUIComponent<KeyOrderSelectionUI>(
			"res://Scenes/UI/KeyOrderSelectionUI.tscn",
			() => new KeyOrderSelectionUI(),
			uiCanvas
		);
		if (keyOrderSelectionUI != null)
		{
			keyOrderSelectionUI.Position = new Vector2(100, 200);
		}
		GD.Print("[BattleManager] 钥令选择UI初始化成功");

		keyOrderDescriptionUI = LoadUIComponent<KeyOrderDescriptionUI>(
			"res://Scenes/UI/KeyOrderDescriptionUI.tscn",
			() => new KeyOrderDescriptionUI(),
			uiCanvas
		);
		if (keyOrderDescriptionUI != null)
		{
			keyOrderDescriptionUI.Position = new Vector2(300, 250);
		}
		GD.Print("[BattleManager] 钥令说明UI初始化成功");
	}

	/// <summary>
	/// 点击角色槽位 → 尝试释放该角色的大招（tasks.md 1.4.2 / 1.4.3）。
	/// </summary>
	/// <param name="slotIndex">槽位索引 0~3</param>
	private void OnUltimateSlotClicked(int slotIndex)
	{
		GD.Print($"[BattleManager] OnUltimateSlotClicked called: slot={slotIndex}");

		if (rageManager == null || !isPlayerTurn) return;
		if (slotIndex < 0 || slotIndex >= characterPositions.Count) return;

		CharacterPosition position = characterPositions[slotIndex];
		if (position == null || !position.CanUseUltimate())
		{
			AddMessage("怒气不足，无法释放大招!", LogType.System);
			return;
		}

		bool released = rageManager.UseUltimate(slotIndex, player, enemies.ToArray());
		if (!released)
		{
			AddMessage("大招释放失败!", LogType.System);
			return;
		}

		// 释放动画 + 表现刷新
		position.PlayUltimateAnimation(player, enemies.ToArray());
		AddMessage($"{position.Character?.Name ?? "角色"} 释放了大招！", LogType.System);

		UpdateRageDisplay();
		UpdateUI();
		UpdateEnemyUI();
		UpdateBuffDebuffDisplay();
		UpdateEnemyStatusIcons();

		CheckGameOver();
		GD.Print($"[BattleManager] OnUltimateSlotClicked completed: slot={slotIndex}");
	}

	/// <summary>
	/// 通用UI组件加载方法
	/// 优先从.tscn场景文件加载，如果失败则使用代码创建
	/// </summary>
	private T LoadUIComponent<T>(string scenePath, System.Func<T> fallbackCreator, Node parent) where T : Control
	{
		try
		{
			if (ResourceLoader.Exists(scenePath))
			{
				var packedScene = ResourceLoader.Load<PackedScene>(scenePath);
				if (packedScene != null)
				{
					var instance = packedScene.Instantiate<T>();
					if (instance != null)
					{
						parent.AddChild(instance);
						GD.Print($"[BattleManager] 从场景加载: {scenePath}");
						return instance;
					}
				}
			}

			GD.Print($"[BattleManager] 场景加载失败，使用代码创建: {scenePath}");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[BattleManager] 加载场景异常: {scenePath}, {ex.Message}");
		}

		var fallback = fallbackCreator();
		if (fallback != null)
		{
			parent.AddChild(fallback);
		}
		return fallback;
	}

	/// <summary>
	/// 初始化角色位置并注册到怒气管理器
	/// 创建4个CharacterPosition实例并绑定到attacker的角色
	/// </summary>
	private void InitializeCharacterPositions()
	{
		characterPositions.Clear();
		if (attacker != null && attacker.Characters != null)
		{
			for (int i = 0; i < 4 && i < attacker.Characters.Length; i++)
			{
				CharacterPosition pos = new CharacterPosition();
				pos.PositionIndex = i;
				pos.Character = attacker.Characters[i];
				AddChild(pos);
				characterPositions.Add(pos);
				rageManager.RegisterCharacterPosition(pos);

				if (rageDisplayUI != null && attacker.Characters[i] != null)
				{
					Color charColor = new Color(1f, 0.9f, 0.7f);
					rageDisplayUI.SetCharacterInfo(i, attacker.Characters[i].Name, charColor);
				}
			}
		}

		UpdateRageDisplay();
		GD.Print($"[BattleManager] 角色位置初始化完成，共{characterPositions.Count}个角色");
	}

	private void InitializeBattleLogWindow()
	{
		battleLogWindow = resourceManager.BattleLogScene.Instantiate<BattleLogWindow>();
		GetNode<CanvasLayer>("UI").AddChild(battleLogWindow);
		intentDisplay = new EnemyIntentDisplay();
	}

	/// <summary>
	/// 初始化玩家属性
	/// 属性来源：attacker（攻击体）或默认值
	/// </summary>
	private void InitializePlayer()
	{
		GD.Print("[BattleManager] 开始初始化Player...");
		player = new Player();
		AddChild(player);

		if (attacker != null)
		{
			player.MaxHealth = attacker.TotalHealth;
			player.MaxEnergy = attacker.TotalEnergy;
			player.DrawCount = attacker.TotalDrawCount;
			player.Class = "攻击者";
			GD.Print($"[BattleManager] 使用Attacker属性: HP={player.MaxHealth}, Energy={player.MaxEnergy}, Draw={player.DrawCount}");
		}
		else
		{
			player.MaxHealth = 100;
			player.MaxEnergy = 3;
			player.DrawCount = 5;
			GD.Print("[BattleManager] 使用默认Player属性");
		}

		player.CurrentHealth = player.MaxHealth;

		// 带着地图上的当前血量进战斗（血量在地图与战斗之间是同一份）
		BattleTransitionManager transitionManager = BattleTransitionManager.Instance;
		if (transitionManager != null && transitionManager.HasSavedMap)
		{
			float carriedHealth = transitionManager.GetSavedPlayerHealth();
			if (carriedHealth > 0f)
			{
				player.CurrentHealth = (int)Mathf.Min(carriedHealth, player.MaxHealth);
				GD.Print($"[BattleManager] 携带地图血量进入战斗: {player.CurrentHealth}/{player.MaxHealth}");
			}
		}

		player.MaxHandSize = 10;
		GD.Print("[BattleManager] Player初始化完成");
	}

	/// <summary>
	/// 初始化敌人
	/// 敌人组合由地图上的战斗格子决定（普通 / 精英 / BOSS），配置见 Data/enemies.json
	/// </summary>
	private void InitializeEnemies()
	{
		GD.Print("[BattleManager] 开始初始化敌人...");
		ClearEnemyUI();
		ClearEnemies();

		string waveKey = ResolveEnemyWaveKey();
		List<Enemy> waveEnemies = EnemyConfigLoader.CreateEnemies(waveKey);

		if (waveEnemies.Count == 0)
		{
			GD.PrintErr($"[BattleManager] 波次 '{waveKey}' 没有配置敌人，回退到默认阵容");
			AddEnemy("守卫 A", 54, 8, 0, "balanced", 2);
			AddEnemy("精锐 B", 50, 10, 1, "aggressive", 1);
			AddEnemy("背信 C", 38, 6, 2, "defensive", 0);
		}
		else
		{
			foreach (Enemy enemy in waveEnemies)
			{
				enemies.Add(enemy);
				AddChild(enemy);
				CreateEnemyUI(enemy);
				GD.Print($"[BattleManager] 创建敌人: {enemy.EnemyName} HP={enemy.MaxHealth} ATK={enemy.Attack} DEF={enemy.Defense} 行为={enemy.Behavior}");
			}
		}

		enemyManager.SetEnemies(enemies);
		GD.Print($"[BattleManager] 敌人初始化完成，共{enemies.Count}个敌人 (波次: {waveKey})");
	}

	/// <summary>
	/// 解析本次战斗使用的敌人波次键
	/// 优先取地图战斗格子指定的配置，没有则用普通波次
	/// </summary>
	private string ResolveEnemyWaveKey()
	{
		BattleTransitionManager transitionManager = BattleTransitionManager.Instance;
		if (transitionManager != null && !string.IsNullOrEmpty(transitionManager.CurrentEnemyConfig))
		{
			GD.Print($"[BattleManager] 使用地图指定的敌人波次: {transitionManager.CurrentEnemyConfig}");
			return transitionManager.CurrentEnemyConfig;
		}

		GD.Print("[BattleManager] 未指定敌人波次，回退到 normal_wave");
		return "normal_wave";
	}

	private void ClearEnemies()
	{
		foreach (Enemy enemy in enemies)
		{
			if (enemy != null)
			{
				enemy.QueueFree();
			}
		}
		enemies.Clear();
	}

	private void AddEnemy(string name, int maxHealth, int attack, int position, string behavior = "aggressive", int defense = 0, bool isElite = false, bool isBoss = false)
	{
		Enemy enemy = new Enemy
		{
			EnemyName = name,
			MaxHealth = maxHealth,
			CurrentHealth = maxHealth,
			Attack = attack,
			Defense = defense,
			Position = position,
			Behavior = behavior,
			IsElite = isElite,
			IsBoss = isBoss
		};
		enemies.Add(enemy);
		AddChild(enemy);
		CreateEnemyUI(enemy);
	}

	private void CreateEnemyUI(Enemy enemy)
	{
		if (enemyContainer == null)
		{
			GD.PrintErr("[BattleManager] enemyContainer为null，无法创建敌人UI!");
			return;
		}

		EnemyUI enemyUI = resourceManager.EnemyScene.Instantiate<EnemyUI>();
		enemyUI.Setup(enemy);
		enemyUI.SetSprite(resourceManager.EnemyTexture);
		enemyContainer.AddChild(enemyUI);
		enemyUIs.Add(enemyUI);
		GD.Print($"[BattleManager] 创建敌人UI: {enemy.EnemyName}");
	}

	private void ClearEnemyUI()
	{
		foreach (EnemyUI ui in enemyUIs)
		{
			ui.QueueFree();
		}
		enemyUIs.Clear();
	}

	/// <summary>
	/// 初始化卡组
	/// 卡组来源：attacker.GetTeamDeck() 或 CardConfigLoader
	/// </summary>
	private void InitializeCards()
	{
		GD.Print("[BattleManager] 开始初始化卡牌...");
		if (player == null)
		{
			GD.PrintErr("[BattleManager] Player为null，无法初始化卡牌!");
			return;
		}

		if (attacker != null)
		{
			player.Deck = attacker.GetTeamDeck();
			GD.Print($"[BattleManager] 使用Attacker卡组，共{player.Deck.Count}张牌");
		}
		else
		{
			CardConfigLoader.LoadCards();
			player.Deck = CardConfigLoader.CreateDeck();
			GD.Print($"[BattleManager] 使用默认卡组，共{player.Deck.Count}张牌");
		}

		foreach (var card in player.Deck)
		{
			GD.Print($"  [Card] {card.Name}, Cost: {card.Cost}, Damage: {card.Damage}");
		}

		player.ShuffleDeck();

		// 刻印回填：卡组每场战斗都重新 new 出来，不回填就等于「上回合刻的印」全丢了
		EngravingManager engravingManager = EngravingManager.Instance;
		if (engravingManager != null)
		{
			int engravedCount = engravingManager.ApplyToDeck(player.Deck);
			GD.Print($"[BattleManager] 刻印回填完成: {engravedCount} 张卡带刻印");
		}

		GD.Print("[BattleManager] 卡牌初始化完成");
	}

	/// <summary>
	/// 战斗开始入口
	/// 初始化怒气统计 → 发送系统消息 → 开始第一回合
	/// </summary>
	/// <summary>
	/// 刷新战斗中的 Buff / Debuff 图标层（角色区下方那排）
	/// </summary>
	private void UpdateBuffDebuffDisplay()
	{
		if (_buffDebuffDisplay == null)
		{
		_buffDebuffDisplay = GetNodeOrNull<BuffDebuffDisplay>("UI/MainArea/BattleZones/PlayerZone/VBoxContainer/BuffDebuffDisplay");
		}
		if (_buffDebuffDisplay == null) return;
		_buffDebuffDisplay.RefreshStatus(player?.StatusEffects);
	}

	/// <summary>
	/// 刷新敌人身上的 Buff / Debuff 图标（敌人血条下方那排）。
	/// </summary>
	private void UpdateEnemyStatusIcons()
	{
		for (int i = 0; i < enemies.Count && i < enemyUIs.Count; i++)
		{
			if (enemyUIs[i] == null || enemies[i] == null) continue;
			enemyUIs[i].UpdateStatusIcons(enemies[i].StatusEffects);
		}
	}

	private void StartBattle()
	{
		GD.Print("[BattleManager] ========== 战斗开始 ==========");
		battleEndRageHandler?.OnBattleStart();
		AddMessage("战斗开始!", LogType.System);
		StartTurn();
	}

	private void StartTurn()
	{
		UpdateBuffDebuffDisplay();

		AddMessage($"第 {turnCount} 回合开始", LogType.System);

		isPlayerTurn = true;
		player.StartTurn();
		player.Shield = 0;

		keyOrderManager.ResetTurnUsage();

		UpdateUI();
		UpdateHandUI();
		UpdateEnemyUI();
		UpdateSilverKeyUI();

		endTurnButton.Text = "结束回合";
		endTurnButton.Disabled = false;
	}

	private void EndTurn()
	{
		if (!isPlayerTurn) return;

		battleEndRageHandler?.OnTurnEnd(turnCount);
		AddMessage("玩家结束了回合", LogType.System);
		endTurnButton.Disabled = true;

		player.DiscardHand();
		isPlayerTurn = false;

		CallDeferred(nameof(EnemyTurn));
	}

	private void EnemyTurn()
	{
		AddMessage("敌人回合", LogType.Enemy);

		foreach (Enemy enemy in enemyManager.GetLivingEnemies())
		{
			enemy.PerformAction(player, enemies);
			AIAction action = enemy.CurrentAction;
			if (action != null)
			{
				if (action.Type == AIActionType.Attack)
				{
					battleEndRageHandler?.OnPlayerDamaged(enemy.Attack);
				}

				string actionDesc = action.Type switch
				{
					AIActionType.Attack => $"攻击了你，造成 {enemy.Attack} 点伤害",
					AIActionType.Defend => $"增加了 {action.Value} 点防御",
					AIActionType.Buff => $"增加了 {action.Value} 点攻击",
					AIActionType.Heal => $"恢复了 {action.Value} 点生命",
					_ => "采取了行动"
				};
				AddMessage($"{enemy.EnemyName} {actionDesc}", LogType.Damage);
			}
		}

		UpdateUI();
		UpdateEnemyUI();
		UpdateBuffDebuffDisplay();
		UpdateEnemyStatusIcons();

		if (CheckGameOver())
		{
			return;
		}

		turnCount++;
		CallDeferred(nameof(StartTurn));
	}

	private bool CheckGameOver()
	{
		if (player.CurrentHealth <= 0)
		{
			battleEndRageHandler?.OnBattleEnd(false, player.CurrentHealth, player.MaxHealth);
			ProcessPostBattleRage(false);
			AddMessage("你输掉了战斗!", LogType.System);
			endTurnButton.Text = "重新开始";
			endTurnButton.Disabled = false;
			endTurnButton.Pressed -= OnEndTurnPressed;
			endTurnButton.Pressed += OnRestartPressed;
			return true;
		}

		if (!enemyManager.HasLivingEnemies())
		{
			battleEndRageHandler?.OnBattleEnd(true, player.CurrentHealth, player.MaxHealth);
			ProcessPostBattleRage(true);

			// 战斗胜利：把剩余血量写回地图存档（战败不写回，战斗会重开，血量保持战前值）
			BattleTransitionManager.Instance?.UpdateSavedPlayerHealth(player.CurrentHealth);

			AddMessage("你赢得了战斗!", LogType.System);
			endTurnButton.Text = "下一关";
			endTurnButton.Disabled = false;
			endTurnButton.Pressed -= OnEndTurnPressed;
			endTurnButton.Pressed += OnNextLevelPressed;
			return true;
		}

		return false;
	}

	private Enemy ResolveCardTarget(Card card)
	{
		return enemyManager.ResolveTarget(card.Target, card.TargetPosition);
	}

	private void ApplyCardEffects(Card card)
	{
		if (card.ShieldGain > 0)
		{
			player.AddShield(card.ShieldGain);
			AddMessage($"你使用了{card.Name}，获得{card.ShieldGain}点护盾", LogType.Shield);
		}

		if (card.EnergyGain > 0)
		{
			player.CurrentEnergy += card.EnergyGain;
			AddMessage($"你使用了{card.Name}，获得{card.EnergyGain}点能量", LogType.Energy);
		}

		if (card.HealAmount > 0)
		{
			player.Heal(card.HealAmount);
			AddMessage($"你使用了{card.Name}，回复{card.HealAmount}点生命", LogType.Heal);
		}

		if (card.DrawCount > 0)
		{
			player.DrawCards(card.DrawCount);
			AddMessage($"你使用了{card.Name}，抽了{card.DrawCount}张牌", LogType.System);
		}

		if (!string.IsNullOrEmpty(card.ApplyBuffName) && enemyManager.HasLivingEnemies())
		{
			StatusEffect buff = card.CreateBuffFromCard();
			if (buff != null)
			{
				Enemy target = ResolveCardTarget(card);
				if (target != null)
				{
					target.AddStatusEffect(buff);
					AddMessage($"你使用了{card.Name}，给{target.EnemyName}附加了增益效果", LogType.System);
				}
			}
		}

		if (!string.IsNullOrEmpty(card.ApplyDebuffName) && enemyManager.HasLivingEnemies())
		{
			StatusEffect debuff = card.CreateDebuffFromCard();
			if (debuff != null)
			{
				Enemy target = ResolveCardTarget(card);
				if (target != null)
				{
					target.AddStatusEffect(debuff);
					AddMessage($"你使用了{card.Name}，给{target.EnemyName}附加了减益效果", LogType.System);
				}
			}
		}
	}

	private void ApplyCardDamage(Card card)
	{
		if (!card.IsAttack || !enemyManager.HasLivingEnemies())
		{
			return;
		}

		if (card.IsAreaAttack)
		{
			foreach (Enemy enemy in enemyManager.GetLivingEnemies())
			{
				enemy.TakeDamage(card.Damage);
				AddMessage($"你使用了{card.Name}，对{enemy.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
			}

			return;
		}

		Enemy target = ResolveCardTarget(card);
		if (target == null)
		{
			if (card.Target == TargetType.Position)
			{
				AddMessage($"你使用了{card.Name}，但{card.TargetPosition}号位置没有存活的敌人", LogType.System);
			}

			return;
		}

		target.TakeDamage(card.Damage);
		string targetDescription = card.Target switch
		{
			TargetType.Front => $"最前的敌人{target.EnemyName}",
			TargetType.Rear => $"最后的敌人{target.EnemyName}",
			_ => target.EnemyName
		};

		AddMessage($"你使用了{card.Name}，对{targetDescription}造成{card.Damage}点伤害", LogType.Damage);
	}

	private void UpdateUI()
	{
		if (playerStatsUI == null)
		{
			playerStatsUI = resourceManager.PlayerStatsScene.Instantiate<PlayerStatsUI>();
			playerStatsContainer.AddChild(playerStatsUI);
		}
		playerStatsUI.Setup(player, turnCount);
		if (playerZoneUI != null)
		{
			playerZoneUI.Setup(player);
		}
		handInfoLabel.Text = $"手牌: {player.Hand.Count} | 抽牌堆: {player.Deck.Count} | 弃牌堆: {player.DiscardPile.Count}";

		UpdateRageDisplay();
	}

	private void UpdateEnemyUI()
	{
		for (int i = 0; i < enemies.Count && i < enemyUIs.Count; i++)
		{
			enemyUIs[i].UpdateHealth();
			Enemy enemy = enemies[i];
			if (!enemy.IsDead && intentDisplay != null)
			{
				AIAction predictedAction = intentDisplay.PredictEnemyAction(enemy, player, enemies);
				if (predictedAction != null)
				{
					enemyUIs[i].UpdateIntent(predictedAction.Type, predictedAction.Value);
				}
			}
		}
	}

	/// <summary>
	/// 更新怒气显示
	/// 同步RageManager的怒气值到UI组件
	/// </summary>
	private void UpdateRageDisplay()
	{
		if (rageDisplayUI == null || characterPositions.Count == 0) return;

		var rageValues = rageManager.GetAllRageValues();
		rageDisplayUI.UpdateAllRage(rageValues);

		for (int i = 0; i < characterPositions.Count; i++)
		{
			if (characterPositions[i] != null)
			{
				bool canUse = characterPositions[i].CanUseUltimate();
				attackerDisplayUI?.UpdateCharacterStatus(i, characterPositions[i].CurrentRage, canUse);
			}
		}
	}

	/// <summary>
	/// 处理战斗后的怒气获取
	/// 算法：BattleEndRageHandler计算 → 分配到各角色 → 更新UI显示
	/// </summary>
	/// <param name="victory">是否胜利</param>
	private void ProcessPostBattleRage(bool victory)
	{
		if (battleEndRageHandler == null || characterPositions.Count == 0) return;

		var rageResults = battleEndRageHandler.CalculateRageGains(victory);
		int totalRageGained = 0;

		for (int i = 0; i < rageResults.Length && i < characterPositions.Count; i++)
		{
			var result = rageResults[i];
			if (characterPositions[i] != null)
			{
				int oldRage = characterPositions[i].CurrentRage;
				characterPositions[i].AddRage(result.TotalRage);
				int actualGain = characterPositions[i].CurrentRage - oldRage;
				totalRageGained += actualGain;

				GD.Print($"[BattleManager] 位置{i}获得{actualGain}怒气 (基础:{result.BaseRage}, 奖励:{result.BonusRage})");

				rageDisplayUI?.ShowRageGainAnimation(i, actualGain);
			}
		}

		AddMessage($"战斗结束，获得{totalRageGained}点怒气!", LogType.System);
		UpdateRageDisplay();

		if (victory && attackerDisplayUI != null)
		{
			attackerDisplayUI.HighlightCharacterWithFullRage();
		}
	}

	private void UpdateHandUI()
	{
		ClearHandUI();

		for (int i = 0; i < player.Hand.Count; i++)
		{
			CreateCardUI(player.Hand[i], i);
		}

		hoveredCardIndex = -1;
	}

	private void ClearHandUI()
	{
		foreach (CardUI ui in cardUIs)
		{
			ui.QueueFree();
		}
		cardUIs.Clear();
	}

	private void CreateCardUI(Card card, int cardIndex)
	{
		CardUI cardUI = resourceManager.CardScene.Instantiate<CardUI>();
		bool canPlay = isPlayerTurn && player.CurrentEnergy >= card.Cost;
		cardUI.Setup(card, cardIndex, canPlay);

		if (resourceManager.CardTextures.TryGetValue(card.Name, out Texture2D texture))
		{
			cardUI.SetTexture(texture);
		}

		int capturedIndex = cardIndex;
		cardUI.SetPlayCallback(() => OnCardPlayed(card, capturedIndex));

		handContainer.AddChild(cardUI);
		cardUIs.Add(cardUI);
		cardUI.ZIndex = cardIndex;

		PlayCardDrawAnimation(cardUI, cardIndex);
	}

	private void PlayCardDrawAnimation(CardUI cardUI, int cardIndex)
	{
		Vector2 startPos = cardLayoutManager.GetCardDrawStartPosition(handContainer);

		cardUI.OffsetLeft = startPos.X;
		cardUI.OffsetTop = startPos.Y;
		cardUI.OffsetRight = startPos.X + cardLayoutManager.CardWidth;
		cardUI.OffsetBottom = startPos.Y + cardLayoutManager.CardHeight;
		cardUI.Scale = new Vector2(0.5f, 0.5f);
		cardUI.Modulate = new Color(1f, 1f, 1f, 0.5f);

		Tween tween = CreateTween();
		tween.SetParallel(true);

		int totalCards = player.Hand.Count;
		var (targetX, targetY) = cardLayoutManager.GetCardDrawTargetPosition(handContainer, cardIndex, totalCards);

		tween.TweenProperty(cardUI, "offset_left", targetX, 0.3f).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(cardUI, "offset_top", targetY, 0.3f).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(cardUI, "scale", Vector2.One, 0.3f).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(cardUI, "modulate:a", 1f, 0.2f);

		tween.Play();
	}

	private void OnCardPlayed(Card card, int cardIndex)
	{
		if (!isPlayerTurn || player.CurrentEnergy < card.Cost)
		{
			return;
		}

		player.CurrentEnergy -= card.Cost;
		AddSilverKey(card.Cost);

		TrackRageFromCard(card);

		ApplyCardEffects(card);
		ApplyCardDamage(card);
		TriggerEngraving(card);

		if (player.Hand.Contains(card))
		{
			player.Hand.Remove(card);
			player.DiscardPile.Add(card);
		}

		UpdateUI();
		UpdateEnemyUI();
		UpdateHandUI();
		UpdateBuffDebuffDisplay();
		UpdateEnemyStatusIcons();

		CheckGameOver();
	}

	/// <summary>
	/// 触发这张卡的刻印效果（tasks.md 1.2.2）。
	/// 没有刻印就什么都不做；触发了就把描述打进战斗日志。
	/// </summary>
	private void TriggerEngraving(Card card)
	{
		EngravingManager engravingManager = EngravingManager.Instance;
		if (engravingManager == null || card == null) return;
		if (!card.IsEngraved) return;

		CharacterPosition owner = ResolveCardOwner(card);
		string description = engravingManager.OnCardPlayed(card, player, enemies.ToArray(), owner, characterPositions);

		if (!string.IsNullOrEmpty(description))
		{
			AddMessage(description, LogType.System);
		}
	}

	/// <summary>
	/// 找出这张卡属于哪个角色（用于「自身怒气」「团队怒气」这类需要出牌者的刻印）。
	/// 找不到就返回 null，刻印里会做 null 保护。
	/// </summary>
	private CharacterPosition ResolveCardOwner(Card card)
	{
		if (card == null || attacker == null) return null;

		CharacterDefinition character = attacker.GetCharacterByCard(card);
		if (character == null) return null;

		for (int i = 0; i < attacker.Characters.Length && i < characterPositions.Count; i++)
		{
			if (attacker.Characters[i] == character)
			{
				return characterPositions[i];
			}
		}

		return null;
	}

	private void AddSilverKey(int energyCost)
	{
		int silverToAdd = (int)(energyCost * silverKeyConfig.SilverPerEnergy);
		currentSilverKey = Mathf.Min(currentSilverKey + silverToAdd, silverKeyConfig.MaxStackSilverKey);
		UpdateSilverKeyUI();
	}

	private void UpdateSilverKeyUI()
	{
		if (silverKeyLabel != null)
		{
			silverKeyLabel.Text = $"银钥: {currentSilverKey}/{silverKeyConfig.BaseMaxSilverKey}";
		}

		if (silverKeyProgressIndicator != null)
		{
			silverKeyProgressIndicator.SetValue(currentSilverKey);
		}

		if (keyOrderButton != null)
		{
			bool canUse = currentSilverKey >= silverKeyConfig.BaseMaxSilverKey &&
						 keyOrderManager.CanUseKeyOrder();
			keyOrderButton.Disabled = !canUse;
		}

		OnSilverKeyChanged?.Invoke(currentSilverKey, silverKeyConfig.BaseMaxSilverKey);
	}

	/// <summary>
	/// 追踪卡牌使用产生的怒气
	/// 规则：攻击卡+15怒气，防御卡+10怒气
	/// </summary>
	/// <param name="card">使用的卡牌</param>
	private void TrackRageFromCard(Card card)
	{
		if (rageManager == null || characterPositions.Count == 0) return;

		CharacterPosition targetPosition = null;

		if (attacker != null && attacker.GetCharacterByCard(card) != null)
		{
			var character = attacker.GetCharacterByCard(card);
			for (int i = 0; i < attacker.Characters.Length; i++)
			{
				if (attacker.Characters[i] == character && i < characterPositions.Count)
				{
					targetPosition = characterPositions[i];
					break;
				}
			}
		}

		if (targetPosition == null && characterPositions.Count > 0)
		{
			targetPosition = characterPositions[0];
		}

		if (card.IsAttack)
		{
			rageManager.OnAttackCardPlayed(targetPosition);
			AddMessage($"获得15点怒气", LogType.System);
		}
		else if (card.IsDefense || card.ShieldGain > 0)
		{
			rageManager.OnDefenseCardPlayed(targetPosition);
			AddMessage($"获得10点怒气", LogType.System);
		}

		UpdateRageDisplay();
	}

	/// <summary>
	/// 钥令按钮点击处理
	/// 逻辑分支（每回合上限 2 次）：
	/// - 银钥不足：显示钥令说明（使用按钮禁用）
	/// - 本回合 0 次用过：显示钥令说明（使用按钮启用）→ 释放「已装备的钥令」
	/// - 本回合 1 次用过：提示 + 随机 3 个钥令三选一
	/// - 本回合 2 次用过（已用满）：拒绝，给出提示
	/// </summary>
	private void OnKeyOrderPressed()
	{
		if (isKeyOrderSelectionOpen || !isPlayerTurn) return;

		KeyOrder equippedOrder = GlobalData.EquippedKeyOrder;

		if (equippedOrder == null)
		{
			AddMessage("未装备钥令!", LogType.System);
			return;
		}

		if (currentSilverKey < silverKeyConfig.BaseMaxSilverKey)
		{
			ShowKeyOrderDescription(false);
			return;
		}

		if (!keyOrderManager.CanUseKeyOrder())
		{
			AddMessage($"本回合钥令已用满（{keyOrderManager.GetUsedCountThisTurn()}/{KeyOrderManager.MaxKeyOrderUsePerTurn} 次）!", LogType.System);
			return;
		}

		if (keyOrderManager.IsFirstUseThisTurn())
		{
			// 第一次用：释放已装备的钥令
			ShowKeyOrderDescription(true);
		}
		else
		{
			// 第二次用：从随机 3 个钥令里挑一个
			ShowPromptAndRandomSelection();
		}
	}

	/// <summary>
	/// 显示钥令说明界面
	/// </summary>
	/// <param name="useButtonEnabled">使用按钮是否启用</param>
	private void ShowKeyOrderDescription(bool useButtonEnabled)
	{
		if (keyOrderDescriptionUI == null || GlobalData.EquippedKeyOrder == null)
		{
			GD.PrintErr("[BattleManager] KeyOrderDescriptionUI或钥令数据未初始化");
			return;
		}

		keyOrderDescriptionUI.ShowKeyOrderDescription(
			GlobalData.EquippedKeyOrder,
			useButtonEnabled,
			(keyOrder) =>
			{
				UseEquippedKeyOrder();
			},
			() =>
			{
				AddMessage("取消了钥令选择", LogType.System);
			}
		);
	}

	/// <summary>
	/// 使用已装备的钥令
	/// </summary>
	private void UseEquippedKeyOrder()
	{
		KeyOrder equippedOrder = GlobalData.EquippedKeyOrder;

		if (equippedOrder == null || currentSilverKey < equippedOrder.SilverKeyCost)
		{
			AddMessage("银钥不足，无法释放钥令!", LogType.System);
			return;
		}

		if (!keyOrderManager.CanUseKeyOrder())
		{
			AddMessage($"本回合钥令已用满（{keyOrderManager.GetUsedCountThisTurn()}/{KeyOrderManager.MaxKeyOrderUsePerTurn} 次）!", LogType.System);
			return;
		}

		currentSilverKey -= equippedOrder.SilverKeyCost;
		// 使用次数由 KeyOrderManager.ApplyKeyOrderEffect 内部记录，这里不要再记一次
		if (!keyOrderManager.ApplyKeyOrderEffect(equippedOrder, player, enemies.ToArray()))
		{
			// 释放失败要把银钥退回去
			currentSilverKey += equippedOrder.SilverKeyCost;
			AddMessage("钥令释放失败!", LogType.System);
			return;
		}
		AddMessage($"你释放了钥令：{equippedOrder.Name}！", LogType.System);

		UpdateSilverKeyUI();
		UpdateUI();
		UpdateEnemyUI();
		UpdateBuffDebuffDisplay();
		UpdateEnemyStatusIcons();

		// 第 1 次用完后仍有余量 → 提示玩家可以再挑一个随机钥令（第 2 次）
		if (keyOrderManager.CanUseKeyOrder() && currentSilverKey >= silverKeyConfig.BaseMaxSilverKey)
		{
			AddMessage("本回合还可以再使用 1 次钥令！", LogType.System);
		}
	}

	/// <summary>
	/// 显示提示和随机钥令选择
	/// </summary>
	private void ShowPromptAndRandomSelection()
	{
		AddMessage("本回合已使用过一次钥令，请选择额外钥令", LogType.System);

		keyOrderDescriptionUI.ShowPrompt(
			"本回合已使用过一次钥令",
			() =>
			{
				ShowRandomKeyOrderSelection();
			},
			() =>
			{
				AddMessage("取消了钥令选择", LogType.System);
			}
		);
	}

	/// <summary>
	/// 显示随机钥令选择界面
	/// 从KeyOrderManager获取3个随机钥令供玩家选择
	/// </summary>
	private void ShowRandomKeyOrderSelection()
	{
		if (keyOrderSelectionUI == null)
		{
			GD.PrintErr("[BattleManager] KeyOrderSelectionUI未初始化");
			return;
		}

		isKeyOrderSelectionOpen = true;
		List<KeyOrder> randomOrders = keyOrderManager.GetRandomKeyOrders(3);

		AddMessage("选择额外的钥令技能", LogType.System);

		keyOrderSelectionUI.ShowSelection(
			randomOrders,
			(selectedOrder) =>
			{
				if (!keyOrderManager.CanUseKeyOrder())
				{
					AddMessage($"本回合钥令已用满（{keyOrderManager.GetUsedCountThisTurn()}/{KeyOrderManager.MaxKeyOrderUsePerTurn} 次）!", LogType.System);
				}
				else if (currentSilverKey >= selectedOrder.SilverKeyCost)
				{
					currentSilverKey -= selectedOrder.SilverKeyCost;
					// 使用次数由 KeyOrderManager.ApplyKeyOrderEffect 内部记录
					if (!keyOrderManager.ApplyKeyOrderEffect(selectedOrder, player, enemies.ToArray()))
					{
						// 释放失败要把银钥退回去
						currentSilverKey += selectedOrder.SilverKeyCost;
						AddMessage("钥令释放失败!", LogType.System);
						isKeyOrderSelectionOpen = false;
						return;
					}
					AddMessage($"你释放了钥令：{selectedOrder.Name}！", LogType.System);
					UpdateSilverKeyUI();
					UpdateUI();
					UpdateEnemyUI();
					UpdateBuffDebuffDisplay();
					UpdateEnemyStatusIcons();
				}
				else
				{
					AddMessage("银钥不足，无法释放!", LogType.System);
				}
				isKeyOrderSelectionOpen = false;
			},
			() =>
			{
				AddMessage("取消了钥令选择", LogType.System);
				isKeyOrderSelectionOpen = false;
			}
		);
	}

	private void AddMessage(string message, LogType logType = LogType.Info)
	{
		if (battleLogWindow != null)
		{
			battleLogWindow.AddLog(message, logType);
		}
	}

	private void OnEndTurnPressed()
	{
		EndTurn();
	}

	private void OnRestartPressed()
	{
		GetTree().ReloadCurrentScene();
	}


	private void OnNextLevelPressed()
	{
		GD.Print("[BattleManager] OnNextLevelPressed called");
		BattleTransitionManager transitionManager = BattleTransitionManager.Instance;

		if (transitionManager == null)
		{
			GD.PrintErr("[BattleManager] BattleTransitionManager 为空，直接回关卡地图");
			GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
			return;
		}

		// 战斗胜利流程：奖励三选一 → 「调查完成」结算界面 → 回地图
		// 奖励先于结算（对照参考图 R-10 / R-11 的「中途选择」语义）
		BattleResultData result = BuildResultData();
		transitionManager.SetPendingResult(result);

		List<BattleRewardChoice> choices = GenerateRewardChoices();
		if (choices.Count > 0)
		{
			transitionManager.ShowBattleReward(choices);
			return;
		}

		// 没有奖励可发（例如背包满且无刻印候选）→ 直接进结算
		transitionManager.ShowBattleResult(result);
	}

	/// <summary>
	/// 生成本场战斗的奖励候选（三选一）。
	/// </summary>
	private List<BattleRewardChoice> GenerateRewardChoices()
	{
		BattleRewardManager rewardManager = BattleRewardManager.Instance;
		if (rewardManager == null)
		{
			GD.PrintErr("[BattleManager] BattleRewardManager 未初始化，本次不发奖励");
			return new List<BattleRewardChoice>();
		}

		return rewardManager.GenerateChoices();
	}

	/// <summary>
	/// 组装战斗结算数据（星级评价 / 唤醒体同调率 / 调查奖励）
	/// </summary>
	private BattleResultData BuildResultData()
	{
		GD.Print("[BattleManager] BuildResultData called");

		BattleResultData data = new BattleResultData();
		data.MapId = GlobalData.CurrentMap?.MapId ?? "未知关卡";
		data.MapName = GlobalData.CurrentMap?.Name ?? "";
		data.TurnCount = turnCount;
		data.StarCount = BattleResultRules.GetStarCount(turnCount);
		data.StarDescription = BattleResultRules.GetStarDescription(turnCount);
		data.SyncEntries = BattleResultRules.BuildSyncEntries(GlobalData.SelectedCharacters);
		data.Rewards = BuildRewards();

		GD.Print($"[BattleManager] BuildResultData completed: map={data.MapId}, turns={data.TurnCount}, stars={data.StarCount}, sync={data.SyncEntries.Count}, rewards={data.Rewards.Count}");
		return data;
	}

	/// <summary>
	/// 组装调查奖励：金币 + 造物（取前 3 个，确定性，不随机）
	/// </summary>
	private List<BattleRewardEntry> BuildRewards()
	{
		List<BattleRewardEntry> rewards = new List<BattleRewardEntry>();

		int gold = MapManager.Instance?.GetMapReward() ?? 0;
		if (gold > 0)
		{
			rewards.Add(new BattleRewardEntry
			{
				RewardName = "金币",
				IconPath = "",
				Amount = gold
			});
		}

		BlackMarkShopManager shop = BlackMarkShopManager.Instance;
		if (shop != null)
		{
			List<ArtifactData> allArtifacts = shop.GetAllArtifacts();
			int count = Mathf.Min(3, allArtifacts.Count);
			for (int i = 0; i < count; i++)
			{
				rewards.Add(new BattleRewardEntry
				{
					RewardName = allArtifacts[i].name,
					IconPath = allArtifacts[i].icon,
					Amount = 1
				});
			}
		}

		GD.Print($"[BattleManager] BuildRewards completed: {rewards.Count} 项");
		return rewards;
	}
	private void OnMapPressed()
	{
		GD.Print("[BattleManager] OnMapPressed called");
		if (TryReturnToHexMap(false))
		{
			return;
		}
		GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
	}

	/// <summary>
	/// 若本次战斗是从六边形地图进入的，则回到地图并恢复状态；否则返回关卡地图
	/// </summary>
	/// <param name="victory">是否胜利</param>
	/// <returns>true 表示已切回六边形地图</returns>
	private bool TryReturnToHexMap(bool victory)
	{
		BattleTransitionManager transitionManager = BattleTransitionManager.Instance;

		if (transitionManager == null || !transitionManager.HasSavedMap)
		{
			GD.Print("[BattleManager] 无地图存档，返回关卡地图");
			transitionManager?.ClearSavedState();
			return false;
		}

		GD.Print($"[BattleManager] 返回六边形地图 (胜利: {victory})");
		transitionManager.EndBattle(victory);
		return true;
	}
    }
}

