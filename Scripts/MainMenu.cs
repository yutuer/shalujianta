using Godot;

public partial class MainMenu : Control
{
    private Button _startButton;
    private Button _testButton;
    private Button _continueButton;
    private Button _exitButton;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("Panel/VBoxContainer/StartButton");
        _testButton = GetNode<Button>("Panel/VBoxContainer/TestButton");
        _continueButton = GetNode<Button>("Panel/VBoxContainer/ContinueButton");
        _exitButton = GetNode<Button>("Panel/VBoxContainer/ExitButton");

        _startButton.Pressed += OnStartPressed;
        _testButton.Pressed += OnTestPressed;
        _continueButton.Pressed += OnContinuePressed;
        _exitButton.Pressed += OnExitPressed;

        _continueButton.Disabled = true;
    }

    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelMap.tscn");
    }

    private void OnTestPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnContinuePressed()
    {
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
