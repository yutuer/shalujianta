using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;

public partial class CharacterSelection : Control
{
    private GridContainer _characterContainer;
    private Label _selectedTitle;
    private HBoxContainer _selectedSlots;
    private HBoxContainer _keyOrderContainer;
    private Panel _keyOrderSlot;
    private Label _healthStat;
    private Label _attackStat;
    private Label _defenseStat;
    private Label _energyStat;
    private Button _backButton;
    private Button _startButton;

    private CharacterDefinition[] _allCharacters;
    private List<CharacterDefinition> _selectedCharacters = new List<CharacterDefinition>();
    private List<KeyOrder> _availableKeyOrders = new List<KeyOrder>();
    private KeyOrder _selectedKeyOrder;
    private const int MaxSelection = 4;

    private List<Label> _slotIcons = new List<Label>();
    private List<Label> _slotLabels = new List<Label>();
    private Label _keyOrderNameLabel;
    private Label _keyOrderDescLabel;

    public override void _Ready()
    {
        _characterContainer = GetNode<GridContainer>("SafeArea/Content/MainContent/CharacterGrid/CharacterContainer");
        _selectedTitle = GetNode<Label>("SafeArea/Content/MainContent/RightPanel/SelectedPanel/SelectedMargin/SelectedContent/SelectedTitle");
        _selectedSlots = GetNode<HBoxContainer>("SafeArea/Content/MainContent/RightPanel/SelectedPanel/SelectedMargin/SelectedContent/SelectedSlots");
        _keyOrderContainer = GetNode<HBoxContainer>("SafeArea/Content/MainContent/RightPanel/KeyOrderPanel/KeyOrderMargin/KeyOrderContent/KeyOrderContainer");
        _keyOrderSlot = GetNode<Panel>("SafeArea/Content/MainContent/RightPanel/KeyOrderPanel/KeyOrderMargin/KeyOrderContent/KeyOrderSlot");
        _healthStat = GetNode<Label>("SafeArea/Content/MainContent/RightPanel/StatsPanel/StatsMargin/StatsContent/HealthStat");
        _attackStat = GetNode<Label>("SafeArea/Content/MainContent/RightPanel/StatsPanel/StatsMargin/StatsContent/AttackStat");
        _defenseStat = GetNode<Label>("SafeArea/Content/MainContent/RightPanel/StatsPanel/StatsMargin/StatsContent/DefenseStat");
        _energyStat = GetNode<Label>("SafeArea/Content/MainContent/RightPanel/StatsPanel/StatsMargin/StatsContent/EnergyStat");
        _backButton = GetNode<Button>("SafeArea/Content/BottomBar/BackButton");
        _startButton = GetNode<Button>("SafeArea/Content/BottomBar/StartButton");

        _backButton.Pressed += OnBackPressed;
        _startButton.Pressed += OnStartPressed;

        _allCharacters = CharacterDefinition.GetAllCharacters();
        LoadKeyOrders();
        BuildCharacterGrid();
        BuildSelectedSlots();
        BuildKeyOrderSelection();
        UpdateStats();
    }

    private void LoadKeyOrders()
    {
        _availableKeyOrders.Add(KeyOrder.CreateSilverSlash());
        _availableKeyOrders.Add(KeyOrder.CreateKeyShield());
        _availableKeyOrders.Add(KeyOrder.CreateEnergyInfusion());
        _availableKeyOrders.Add(KeyOrder.CreateArmorBreak());
        _availableKeyOrders.Add(KeyOrder.CreateLifeKey());
    }

    private void BuildCharacterGrid()
    {
        foreach (var character in _allCharacters)
        {
            CreateCharacterButton(character);
        }
    }

    private void CreateCharacterButton(CharacterDefinition character)
    {
        Panel card = new Panel();
        card.CustomMinimumSize = new Vector2(120, 160);
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

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
            ContentMarginLeft = 8,
            ContentMarginTop = 8,
            ContentMarginRight = 8,
            ContentMarginBottom = 8
        };
        card.AddThemeStyleboxOverride("normal", normalStyle);

        StyleBoxFlat hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
        hoverStyle.BorderColor = new Color(0.5f, 0.6f, 0.8f);
        card.AddThemeStyleboxOverride("hover", hoverStyle);

        VBoxContainer content = new VBoxContainer();
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.Alignment = VBoxContainer.AlignmentMode.Center;
        card.AddChild(content);

        Label iconLabel = new Label();
        iconLabel.Text = GetCharacterEmoji(character.CharacterId);
        iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
        iconLabel.AddThemeFontSizeOverride("font_size", 48);
        content.AddChild(iconLabel);

        Label nameLabel = new Label();
        nameLabel.Text = character.Name;
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        content.AddChild(nameLabel);

        Label statsLabel = new Label();
        statsLabel.Text = $"HP:{character.BaseHealth} ATK:{character.BaseAttack}";
        statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statsLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        statsLabel.AddThemeFontSizeOverride("font_size", 12);
        content.AddChild(statsLabel);

        Button selectBtn = new Button();
        selectBtn.Text = "选择";
        selectBtn.CustomMinimumSize = new Vector2(80, 32);
        content.AddChild(selectBtn);

        CharacterDefinition capturedChar = character;
        selectBtn.Pressed += () => OnCharacterSelected(capturedChar);

        _characterContainer.AddChild(card);
    }

    private string GetCharacterEmoji(string characterId)
    {
        return characterId switch
        {
            "rat" => "🐭",
            "ox" => "🐂",
            "tiger" => "🐯",
            "rabbit" => "🐰",
            "dragon" => "🐲",
            "snake" => "🐍",
            "horse" => "🐴",
            "goat" => "🐐",
            "monkey" => "🐵",
            "rooster" => "🐔",
            "dog" => "🐶",
            "pig" => "🐷",
            _ => "❓"
        };
    }

    private void BuildSelectedSlots()
    {
        for (int i = 0; i < MaxSelection; i++)
        {
            Panel slot = new Panel();
            slot.CustomMinimumSize = new Vector2(60, 80);
            slot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            StyleBoxFlat slotStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.1f, 0.12f, 0.18f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderColor = new Color(0.25f, 0.3f, 0.4f),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusBottomLeft = 8
            };
            slot.AddThemeStyleboxOverride("normal", slotStyle);

            VBoxContainer slotContent = new VBoxContainer();
            slotContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            slotContent.Alignment = VBoxContainer.AlignmentMode.Center;
            slot.AddChild(slotContent);

            Label iconLabel = new Label();
            iconLabel.Text = "·";
            iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
            iconLabel.AddThemeFontSizeOverride("font_size", 32);
            slotContent.AddChild(iconLabel);
            _slotIcons.Add(iconLabel);

            Label nameLabel = new Label();
            nameLabel.Text = "-";
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.AddThemeFontSizeOverride("font_size", 14);
            slotContent.AddChild(nameLabel);
            _slotLabels.Add(nameLabel);

            _selectedSlots.AddChild(slot);
        }
    }

    private void BuildKeyOrderSelection()
    {
        foreach (var keyOrder in _availableKeyOrders)
        {
            CreateKeyOrderButton(keyOrder);
        }

        VBoxContainer slotContent = new VBoxContainer();
        slotContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        slotContent.Alignment = VBoxContainer.AlignmentMode.Center;
        _keyOrderSlot.AddChild(slotContent);

        Label keyIconLabel = new Label();
        keyIconLabel.Text = "🔑";
        keyIconLabel.HorizontalAlignment = HorizontalAlignment.Center;
        keyIconLabel.AddThemeFontSizeOverride("font_size", 32);
        slotContent.AddChild(keyIconLabel);

        _keyOrderNameLabel = new Label();
        _keyOrderNameLabel.Text = "选择钥令";
        _keyOrderNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _keyOrderNameLabel.AddThemeFontSizeOverride("font_size", 14);
        slotContent.AddChild(_keyOrderNameLabel);

        _keyOrderDescLabel = new Label();
        _keyOrderDescLabel.Text = "点击上方选择";
        _keyOrderDescLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _keyOrderDescLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
        _keyOrderDescLabel.AddThemeFontSizeOverride("font_size", 10);
        _keyOrderDescLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        slotContent.AddChild(_keyOrderDescLabel);
    }

    private void CreateKeyOrderButton(KeyOrder keyOrder)
    {
        Button button = new Button();
        button.CustomMinimumSize = new Vector2(80, 60);
        button.Text = keyOrder.Name;
        button.AddThemeFontSizeOverride("font_size", 12);

        StyleBoxFlat normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.25f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.4f, 0.4f, 0.5f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8
        };
        button.AddThemeStyleboxOverride("normal", normalStyle);

        KeyOrder capturedOrder = keyOrder;
        button.Pressed += () => OnKeyOrderSelected(capturedOrder);

        _keyOrderContainer.AddChild(button);
    }

    private void OnKeyOrderSelected(KeyOrder keyOrder)
    {
        _selectedKeyOrder = keyOrder;
        _keyOrderNameLabel.Text = keyOrder.Name;
        _keyOrderDescLabel.Text = keyOrder.Description;
        GD.Print($"选择了钥令: {keyOrder.Name}");
        UpdateStartButton();
    }

    private void OnCharacterSelected(CharacterDefinition character)
    {
        if (_selectedCharacters.Contains(character))
        {
            _selectedCharacters.Remove(character);
            GD.Print($"取消选择: {character.Name}");
        }
        else if (_selectedCharacters.Count < MaxSelection)
        {
            _selectedCharacters.Add(character);
            GD.Print($"选择角色: {character.Name}");
        }
        else
        {
            GD.Print("已达最大选择数量!");
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        _selectedTitle.Text = $"已选择角色 ({_selectedCharacters.Count}/{MaxSelection})";

        for (int i = 0; i < MaxSelection; i++)
        {
            if (i < _selectedCharacters.Count)
            {
                var character = _selectedCharacters[i];
                _slotIcons[i].Text = GetCharacterEmoji(character.CharacterId);
                _slotLabels[i].Text = character.Name;
            }
            else
            {
                _slotIcons[i].Text = "·";
                _slotLabels[i].Text = "-";
            }
        }

        UpdateStats();
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        _startButton.Disabled = _selectedCharacters.Count != MaxSelection || _selectedKeyOrder == null;
    }

    private void UpdateStats()
    {
        int totalHealth = 0;
        int totalAttack = 0;
        int totalDefense = 0;
        int totalEnergy = 0;

        foreach (var character in _selectedCharacters)
        {
            totalHealth += character.BaseHealth;
            totalAttack += character.BaseAttack;
            totalDefense += character.BaseDefense;
            totalEnergy += character.BaseEnergy;
        }

        _healthStat.Text = $"生命: {totalHealth}";
        _attackStat.Text = $"攻击: {totalAttack}";
        _defenseStat.Text = $"防御: {totalDefense}";
        _energyStat.Text = $"能量: {totalEnergy}";
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }

    private void OnStartPressed()
    {
        if (_selectedCharacters.Count != MaxSelection)
        {
            GD.Print("请选择4个角色!");
            return;
        }

        if (_selectedKeyOrder == null)
        {
            GD.Print("请选择一个钥令!");
            return;
        }

        GlobalData.SelectedCharacters = _selectedCharacters.ToArray();
        GlobalData.EquippedKeyOrder = _selectedKeyOrder;
        GD.Print($"选择完成，角色: {string.Join(", ", _selectedCharacters.ConvertAll(c => c.Name))}, 钥令: {_selectedKeyOrder.Name}");

        if (GlobalData.EnterHexMapMode)
        {
            GlobalData.EnterHexMapMode = false;
            GetTree().ChangeSceneToFile("res://Scenes/HexMapScene.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
        }
    }
}
