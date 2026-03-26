using System.Collections.Generic;
using System.Linq;
using Godot;
using FishEatFish.Battle.HexMap;

public partial class MainMenu : Control
{
    private Button _startButton;
    private Button _hexMapButton;
    private Button _levelSelectButton;
    private Button _testButton;
    private Button _holeTestButton;
    private Button _continueButton;
    private Button _exitButton;
    private Label _resultLabel;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("Panel/VBoxContainer/StartButton");
        _hexMapButton = GetNode<Button>("Panel/VBoxContainer/HexMapButton");
        _levelSelectButton = GetNode<Button>("Panel/VBoxContainer/LevelSelectButton");
        _testButton = GetNode<Button>("Panel/VBoxContainer/TestButton");
        _holeTestButton = GetNode<Button>("Panel/VBoxContainer/HoleTestButton");
        _continueButton = GetNode<Button>("Panel/VBoxContainer/ContinueButton");
        _exitButton = GetNode<Button>("Panel/VBoxContainer/ExitButton");

        _startButton.Pressed += OnStartPressed;
        _hexMapButton.Pressed += OnHexMapPressed;
        _levelSelectButton.Pressed += OnLevelSelectPressed;
        _testButton.Pressed += OnTestPressed;
        _holeTestButton.Pressed += OnHoleTestPressed;
        _continueButton.Pressed += OnContinuePressed;
        _exitButton.Pressed += OnExitPressed;

        _continueButton.Disabled = true;
    }

    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }

    private void OnHexMapPressed()
    {
        GlobalData.EnterHexMapMode = true;
        GetTree().ChangeSceneToFile("res://Scenes/CharacterSelection.tscn");
    }

    private void OnLevelSelectPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }

    private void OnTestPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnHoleTestPressed()
    {
        int testRadius = 4;
        int playerLevel = 7;
        int totalMaps = 100;
        int successCount = 0;
        int failCount = 0;
        int holeCount = 0;
        int holeGroupCount = 0;

        for (int i = 0; i < totalMaps; i++)
        {
            var generator = new HexMapGenerator();
            var hexMap = generator.Generate(testRadius, playerLevel);

            var holes = hexMap.GetHoleTiles().ToList();
            holeCount += holes.Count;

            var tilesDict = hexMap.GetAllTiles().ToDictionary(t => t.Coord);
            var connectedGroups = FindConnectedHoleGroups(tilesDict);
            holeGroupCount += connectedGroups.Count;

            if (connectedGroups.Count >= 2)
            {
                GD.Print($"[测试] 地图 {i + 1}: 发现 {connectedGroups.Count} 组相邻洞穴");
            }

            successCount++;
        }

        float avgHoles = (float)holeCount / totalMaps;
        float avgGroups = (float)holeGroupCount / totalMaps;

        string result = $"验证完成!\n" +
                        $"测试地图数: {totalMaps}\n" +
                        $"生成成功: {successCount}\n" +
                        $"生成失败: {failCount}\n" +
                        $"平均洞穴数: {avgHoles:F1}\n" +
                        $"平均相邻洞穴组: {avgGroups:F1}";

        GD.Print(result);

        ShowResultDialog(result);
    }

    private List<HashSet<HexCoord>> FindConnectedHoleGroups(Dictionary<HexCoord, HexTile> tiles)
    {
        var groups = new List<HashSet<HexCoord>>();
        var visited = new HashSet<HexCoord>();
        var holes = tiles.Values.Where(t => t.EventType == HexEventType.Hole).Select(t => t.Coord).ToHashSet();

        foreach (var hole in holes)
        {
            if (visited.Contains(hole))
                continue;

            var group = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(hole);
            visited.Add(hole);
            group.Add(hole);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.GetNeighbors())
                {
                    if (holes.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        group.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (group.Count > 0)
                groups.Add(group);
        }

        return groups;
    }

    private void ShowResultDialog(string message)
    {
        var dialog = new AcceptDialog();
        dialog.Title = "洞穴地图验证结果";
        dialog.DialogText = message;
        dialog.OkButtonText = "确定";
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnContinuePressed()
    {
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
