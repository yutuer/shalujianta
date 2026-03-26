using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.HexMap;
using FishEatFish.Shop;

namespace FishEatFish.UI.HexMap
{
    public partial class HexMapUI : Control
    {
        [Export]
        private PackedScene _tileViewScene;

        [Export]
        private PackedScene _shopItemCardScene;

        [Export]
        private PackedScene _engravingCardSlotScene;

        private HexMapController _controller;

        private Control _mapContainer;
        private Control _tileViewsContainer;
        private PlayerIcon _playerIcon;
        private HealthBar _healthBar;
        private HBoxContainer _rageCirclesContainer;

        private Button _skipButton;
        private Button _deathResistanceButton;
        private Button _blackMarkButton;
        private Button _settingsButton;

        private Label _blackMarkLabel;

        private Control _teleportDialog;
        private Button _teleportConfirmButton;
        private Button _teleportCancelButton;

        private Control _shopContainer;
        private Control _shopItemsContainer;
        private Button _shopCloseButton;

        private Control _engravingSelectContainer;
        private Control _engravingCardsContainer;
        private Button _engravingConfirmButton;
        private Button _engravingCancelButton;

        private Control _failurePanel;
        private Label _failureMessage;
        private Button _failureOkButton;

        private Control _backpackContainer;
        private Control _backpackItemsContainer;

        private Dictionary<HexCoord, HexTileView> _tileViews = new Dictionary<HexCoord, HexTileView>();
        private List<RageCircle> _rageCircles = new List<RageCircle>();

        private Vector2 _hexSize = new Vector2(180, 156);

        public override void _Ready()
        {
            GD.Print("[HexMapUI] _Ready called");

            if (_tileViewScene == null)
            {
                _tileViewScene = GD.Load<PackedScene>("res://Scenes/UI/HexTileView.tscn");
            }
            if (_shopItemCardScene == null)
            {
                _shopItemCardScene = GD.Load<PackedScene>("res://Scenes/UI/ShopItemCard.tscn");
            }
            if (_engravingCardSlotScene == null)
            {
                _engravingCardSlotScene = GD.Load<PackedScene>("res://Scenes/UI/EngravingCardSlot.tscn");
            }

            if (_tileViewScene == null)
            {
                GD.PrintErr("[HexMapUI] HexTileView scene not found!");
            }
            else
            {
                GD.Print("[HexMapUI] HexTileView scene loaded");
            }

            InitializeComponents();
            GD.Print("[HexMapUI] Components initialized");
            SetupEventConnections();
            GD.Print("[HexMapUI] Events connected");

            _controller = HexMapController.Instance;
            GD.Print($"[HexMapUI] Controller Instance: {_controller}");
            if (_controller != null)
            {
                GD.Print($"[HexMapUI] Controller found, refreshing map...");
                RefreshMap();
            }
            else
            {
                GD.PrintErr("[HexMapUI] Controller is null!");
            }
            GD.Print("[HexMapUI] _Ready complete");
        }

        private void InitializeComponents()
        {
            GD.Print("[HexMapUI] InitializeComponents started");

            var viewportSize = GetViewportRect().Size;
            GD.Print($"[HexMapUI] Viewport size: {viewportSize}, screenCenter: {viewportSize / 2}");

            GD.Print($"[HexMapUI] Has HealthBar: {HasNode("HealthBar")}");
            GD.Print($"[HexMapUI] Has MapContainer: {HasNode("MapContainer")}");
            GD.Print($"[HexMapUI] Has RageCircles: {HasNode("RageCircles")}");

            _mapContainer = GetNodeOrNull<Control>("MapContainer");
            GD.Print($"[HexMapUI] _mapContainer: {_mapContainer}");

            _tileViewsContainer = GetNodeOrNull<Control>("MapContainer/TileViews");
            if (_tileViewsContainer != null)
            {
                _tileViewsContainer.Position = Vector2.Zero;
                GD.Print($"[HexMapUI] TileViewsContainer position reset to: (0, 0)");
            }
            GD.Print($"[HexMapUI] _tileViewsContainer: {_tileViewsContainer}, position: {_tileViewsContainer?.Position}");

            var playerIconControl = GetNodeOrNull<Control>("MapContainer/TileViews/PlayerIcon");
            GD.Print($"[HexMapUI] Get PlayerIcon as Control: {playerIconControl}");
            if (playerIconControl != null)
            {
                GD.Print($"[HexMapUI] PlayerIcon Script: {playerIconControl.GetScript()}");
                GD.Print($"[HexMapUI] PlayerIcon HasScript: {playerIconControl.HasMethod("_Ready")}");
            }
            _playerIcon = playerIconControl as PlayerIcon;
            if (_playerIcon == null && playerIconControl != null)
            {
                GD.PrintErr($"[HexMapUI] PlayerIcon control found but cast failed! Type: {playerIconControl.GetType()}");
            }
            else if (_playerIcon != null)
            {
                GD.Print($"[HexMapUI] _playerIcon found: {_playerIcon}, initial Position: {_playerIcon.Position}, Size: {_playerIcon.Size}");
            }
            else
            {
                GD.PrintErr("[HexMapUI] PlayerIcon not found!");
            }

            var healthBarControl = GetNodeOrNull<Control>("HealthBar");
            GD.Print($"[HexMapUI] Get HealthBar as Control: {healthBarControl}");
            if (healthBarControl != null)
            {
                GD.Print($"[HexMapUI] HealthBar Script: {healthBarControl.GetScript()}");
                GD.Print($"[HexMapUI] HealthBar HasScript: {healthBarControl.HasMethod("_Ready")}");
            }
            _healthBar = healthBarControl as HealthBar;
            if (_healthBar == null && healthBarControl != null)
            {
                GD.PrintErr($"[HexMapUI] HealthBar control found but cast failed! Type: {healthBarControl.GetType()}");
            }
            else if (_healthBar != null)
            {
                GD.Print($"[HexMapUI] _healthBar found: {_healthBar}, Size: {_healthBar.CustomMinimumSize}");
            }
            else
            {
                GD.PrintErr("[HexMapUI] HealthBar not found!");
            }

            if (_healthBar != null)
            {
                PositionHealthBar();
            }

            _rageCirclesContainer = GetNodeOrNull<HBoxContainer>("RageCircles");
            GD.Print($"[HexMapUI] RageCircles container: {_rageCirclesContainer}, Visible: {_rageCirclesContainer?.Visible}, GlobalPos: {_rageCirclesContainer?.GlobalPosition}, Size: {_rageCirclesContainer?.Size}");
            _rageCircles.Clear();
            for (int i = 0; i < 4; i++)
            {
                var rageCircleAsControl = GetNodeOrNull<Control>($"RageCircles/RageCircle{i}");
                GD.Print($"[HexMapUI] RageCircle{i} as Control: {rageCircleAsControl}, Script: {rageCircleAsControl?.GetScript()}");
                var rageCircle = rageCircleAsControl as RageCircle;
                if (rageCircle != null)
                {
                    GD.Print($"[HexMapUI] RageCircle{i} cast SUCCESS: {rageCircle}, Visible: {rageCircle.Visible}, Size: {rageCircle.Size}, MinSize: {rageCircle.CustomMinimumSize}, GlobalPos: {rageCircle.GlobalPosition}, Scale: {rageCircle.Scale}");
                    rageCircle.SetCharacterName($"角色{i + 1}");
                    rageCircle.SetRage(0, 100);
                    _rageCircles.Add(rageCircle);
                }
                else
                {
                    GD.PrintErr($"[HexMapUI] RageCircle{i} cast FAILED! Is Control: {rageCircleAsControl != null}, Type: {rageCircleAsControl?.GetType()}");
                }
            }
            if (_rageCirclesContainer != null)
            {
                PositionRageCircles();
                GD.Print($"[HexMapUI] After PositionRageCircles - Container GlobalPos: {_rageCirclesContainer.GlobalPosition}, Size: {_rageCirclesContainer.Size}");
            }
            GD.Print($"[HexMapUI] _rageCircles: {_rageCircles.Count} circles");

            ConnectButton("TopRightButtons/SkipButton", OnSkipButtonPressed);
            ConnectButton("TopRightButtons/DeathResistanceButton", OnDeathResistanceButtonPressed);
            ConnectButton("TopRightButtons/BlackMarkButton", OnBlackMarkButtonPressed);
            ConnectButton("TopRightButtons/SettingsButton", OnSettingsButtonPressed);
            if (HasNode("TopRightButtons"))
            {
                PositionTopRightButtons();
            }
            GD.Print("[HexMapUI] TopRightButtons connected");

            _blackMarkLabel = GetNodeOrNull<Label>("BlackMarkLabel");
            GD.Print($"[HexMapUI] _blackMarkLabel: {_blackMarkLabel}");

            _teleportDialog = GetNodeOrNull<Control>("TeleportDialog");
            if (_teleportDialog != null)
            {
                ConnectButton("TeleportDialog/VBoxContainer/ButtonBox/CancelButton", OnTeleportCancelPressed);
                ConnectButton("TeleportDialog/VBoxContainer/ButtonBox/ConfirmButton", OnTeleportConfirmPressed);
            }
            GD.Print($"[HexMapUI] TeleportDialog: {_teleportDialog}");

            _shopContainer = GetNodeOrNull<Control>("ShopContainer");
            if (_shopContainer != null)
            {
                _shopItemsContainer = GetNodeOrNull<Control>("ShopContainer/VBoxContainer/ShopItems");
                ConnectButton("ShopContainer/VBoxContainer/HeaderBox/CloseButton", OnShopClosePressed);
            }
            GD.Print($"[HexMapUI] ShopContainer: {_shopContainer}");

            _engravingSelectContainer = GetNodeOrNull<Control>("EngravingSelectContainer");
            if (_engravingSelectContainer != null)
            {
                _engravingCardsContainer = GetNodeOrNull<Control>("EngravingSelectContainer/VBoxContainer/EngravingCards");
                ConnectButton("EngravingSelectContainer/VBoxContainer/ButtonBox/CancelButton", OnEngravingCancelPressed);
                ConnectButton("EngravingSelectContainer/VBoxContainer/ButtonBox/ConfirmButton", OnEngravingConfirmPressed);
                ConnectButton("EngravingSelectContainer/VBoxContainer/HeaderBox/CloseButton", OnEngravingClosePressed);
            }
            GD.Print($"[HexMapUI] EngravingSelectContainer: {_engravingSelectContainer}");

            _failurePanel = GetNodeOrNull<Control>("FailurePanel");
            if (_failurePanel != null)
            {
                _failureMessage = GetNodeOrNull<Label>("FailurePanel/VBoxContainer/FailureMessage");
                ConnectButton("FailurePanel/VBoxContainer/OkButton", OnFailureOkPressed);
            }
            GD.Print($"[HexMapUI] FailurePanel: {_failurePanel}");

            _backpackContainer = GetNodeOrNull<Control>("BackpackContainer");
            if (_backpackContainer != null)
            {
                _backpackItemsContainer = GetNodeOrNull<Control>("BackpackContainer/HBoxContainer/BackpackItems");
            }
            GD.Print($"[HexMapUI] BackpackContainer: {_backpackContainer}");

            GD.Print("[HexMapUI] InitializeComponents completed!");
        }

        private void ConnectButton(string path, Action handler)
        {
            var button = GetNodeOrNull<Button>(path);
            if (button != null)
            {
                button.Pressed += handler;
            }
            else
            {
                GD.PrintErr($"[HexMapUI] Button not found: {path}");
            }
        }

        private void PositionHealthBar()
        {
            if (_healthBar == null) return;
            var screenSize = GetViewportRect().Size;
            var targetPos = new Vector2(20, screenSize.Y - _healthBar.CustomMinimumSize.Y - 20);
            GD.Print($"[HexMapUI] PositionHealthBar: screenSize={screenSize}, customSize={_healthBar.CustomMinimumSize}, targetPos={targetPos}");
            _healthBar.GlobalPosition = targetPos;
            GD.Print($"[HexMapUI] HealthBar GlobalPosition after set: {_healthBar.GlobalPosition}");
        }

        private void PositionRageCircles()
        {
            if (_rageCirclesContainer == null) return;
            GD.Print($"[HexMapUI] PositionRageCircles: using anchor positioning (no code position needed)");
        }

        public override void _Process(double delta)
        {
            if (_rageCirclesContainer != null && Input.IsKeyPressed(Key.D))
            {
                GD.Print($"[DEBUG] RageCircles: Visible={_rageCirclesContainer.Visible}, GlobalPos={_rageCirclesContainer.GlobalPosition}, Size={_rageCirclesContainer.Size}");
                foreach (var circle in _rageCircles)
                {
                    GD.Print($"[DEBUG] Circle: Name={circle.Name}, Visible={circle.Visible}, GlobalPos={circle.GlobalPosition}, Size={circle.Size}, CustomMin={circle.CustomMinimumSize}");
                }
            }
        }

        private void PositionTopRightButtons()
        {
            var topRightContainer = GetNodeOrNull<HBoxContainer>("TopRightButtons");
            if (topRightContainer == null) return;
            var screenSize = GetViewportRect().Size;
            float buttonContainerWidth = 100 * 4 + 10 * 3 + 40;
            topRightContainer.GlobalPosition = new Vector2(screenSize.X - buttonContainerWidth - 20, 20);
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            GetTree().Connect("screen_resized", new Callable(this, nameof(OnScreenResized)));
        }

        private void OnScreenResized()
        {
            PositionHealthBar();
            PositionRageCircles();
            PositionTopRightButtons();
        }

        private void SetupEventConnections()
        {
            if (_controller != null)
            {
                _controller.OnPlayerMoved += OnPlayerMoved;
                _controller.OnTileTriggered += OnTileTriggered;
                _controller.OnTeleportTriggered += OnTeleportTriggered;
                _controller.OnHealthChanged += OnHealthChanged;
                _controller.OnBlackMarkChanged += OnBlackMarkChanged;
                _controller.OnMapCompleted += OnMapCompleted;
                _controller.OnChallengeFailed += OnChallengeFailed;
                _controller.OnShopOpened += OnShopOpened;
                _controller.OnShopClosed += OnShopClosed;
            }

            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.OnBlackMarkChanged += UpdateBlackMarkDisplay;
            }
        }

        private void RefreshMap()
        {
            GD.Print("[HexMapUI] RefreshMap started");
            ClearTileViews();

            if (_controller?.CurrentMap == null)
            {
                GD.PrintErr("[HexMapUI] CurrentMap is null!");
                return;
            }

            GD.Print($"[HexMapUI] Creating tile views...");
            int tileCount = 0;
            foreach (var tile in _controller.CurrentMap.GetAllTiles())
            {
                CreateTileView(tile);
                tileCount++;
            }
            GD.Print($"[HexMapUI] Created {tileCount} tile views, tileViews count={_tileViews.Count}");

            if (_playerIcon != null)
            {
                _tileViewsContainer.MoveChild(_playerIcon, -1);
            }

            CenterOnPlayer(_controller.CurrentPosition, false);
            UpdatePlayerPosition();
            GD.Print("[HexMapUI] RefreshMap completed");
        }

        private void CenterOnPlayer(HexCoord playerCoord, bool animate = true)
        {
            if (_tileViewsContainer == null) return;

            var screenCenter = GetViewportRect().Size / 2;
            var playerWorldPos = HexToWorld(playerCoord) + _hexSize / 2;
            var targetOffset = screenCenter - playerWorldPos;

            GD.Print($"[HexMapUI] CenterOnPlayer: playerCoord={playerCoord}, playerWorldPos={playerWorldPos}, targetOffset={targetOffset}");

            if (animate)
            {
                var tween = CreateTween();
                tween.SetEase(Tween.EaseType.Out);
                tween.SetTrans(Tween.TransitionType.Quad);
                tween.TweenProperty(_tileViewsContainer, "position", targetOffset, 0.3f);
            }
            else
            {
                _tileViewsContainer.Position = targetOffset;
            }
        }

        private void ClearTileViews()
        {
            foreach (var view in _tileViews.Values)
            {
                view.QueueFree();
            }
            _tileViews.Clear();
        }

        private void CreateTileView(HexTile tile)
        {
            if (_tileViewScene == null)
            {
                GD.PrintErr("[HexMapUI] _tileViewScene is null!");
                return;
            }

            var tileView = (HexTileView)_tileViewScene.Instantiate();
            tileView.Size = _hexSize;
            var worldPos = HexToWorld(tile.Coord);
            tileView.Position = worldPos;
            tileView.SetHexWorldPosition(worldPos + _hexSize / 2);

            tileView.OnTileClicked += OnTileClicked;
            tileView.OnTileHovered += OnTileHovered;

            _tileViewsContainer.AddChild(tileView);
            tileView.SetTile(tile);

            _tileViews[tile.Coord] = tileView;
        }

        private Vector2 HexToWorld(HexCoord coord)
        {
            float x = 150f * coord.Q + 75f * coord.R;
            float y = 117f * coord.R;
            return new Vector2(x, y);
        }

        private HexCoord WorldToHex(Vector2 worldPos)
        {
            var screenCenter = GetViewportRect().Size / 2;
            var pos = worldPos - screenCenter;

            int r = (int)Mathf.Round(pos.Y / 117f);
            int q = (int)Mathf.Round((pos.X - 75f * r) / 150f);

            return new HexCoord(q, r);
        }

        private void OnTileClicked(HexTileView tileView)
        {
            if (_controller == null) return;

            var tile = tileView.Tile;
            if (tile == null) return;

            if (!tile.CanEnter) return;

            var currentPos = _controller.CurrentPosition;
            var neighbors = currentPos.GetNeighbors();

            GD.Print($"[HexMapUI] OnTileClicked: current={currentPos}, clicked={tile.Coord}");

            if (tile.Coord.ContainsInArray(neighbors))
            {
                GD.Print($"[HexMapUI] Moving to {tile.Coord}");
                _controller.MoveTo(tile.Coord);
            }
            else if (tile.Coord == _controller.CurrentMap.End)
            {
                GD.Print($"[HexMapUI] Quick moving to end {tile.Coord}");
                _controller.QuickMoveTo(tile.Coord);
            }
            else
            {
                GD.Print($"[HexMapUI] Clicked tile is not adjacent to current position");
            }
        }

        private void OnTileHovered(HexTileView tileView)
        {
            if (_controller == null) return;

            ClearPathHighlights();

            var tile = tileView.Tile;
            if (tile != null && tile.CanEnter)
            {
                var path = _controller.GetPathTo(tile.Coord);
                HighlightPath(path);
            }
        }

        private void ClearPathHighlights()
        {
            foreach (var view in _tileViews.Values)
            {
                view.SetAsPath(false);
            }
        }

        private void HighlightPath(List<HexCoord> path)
        {
            foreach (var coord in path)
            {
                if (_tileViews.ContainsKey(coord))
                {
                    _tileViews[coord].SetAsPath(true);
                }
            }
        }

        private void OnPlayerMoved(HexCoord newPos)
        {
            GD.Print($"[HexMapUI] OnPlayerMoved: newPos={newPos}");
            if (_playerIcon == null)
            {
                GD.PrintErr("[HexMapUI] OnPlayerMoved: _playerIcon is null!");
                return;
            }
            if (_tileViews.ContainsKey(newPos))
            {
                var tileView = _tileViews[newPos];
                GD.Print($"[HexMapUI] TileView found: pos={tileView.Position}, size={tileView.Size}");
                var tileWorldPos = _tileViewsContainer.Position + tileView.Position + tileView.Size / 2;
                GD.Print($"[HexMapUI] Moving player to: {tileWorldPos}");
                _playerIcon.MoveTo(tileWorldPos);
            }
            else
            {
                GD.PrintErr($"[HexMapUI] TileView not found for {newPos}!");
            }

            ClearPathHighlights();
        }

        private void UpdatePlayerPosition()
        {
            if (_controller == null) return;
            if (_playerIcon == null)
            {
                GD.PrintErr("[HexMapUI] UpdatePlayerPosition: _playerIcon is null!");
                return;
            }

            var currentPos = _controller.CurrentPosition;
            GD.Print($"[HexMapUI] UpdatePlayerPosition: currentPos={currentPos}");

            CenterOnPlayer(currentPos, true);

            var playerWorldPos = HexToWorld(currentPos);
            var tileCenterPos = playerWorldPos + _hexSize / 2 - _playerIcon.Size / 2;
            GD.Print($"[HexMapUI] UpdatePlayerPosition: playerWorldPos={playerWorldPos}, tileCenterPos={tileCenterPos}, _playerIcon.Size={_playerIcon.Size}");
            _playerIcon.TeleportTo(tileCenterPos);
        }

        private void OnTileTriggered(HexTile tile)
        {
            if (_tileViews.ContainsKey(tile.Coord))
            {
                var tileView = _tileViews[tile.Coord];
                tileView.SetTile(tile);

                if (tile.EventType == HexEventType.Hole && tile.IsDisappeared)
                {
                    tileView.AnimateDisappear();
                    _tileViews.Remove(tile.Coord);
                }
            }

            ShowEventNotification(tile);
        }

        private void ShowEventNotification(HexTile tile)
        {
            var notification = new Label();
            notification.Text = tile.DisplayName ?? GetEventDisplayName(tile.EventType);
            notification.HorizontalAlignment = HorizontalAlignment.Center;
            notification.Position = new Vector2(GetViewportRect().Size.X / 2 - 100, 100);
            notification.AddThemeFontSizeOverride("font_size", 20);
            notification.Modulate = Colors.White;
            AddChild(notification);

            var tween = CreateTween();
            tween.TweenProperty(notification, "position:y", 80, 1.0f);
            tween.TweenProperty(notification, "modulate:a", 0f, 0.5f);
            tween.TweenCallback(Callable.From(() => notification.QueueFree()));
        }

        private string GetEventDisplayName(HexEventType eventType)
        {
            return eventType switch
            {
                HexEventType.BattleNormal => "遭遇敌人",
                HexEventType.BattleElite => "精英敌人",
                HexEventType.BattleBoss => "BOSS",
                HexEventType.Swamp => "沼泽",
                HexEventType.GainBlackMark => "获得黑印",
                HexEventType.Shop => "商店",
                HexEventType.Heal => "生命之泉",
                HexEventType.TwoWayTeleport => "双向传送门",
                HexEventType.Hole => "洞穴",
                HexEventType.OneDirectionTele => "单向传送门",
                _ => "事件"
            };
        }

        private void OnTeleportTriggered(HexCoord position)
        {
            _teleportDialog.Visible = true;
        }

        private void OnTeleportConfirmPressed()
        {
            _teleportDialog.Visible = false;
            _controller?.ConfirmTeleport();
        }

        private void OnTeleportCancelPressed()
        {
            _teleportDialog.Visible = false;
            _controller?.CancelTeleport();
        }

        private void OnHealthChanged(float current, float max)
        {
            _healthBar?.SetHealth(max, current);
        }

        private void OnBlackMarkChanged(int amount)
        {
            UpdateBlackMarkDisplay(amount);
        }

        private void UpdateBlackMarkDisplay(int amount)
        {
            if (_blackMarkLabel != null)
            {
                _blackMarkLabel.Text = $"黑印: {amount}";
            }
        }

        private void OnSkipButtonPressed()
        {
            _controller?.SkipMap();
        }

        private void OnDeathResistanceButtonPressed()
        {
            GD.Print("[HexMapUI] 死亡抵抗按钮被点击");
        }

        private void OnBlackMarkButtonPressed()
        {
            GD.Print("[HexMapUI] 黑印按钮被点击");
        }

        private void OnSettingsButtonPressed()
        {
            GD.Print("[HexMapUI] 设置按钮被点击");
        }

        private void OnShopOpened()
        {
            RefreshShopItems();
            _shopContainer.Visible = true;
        }

        private void OnShopClosed()
        {
            _shopContainer.Visible = false;
        }

        private void OnShopClosePressed()
        {
            _controller?.CloseShop();
        }

        private void RefreshShopItems()
        {
            foreach (var child in _shopItemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (BlackMarkShopManager.Instance == null) return;

            foreach (var item in BlackMarkShopManager.Instance.CurrentShopItems)
            {
                var itemCard = CreateShopItemCard(item);
                _shopItemsContainer.AddChild(itemCard);
            }
        }

        private ShopItemCard CreateShopItemCard(ShopItem item)
        {
            var card = (ShopItemCard)_shopItemCardScene.Instantiate();
            bool canAfford = BlackMarkShopManager.Instance?.CanAfford(item) ?? false;
            card.SetItem(item, canAfford);
            card.OnBuyClicked += OnShopItemClicked;
            return card;
        }

        private void OnShopItemClicked(ShopItem item)
        {
            if (item.ItemType == ShopItemType.Artifact)
            {
                BuyArtifact(item);
            }
            else if (item.ItemType == ShopItemType.Engraving)
            {
                StartEngravingSelection(item);
            }
        }

        private void BuyArtifact(ShopItem item)
        {
            if (BlackMarkShopManager.Instance == null) return;

            if (BlackMarkShopManager.Instance.PurchaseArtifact(item))
            {
                RefreshShopItems();
                RefreshBackpack();
            }
        }

        private void StartEngravingSelection(ShopItem item)
        {
            if (BlackMarkShopManager.Instance == null) return;

            if (BlackMarkShopManager.Instance.StartEngravingPurchase(item))
            {
                _shopContainer.Visible = false;
                RefreshEngravingCards();
                _engravingSelectContainer.Visible = true;
            }
        }

        private void RefreshEngravingCards()
        {
            foreach (var child in _engravingCardsContainer.GetChildren())
            {
                child.QueueFree();
            }

            GD.Print("[HexMapUI] 刷新刻印选择界面（模拟4个角色的卡牌）");

            for (int i = 0; i < 12; i++)
            {
                var cardSlot = CreateEngravingCardSlot($"Card_{i}", $"卡牌{i + 1}");
                _engravingCardsContainer.AddChild(cardSlot);
            }
        }

        private EngravingCardSlot CreateEngravingCardSlot(string cardId, string cardName)
        {
            var slot = (EngravingCardSlot)_engravingCardSlotScene.Instantiate();
            slot.SetCard(cardId, cardName);
            slot.OnSelected += OnEngravingCardSelected;
            return slot;
        }

        private string _selectedEngravingCardId = null;

        private void OnEngravingCardSelected(EngravingCardSlot cardSlot, string cardId)
        {
            _selectedEngravingCardId = cardId;
            _engravingConfirmButton.Disabled = false;

            foreach (var child in _engravingCardsContainer.GetChildren())
            {
                if (child is EngravingCardSlot slot)
                {
                    slot.SetSelected(child == cardSlot);
                }
            }
        }

        private void OnEngravingConfirmPressed()
        {
            if (BlackMarkShopManager.Instance == null || string.IsNullOrEmpty(_selectedEngravingCardId))
                return;

            if (BlackMarkShopManager.Instance.ConfirmEngraving(_selectedEngravingCardId))
            {
                _engravingSelectContainer.Visible = false;
                _selectedEngravingCardId = null;
                _controller?.CloseShop();
            }
        }

        private void OnEngravingCancelPressed()
        {
            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.CancelEngraving();
            }

            _engravingSelectContainer.Visible = false;
            _shopContainer.Visible = true;
        }

        private void OnEngravingClosePressed()
        {
            OnEngravingCancelPressed();
        }

        private void RefreshBackpack()
        {
            foreach (var child in _backpackItemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (BlackMarkShopManager.Instance == null) return;

            foreach (var artifact in BlackMarkShopManager.Instance.GetOwnedArtifacts())
            {
                var icon = new TextureRect();
                icon.CustomMinimumSize = new Vector2(40, 40);
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                if (artifact.icon != null)
                {
                    icon.Texture = GD.Load<Texture2D>(artifact.icon);
                }
                icon.TooltipText = $"{artifact.name}: {artifact.description}";
                _backpackItemsContainer.AddChild(icon);
            }
        }

        private void OnMapCompleted()
        {
            GD.Print("[HexMapUI] 地图探索完成!");
        }

        private void OnChallengeFailed()
        {
            _failureMessage.Text = "挑战失败！\n您选择了返回而未跳过地图";
            _failurePanel.Visible = true;
        }

        private void OnFailureOkPressed()
        {
            _failurePanel.Visible = false;
            GetTree().ChangeSceneToFile("res://Scenes/CharacterSelect.tscn");
        }

        public void SetCharacterRages(List<int> rageValues, int maxRage)
        {
            for (int i = 0; i < Math.Min(rageValues.Count, _rageCircles.Count); i++)
            {
                _rageCircles[i].SetRage(rageValues[i], maxRage);
            }
        }

        public void AddRageToCharacter(int characterIndex, int rageAmount)
        {
            if (characterIndex >= 0 && characterIndex < _rageCircles.Count)
            {
                _rageCircles[characterIndex].AddRage(rageAmount);
            }
        }
    }
}
