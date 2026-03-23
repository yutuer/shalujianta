using Godot;
using System.Collections.Generic;

public partial class BattleManager : Node2D
{
    private Player player;
    private List<Enemy> enemies = new List<Enemy>();
    private int turnCount = 1;
    private bool isPlayerTurn = true;

    private HBoxContainer handContainer;
    private HBoxContainer enemyContainer;
    private HBoxContainer playerStatsContainer;
    private Button endTurnButton;
    private Label handInfoLabel;
    private Button mapButton;
    private Label silverKeyLabel;
    private Button keyOrderButton;

    private List<EnemyUI> enemyUIs = new List<EnemyUI>();
    private List<CardUI> cardUIs = new List<CardUI>();
    private PlayerStatsUI playerStatsUI;
    private PlayerZoneUI playerZoneUI;

    private BattleLogWindow battleLogWindow;
    private EnemyIntentDisplay intentDisplay;
    private CardLayoutManager cardLayoutManager;
    private EnemyManager enemyManager;
    private ResourceManager resourceManager;

    private SilverKeyConfig silverKeyConfig;
    private KeyOrderManager keyOrderManager;
    private int currentSilverKey = 0;
    private bool isKeyOrderSelectionOpen = false;

    private int hoveredCardIndex = -1;

    public override void _Ready()
    {
        resourceManager = new ResourceManager();
        resourceManager.LoadAll();

        enemyManager = new EnemyManager();

        silverKeyConfig = SilverKeyConfig.CreateDefault();
        keyOrderManager = new KeyOrderManager();
        AddChild(keyOrderManager);

        InitializeUI();
        InitializePlayer();
        InitializeEnemies();
        InitializeCards();
        StartBattle();
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

    private void InitializeUI()
    {
        handContainer = GetNode<HBoxContainer>("UI/HandArea/HandContainer");
        enemyContainer = GetNode<HBoxContainer>("UI/MainArea/BattleZones/EnemyZone/EnemyContainer");
        playerStatsContainer = GetNode<HBoxContainer>("UI/TopBar/PlayerStats");
        endTurnButton = GetNode<Button>("UI/BottomBar/EndTurnButton");
        handInfoLabel = GetNode<Label>("UI/BottomBar/HandInfo");
        mapButton = GetNode<Button>("UI/BottomBar/MapButton");
        silverKeyLabel = GetNode<Label>("UI/TopBar/SilverKeyLabel");
        keyOrderButton = GetNode<Button>("UI/BottomBar/KeyOrderButton");

        Control playerZone = GetNode<Control>("UI/MainArea/BattleZones/PlayerZone");
        playerZoneUI = new PlayerZoneUI();
        playerZone.AddChild(playerZoneUI);

        cardLayoutManager = new CardLayoutManager();

        endTurnButton.Pressed += OnEndTurnPressed;
        mapButton.Pressed += OnMapPressed;
        keyOrderButton.Pressed += OnKeyOrderPressed;

        keyOrderButton.Disabled = true;

        InitializeBattleLogWindow();
    }

    private void InitializeBattleLogWindow()
    {
        battleLogWindow = resourceManager.BattleLogScene.Instantiate<BattleLogWindow>();
        GetNode<CanvasLayer>("UI").AddChild(battleLogWindow);
        intentDisplay = new EnemyIntentDisplay();
    }

    private void InitializePlayer()
    {
        player = new Player();
        AddChild(player);
        player.MaxHealth = 100;
        player.CurrentHealth = 100;
        player.MaxEnergy = 3;
        player.DrawCount = 5;
        player.MaxHandSize = 10;
    }

    private void InitializeEnemies()
    {
        ClearEnemyUI();
        enemies.Clear();

        AddEnemy("守卫 A", 54, 8, 2, "balanced");
        AddEnemy("精锐 B", 50, 10, 1, "aggressive");
        AddEnemy("背信 C", 38, 6, 0, "defensive");

        enemyManager.SetEnemies(enemies);
    }

    private void AddEnemy(string name, int maxHealth, int attack, int position, string behavior = "aggressive")
    {
        Enemy enemy = new Enemy
        {
            EnemyName = name,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth,
            Attack = attack,
            Position = position,
            Behavior = behavior
        };
        enemies.Add(enemy);
        CreateEnemyUI(enemy);
    }

    private void CreateEnemyUI(Enemy enemy)
    {
        EnemyUI enemyUI = resourceManager.EnemyScene.Instantiate<EnemyUI>();
        enemyUI.Setup(enemy);
        enemyUI.SetSprite(resourceManager.EnemyTexture);
        enemyContainer.AddChild(enemyUI);
        enemyUIs.Add(enemyUI);
    }

    private void ClearEnemyUI()
    {
        foreach (EnemyUI ui in enemyUIs)
        {
            ui.QueueFree();
        }
        enemyUIs.Clear();
    }

    private void InitializeCards()
    {
        CardConfigLoader.LoadCards();
        player.Deck = CardConfigLoader.CreateDeck();
        player.ShuffleDeck();
    }

    private void StartBattle()
    {
        AddMessage("战斗开始!", LogType.System);
        StartTurn();
    }

    private void StartTurn()
    {
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
            AddMessage("你输掉了战斗!", LogType.System);
            endTurnButton.Text = "重新开始";
            endTurnButton.Disabled = false;
            endTurnButton.Pressed -= OnEndTurnPressed;
            endTurnButton.Pressed += OnRestartPressed;
            return true;
        }

        if (!enemyManager.HasLivingEnemies())
        {
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
                int actualDamage = enemy.TakeDamage(card.Damage);
                AddMessage($"你使用了{card.Name}，对{enemy.EnemyName}造成{actualDamage}点伤害", LogType.Damage);
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

        int damage = target.TakeDamage(card.Damage);
        string targetDescription = card.Target switch
        {
            TargetType.Front => $"最前的敌人{target.EnemyName}",
            TargetType.Rear => $"最后的敌人{target.EnemyName}",
            _ => target.EnemyName
        };

        AddMessage($"你使用了{card.Name}，对{targetDescription}造成{damage}点伤害", LogType.Damage);
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
    }

    private void UpdateEnemyUI()
    {
        for (int i = 0; i < enemies.Count && i < enemyUIs.Count; i++)
        {
            enemyUIs[i].UpdateHealth();
            Enemy enemy = enemies[i];
            if (!enemy.IsDead() && intentDisplay != null)
            {
                AIAction predictedAction = intentDisplay.PredictEnemyAction(enemy, player, enemies);
                if (predictedAction != null)
                {
                    enemyUIs[i].UpdateIntent(predictedAction.Type, predictedAction.Value);
                }
            }
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
        ApplyCardEffects(card);
        ApplyCardDamage(card);

        if (player.Hand.Contains(card))
        {
            player.Hand.Remove(card);
            player.DiscardPile.Add(card);
        }

        UpdateUI();
        UpdateEnemyUI();
        UpdateHandUI();

        CheckGameOver();
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

        if (keyOrderButton != null)
        {
            bool canUse = currentSilverKey >= silverKeyConfig.BaseMaxSilverKey &&
                         keyOrderManager.CanUseKeyOrder();
            keyOrderButton.Disabled = !canUse;
        }
    }

    private void OnKeyOrderPressed()
    {
        if (isKeyOrderSelectionOpen) return;

        KeyOrder equippedOrder = keyOrderManager.GetEquippedKeyOrder();

        if (equippedOrder != null && currentSilverKey >= silverKeyConfig.BaseMaxSilverKey)
        {
            currentSilverKey -= equippedOrder.SilverKeyCost;
            keyOrderManager.ApplyKeyOrderEffect(equippedOrder, player, enemies.ToArray());
            AddMessage($"你释放了钥令：{equippedOrder.Name}！", LogType.System);

            if (currentSilverKey >= silverKeyConfig.BaseMaxSilverKey &&
                keyOrderManager.CanUseKeyOrder())
            {
                ShowRandomKeyOrderSelection();
            }

            UpdateSilverKeyUI();
            UpdateUI();
            UpdateEnemyUI();
        }
    }

    private void ShowRandomKeyOrderSelection()
    {
        isKeyOrderSelectionOpen = true;

        List<KeyOrder> randomOrders = keyOrderManager.GetRandomKeyOrders(3);

        AddMessage("选择额外的钥令技能", LogType.System);

        isKeyOrderSelectionOpen = false;
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
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }

    private void OnMapPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }
}
