using Godot;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.HexMap;

namespace FishEatFish.Scenes
{
    public partial class LevelMap : Control
    {
        private VBoxContainer _mapContainer;
        private Button _backButton;
        private Button _battleButton;

        private MapDefinition _selectedMap;
        private MapDefinition[] _availableMaps;

        public override void _Ready()
        {
            _mapContainer = GetNode<VBoxContainer>("SafeArea/Content/MapList/MapContainer");
            _backButton = GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/BackButton");
            _battleButton = GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/BattleButton");

            _backButton.Pressed += OnBackPressed;
            _battleButton.Pressed += OnBattlePressed;

            LoadMaps();
            BuildMapList();
        }

        private void LoadMaps()
        {
            _availableMaps = MapDefinition.GetAllMaps();
        }

        private void BuildMapList()
        {
            foreach (var map in _availableMaps)
            {
                CreateMapCard(map);
            }
        }

        private void CreateMapCard(MapDefinition map)
        {
            Panel card = new Panel();
            card.CustomMinimumSize = new Vector2(0, 120);
            card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            StyleBoxFlat normalStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.15f, 0.18f, 0.25f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderColor = new Color(0.3f, 0.35f, 0.45f),
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomRight = 12,
                CornerRadiusBottomLeft = 12,
                ContentMarginLeft = 20,
                ContentMarginTop = 15,
                ContentMarginRight = 20,
                ContentMarginBottom = 15
            };
            card.AddThemeStyleboxOverride("normal", normalStyle);

            HBoxContainer content = new HBoxContainer();
            content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            card.AddChild(content);

            VBoxContainer info = new VBoxContainer();
            info.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            info.Alignment = VBoxContainer.AlignmentMode.Center;
            content.AddChild(info);

            Label nameLabel = new Label();
            nameLabel.Text = map.Name;
            nameLabel.AddThemeFontSizeOverride("font_size", 24);
            info.AddChild(nameLabel);

            Label descLabel = new Label();
            descLabel.Text = map.Description;
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            descLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            info.AddChild(descLabel);

            VBoxContainer stats = new VBoxContainer();
            stats.Alignment = VBoxContainer.AlignmentMode.Center;
            stats.CustomMinimumSize = new Vector2(200, 0);
            content.AddChild(stats);

            Label difficultyLabel = new Label();
            difficultyLabel.Text = $"难度: {GetDifficultyText(map.Difficulty)}";
            difficultyLabel.HorizontalAlignment = HorizontalAlignment.Right;
            stats.AddChild(difficultyLabel);

            Label battleLabel = new Label();
            battleLabel.Text = $"战斗: {map.BattleCount}场";
            battleLabel.HorizontalAlignment = HorizontalAlignment.Right;
            stats.AddChild(battleLabel);

            Label rewardLabel = new Label();
            rewardLabel.Text = $"奖励: {map.GoldReward}金币";
            rewardLabel.HorizontalAlignment = HorizontalAlignment.Right;
            stats.AddChild(rewardLabel);

            Button selectButton = new Button();
            selectButton.Text = "选择";
            selectButton.CustomMinimumSize = new Vector2(100, 50);
            selectButton.Pressed += () => OnMapSelected(map);
            content.AddChild(selectButton);

            _mapContainer.AddChild(card);
        }

        private string GetDifficultyText(MapDifficulty difficulty)
        {
            return difficulty switch
            {
                MapDifficulty.Easy => "简单",
                MapDifficulty.Normal => "普通",
                MapDifficulty.Hard => "困难",
                _ => "未知"
            };
        }

        private void OnMapSelected(MapDefinition map)
        {
            _selectedMap = map;
            GD.Print($"选择了地图: {map.Name}");

            MapManager mapManager = new MapManager();
            mapManager.StartMap(map);

            GlobalData.CurrentMap = map;
            GetTree().ChangeSceneToFile("res://Scenes/CharacterSelection.tscn");
        }

        private void OnBackPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }

        private void OnBattlePressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
        }
    }

    public static class GlobalData
    {
        public static MapDefinition CurrentMap { get; set; }
        public static CharacterDefinition[] SelectedCharacters { get; set; }
        public static KeyOrder EquippedKeyOrder { get; set; }
        public static bool EnterHexMapMode { get; set; } = false;
    }
}
