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
    private Panel playerPanel;
    private Button mapButton;

    private List<EnemyUI> enemyUIs = new List<EnemyUI>();
    private List<CardUI> cardUIs = new List<CardUI>();
    private PlayerStatsUI playerStatsUI;

    private Dictionary<string, Texture2D> cardTextures = new Dictionary<string, Texture2D>();
    private Texture2D enemyTexture;

    private PackedScene cardScene;
    private PackedScene enemyScene;
    private PackedScene playerStatsScene;
    private PackedScene battleLogScene;

    private BattleLogWindow battleLogWindow;
    private EnemyIntentDisplay intentDisplay;

    private int hoveredCardIndex = -1;

    private const float CARD_WIDTH = 140f;
    private const float CARD_HEIGHT = 200f;
    private const float OVERLAP_OFFSET = 80f;
    private const float HOVER_LIFT = 50f;

    public override void _Ready()
    {
        LoadScenes();
        LoadResources();
        InitializeUI();
        InitializePlayer();
        InitializeEnemies();
        InitializeCards();
        StartBattle();
    }

    public override void _Process(double delta)
    {
        CheckCardHover();
        UpdateCardPositions();
    }

    private void LoadScenes()
    {
        cardScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Card.tscn");
        enemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Enemy.tscn");
        playerStatsScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/PlayerStats.tscn");
        battleLogScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/BattleLogWindow.tscn");
    }

    private void LoadResources()
    {
        cardTextures["打击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/strike.png");
        cardTextures["铁壁"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/defend.png");
        cardTextures["猛击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/bash.png");
        cardTextures["背上疯狂"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/whirlwind.png");
        cardTextures["双重打击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/twin_strike.png");
        enemyTexture = ResourceLoader.Load<Texture2D>("res://Assets/Icons/enemy_elite.svg");
    }

    private void InitializeUI()
    {
        handContainer = GetNode<HBoxContainer>("UI/HandArea/HandContainer");
        enemyContainer = GetNode<HBoxContainer>("UI/MainArea/BattleArea/EnemyContainer");
        playerStatsContainer = GetNode<HBoxContainer>("UI/TopBar/PlayerStats");
        endTurnButton = GetNode<Button>("UI/BottomBar/EndTurnButton");
        handInfoLabel = GetNode<Label>("UI/BottomBar/HandInfo");
        playerPanel = GetNode<Panel>("UI/MainArea/PlayerPanel");
        mapButton = GetNode<Button>("UI/BottomBar/MapButton");

        endTurnButton.Pressed += OnEndTurnPressed;
        mapButton.Pressed += OnMapPressed;

        InitializeBattleLogWindow();
    }

    private void InitializeBattleLogWindow()
    {
        battleLogWindow = battleLogScene.Instantiate<BattleLogWindow>();
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
        EnemyUI enemyUI = enemyScene.Instantiate<EnemyUI>();
        enemyUI.Setup(enemy);
        enemyUI.SetSprite(enemyTexture);
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

        UpdateUI();
        UpdateHandUI();
        UpdateEnemyUI();

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

        foreach (Enemy enemy in GetLivingEnemies())
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

        if (!HasLivingEnemies())
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

    private List<Enemy> GetLivingEnemies()
    {
        List<Enemy> livingEnemies = new List<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                livingEnemies.Add(enemy);
            }
        }

        return livingEnemies;
    }

    private bool HasLivingEnemies()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                return true;
            }
        }

        return false;
    }

    private Enemy GetFrontmostEnemy()
    {
        Enemy frontmost = null;
        int minPosition = int.MaxValue;

        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position < minPosition)
            {
                minPosition = enemy.Position;
                frontmost = enemy;
            }
        }

        return frontmost;
    }

    private Enemy GetRearmostEnemy()
    {
        Enemy rearMost = null;
        int maxPosition = int.MinValue;

        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position > maxPosition)
            {
                maxPosition = enemy.Position;
                rearMost = enemy;
            }
        }

        return rearMost;
    }

    private Enemy GetEnemyAtPosition(int position)
    {
        foreach (Enemy enemy in GetLivingEnemies())
        {
            if (enemy.Position == position)
            {
                return enemy;
            }
        }

        return null;
    }

    private Enemy ResolveCardTarget(Card card)
    {
        return card.Target switch
        {
            TargetType.Front => GetFrontmostEnemy(),
            TargetType.Rear => GetRearmostEnemy(),
            TargetType.Position => GetEnemyAtPosition(card.TargetPosition),
            _ => GetFrontmostEnemy()
        };
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

        if (!string.IsNullOrEmpty(card.ApplyBuffName) && HasLivingEnemies())
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

        if (!string.IsNullOrEmpty(card.ApplyDebuffName) && HasLivingEnemies())
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
        if (!card.IsAttack || !HasLivingEnemies())
        {
            return;
        }

        if (card.IsAreaAttack)
        {
            foreach (Enemy enemy in GetLivingEnemies())
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
            playerStatsUI = playerStatsScene.Instantiate<PlayerStatsUI>();
            playerStatsContainer.AddChild(playerStatsUI);
        }
        playerStatsUI.Setup(player, turnCount);
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
        CardUI cardUI = cardScene.Instantiate<CardUI>();
        bool canPlay = isPlayerTurn && player.CurrentEnergy >= card.Cost;
        cardUI.Setup(card, cardIndex, canPlay);

        if (cardTextures.TryGetValue(card.Name, out Texture2D texture))
        {
            cardUI.SetTexture(texture);
        }

        int capturedIndex = cardIndex;
        cardUI.SetPlayCallback(() => OnCardPlayed(card, capturedIndex));

        handContainer.AddChild(cardUI);
        cardUIs.Add(cardUI);
        cardUI.ZIndex = cardIndex;
    }

    private void CheckCardHover()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        int newHoveredIndex = -1;

        if (cardUIs.Count == 0) return;

        int totalCards = cardUIs.Count;
        float handWidth = OVERLAP_OFFSET * (totalCards - 1) + CARD_WIDTH;
        float startX = (handContainer.Size.X - handWidth) / 2f;
        float baseY = handContainer.Size.Y - CARD_HEIGHT - 10;

        Vector2 containerGlobalPos = handContainer.GlobalPosition;

        for (int i = 0; i < cardUIs.Count; i++)
        {
            float x = startX + i * OVERLAP_OFFSET;
            float y = baseY;

            Rect2 cardRect = new Rect2(
                containerGlobalPos.X + x,
                containerGlobalPos.Y + y,
                CARD_WIDTH,
                CARD_HEIGHT
            );

            if (cardRect.HasPoint(mousePos))
            {
                newHoveredIndex = i;
                break;
            }
        }

        if (newHoveredIndex != hoveredCardIndex)
        {
            hoveredCardIndex = newHoveredIndex;
        }
    }

    private void UpdateCardPositions()
    {
        if (cardUIs.Count == 0) return;

        int totalCards = cardUIs.Count;
        float handWidth = OVERLAP_OFFSET * (totalCards - 1) + CARD_WIDTH;
        float startX = (handContainer.Size.X - handWidth) / 2f;
        float baseY = handContainer.Size.Y - CARD_HEIGHT - 10;

        for (int i = 0; i < cardUIs.Count; i++)
        {
            CardUI cardUI = cardUIs[i];

            float x = startX + i * OVERLAP_OFFSET;
            float y = baseY;

            if (i == hoveredCardIndex)
            {
                y -= HOVER_LIFT;
            }

            cardUI.OffsetLeft = x;
            cardUI.OffsetTop = y;
            cardUI.OffsetRight = x + CARD_WIDTH;
            cardUI.OffsetBottom = y + CARD_HEIGHT;

            int zIndex = i;
            if (i == hoveredCardIndex)
            {
                zIndex = totalCards + 10;
            }
            cardUI.ZIndex = zIndex;
        }
    }

    private void OnCardPlayed(Card card, int cardIndex)
    {
        if (!isPlayerTurn || player.CurrentEnergy < card.Cost)
        {
            return;
        }

        player.CurrentEnergy -= card.Cost;
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
