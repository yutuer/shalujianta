using Godot;

public partial class LevelMap : Control
{
    private readonly string[] nodeTypes =
    {
        "起点",
        "普通战斗",
        "事件",
        "商店",
        "精英",
        "篝火",
        "Boss"
    };

    private readonly string[] nodeDescriptions =
    {
        "整顿卡组并观察路线。",
        "基础敌人，适合稳定获取奖励。",
        "随机事件，可能带来收益或代价。",
        "消耗金币购买卡牌与遗物。",
        "高风险高收益，建议准备充足。",
        "恢复生命并可调整卡组。",
        "章节终点，胜利后进入下一段冒险。"
    };

    private readonly Color[] nodeColors =
    {
        new(0.28f, 0.65f, 0.93f),
        new(0.90f, 0.42f, 0.34f),
        new(0.71f, 0.52f, 0.92f),
        new(0.95f, 0.76f, 0.30f),
        new(0.81f, 0.24f, 0.24f),
        new(0.34f, 0.74f, 0.44f),
        new(0.98f, 0.55f, 0.18f)
    };

    private Label routeTitleLabel;
    private Label routeDescriptionLabel;
    private Label progressLabel;
    private Label hintLabel;
    private GridContainer nodeGrid;
    private VBoxContainer legendContainer;
    private OptionButton deckDropdown;
    private Button continueButton;

    private int selectedNodeIndex = 1;

    public override void _Ready()
    {
        routeTitleLabel = GetNode<Label>("SafeArea/Content/TopSection/Header/RouteTitle");
        routeDescriptionLabel = GetNode<Label>("SafeArea/Content/TopSection/Header/RouteDescription");
        progressLabel = GetNode<Label>("SafeArea/Content/TopSection/Sidebar/ProgressPanel/ProgressLabel");
        hintLabel = GetNode<Label>("SafeArea/Content/TopSection/Sidebar/HintPanel/HintLabel");
        nodeGrid = GetNode<GridContainer>("SafeArea/Content/TopSection/Header/MapCard/MapPanel/NodeGrid");
        legendContainer = GetNode<VBoxContainer>("SafeArea/Content/TopSection/Sidebar/LegendPanel/LegendMargin/LegendBox/LegendList");
        deckDropdown = GetNode<OptionButton>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/DeckPanel/DeckDropdown");
        continueButton = GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ActionButtons/ContinueButton");

        SetupDeckOptions();
        BuildMapNodes();
        BuildLegend();
        UpdateSelection(selectedNodeIndex);

        continueButton.Pressed += OnContinuePressed;
        GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ActionButtons/BattleButton").Pressed += OnBattlePressed;
        GetNode<Button>("SafeArea/Content/BottomBar/BottomMargin/BottomLayout/ActionButtons/BackButton").Pressed += OnBackPressed;
    }

    private void SetupDeckOptions()
    {
        if (deckDropdown.ItemCount > 0)
        {
            return;
        }

        deckDropdown.AddItem("均衡流派 - 攻守均衡，适合首次开局");
        deckDropdown.AddItem("防御流派 - 容错更高，推进更稳");
        deckDropdown.AddItem("攻击流派 - 输出爆发，但更依赖节奏");
        deckDropdown.AddItem("连击流派 - 依赖手牌联动与费用运营");
        deckDropdown.Selected = 0;
    }

    private void BuildMapNodes()
    {
        for (int i = 0; i < nodeTypes.Length; i++)
        {
            Button button = new Button
            {
                Text = $"{i + 1}\n{nodeTypes[i]}",
                CustomMinimumSize = new Vector2(0, 96),
                TooltipText = nodeDescriptions[i],
                Alignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Center,
                FocusMode = Control.FocusModeEnum.None,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };

            StyleBoxFlat style = new StyleBoxFlat
            {
                BgColor = nodeColors[i],
                CornerRadiusTopLeft = 18,
                CornerRadiusTopRight = 18,
                CornerRadiusBottomRight = 18,
                CornerRadiusBottomLeft = 18,
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderColor = Colors.White,
                ShadowColor = new Color(0, 0, 0, 0.18f),
                ShadowSize = 6,
                ContentMarginLeft = 8,
                ContentMarginTop = 8,
                ContentMarginRight = 8,
                ContentMarginBottom = 8
            };

            StyleBox hoverStyle = (StyleBox)style.Duplicate();
            StyleBox pressedStyle = (StyleBox)style.Duplicate();
            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", pressedStyle);

            int capturedIndex = i;
            button.Pressed += () => UpdateSelection(capturedIndex);
            nodeGrid.AddChild(button);
        }
    }

    private void BuildLegend()
    {
        for (int i = 0; i < nodeTypes.Length; i++)
        {
            HBoxContainer row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);

            ColorRect swatch = new ColorRect
            {
                Color = nodeColors[i],
                CustomMinimumSize = new Vector2(18, 18)
            };

            Label label = new Label
            {
                Text = $"{nodeTypes[i]}：{nodeDescriptions[i]}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            row.AddChild(swatch);
            row.AddChild(label);
            legendContainer.AddChild(row);
        }
    }

    private void UpdateSelection(int index)
    {
        selectedNodeIndex = index;
        routeTitleLabel.Text = $"当前预览：第 {index + 1} 站 · {nodeTypes[index]}";
        routeDescriptionLabel.Text = nodeDescriptions[index];
        progressLabel.Text = $"当前路线进度\n已解锁节点：{index + 1}/{nodeTypes.Length}\n推荐能量：{Mathf.Clamp(index + 2, 3, 6)} 点";
        hintLabel.Text = index >= nodeTypes.Length - 1
            ? "Boss 节点前建议优先补血、升级核心卡牌，并留意高费输出节奏。"
            : $"下一站建议：{nodeTypes[Mathf.Min(index + 1, nodeTypes.Length - 1)]}。可根据当前卡组强度调整路线。";

        for (int i = 0; i < nodeGrid.GetChildCount(); i++)
        {
            if (nodeGrid.GetChild(i) is not Button button)
            {
                continue;
            }

            bool isSelected = i == index;
            button.Scale = isSelected ? new Vector2(1.03f, 1.03f) : Vector2.One;
            button.Modulate = isSelected ? Colors.White : new Color(1, 1, 1, 0.92f);
            button.Text = isSelected ? $"▶ {i + 1}\n{nodeTypes[i]}" : $"{i + 1}\n{nodeTypes[i]}";
        }
    }

    private void OnContinuePressed()
    {
        GD.Print($"继续推进到节点 {selectedNodeIndex + 1}: {nodeTypes[selectedNodeIndex]}");
    }

    private void OnBattlePressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
