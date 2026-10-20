using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.HexMap;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Shop;
using FishEatFish.Scenes;
using FishEatFish.UI.ShopItemCard;

namespace FishEatFish.UI.HexMap
{
    public partial class HexMapUI : Control
    {
        [Export]
        private PackedScene _tileViewScene;

        private HexMapController _controller;

        private Control _mapContainer;
        private Control _tileViewsContainer;
        private PlayerIcon _playerIcon;
        private HealthBar _healthBar;
        private HBoxContainer _rageCirclesContainer;

        private Button _deathResistanceButton;
        private Label _deathResistanceLabel;
        private Button _blackMarkButton;
        private Button _settingsButton;

        private Label _blackMarkLabel;
        private SilverKeyProgressIndicator _silverKeyProgressIndicator;
        private bool _isInteractionBlocked;
        private HexCoord? _pendingMoveTarget;

        private FishEatFish.UI.EventDialog _eventDialog;

        private Control _failurePanel;

        private FishEatFish.UI.ShopUI.ShopUI _shopUI;
        private FishEatFish.UI.BackpackUI.BackpackUI _backpackUI;

        private Dictionary<HexCoord, HexTileView> _tileViews = new Dictionary<HexCoord, HexTileView>();
        private List<RageCircle> _rageCircles = new List<RageCircle>();

        private Vector2 _hexSize = new Vector2(180, 156);

        public override void _Ready()
        {
            GD.Print($"[HexMapUI] _Ready called");

            if (_tileViewScene == null)
            {
                _tileViewScene = GD.Load<PackedScene>("res://Scenes/UI/HexTileView.tscn");
            }
            _controller = HexMapController.Instance;
            InitializeComponents();
            SetupEventConnections();

            if (_controller != null)
            {
                // 控制器比本 UI 先就绪，初始血量事件不会补发，这里手动推一次
                _healthBar?.SetHealth(_controller.PlayerCurrentHealth, _controller.PlayerMaxHealth);
                RefreshMap();
            }
        }

        private void OnEngravingItemConfirmed(ShopItem engravingItem)
        {
            GD.Print($"[HexMapUI] OnEngravingItemConfirmed called: {engravingItem?.Name}");

            if (!BlackMarkShopManager.Instance.StartEngravingPurchase(engravingItem))
            {
                GD.PrintErr($"[HexMapUI] Failed to start engraving purchase!");
                return;
            }

            var cardList = new List<FishEatFish.Battle.Card.Card>();

            if (GlobalData.SelectedCharacters != null)
            {
                foreach (var character in GlobalData.SelectedCharacters)
                {
                    if (character != null)
                    {
                        if (!string.IsNullOrEmpty(character.AttackCardId))
                        {
                            var attackCard = FishEatFish.Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(character.AttackCardId);
                            if (attackCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(attackCard.CardId)) cardList.Add(attackCard);
                        }

                        if (!string.IsNullOrEmpty(character.DefenseCardId))
                        {
                            var defenseCard = FishEatFish.Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(character.DefenseCardId);
                            if (defenseCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(defenseCard.CardId)) cardList.Add(defenseCard);
                        }

                        if (character.SpecialCardIds != null)
                        {
                            foreach (var specialCardId in character.SpecialCardIds)
                            {
                                if (!string.IsNullOrEmpty(specialCardId))
                                {
                                    var specialCard = FishEatFish.Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(specialCardId);
                                    if (specialCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(specialCard.CardId))
                                    {
                                        cardList.Add(specialCard);
                                    }
                                    else if (specialCard == null)
                                    {
                                        var baseSpecialCardId = specialCardId.Replace("1", "").Replace("2", "").Replace("3", "");
                                        specialCard = FishEatFish.Battle.CharacterSystem.CharacterCardLoader.GetCharacterCard(baseSpecialCardId);
                                        if (specialCard != null && !BlackMarkShopManager.Instance.IsCardEngraved(specialCard.CardId)) cardList.Add(specialCard);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GD.Print($"[HexMapUI] Showing card selection for {cardList.Count} cards");
            _shopUI?.ShowEngravingCardSelection(engravingItem, cardList);
        }

        private void OnArtifactItemConfirmed(ShopItem artifactItem)
        {
            GD.Print($"[HexMapUI] OnArtifactItemConfirmed called: {artifactItem?.Name}");

            if (artifactItem == null)
            {
                GD.PrintErr("[HexMapUI] OnArtifactItemConfirmed: artifactItem is null!");
                return;
            }

            GD.Print($"[HexMapUI] OnArtifactItemConfirmed: _backpackUI={_backpackUI}");
            _shopUI?.HideArtifactDescription();
            _shopUI?.RefreshShopItems(BlackMarkShopManager.Instance?.CurrentShopItems ?? new List<ShopItem>());
            UpdateBlackMarkDisplay(BlackMarkShopManager.Instance?.BlackMarkCount ?? 0);
            _backpackUI?.Refresh();
            var ownedArtifacts = BlackMarkShopManager.Instance?.GetOwnedArtifacts();
            GD.Print($"[HexMapUI] OnArtifactItemConfirmed: ownedArtifacts count={ownedArtifacts?.Count ?? 0}");
        }

        private void OnEngravingCompleted()
        {
            GD.Print($"[HexMapUI] OnEngravingCompleted");
            _shopUI?.RefreshShopItems(BlackMarkShopManager.Instance?.CurrentShopItems ?? new List<ShopItem>());
            UpdateBlackMarkDisplay(BlackMarkShopManager.Instance?.BlackMarkCount ?? 0);
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

            ConnectButton("TopRightButtons/DeathResistanceButtonContainer/DeathResistanceButton", OnDeathResistanceButtonPressed);
            ConnectButton("TopRightButtons/BlackMarkButtonContainer/BlackMarkButton", OnBlackMarkButtonPressed);
            ConnectButton("TopRightButtons/SettingsButtonContainer/SettingsButton", OnSettingsButtonPressed);
            if (HasNode("TopRightButtons"))
            {
                PositionTopRightButtons();
            }

            _blackMarkLabel = GetNodeOrNull<Label>("TopRightButtons/BlackMarkButtonContainer/BlackMarkLabel");
            _deathResistanceLabel = GetNodeOrNull<Label>("TopRightButtons/DeathResistanceButtonContainer/DeathResistanceLabel");
            _silverKeyProgressIndicator = GetNodeOrNull<SilverKeyProgressIndicator>("SilverKeyProgressIndicator");

            if (_silverKeyProgressIndicator != null)
            {
                GD.Print($"[HexMapUI] SilverKeyProgressIndicator 找到了!");
                GD.Print($"[HexMapUI] 初始Size: {_silverKeyProgressIndicator.Size}");
                GD.Print($"[HexMapUI] CustomMinimumSize: {_silverKeyProgressIndicator.CustomMinimumSize}");
                GD.Print($"[HexMapUI] Visible: {_silverKeyProgressIndicator.Visible}");
                PositionSilverKeyProgressIndicator();
                GD.Print($"[HexMapUI] 定位后Size: {_silverKeyProgressIndicator.Size}");
                GD.Print($"[HexMapUI] 定位后GlobalPosition: {_silverKeyProgressIndicator.GlobalPosition}");
            }
            else
            {
                GD.PrintErr("[HexMapUI] SilverKeyProgressIndicator 没有找到!");
            }

            if (_healthBar != null)
            {
                PositionHealthBar();
                GD.Print($"[HexMapUI] 血条位置已调整: {_healthBar.GlobalPosition}");
            }

            UpdateDeathResistanceDisplay(GetCurrentDeathResistance());

            // 通用事件弹窗（标题 / 主图 / 文案 / 多个选项），数据来自 Data/events.json
            _eventDialog = GetNodeOrNull<FishEatFish.UI.EventDialog>("EventDialog");
            if (_eventDialog == null)
            {
                var eventScene = GD.Load<PackedScene>("res://Scenes/UI/EventDialog.tscn");
                if (eventScene != null)
                {
                    var eventNode = eventScene.Instantiate();
                    AddChild(eventNode);
                    _eventDialog = eventNode as FishEatFish.UI.EventDialog;
                }
            }
            if (_eventDialog != null)
            {
                _eventDialog.OnOptionSelected += OnEventOptionSelected;
            }

            _failurePanel = GetNodeOrNull<Control>("FailurePanel");
            if (_failurePanel == null)
            {
                var failureScene = GD.Load<PackedScene>("res://Scenes/UI/FailurePanel.tscn");
                if (failureScene != null)
                {
                    var failureNode = failureScene.Instantiate();
                    AddChild(failureNode);
                    _failurePanel = failureNode as Control;
                    if (_failurePanel != null)
                    {
                        _failurePanel.Position = Vector2.Zero;
                    }
                }
            }
            if (_failurePanel != null)
            {
                var panel = _failurePanel as FishEatFish.UI.FailurePanel.FailurePanel;
                if (panel != null)
                {
                    panel.OnOkPressed += OnFailureOkPressed;
                }
            }

            _backpackUI = GetNodeOrNull<FishEatFish.UI.BackpackUI.BackpackUI>("BackpackContainer");
            if (_backpackUI == null)
            {
                var backpackScene = GD.Load<PackedScene>("res://Scenes/UI/BackpackUI.tscn");
                if (backpackScene != null)
                {
                    var backpackNode = backpackScene.Instantiate();
                    AddChild(backpackNode);
                    _backpackUI = backpackNode as FishEatFish.UI.BackpackUI.BackpackUI;
                    if (_backpackUI != null)
                    {
                        _backpackUI.Refresh();
                        GD.Print($"[HexMapUI] BackpackUI loaded successfully");
                    }
                }
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

            if (_silverKeyProgressIndicator != null)
            {
                var silverKeyY = _silverKeyProgressIndicator.GlobalPosition.Y;
                var silverKeyBottom = silverKeyY + _silverKeyProgressIndicator.Size.Y;
                var healthBarHeight = _healthBar.CustomMinimumSize.Y;
                var targetPos = new Vector2(_silverKeyProgressIndicator.GlobalPosition.X + _silverKeyProgressIndicator.Size.X + 10, silverKeyBottom - healthBarHeight - 20);
                _healthBar.GlobalPosition = targetPos;
            }
            else
            {
                var targetPos = new Vector2(20, screenSize.Y - _healthBar.CustomMinimumSize.Y - 20);
                _healthBar.GlobalPosition = targetPos;
            }
        }

        private void PositionSilverKeyProgressIndicator()
        {
            if (_silverKeyProgressIndicator == null) return;
            var screenSize = GetViewportRect().Size;
            var silverKeySize = _silverKeyProgressIndicator.CustomMinimumSize;
            _silverKeyProgressIndicator.Size = silverKeySize;
            var targetPos = new Vector2(10, screenSize.Y - silverKeySize.Y - 10);
            _silverKeyProgressIndicator.GlobalPosition = targetPos;
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
            // y=132：让开顶部公共 HUD（CommonHUD）的统计位，HUD 高 120
            topRightContainer.GlobalPosition = new Vector2(screenSize.X - buttonContainerWidth - 20, 132);
        }

        public override void _EnterTree()
        {
            base._EnterTree();

            // Godot 4 已移除 SceneTree.screen_resized，窗口尺寸变化由 Viewport.SizeChanged 通知
            GetViewport().SizeChanged += OnScreenResized;
        }

        public override void _ExitTree()
        {
            if (GetViewport() != null)
            {
                GetViewport().SizeChanged -= OnScreenResized;
            }

            // BlackMarkShopManager 是 autoload（跨场景存活），事件必须在这里退订：
            // 否则本场景的 HexMapUI 被释放后，autoload 的委托里还指着它，
            // 下次触发 OnBlackMarkChanged 就会访问已释放的 Label（ObjectDisposedException）。
            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.OnBlackMarkChanged -= UpdateBlackMarkDisplay;
            }

            base._ExitTree();
        }

        private void OnScreenResized()
        {
            PositionHealthBar();
            PositionSilverKeyProgressIndicator();
            PositionRageCircles();
            PositionTopRightButtons();
        }

        private void SetupEventConnections()
        {
            if (_controller != null)
            {
                _controller.OnPlayerMoved += OnPlayerMoved;
            _controller.OnRevealedChanged += UpdateFogAndHighlight;
                _controller.OnPlayerTeleported += OnPlayerTeleported;
                _controller.OnTileTriggered += OnTileTriggered;
                _controller.OnTeleportTriggered += OnTeleportTriggered;
                _controller.OnHealthChanged += OnHealthChanged;
                _controller.OnBlackMarkChanged += OnBlackMarkChanged;
                _controller.OnSilverKeyChanged += OnSilverKeyChanged;
                _controller.OnMapCompleted += OnMapCompleted;
                _controller.OnChallengeFailed += OnChallengeFailed;
                _controller.OnShopOpened += OnShopOpened;
                _controller.OnShopClosed += OnShopClosed;
            }

            if (_playerIcon != null)
            {
                _playerIcon.OnMoveCompleted += OnPlayerMoveCompleted;
            }

            if (BlackMarkShopManager.Instance != null)
            {
                BlackMarkShopManager.Instance.OnBlackMarkChanged += UpdateBlackMarkDisplay;
                UpdateBlackMarkDisplay(BlackMarkShopManager.Instance.BlackMarkCount);
            }

            if (HexEventManager.Instance != null)
            {
                HexEventManager.Instance.OnBlackMarkGained += OnBlackMarkGained;
                HexEventManager.Instance.OnEventTriggered += OnMapEventTriggered;
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

            // 初始照亮玩家所在位置周围一圈（否则开局全黑）
            _controller.RevealTiles(_controller.CurrentPosition, 1);
            UpdateFogAndHighlight();

            if (_playerIcon != null)
            {
                _playerIcon.GetParent().MoveChild(_playerIcon, -1);
            }

            CenterOnPlayer(_controller.CurrentPosition, false);
            UpdatePlayerPosition();
            UpdatePlayerCurrentTile(_controller.CurrentPosition);
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

            // 战争迷雾：未照亮的格子显示为黑色
            tileView.SetFogged(!(_controller?.IsRevealed(tile.Coord) ?? false));

            _tileViews[tile.Coord] = tileView;
        }

        /// <summary>
        /// 刷新迷雾与「可交互高亮」
        ///   - 迷雾：只有已照亮的格子可见
        ///   - 高亮：玩家相邻的一环格子亮青色边框（对照参考图 R-17）
        /// </summary>
        private void UpdateFogAndHighlight()
        {
            if (_controller == null) return;

            HashSet<HexCoord> neighbors = new HashSet<HexCoord>();
            foreach (HexCoord n in _controller.CurrentPosition.GetNeighbors())
            {
                neighbors.Add(n);
            }

            foreach (var pair in _tileViews)
            {
                HexTileView view = pair.Value;
                HexTile tile = view.Tile;
                if (tile == null) continue;

                view.SetFogged(!_controller.IsRevealed(tile.Coord));
                view.SetHighlight(neighbors.Contains(tile.Coord));
            }
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
            if (_isInteractionBlocked) return;
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
            if (_isInteractionBlocked) return;
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

		    /// <summary>
		    /// 踩到探照灯：弹出事件面板（复用 D7 的通用 EventDialog），选「打开开关」后驱散半径 3 格的迷雾
		    /// </summary>
		    /// <summary>
		    /// 地图事件触发（来自 HexEventManager）。
		    /// 注意：不能用 controller 的 OnTileTriggered —— 那里 tile.Trigger() 已经把一次性事件的
		    /// EventType 清成 Empty 了，探照灯就认不出来。
		    /// </summary>
		    private void OnMapEventTriggered(string eventName, string coord)
		    {
		        GD.Print($"[HexMapUI] OnMapEventTriggered: {eventName} @ {coord}");

		        if (eventName == "searchlight")
		        {
		            ShowSearchlightEvent();
		        }
		    }
		    private void ShowSearchlightEvent()
		    {
		        if (_eventDialog == null)
		        {
		            GD.PrintErr("[HexMapUI] ShowSearchlightEvent: 事件弹窗不存在");
		            return;
		        }

		        _eventDialog.OnOptionSelected -= OnSearchlightOptionSelected;
		        _eventDialog.OnOptionSelected += OnSearchlightOptionSelected;
		        _eventDialog.ShowEvent(FishEatFish.Battle.MapEventData.Load("searchlight"));
		        SetInteractionBlocked(true);
		    }

		    private void OnSearchlightOptionSelected(int index)
		    {
		        GD.Print($"[HexMapUI] OnSearchlightOptionSelected: index={index}");
		        SetInteractionBlocked(false);

		        // 探照灯打开开关：驱散半径 3 格的迷雾（主人确认：3 格）
		        _controller?.RevealTiles(_controller.CurrentPosition, 3);
		        UpdateFogAndHighlight();
		    }
        private void OnPlayerMoved(HexCoord newPos)
        {
            if (_isInteractionBlocked) return;
            if (_playerIcon == null) return;

            CenterOnPlayer(newPos, true);

            // 走到哪照亮哪（周围一圈）+ 刷新迷雾与高亮
            _controller?.RevealTiles(newPos, 1);
            UpdateFogAndHighlight();

            _pendingMoveTarget = newPos;

            var iconSize = _playerIcon.Size == Vector2.Zero
                ? _playerIcon.CustomMinimumSize
                : _playerIcon.Size;

            var playerWorldPos = HexToWorld(newPos);
            var tileCenterPos = playerWorldPos + _hexSize / 2 - iconSize / 2;
            _playerIcon.MoveTo(tileCenterPos);

            ClearPathHighlights();
        }

        private void OnPlayerMoveCompleted()
        {
            if (_pendingMoveTarget.HasValue)
            {
                UpdatePlayerCurrentTile(_pendingMoveTarget.Value);
                _pendingMoveTarget = null;
            }
        }

        private void OnPlayerTeleported(HexCoord newPos)
        {
            if (_playerIcon == null) return;

            CenterOnPlayer(newPos, false);

            var iconSize = _playerIcon.Size == Vector2.Zero
                ? _playerIcon.CustomMinimumSize
                : _playerIcon.Size;

            var playerWorldPos = HexToWorld(newPos);
            var tileCenterPos = playerWorldPos + _hexSize / 2 - iconSize / 2;
            _playerIcon.TeleportTo(tileCenterPos);

            UpdatePlayerCurrentTile(newPos);
            ClearPathHighlights();
        }

        private void UpdatePlayerCurrentTile(HexCoord playerPos)
        {
            foreach (var view in _tileViews.Values)
            {
                view.SetPlayerCurrent(false);
            }

            if (_tileViews.ContainsKey(playerPos))
            {
                _tileViews[playerPos].SetPlayerCurrent(true);
            }
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
            // 触发战斗/传送等会切场景，此时本 UI 可能已脱离场景树，不能再操作节点
            if (!IsInsideTree())
            {
                GD.Print("[HexMapUI] OnTileTriggered: UI 已脱离场景树，跳过处理");
                return;
            }

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

            UpdateDeathResistanceDisplay(GetCurrentDeathResistance());
            ShowEventNotification(tile);
        }

        private void ShowEventNotification(HexTile tile)
        {
            if (!IsInsideTree()) return;

            var notification = new Label();
            notification.Text = tile.DisplayName ?? GetEventDisplayName(tile.EventType);
            notification.HorizontalAlignment = HorizontalAlignment.Center;
            // y=140：让开顶部公共 HUD（CommonHUD 高 120），否则文字会压在 HUD 上
            notification.Position = new Vector2(GetViewportRect().Size.X / 2 - 100, 140);
            notification.AddThemeFontSizeOverride("font_size", 20);
            notification.Modulate = Colors.White;
            AddChild(notification);

            var tween = CreateTween();
            tween.TweenProperty(notification, "position:y", 120, 1.0f);
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
                HexEventType.OneDirectionTele => "单向传送门",
                _ => "事件"
            };
        }

        private void OnTeleportTriggered(HexCoord position)
        {
            GD.Print($"[HexMapUI] OnTeleportTriggered: {position}");
            ShowEventDialog("teleport_two_way");
        }

        /// <summary>
        /// 显示一个事件弹窗（按事件 id 从 Data/events.json 取文案）
        /// </summary>
        private void ShowEventDialog(string eventId)
        {
            if (_eventDialog == null)
            {
                GD.PrintErr($"[HexMapUI] ShowEventDialog: 事件弹窗不存在，事件 {eventId} 不会弹出");
                return;
            }

            _eventDialog.ShowEvent(FishEatFish.Battle.MapEventData.Load(eventId));
            SetInteractionBlocked(true);
        }

        public void SetInteractionBlocked(bool blocked)
        {
            GD.Print($"[HexMapUI] SetInteractionBlocked called: blocked={blocked}");
            _isInteractionBlocked = blocked;
        }

        /// <summary>
        /// 事件弹窗选项回调：第 0 项 = 确认（进入），第 1 项 = 取消（离开）
        /// </summary>
        private void OnEventOptionSelected(int index)
        {
            GD.Print($"[HexMapUI] OnEventOptionSelected: index={index}");
            SetInteractionBlocked(false);

            if (index == 0)
            {
                _controller?.ConfirmTeleport();
            }
            else
            {
                _controller?.CancelTeleport();
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            _healthBar?.SetHealth(current, max);
        }

        private void OnBlackMarkGained(string coord, int amount)
        {
            GD.Print($"[HexMapUI] OnBlackMarkGained called: coord={coord}, amount={amount}");

            var popupScene = GD.Load<PackedScene>("res://Scenes/UI/BlackMarkPopup.tscn");
            if (popupScene == null)
            {
                GD.PrintErr($"[HexMapUI] OnBlackMarkGained: failed to load BlackMarkPopup scene!");
                return;
            }

            var popup = popupScene.Instantiate<FishEatFish.UI.HexMap.BlackMarkPopup>();
            if (popup == null)
            {
                GD.PrintErr($"[HexMapUI] OnBlackMarkGained: failed to instantiate BlackMarkPopup!");
                return;
            }

            var screenSize = GetViewportRect().Size;
            popup.Position = new Vector2((screenSize.X - popup.CustomMinimumSize.X) / 2, (screenSize.Y - popup.CustomMinimumSize.Y) / 2);
            AddChild(popup);

            _isInteractionBlocked = true;
            popup.OnClosed += () =>
            {
                _isInteractionBlocked = false;
                GD.Print($"[HexMapUI] BlackMarkPopup closed, interaction enabled");
            };

            GD.Print($"[HexMapUI] OnBlackMarkGained: popup added, Position={popup.Position}, Size={popup.Size}, CustomMinimumSize={popup.CustomMinimumSize}, Visible={popup.Visible}, Modulate={popup.Modulate}");

            popup.ShowPopup(amount);

            GD.Print($"[HexMapUI] OnBlackMarkGained completed");
        }

        private void OnBlackMarkChanged(int amount)
        {
            UpdateBlackMarkDisplay(amount);
        }

        private void OnSilverKeyChanged(int current, int max)
        {
            GD.Print($"[HexMapUI] OnSilverKeyChanged called: current={current}, max={max}");
            if (_silverKeyProgressIndicator != null)
            {
                GD.Print($"[HexMapUI] 设置银钥进度条: current={current}, max={max}");
                _silverKeyProgressIndicator.SetValue(current, true);
                _silverKeyProgressIndicator.SetMaxValue(max);
                _silverKeyProgressIndicator.QueueRedraw();
            }
            else
            {
                GD.PrintErr("[HexMapUI] OnSilverKeyChanged: _silverKeyProgressIndicator 是 null!");
            }
        }

        private void UpdateBlackMarkDisplay(int amount)
        {
            // 双保险：即使事件退订漏了，也不要碰已释放的节点
            if (_blackMarkLabel != null && GodotObject.IsInstanceValid(_blackMarkLabel))
            {
                _blackMarkLabel.Text = $"{amount}";
            }
        }

        private void UpdateDeathResistanceDisplay(int amount)
        {
            if (_deathResistanceLabel != null)
            {
                _deathResistanceLabel.Text = $"{amount}%";
            }
        }

        private int GetCurrentDeathResistance()
        {
            // 地图场景里没有 BattleManager（且原来的绝对路径 /root/BattleManager/Attacker 也是错的），
            // 直接用已选角色的死亡抵抗
            if (GlobalData.SelectedCharacters != null)
            {
                foreach (FishEatFish.Battle.CharacterSystem.CharacterDefinition character in GlobalData.SelectedCharacters)
                {
                    if (character != null)
                    {
                        return character.BaseDeathResistance;
                    }
                }
            }
            return 10;
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
            GD.Print($"[HexMapUI] OnShopOpened called: _shopUI={_shopUI}");

            if (_shopUI == null)
            {
                GD.Print($"[HexMapUI] OnShopOpened: ShopUI not initialized, loading now...");
                GD.Print($"[HexMapUI] OnShopOpened: Loading ShopUI.tscn...");
                var shopScene = GD.Load<PackedScene>("res://Scenes/UI/ShopUI.tscn");
                GD.Print($"[HexMapUI] OnShopOpened: shopScene loaded: {shopScene != null}");

                if (shopScene != null)
                {
                    GD.Print($"[HexMapUI] OnShopOpened: Instantiating scene...");
                    var shopNode = shopScene.Instantiate();
                    GD.Print($"[HexMapUI] OnShopOpened: shopNode instantiated: {shopNode != null}, type={shopNode?.GetType().Name}");

                    if (shopNode != null)
                    {
                        GD.Print($"[HexMapUI] OnShopOpened: Adding to tree...");
                        AddChild(shopNode);
                        _shopUI = shopNode as FishEatFish.UI.ShopUI.ShopUI;
                        GD.Print($"[HexMapUI] OnShopOpened: ShopUI loaded: {_shopUI != null}");

                        if (_shopUI != null)
                        {
                            _shopUI.SetHexMapUI(this);
                            _shopUI.OnEngravingItemConfirmed += OnEngravingItemConfirmed;
                            _shopUI.OnArtifactItemConfirmed += OnArtifactItemConfirmed;
                            _shopUI.OnEngravingCompleted += OnEngravingCompleted;
                            _shopUI.OnCloseClicked += OnShopClosePressed;
                            _shopUI.OnRefreshClicked += OnShopRefreshClicked;
                        }
                    }
                }
            }

            if (_shopUI == null)
            {
                GD.PrintErr($"[HexMapUI] OnShopOpened: Failed to load ShopUI!");
                return;
            }

            int refreshCount = 2;
            if (_controller?.CurrentMap != null)
            {
                var currentTile = _controller.CurrentMap.GetTile(_controller.CurrentPosition);
                if (currentTile != null && currentTile.EventType == HexEventType.Shop)
                {
                    refreshCount = currentTile.ShopRefreshCount;
                }
            }

            _shopUI.SetRefreshCount(refreshCount);
            _shopUI.RefreshShopItems(BlackMarkShopManager.Instance?.CurrentShopItems ?? new List<ShopItem>());
            _shopUI.ShowShop();

            GD.Print($"[HexMapUI] OnShopOpened completed, refreshCount={refreshCount}");
        }

        private void OnShopClosed()
        {
            if (_shopUI != null)
            {
                if (_controller?.CurrentMap != null)
                {
                    var currentTile = _controller.CurrentMap.GetTile(_controller.CurrentPosition);
                    if (currentTile != null && currentTile.EventType == HexEventType.Shop)
                    {
                        currentTile.ShopRefreshCount = _shopUI.GetRefreshCount();
                    }
                }
                _shopUI.HideShop();
            }
        }

        private void OnShopClosePressed()
        {
            _controller?.CloseShop();
        }

        private void OnShopRefreshClicked(int current, int max)
        {
            GD.Print($"[HexMapUI] OnShopRefreshClicked: current={current}, max={max}");

            if (_shopUI != null && _shopUI.TrySpendRefresh())
            {
                BlackMarkShopManager.Instance.RegenerateShopItems();
                _shopUI.RefreshShopItems(BlackMarkShopManager.Instance?.CurrentShopItems ?? new List<ShopItem>());
                UpdateBlackMarkDisplay(BlackMarkShopManager.Instance?.BlackMarkCount ?? 0);
            }
        }

        private void OnMapCompleted()
        {
            GD.Print("[HexMapUI] 地图探索完成!");
        }

        private void OnChallengeFailed()
        {
            var panel = _failurePanel as FishEatFish.UI.FailurePanel.FailurePanel;
            if (panel != null)
            {
                panel.ShowFailure("挑战失败！\n您选择了返回而未跳过地图");
            }
        }

        private void OnFailureOkPressed()
        {
            var panel = _failurePanel as FishEatFish.UI.FailurePanel.FailurePanel;
            if (panel != null)
            {
                panel.HideFailure();
            }
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
