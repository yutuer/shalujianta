using Godot;

public partial class MainMenu : Control
{
	private Button startButton;
	private Button testButton;
	private Button continueButton;
	private Button settingsButton;
	private Button englishButton;
	private Button toolsButton;
	private Button exitButton;
	private OptionButton deckDropdown;
	
	public override void _Ready()
	{
		// 获取UI元素
		startButton = GetNode<Button>("Panel/VBoxContainer/StartButton");
		testButton = GetNode<Button>("Panel/VBoxContainer/TestButton");
		continueButton = GetNode<Button>("Panel/VBoxContainer/ContinueButton");
		settingsButton = GetNode<Button>("Panel/VBoxContainer/SettingsButton");
		englishButton = GetNode<Button>("Panel/VBoxContainer/EnglishButton");
		toolsButton = GetNode<Button>("Panel/VBoxContainer/ToolsButton");
		exitButton = GetNode<Button>("Panel/VBoxContainer/ExitButton");
		deckDropdown = GetNode<OptionButton>("Panel/VBoxContainer/DeckDropdown");
		
		// 添加卡组选项
		deckDropdown.AddItem("均衡流派 - 稳定攻守均衡组合，成长简单稳定");
		deckDropdown.AddItem("防御流派 - 高防御低攻击，适合新手");
		deckDropdown.AddItem("攻击流派 - 高攻击低防御，适合老手");
		deckDropdown.AddItem("连击流派 - 依靠连击触发特效");
		
		// 连接信号
		startButton.Pressed += OnStartButtonPressed;
		testButton.Pressed += OnTestButtonPressed;
		continueButton.Pressed += OnContinueButtonPressed;
		settingsButton.Pressed += OnSettingsButtonPressed;
		englishButton.Pressed += OnEnglishButtonPressed;
		toolsButton.Pressed += OnToolsButtonPressed;
		exitButton.Pressed += OnExitButtonPressed;
		
		// 禁用继续游戏按钮
		continueButton.Disabled = true;
	}
	
	private void OnStartButtonPressed()
	{
		// 开始游戏 - 跳转到地图场景
		GD.Print("开始游戏按钮点击");
		// 这里可以添加跳转到地图场景的代码
	}
	
	private void OnTestButtonPressed()
	{
		// 直路战斗测试 - 跳转到战斗场景
		GD.Print("直路战斗测试按钮点击");
		GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
	}
	
	private void OnContinueButtonPressed()
	{
		// 继续游戏 - 暂未实现
		GD.Print("继续游戏按钮点击");
	}
	
	private void OnSettingsButtonPressed()
	{
		// 设置按钮点击
		GD.Print("设置按钮点击");
		// 这里可以添加打开设置菜单的代码
	}
	
	private void OnEnglishButtonPressed()
	{
		// 切换语言按钮点击
		GD.Print("切换语言按钮点击");
		// 这里可以添加切换语言的代码
	}
	
	private void OnToolsButtonPressed()
	{
		// 工具菜单按钮点击
		GD.Print("工具菜单按钮点击");
		// 这里可以添加打开工具菜单的代码
	}
	
	private void OnExitButtonPressed()
	{
		// 退出游戏
		GD.Print("退出游戏按钮点击");
		GetTree().Quit();
	}
}
