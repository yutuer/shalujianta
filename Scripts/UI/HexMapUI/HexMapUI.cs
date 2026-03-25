using System;
using System.Collections.Generic;
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
        private PackedScene _shopItemScene;

        private HexMapController _controller;

        private Control _mapContainer;
        private Control _tileViewsContainer;
        private PlayerIcon _playerIcon;
        private HealthBar _healthBar;
        private VBoxContainer _rageCirclesContainer;

        private Button _skipButton;
        private Button _deathResistanceButton;
        private Button _blackMarkButton;
        private Button _settingsButton;

        private Label _blackMarkLabel;

        private Control _teleportDialog;
        private Button _teleportConfirmButton;
        private Button _teleportCancelButton;
        private Label _teleportMessage;

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

        private Vector2 _hexSize = new Vector2(80, 70);
        private Vector2 _mapCenter = new Vector2(600, 400);

        public override void _Ready()
        {
            InitializeComponents();
            SetupEventConnections();

            _controller = HexMapController.Instance;
            if (_controller != null)
            {
                RefreshMap();
            }
        }

        private void InitializeComponents()
        {
            _mapContainer = new Control();
            _mapContainer.Name = "MapContainer";
            AddChild(_mapContainer);

            _tileViewsContainer = new Control();
            _tileViewsContainer.Name = "TileViews";
            _mapContainer.AddChild(_tileViewsContainer);

            _playerIcon = new PlayerIcon();
            _playerIcon.Name = "PlayerIcon";
            _mapContainer.AddChild(_playerIcon);

            _healthBar = new HealthBar();
            _healthBar.Name = "HealthBar";
            _healthBar.Position = new Vector2(20, 20);
            AddChild(_healthBar);

            _rageCirclesContainer = new VBoxContainer();
            _rageCirclesContainer.Name = "RageCircles";
            _rageCirclesContainer.Position = new Vector2(20, 150);
            _rageCirclesContainer.AddThemeConstantOverride("separation", 10);
            AddChild(_rageCirclesContainer);

            for (int i = 0; i < 4; i++)
            {
                var rageCircle = new RageCircle();
                rageCircle.Name = $"RageCircle{i}";
                rageCircle.SetCharacterName($"角色{i + 1}");
                _rageCircles.Add(rageCircle);
                _rageCirclesContainer.AddChild(rageCircle);
            }

            var topRightContainer = new HBoxContainer();
            topRightContainer.Name = "TopRightButtons";
            topRightContainer.Position = new Vector2(900, 20);
            topRightContainer.Alignment = BoxContainer.AlignmentMode.End;
            topRightContainer.AddThemeConstantOverride("separation", 10);
            AddChild(topRightContainer);

            _skipButton = new Button();
            _skipButton.Text = "跳过地图";
            _skipButton.CustomMinimumSize = new Vector2(100, 40);
            _skipButton.Pressed += OnSkipButtonPressed;
            topRightContainer.AddChild(_skipButton);

            _deathResistanceButton = new Button();
            _deathResistanceButton.Text = "死亡抵抗";
            _deathResistanceButton.CustomMinimumSize = new Vector2(100, 40);
            _deathResistanceButton.Pressed += OnDeathResistanceButtonPressed;
            topRightContainer.AddChild(_deathResistanceButton);

            _blackMarkButton = new Button();
            _blackMarkButton.Text = "黑印";
            _blackMarkButton.CustomMinimumSize = new Vector2(80, 40);
            _blackMarkButton.Pressed += OnBlackMarkButtonPressed;
            topRightContainer.AddChild(_blackMarkButton);

            _settingsButton = new Button();
            _settingsButton.Text = "⚙";
            _settingsButton.CustomMinimumSize = new Vector2(40, 40);
            _settingsButton.Pressed += OnSettingsButtonPressed;
            topRightContainer.AddChild(_settingsButton);

            _blackMarkLabel = new Label();
            _blackMarkLabel.Name = "BlackMarkLabel";
            _blackMarkLabel.Position = new Vector2(20, 500);
            _blackMarkLabel.AddThemeFontSizeOverride("font_size", 24);
            AddChild(_blackMarkLabel);

            InitializeTeleportDialog();
            InitializeShopUI();
            InitializeEngravingSelectUI();
            InitializeFailurePanel();
            InitializeBackpack();
        }

        private void InitializeTeleportDialog()
        {
            _teleportDialog = new PanelContainer();
            _teleportDialog.Name = "TeleportDialog";
            _teleportDialog.Visible = false;
            _teleportDialog.CustomMinimumSize = new Vector2(300, 150);
            _teleportDialog.Position = (GetViewportRect().Size - _teleportDialog.CustomMinimumSize) / 2;
            AddChild(_teleportDialog);

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = _teleportDialog.CustomMinimumSize;
            _teleportDialog.AddChild(vBox);

            _teleportMessage = new Label();
            _teleportMessage.Text = "是否要开始传送？";
            _teleportMessage.HorizontalAlignment = HorizontalAlignment.Center;
            _teleportMessage.VerticalAlignment = VerticalAlignment.Center;
            vBox.AddChild(_teleportMessage);

            var buttonBox = new HBoxContainer();
            buttonBox.Alignment = BoxContainer.AlignmentMode.Center;
            buttonBox.AddThemeConstantOverride("separation", 20);
            vBox.AddChild(buttonBox);

            _teleportCancelButton = new Button();
            _teleportCancelButton.Text = "否";
            _teleportCancelButton.CustomMinimumSize = new Vector2(100, 40);
            _teleportCancelButton.Pressed += OnTeleportCancelPressed;
            buttonBox.AddChild(_teleportCancelButton);

            _teleportConfirmButton = new Button();
            _teleportConfirmButton.Text = "是";
            _teleportConfirmButton.CustomMinimumSize = new Vector2(100, 40);
            _teleportConfirmButton.Pressed += OnTeleportConfirmPressed;
            buttonBox.AddChild(_teleportConfirmButton);
        }

        private void InitializeShopUI()
        {
            _shopContainer = new PanelContainer();
            _shopContainer.Name = "ShopContainer";
            _shopContainer.Visible = false;
            _shopContainer.CustomMinimumSize = new Vector2(700, 500);
            _shopContainer.Position = (GetViewportRect().Size - _shopContainer.CustomMinimumSize) / 2;
            AddChild(_shopContainer);

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = _shopContainer.CustomMinimumSize;
            _shopContainer.AddChild(vBox);

            var headerBox = new HBoxContainer();
            headerBox.Alignment = BoxContainer.AlignmentMode.End;
            vBox.AddChild(headerBox);

            _shopCloseButton = new Button();
            _shopCloseButton.Text = "X";
            _shopCloseButton.CustomMinimumSize = new Vector2(40, 40);
            _shopCloseButton.Pressed += OnShopClosePressed;
            headerBox.AddChild(_shopCloseButton);

            var shopTitle = new Label();
            shopTitle.Text = "神秘商店";
            shopTitle.HorizontalAlignment = HorizontalAlignment.Center;
            shopTitle.AddThemeFontSizeOverride("font_size", 24);
            vBox.AddChild(shopTitle);

            _shopItemsContainer = new GridContainer();
            _shopItemsContainer.Name = "ShopItems";
            _shopItemsContainer.Set("columns", 3);
            _shopItemsContainer.AddThemeConstantOverride("h_separation", 20);
            _shopItemsContainer.AddThemeConstantOverride("v_separation", 20);
            vBox.AddChild(_shopItemsContainer);
        }

        private void InitializeEngravingSelectUI()
        {
            _engravingSelectContainer = new PanelContainer();
            _engravingSelectContainer.Name = "EngravingSelectContainer";
            _engravingSelectContainer.Visible = false;
            _engravingSelectContainer.CustomMinimumSize = new Vector2(800, 600);
            _engravingSelectContainer.Position = (GetViewportRect().Size - _engravingSelectContainer.CustomMinimumSize) / 2;
            AddChild(_engravingSelectContainer);

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = _engravingSelectContainer.CustomMinimumSize;
            _engravingSelectContainer.AddChild(vBox);

            var headerBox = new HBoxContainer();
            headerBox.Alignment = BoxContainer.AlignmentMode.End;
            vBox.AddChild(headerBox);

            var closeButton = new Button();
            closeButton.Text = "X";
            closeButton.CustomMinimumSize = new Vector2(40, 40);
            closeButton.Pressed += OnEngravingClosePressed;
            headerBox.AddChild(closeButton);

            var selectTitle = new Label();
            selectTitle.Text = "选择要刻印的卡牌";
            selectTitle.HorizontalAlignment = HorizontalAlignment.Center;
            selectTitle.AddThemeFontSizeOverride("font_size", 24);
            vBox.AddChild(selectTitle);

            _engravingCardsContainer = new GridContainer();
            _engravingCardsContainer.Name = "EngravingCards";
            _engravingCardsContainer.Set("columns", 4);
            vBox.AddChild(_engravingCardsContainer);

            var buttonBox = new HBoxContainer();
            buttonBox.Alignment = BoxContainer.AlignmentMode.Center;
            vBox.AddChild(buttonBox);

            _engravingCancelButton = new Button();
            _engravingCancelButton.Text = "返回";
            _engravingCancelButton.CustomMinimumSize = new Vector2(100, 40);
            _engravingCancelButton.Pressed += OnEngravingCancelPressed;
            buttonBox.AddChild(_engravingCancelButton);

            _engravingConfirmButton = new Button();
            _engravingConfirmButton.Text = "确定刻印";
            _engravingConfirmButton.CustomMinimumSize = new Vector2(100, 40);
            _engravingConfirmButton.Pressed += OnEngravingConfirmPressed;
            _engravingConfirmButton.Disabled = true;
            buttonBox.AddChild(_engravingConfirmButton);
        }

        private void InitializeFailurePanel()
        {
            _failurePanel = new PanelContainer();
            _failurePanel.Name = "FailurePanel";
            _failurePanel.Visible = false;
            _failurePanel.CustomMinimumSize = new Vector2(400, 200);
            _failurePanel.Position = (GetViewportRect().Size - _failurePanel.CustomMinimumSize) / 2;
            AddChild(_failurePanel);

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = _failurePanel.CustomMinimumSize;
            _failurePanel.AddChild(vBox);

            _failureMessage = new Label();
            _failureMessage.Text = "挑战失败！";
            _failureMessage.HorizontalAlignment = HorizontalAlignment.Center;
            _failureMessage.VerticalAlignment = VerticalAlignment.Center;
            _failureMessage.AddThemeFontSizeOverride("font_size", 32);
            vBox.AddChild(_failureMessage);

            _failureOkButton = new Button();
            _failureOkButton.Text = "确定";
            _failureOkButton.CustomMinimumSize = new Vector2(100, 40);
            _failureOkButton.Pressed += OnFailureOkPressed;
            vBox.AddChild(_failureOkButton);
        }

        private void InitializeBackpack()
        {
            _backpackContainer = new PanelContainer();
            _backpackContainer.Name = "BackpackContainer";
            _backpackContainer.CustomMinimumSize = new Vector2(300, 60);
            _backpackContainer.Position = new Vector2((GetViewportRect().Size.X - 300) / 2, 20);
            AddChild(_backpackContainer);

            var hBox = new HBoxContainer();
            hBox.CustomMinimumSize = _backpackContainer.CustomMinimumSize;
            hBox.Alignment = BoxContainer.AlignmentMode.Center;
            hBox.AddThemeConstantOverride("separation", 10);
            _backpackContainer.AddChild(hBox);

            var backpackLabel = new Label();
            backpackLabel.Text = "背包:";
            hBox.AddChild(backpackLabel);

            _backpackItemsContainer = new HBoxContainer();
            _backpackItemsContainer.AddThemeConstantOverride("separation", 5);
            hBox.AddChild(_backpackItemsContainer);
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
            ClearTileViews();

            if (_controller?.CurrentMap == null) return;

            foreach (var tile in _controller.CurrentMap.GetAllTiles())
            {
                CreateTileView(tile);
            }

            UpdatePlayerPosition();
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
            var tileView = new HexTileView();
            tileView.SetTile(tile);
            tileView.Size = _hexSize;

            var worldPos = HexToWorld(tile.Coord);
            tileView.Position = worldPos;

            tileView.OnTileClicked += OnTileClicked;
            tileView.OnTileHovered += OnTileHovered;

            _tileViewsContainer.AddChild(tileView);
            _tileViews[tile.Coord] = tileView;
        }

        private Vector2 HexToWorld(HexCoord coord)
        {
            float x = _hexSize.X * (3f / 2f * coord.Q);
            float y = _hexSize.Y * (Mathf.Sqrt(3f) / 2f * coord.Q + Mathf.Sqrt(3f) * coord.R);

            return new Vector2(x, y) + _mapCenter - _hexSize / 2;
        }

        private HexCoord WorldToHex(Vector2 worldPos)
        {
            var pos = worldPos - _mapCenter + _hexSize / 2;

            float q = (2f / 3f * pos.X) / _hexSize.X;
            float r = (-1f / 3f * pos.X + Mathf.Sqrt(3f) / 3f * pos.Y) / _hexSize.Y;

            return HexCoord.Round(q, r);
        }

        private void OnTileClicked(HexTileView tileView)
        {
            if (_controller == null) return;

            var tile = tileView.Tile;
            if (tile == null || !tile.CanEnter) return;

            var currentPos = _controller.CurrentPosition;
            if (tile.Coord.ContainsInArray(currentPos.GetNeighbors()))
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
            UpdatePlayerPosition();

            if (_tileViews.ContainsKey(newPos))
            {
                var tileView = _tileViews[newPos];
                _playerIcon.MoveTo(tileView.GetHexWorldPosition());
            }

            ClearPathHighlights();
        }

        private void UpdatePlayerPosition()
        {
            if (_controller == null) return;

            var currentPos = _controller.CurrentPosition;
            if (_tileViews.ContainsKey(currentPos))
            {
                var tileView = _tileViews[currentPos];
                _playerIcon.TeleportTo(tileView.GetHexWorldPosition());
            }
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

        private Control CreateShopItemCard(ShopItem item)
        {
            var card = new PanelContainer();
            card.CustomMinimumSize = new Vector2(200, 280);
            card.AddThemeStyleboxOverride("panel", CreateCardStyle());

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = card.CustomMinimumSize;
            card.AddChild(vBox);

            var nameLabel = new Label();
            nameLabel.Text = item.Name;
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.AddThemeFontSizeOverride("font_size", 18);
            vBox.AddChild(nameLabel);

            var iconRect = new TextureRect();
            iconRect.CustomMinimumSize = new Vector2(100, 100);
            iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            if (item.Icon != null)
            {
                iconRect.Texture = GD.Load<Texture2D>(item.Icon);
            }
            vBox.AddChild(iconRect);

            var descLabel = new Label();
            descLabel.Text = item.Description;
            descLabel.HorizontalAlignment = HorizontalAlignment.Center;
            descLabel.AutowrapMode = TextServer.AutowrapMode.Word;
            descLabel.CustomMinimumSize = new Vector2(180, 60);
            vBox.AddChild(descLabel);

            var priceLabel = new Label();
            priceLabel.Text = $"💰 {item.Price}";
            priceLabel.HorizontalAlignment = HorizontalAlignment.Center;
            priceLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0));
            priceLabel.AddThemeFontSizeOverride("font_size", 20);
            vBox.AddChild(priceLabel);

            var buyButton = new Button();
            buyButton.Text = "购买";
            buyButton.CustomMinimumSize = new Vector2(180, 40);
            buyButton.Pressed += () => OnShopItemClicked(item);
            vBox.AddChild(buyButton);

            if (!BlackMarkShopManager.Instance.CanAfford(item))
            {
                buyButton.Disabled = true;
                priceLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            }

            return card;
        }

        private StyleBoxFlat CreateCardStyle()
        {
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.2f, 0.2f, 0.3f);
            style.BorderWidthLeft = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthTop = 2;
            style.BorderWidthBottom = 2;
            style.BorderColor = new Color(0.4f, 0.4f, 0.5f);
            style.CornerRadiusTopLeft = 10;
            style.CornerRadiusTopRight = 10;
            style.CornerRadiusBottomLeft = 10;
            style.CornerRadiusBottomRight = 10;
            return style;
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

        private Control CreateEngravingCardSlot(string cardId, string cardName)
        {
            var slot = new PanelContainer();
            slot.CustomMinimumSize = new Vector2(150, 200);
            slot.AddThemeStyleboxOverride("panel", CreateCardStyle());

            var vBox = new VBoxContainer();
            vBox.CustomMinimumSize = slot.CustomMinimumSize;
            slot.AddChild(vBox);

            var nameLabel = new Label();
            nameLabel.Text = cardName;
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            vBox.AddChild(nameLabel);

            var selectButton = new Button();
            selectButton.Text = "选择";
            selectButton.CustomMinimumSize = new Vector2(130, 40);
            selectButton.Pressed += () => OnEngravingCardSelected(slot, cardId);
            vBox.AddChild(selectButton);

            return slot;
        }

        private string _selectedEngravingCardId = null;

        private void OnEngravingCardSelected(Control cardSlot, string cardId)
        {
            _selectedEngravingCardId = cardId;
            _engravingConfirmButton.Disabled = false;

            foreach (var child in _engravingCardsContainer.GetChildren())
            {
                var panel = child as PanelContainer;
                if (panel != null)
                {
                    panel.Modulate = child == cardSlot ? new Color(1, 1, 0) : Colors.White;
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
