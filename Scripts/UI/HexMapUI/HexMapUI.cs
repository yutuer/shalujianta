using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.HexMap;
using FishEatFish.Shop;
using FishEatFish.Scenes;
using FishEatFish.UI.ShopItemCard;

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
        private Button _refreshButton;

        private Control _engravingSelectContainer;
        private Control _engravingCardsContainer;
        private Button _engravingConfirmButton;
        private Button _engravingCancelButton;

        private Control _failurePanel;
        private Label _failureMessage;
        private Button _failureOkButton;

        private Control _backpackContainer;
        private Control _backpackItemsContainer;

        private Label _costLabel;
        private int _currentRefreshCount = 2;
        private const int MaxRefreshCount = 2;
        private const int RefreshCost = 5;
        private HexTile _currentShopTile;

        private ArtifactDescriptionUI _artifactDescriptionUI;
        private KeyOrderDescriptionUI _keyOrderDescriptionUI;
        private EngravingCardSelectionUI _engravingCardSelectionUI;

        private Dictionary<HexCoord, HexTileView> _tileViews = new Dictionary<HexCoord, HexTileView>();
        private List<RageCircle> _rageCircles = new List<RageCircle>();

        private Vector2 _hexSize = new Vector2(180, 156);

        public override void _Ready()
        {
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

            _controller = HexMapController.Instance;
            InitializeComponents();
            InitializeShopUI();
            SetupEventConnections();

            if (_controller != null)
            {
                RefreshMap();
            }
        }

        private void InitializeShopUI()
        {
            GD.Print($"[HexMapUI] InitializeShopUI called");

            _artifactDescriptionUI = GetNodeOrNull<ArtifactDescriptionUI>("ArtifactDescriptionUI");
            if (_artifactDescriptionUI == null)
            {
                var artifactUI = GetNodeOrNull<Control>("ArtifactDescriptionUI");
                if (artifactUI != null)
                {
                    _artifactDescriptionUI = artifactUI as ArtifactDescriptionUI;
                }
            }
            if (_artifactDescriptionUI != null)
            {
                GD.Print($"[HexMapUI] ArtifactDescriptionUI found");
                _artifactDescriptionUI.Visible = false;
                _artifactDescriptionUI.OnPurchaseCompleted += OnArtifactPurchased;
                _artifactDescriptionUI.OnCancel += OnArtifactDescriptionCancelled;
            }
            else
            {
                GD.PrintErr($"[HexMapUI] ArtifactDescriptionUI not found!");
            }

            _keyOrderDescriptionUI = GetNodeOrNull<KeyOrderDescriptionUI>("KeyOrderDescriptionUI");
            if (_keyOrderDescriptionUI != null)
            {
                GD.Print($"[HexMapUI] KeyOrderDescriptionUI found");
                _keyOrderDescriptionUI.Visible = false;
            }
            else
            {
                GD.PrintErr($"[HexMapUI] KeyOrderDescriptionUI not found!");
            }

            _engravingCardSelectionUI = GetNodeOrNull<EngravingCardSelectionUI>("EngravingCardSelectionUI");
            if (_engravingCardSelectionUI == null)
            {
                GD.Print($"[HexMapUI] EngravingCardSelectionUI not in tree, trying to load from scene");
                var engravingScene = GD.Load<PackedScene>("res://Scenes/UI/EngravingCardSelectionUI.tscn");
                if (engravingScene != null)
                {
                    GD.Print($"[HexMapUI] Scene loaded successfully");
                    var engravingNode = engravingScene.Instantiate();
                    AddChild(engravingNode);
                    GD.Print($"[HexMapUI] Node instantiated and added");
                    if (engravingNode is Control control)
                    {
                        control.Position = Vector2.Zero;
                    }
                    _engravingCardSelectionUI = engravingNode as EngravingCardSelectionUI;
                    GD.Print($"[HexMapUI] _engravingCardSelectionUI cast result: {_engravingCardSelectionUI != null}");
                }
                else
                {
                    GD.PrintErr($"[HexMapUI] Failed to load EngravingCardSelectionUI scene!");
                }
            }
            if (_engravingCardSelectionUI != null)
            {
                GD.Print($"[HexMapUI] EngravingCardSelectionUI found");
                _engravingCardSelectionUI.Visible = false;
                _engravingCardSelectionUI.OnEngravingCompleted += OnEngravingCompleted;
                _engravingCardSelectionUI.OnCancel += OnEngravingSelectionCancelled;
            }
            else
            {
                GD.PrintErr($"[HexMapUI] EngravingCardSelectionUI not found!");
            }

            GD.Print($"[HexMapUI] InitializeShopUI completed");
        }

        private void OnArtifactPurchased()
        {
            GD.Print($"[HexMapUI] OnArtifactPurchased");
            SetShopInteractionEnabled(true);
            RefreshShopItems();
            UpdateBlackMarkDisplay(BlackMarkShopManager.Instance?.BlackMarkCount ?? 0);
            RefreshBackpack();
        }

        private void OnArtifactDescriptionCancelled()
        {
            GD.Print($"[HexMapUI] OnArtifactDescriptionCancelled");
            SetShopInteractionEnabled(true);
        }

        private void OnEngravingSelectionCancelled()
        {
            GD.Print($"[HexMapUI] OnEngravingSelectionCancelled");
        }

        private void InitializeComponents()
        {
            _mapContainer = GetNodeOrNull<Control>("MapContainer");

            _tileViewsContainer = GetNodeOrNull<Control>("MapContainer/TileViews");
            if (_tileViewsContainer != null)
            {
                _tileViewsContainer.Position = Vector2.Zero;
            }

            var playerIconControl = GetNodeOrNull<Control>("MapContainer/TileViews/PlayerIcon");
            _playerIcon = playerIconControl as PlayerIcon;

            var healthBarControl = GetNodeOrNull<Control>("HealthBar");
            _healthBar = healthBarControl as HealthBar;

            if (_healthBar != null)
            {
                PositionHealthBar();
            }

            _rageCirclesContainer = GetNodeOrNull<HBoxContainer>("RageCircles");
            _rageCircles.Clear();
            for (int i = 0; i < 4; i++)
            {
                var rageCircleAsControl = GetNodeOrNull<Control>($"RageCircles/RageCircle{i}");
                var rageCircle = rageCircleAsControl as RageCircle;
                if (rageCircle != null)
                {
                    rageCircle.SetCharacterName($"角色{i + 1}");
                    rageCircle.SetRage(0, 100);
                    _rageCircles.Add(rageCircle);
                }
            }
            if (_rageCirclesContainer != null)
            {
                PositionRageCircles();
            }

            ConnectButton("TopRightButtons/SkipButton", OnSkipButtonPressed);
            ConnectButton("TopRightButtons/DeathResistanceButton", OnDeathResistanceButtonPressed);
            ConnectButton("TopRightButtons/BlackMarkButtonContainer/BlackMarkButton", OnBlackMarkButtonPressed);
            ConnectButton("TopRightButtons/SettingsButton", OnSettingsButtonPressed);
            if (HasNode("TopRightButtons"))
            {
                PositionTopRightButtons();
            }

            _blackMarkLabel = GetNodeOrNull<Label>("TopRightButtons/BlackMarkButtonContainer/BlackMarkLabel");

            _teleportDialog = GetNodeOrNull<Control>("TeleportDialog");
            if (_teleportDialog != null)
            {
                ConnectButton("TeleportDialog/VBoxContainer/ButtonBox/CancelButton", OnTeleportCancelPressed);
                ConnectButton("TeleportDialog/VBoxContainer/ButtonBox/ConfirmButton", OnTeleportConfirmPressed);
            }

            _shopContainer = GetNodeOrNull<Control>("ShopContainer");
            if (_shopContainer != null)
            {
                _shopItemsContainer = GetNodeOrNull<Control>("ShopContainer/VBoxContainer/ShopItemsMargin/ShopItems");
                _shopCloseButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/HeaderBox/CloseButton");
                ConnectButton("ShopContainer/VBoxContainer/HeaderBox/CloseButton", OnShopClosePressed);
                _refreshButton = GetNodeOrNull<Button>("ShopContainer/VBoxContainer/RefreshBar/RefreshButton");
                _costLabel = GetNodeOrNull<Label>("ShopContainer/VBoxContainer/RefreshBar/CostLabel");
                if (_refreshButton != null)
                {
                    _refreshButton.Pressed += OnRefreshPressed;
                }
            }

            _engravingSelectContainer = GetNodeOrNull<Control>("EngravingSelectContainer");
            if (_engravingSelectContainer != null)
            {
                _engravingCardsContainer = GetNodeOrNull<Control>("EngravingSelectContainer/VBoxContainer/EngravingCards");
                ConnectButton("EngravingSelectContainer/VBoxContainer/ButtonBox/CancelButton", OnEngravingCancelPressed);
                ConnectButton("EngravingSelectContainer/VBoxContainer/ButtonBox/ConfirmButton", OnEngravingConfirmPressed);
                ConnectButton("EngravingSelectContainer/VBoxContainer/HeaderBox/CloseButton", OnEngravingClosePressed);
            }

            _failurePanel = GetNodeOrNull<Control>("FailurePanel");
            if (_failurePanel != null)
            {
                _failureMessage = GetNodeOrNull<Label>("FailurePanel/VBoxContainer/FailureMessage");
                ConnectButton("FailurePanel/VBoxContainer/OkButton", OnFailureOkPressed);
            }

            _backpackContainer = GetNodeOrNull<Control>("BackpackContainer");
            if (_backpackContainer != null)
            {
                _backpackItemsContainer = GetNodeOrNull<Control>("BackpackContainer/HBoxContainer/BackpackItems");
            }
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
            _healthBar.GlobalPosition = targetPos;
        }

        private void PositionRageCircles()
        {
            if (_rageCirclesContainer == null) return;
        }

        public override void _Process(double delta)
        {
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
                UpdateBlackMarkDisplay(BlackMarkShopManager.Instance.BlackMarkCount);
            }
        }

        private void RefreshMap()
        {
            ClearTileViews();

            if (_controller?.CurrentMap == null) return;

            foreach (var tile in _controller.CurrentMap.GetAllTiles())
            {
                CreateTileView(tile);
            }

            if (_playerIcon != null)
            {
                _playerIcon.GetParent().MoveChild(_playerIcon, -1);
            }

            CenterOnPlayer(_controller.CurrentPosition, false);
            UpdatePlayerPosition();
        }

        private void CenterOnPlayer(HexCoord playerCoord, bool animate = true)
        {
            if (_tileViewsContainer == null) return;

            var screenCenter = GetViewportRect().Size / 2;
            var playerWorldPos = HexToWorld(playerCoord) + _hexSize / 2;
            var targetOffset = screenCenter - playerWorldPos;

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
            if (_tileViewScene == null) return;

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

            if (tile.Coord.ContainsInArray(neighbors))
            {
                _controller.MoveTo(tile.Coord);
            }
            else if (tile.Coord == _controller.CurrentMap.End)
            {
                _controller.QuickMoveTo(tile.Coord);
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
            if (_playerIcon == null) return;

            CenterOnPlayer(newPos, true);

            var iconSize = _playerIcon.Size == Vector2.Zero
                ? _playerIcon.CustomMinimumSize
                : _playerIcon.Size;

            var playerWorldPos = HexToWorld(newPos);
            var tileCenterPos = playerWorldPos + _hexSize / 2 - iconSize / 2;
            _playerIcon.MoveTo(tileCenterPos);

            ClearPathHighlights();
        }

        private void UpdatePlayerPosition()
        {
            if (_controller == null) return;
            if (_playerIcon == null) return;

            var currentPos = _controller.CurrentPosition;

            CenterOnPlayer(currentPos, false);

            var iconSize = _playerIcon.Size == Vector2.Zero
                ? _playerIcon.CustomMinimumSize
                : _playerIcon.Size;

            var playerWorldPos = HexToWorld(currentPos);
            var tileCenterPos = playerWorldPos + _hexSize / 2 - iconSize / 2;
            _playerIcon.TeleportTo(tileCenterPos);
        }

        private void OnTileTriggered(HexTile tile)
        {
            GD.Print($"[HexMapUI] OnTileTriggered: tile={tile.Coord}, EventType={tile.EventType}");
            if (_tileViews.ContainsKey(tile.Coord))
            {
                var tileView = _tileViews[tile.Coord];
                GD.Print($"[HexMapUI] OnTileTriggered: found tileView, calling SetTile");
                tileView.SetTile(tile);

                if (tile.EventType == HexEventType.Hole && tile.IsDisappeared)
                {
                    tileView.AnimateDisappear();
                    _tileViews.Remove(tile.Coord);
                }
            }
            else
            {
                GD.Print($"[HexMapUI] OnTileTriggered: tileView not found for {tile.Coord}");
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
                _blackMarkLabel.Text = $"{amount}";
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
            GD.Print($"[HexMapUI] OnShopOpened called: _shopContainer={_shopContainer}");

            if (_controller?.CurrentMap != null)
            {
                var currentTile = _controller.CurrentMap.GetTile(_controller.CurrentPosition);
                if (currentTile != null && currentTile.EventType == HexEventType.Shop)
                {
                    _currentShopTile = currentTile;
                    _currentRefreshCount = _currentShopTile.ShopRefreshCount;
                }
                else
                {
                    _currentShopTile = null;
                    _currentRefreshCount = 0;
                }
            }
            else
            {
                _currentShopTile = null;
                _currentRefreshCount = 0;
            }

            UpdateRefreshUI();
            RefreshShopItems();
            _shopContainer.Visible = true;
            GD.Print($"[HexMapUI] OnShopOpened completed, refreshCount={_currentRefreshCount}");
        }

        private void OnShopClosed()
        {
            if (_currentShopTile != null)
            {
                _currentShopTile.ShopRefreshCount = _currentRefreshCount;
            }
            _shopContainer.Visible = false;
        }

        private void OnShopClosePressed()
        {
            _controller?.CloseShop();
        }

        private void OnRefreshPressed()
        {
            GD.Print($"[HexMapUI] OnRefreshPressed: _currentRefreshCount={_currentRefreshCount}");

            if (_currentRefreshCount <= 0)
            {
                GD.Print($"[HexMapUI] No refreshes left!");
                return;
            }

            if (BlackMarkShopManager.Instance.BlackMarkCount < RefreshCost)
            {
                GD.Print($"[HexMapUI] Not enough black marks! Need {RefreshCost}, have {BlackMarkShopManager.Instance.BlackMarkCount}");
                return;
            }

            bool spent = BlackMarkShopManager.Instance.SpendBlackMark(RefreshCost);
            if (!spent)
            {
                GD.Print($"[HexMapUI] Failed to spend black marks!");
                return;
            }

            _currentRefreshCount--;
            UpdateRefreshUI();

            if (_currentShopTile != null)
            {
                BlackMarkShopManager.Instance.RegenerateShopItems();
                RefreshShopItems();
            }

            GD.Print($"[HexMapUI] Shop refreshed! Remaining: {_currentRefreshCount}");
        }

        private void UpdateRefreshUI()
        {
            if (_refreshButton != null)
            {
                _refreshButton.Text = $"刷新 {_currentRefreshCount}/{MaxRefreshCount}";
                _refreshButton.Disabled = _currentRefreshCount <= 0;
            }

            if (_costLabel != null)
            {
                _costLabel.Text = $"花费 {RefreshCost}点";
            }
        }

        private void RefreshShopItems()
        {
            GD.Print($"[HexMapUI] RefreshShopItems called: _shopContainer={_shopContainer}, _shopItemsContainer={_shopItemsContainer}");

            var childrenToRemove = _shopItemsContainer.GetChildren().ToList();
            GD.Print($"[HexMapUI] RefreshShopItems: found {childrenToRemove.Count} children to remove");
            foreach (var child in childrenToRemove)
            {
                GD.Print($"[HexMapUI] RefreshShopItems: removing child {child.Name}");
                _shopItemsContainer.RemoveChild(child);
                GD.Print($"[HexMapUI] RefreshShopItems: after RemoveChild, child={child}, parent={child.GetParent()}");
            }
            GD.Print($"[HexMapUI] RefreshShopItems: after ALL removals, container has {_shopItemsContainer.GetChildCount()} children");

            GD.Print($"[HexMapUI] BlackMarkShopManager.Instance={BlackMarkShopManager.Instance}");
            if (BlackMarkShopManager.Instance == null)
            {
                GD.PrintErr("[HexMapUI] BlackMarkShopManager.Instance is null!");
                return;
            }

            GD.Print($"[HexMapUI] CurrentShopItems count={BlackMarkShopManager.Instance.CurrentShopItems.Count}");
            if (_shopItemCardScene == null)
            {
                GD.PrintErr("[HexMapUI] _shopItemCardScene is null!");
                return;
            }

            foreach (var item in BlackMarkShopManager.Instance.CurrentShopItems)
            {
                GD.Print($"[HexMapUI] Creating card for: {item.Name}, Icon={item.Icon}");
                var itemCard = CreateShopItemCard(item);
                if (itemCard == null)
                {
                    GD.PrintErr("[HexMapUI] CreateShopItemCard returned null!");
                    continue;
                }
                GD.Print($"[HexMapUI] Adding card to container...");
                _shopItemsContainer.AddChild(itemCard);
                GD.Print($"[HexMapUI] Card added successfully, calling SetItem...");
                itemCard.SetItem(item);
                GD.Print($"[HexMapUI] SetItem called successfully");
                GD.Print($"[HexMapUI] Binding OnCardClicked event...");
                itemCard.OnCardClicked += OnShopItemCardClicked;
                GD.Print($"[HexMapUI] Event bound successfully");
            }
            GD.Print($"[HexMapUI] RefreshShopItems completed. Container has {_shopItemsContainer.GetChildCount()} children");
        }

        private void SetShopInteractionEnabled(bool enabled)
        {
            GD.Print($"[HexMapUI] SetShopInteractionEnabled called: enabled={enabled}, _shopCloseButton={_shopCloseButton != null}, _refreshButton={_refreshButton != null}");

            if (_shopCloseButton != null)
            {
                _shopCloseButton.Disabled = !enabled;
                GD.Print($"[HexMapUI] _shopCloseButton.Disabled set to {!enabled}");
            }
            else
            {
                GD.PrintErr($"[HexMapUI] _shopCloseButton is null!");
            }

            if (_refreshButton != null)
            {
                _refreshButton.Disabled = !enabled || _currentRefreshCount <= 0;
                GD.Print($"[HexMapUI] _refreshButton.Disabled set to {!enabled || _currentRefreshCount <= 0}");
            }
            if (_shopItemsContainer != null)
            {
                foreach (var child in _shopItemsContainer.GetChildren())
                {
                    if (child is FishEatFish.UI.ShopItemCard.ShopItemCard card)
                    {
                        card.SetClickEnabled(enabled);
                    }
                }
            }
            GD.Print($"[HexMapUI] SetShopInteractionEnabled: enabled={enabled} completed");
        }

        private FishEatFish.UI.ShopItemCard.ShopItemCard CreateShopItemCard(ShopItem item)
        {
            GD.Print($"[HexMapUI] CreateShopItemCard: Instantiating card...");
            var card = (FishEatFish.UI.ShopItemCard.ShopItemCard)_shopItemCardScene.Instantiate();
            GD.Print($"[HexMapUI] CreateShopItemCard: card={card}, IsInstanceValid={card != null}, Script={card?.GetScript()}");

            if (card == null)
            {
                GD.PrintErr("[HexMapUI] CreateShopItemCard: card is null!");
                return null;
            }

            GD.Print($"[HexMapUI] CreateShopItemCard: card type={card.GetType()}, name={card.Name}");
            GD.Print($"[HexMapUI] CreateShopItemCard: returning card...");
            return card;
        }

        private void OnShopItemCardClicked(ShopItem item)
        {
            GD.Print($"[HexMapUI] OnShopItemCardClicked: {item.Name}, Purchased={item.Purchased}");

            if (item.Purchased)
            {
                GD.Print($"[HexMapUI] Item already purchased, ignoring click");
                return;
            }

            if (item.ItemType == ShopItemType.Artifact)
            {
                ShowArtifactDescription(item);
            }
            else if (item.ItemType == ShopItemType.Engraving)
            {
                ShowEngravingDescription(item);
            }
        }

        private void ShowArtifactDescription(ShopItem item)
        {
            GD.Print($"[HexMapUI] ShowArtifactDescription: {item.Name}");
            SetShopInteractionEnabled(false);
            _artifactDescriptionUI.ShowArtifact(item);
        }

        private void ShowEngravingDescription(ShopItem item)
        {
            GD.Print($"[HexMapUI] ShowEngravingDescription: {item.Name}");
            SetShopInteractionEnabled(false);
            _keyOrderDescriptionUI.ShowEngravingDescription(item, OnEngravingItemConfirmed, OnEngravingItemCancelled);
        }

        private void OnEngravingItemConfirmed(ShopItem engravingItem)
        {
            GD.Print($"[HexMapUI] OnEngravingItemConfirmed called: {engravingItem?.Name}");

            if (_engravingCardSelectionUI == null)
            {
                GD.Print($"[HexMapUI] _engravingCardSelectionUI is null, trying to load...");
                var engravingScene = GD.Load<PackedScene>("res://Scenes/UI/EngravingCardSelectionUI.tscn");
                GD.Print($"[HexMapUI] engravingScene = {engravingScene}");
                if (engravingScene != null)
                {
                    var engravingNode = engravingScene.Instantiate();
                    GD.Print($"[HexMapUI] engravingNode = {engravingNode}, type = {engravingNode?.GetType().Name}");
                    AddChild(engravingNode);
                    _engravingCardSelectionUI = engravingNode as EngravingCardSelectionUI;
                    GD.Print($"[HexMapUI] _engravingCardSelectionUI after cast = {_engravingCardSelectionUI}");
                    if (_engravingCardSelectionUI != null)
                    {
                        _engravingCardSelectionUI.OnEngravingCompleted += OnEngravingCompleted;
                        _engravingCardSelectionUI.OnCancel += OnEngravingSelectionCancelled;
                        GD.Print($"[HexMapUI] EngravingCardSelectionUI loaded successfully");
                    }
                }
            }

            if (_engravingCardSelectionUI == null)
            {
                GD.PrintErr($"[HexMapUI] _engravingCardSelectionUI is still null!");
                return;
            }

            if (!BlackMarkShopManager.Instance.StartEngravingPurchase(engravingItem))
            {
                GD.PrintErr($"[HexMapUI] Failed to start engraving purchase!");
                SetShopInteractionEnabled(true);
                return;
            }

            var cardList = new List<Battle.Card.Card>();

            if (GlobalData.SelectedCharacters != null)
            {
                foreach (var character in GlobalData.SelectedCharacters)
                {
                    if (character != null)
                    {
                        if (!string.IsNullOrEmpty(character.AttackCardId))
                        {
                            var attackCard = Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(character.AttackCardId);
                            if (attackCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(attackCard.CardId)) cardList.Add(attackCard);
                        }

                        if (!string.IsNullOrEmpty(character.DefenseCardId))
                        {
                            var defenseCard = Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(character.DefenseCardId);
                            if (defenseCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(defenseCard.CardId)) cardList.Add(defenseCard);
                        }

                        if (character.SpecialCardIds != null)
                        {
                            foreach (var specialCardId in character.SpecialCardIds)
                            {
                                if (!string.IsNullOrEmpty(specialCardId))
                                {
                                    var specialCard = Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(specialCardId);
                                    if (specialCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(specialCard.CardId))
                                    {
                                        cardList.Add(specialCard);
                                    }
                                    else if (specialCard == null)
                                    {
                                        var baseSpecialCardId = specialCardId.Replace("1", "").Replace("2", "").Replace("3", "");
                                        specialCard = Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(baseSpecialCardId);
                                        if (specialCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(specialCard.CardId)) cardList.Add(specialCard);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GD.Print($"[HexMapUI] Showing card selection for {cardList.Count} cards");
            _engravingCardSelectionUI.ShowCardSelection(engravingItem, cardList);
        }

        private void OnEngravingItemCancelled()
        {
            GD.Print($"[HexMapUI] OnEngravingItemCancelled called");
            SetShopInteractionEnabled(true);
            GD.Print($"[HexMapUI] OnEngravingItemCancelled completed");
        }

        private void OnEngravingCompleted()
        {
            GD.Print($"[HexMapUI] OnEngravingCompleted");
            SetShopInteractionEnabled(true);
            RefreshShopItems();
            UpdateBlackMarkDisplay(BlackMarkShopManager.Instance?.BlackMarkCount ?? 0);
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
            GD.Print($"[HexMapUI] RefreshBackpack called: _backpackItemsContainer={_backpackItemsContainer}");

            if (_backpackItemsContainer == null)
            {
                GD.PrintErr("[HexMapUI] _backpackItemsContainer is null!");
                return;
            }

            foreach (var child in _backpackItemsContainer.GetChildren())
            {
                GD.Print($"[HexMapUI] RefreshBackpack: removing child {child.Name}");
                child.QueueFree();
            }

            if (BlackMarkShopManager.Instance == null)
            {
                GD.PrintErr("[HexMapUI] BlackMarkShopManager.Instance is null!");
                return;
            }

            var ownedArtifacts = BlackMarkShopManager.Instance.GetOwnedArtifacts();
            GD.Print($"[HexMapUI] RefreshBackpack: owned artifacts count = {ownedArtifacts.Count}");

            foreach (var artifact in ownedArtifacts)
            {
                GD.Print($"[HexMapUI] RefreshBackpack: creating icon for {artifact.name}, icon path = '{artifact.icon}'");

                TextureRect icon = null;
                bool hasValidTexture = false;

                if (!string.IsNullOrEmpty(artifact.icon))
                {
                    var texture = GD.Load<Texture2D>(artifact.icon);
                    if (texture != null)
                    {
                        icon = new TextureRect();
                        icon.Name = $"Icon_{artifact.artifactId}";
                        icon.CustomMinimumSize = new Vector2(40, 40);
                        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        icon.Texture = texture;
                        icon.TooltipText = $"{artifact.name}: {artifact.description}";
                        hasValidTexture = true;
                        GD.Print($"[HexMapUI] RefreshBackpack: icon loaded successfully");
                    }
                    else
                    {
                        GD.Print($"[HexMapUI] RefreshBackpack: icon path '{artifact.icon}' failed to load, using emoji fallback");
                    }
                }

                if (!hasValidTexture)
                {
                    icon = CreateEmojiBackpackItem(artifact);
                }

                _backpackItemsContainer.AddChild(icon);
                GD.Print($"[HexMapUI] RefreshBackpack: icon added for {artifact.name}");
            }

            GD.Print($"[HexMapUI] RefreshBackpack completed. Container has {_backpackItemsContainer.GetChildCount()} children");
        }

        private TextureRect CreateEmojiBackpackItem(ArtifactData artifact)
        {
            GD.Print($"[HexMapUI] CreateEmojiBackpackItem: creating icon for {artifact.name}");

            var container = new TextureRect();
            container.Name = $"Icon_{artifact.artifactId}";
            container.CustomMinimumSize = new Vector2(40, 40);
            container.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            container.TooltipText = $"{artifact.name}: {artifact.description}";

            if (!string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    container.Texture = texture;
                    GD.Print($"[HexMapUI] CreateEmojiBackpackItem: icon loaded from {artifact.icon}");
                    return container;
                }
            }

            var emojiLabel = new Label();
            emojiLabel.Name = "EmojiLabel";
            emojiLabel.HorizontalAlignment = HorizontalAlignment.Center;
            emojiLabel.VerticalAlignment = VerticalAlignment.Center;
            emojiLabel.AddThemeFontSizeOverride("font_size", 24);
            emojiLabel.Text = "💎";
            container.AddChild(emojiLabel);

            GD.Print($"[HexMapUI] CreateEmojiBackpackItem: using emoji fallback for {artifact.name}");
            return container;
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
