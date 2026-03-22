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

        AddEnemy("守卫 A", 54, 8, 2);
        AddEnemy("精锐 B", 50, 10, 1);
        AddEnemy("背信 C", 38, 6, 0);
    }

    private void AddEnemy(string name, int maxHealth, int attack, int position)
    {
        Enemy enemy = new Enemy
        {
            EnemyName = name,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth,
            Attack = attack,
            Position = position
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

        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                enemy.AttackPlayer(player);
                AddMessage($"{enemy.EnemyName} 攻击了你，造成 {enemy.Attack} 点伤害", LogType.Damage);
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

        bool allEnemiesDead = true;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                allEnemiesDead = false;
                break;
            }
        }

        if (allEnemiesDead)
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

    private Enemy GetFrontmostEnemy()
    {
        Enemy frontmost = null;
        int minPosition = int.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0 && enemy.Position < minPosition)
            {
                minPosition = enemy.Position;
                frontmost = enemy;
            }
        }

        return frontmost ?? enemies[0];
    }

    private Enemy GetRearmostEnemy()
    {
        Enemy rearMost = null;
        int maxPosition = -1;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.CurrentHealth > 0 && enemy.Position > maxPosition)
            {
                maxPosition = enemy.Position;
                rearMost = enemy;
            }
        }

        return rearMost ?? enemies[0];
    }

    private Enemy GetEnemyAtPosition(int position)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Position == position && enemy.CurrentHealth > 0)
            {
                return enemy;
            }
        }
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Position == position)
            {
                return enemy;
            }
        }
        return null;
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
        if (!isPlayerTurn || player.CurrentEnergy < card.Cost) return;

        player.CurrentEnergy -= card.Cost;

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

        if (card.IsAttack && enemies.Count > 0)
        {
            if (card.IsAreaAttack)
            {
                foreach (Enemy enemy in enemies)
                {
                    if (enemy.CurrentHealth > 0)
                    {
                        enemy.TakeDamage(card.Damage);
                        AddMessage($"你使用了{card.Name}，对{enemy.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
                    }
                }
            }
            else if (card.Target == TargetType.Front)
            {
                Enemy target = GetFrontmostEnemy();
                target.TakeDamage(card.Damage);
                AddMessage($"你使用了{card.Name}，对最前的敌人{target.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
            }
            else if (card.Target == TargetType.Rear)
            {
                Enemy target = GetRearmostEnemy();
                target.TakeDamage(card.Damage);
                AddMessage($"你使用了{card.Name}，对最后的敌人{target.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
            }
            else if (card.Target == TargetType.Position)
            {
                Enemy target = GetEnemyAtPosition(card.TargetPosition);
                if (target != null && target.CurrentHealth > 0)
                {
                    target.TakeDamage(card.Damage);
                    AddMessage($"你使用了{card.Name}，对{target.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
                }
                else
                {
                    AddMessage($"你使用了{card.Name}，但{card.TargetPosition}号位置没有敌人", LogType.System);
                }
            }
            else
            {
                Enemy target = GetFrontmostEnemy();
                target.TakeDamage(card.Damage);
                AddMessage($"你使用了{card.Name}，对{target.EnemyName}造成{card.Damage}点伤害", LogType.Damage);
            }
        }

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
