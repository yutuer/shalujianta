using Godot;
using System.Collections.Generic;

public partial class BattleLogWindow : Control
{
    private PanelContainer windowPanel;
    private VBoxContainer mainContainer;
    private HBoxContainer titleBar;
    private Control dragArea;
    private Label titleLabel;
    private Button minimizeButton;
    private Button closeButton;
    private ScrollContainer scrollContainer;
    private VBoxContainer logContainer;
    private Button clearButton;

    private bool isDragging = false;
    private Vector2 dragOffset;
    private bool isResizing = false;
    private bool isMinimized = false;

    private const int MinWidth = 320;
    private const int MinHeight = 200;
    private const int DefaultWidth = 450;
    private const int DefaultHeight = 350;
    private const int MaxVisibleLogs = 500;

    private List<LogEntry> allLogs = new List<LogEntry>();
    private List<Label> visibleLabels = new List<Label>();

    private bool autoScrollEnabled = true;

    public override void _Ready()
    {
        GetNodeReferences();
        ConnectSignals();
        ApplyStyles();

        CustomMinimumSize = new Vector2(MinWidth, MinHeight);
        Size = new Vector2(DefaultWidth, DefaultHeight);
        
        var viewportSize = GetViewportRect().Size;
        Position = new Vector2(
            viewportSize.X - DefaultWidth - 20,
            viewportSize.Y - DefaultHeight - 280
        );
        
        autoScrollEnabled = true;
    }

    private void GetNodeReferences()
    {
        windowPanel = GetNode<PanelContainer>("WindowPanel");
        mainContainer = GetNode<VBoxContainer>("WindowPanel/MainContainer");
        titleBar = GetNode<HBoxContainer>("WindowPanel/MainContainer/TitleBar");
        dragArea = GetNode<Control>("WindowPanel/MainContainer/TitleBar/DragArea");
        titleLabel = GetNode<Label>("WindowPanel/MainContainer/TitleBar/DragArea/TitleLabel");
        minimizeButton = GetNode<Button>("WindowPanel/MainContainer/TitleBar/MinimizeButton");
        closeButton = GetNode<Button>("WindowPanel/MainContainer/TitleBar/CloseButton");
        scrollContainer = GetNode<ScrollContainer>("WindowPanel/MainContainer/ScrollContainer");
        logContainer = GetNode<VBoxContainer>("WindowPanel/MainContainer/ScrollContainer/LogContainer");
        clearButton = GetNode<Button>("WindowPanel/MainContainer/BottomBar/ClearButton");
    }

    private void ConnectSignals()
    {
        minimizeButton.Pressed += OnMinimizePressed;
        closeButton.Pressed += OnClosePressed;
        clearButton.Pressed += OnClearPressed;

        var vScrollBar = scrollContainer.GetVScrollBar();
        vScrollBar.ValueChanged += OnScrollChanged;
    }

    private void ApplyStyles()
    {
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color("1a1a1a");
        panelStyle.BorderColor = new Color("333333");
        panelStyle.SetBorderWidthAll(1);
        panelStyle.SetContentMarginAll(0);
        panelStyle.ShadowColor = new Color(0, 0, 0, 0.3f);
        panelStyle.ShadowSize = 10;
        panelStyle.ShadowOffset = new Vector2(0, 2);
        windowPanel.AddThemeStyleboxOverride("panel", panelStyle);

        var titleBarStyle = new StyleBoxFlat();
        titleBarStyle.BgColor = new Color("252525");
        titleBarStyle.SetContentMarginAll(5);
        titleBar.AddThemeStyleboxOverride("panel", titleBarStyle);

        titleLabel.AddThemeColorOverride("font_color", new Color("e0e0e0"));
        titleLabel.AddThemeFontSizeOverride("font_size", 14);

        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = new Color("333333");
        buttonStyle.SetContentMarginAll(4);
        
        minimizeButton.AddThemeStyleboxOverride("normal", buttonStyle);
        minimizeButton.AddThemeColorOverride("font_color", new Color("e0e0e0"));
        
        closeButton.AddThemeStyleboxOverride("normal", buttonStyle);
        closeButton.AddThemeColorOverride("font_color", new Color("ff6666"));

        clearButton.AddThemeStyleboxOverride("normal", buttonStyle);
        clearButton.AddThemeColorOverride("font_color", new Color("e0e0e0"));

        var scrollStyle = new StyleBoxFlat();
        scrollStyle.BgColor = new Color("1a1a1a");
        scrollContainer.AddThemeStyleboxOverride("panel", scrollStyle);

        var vScrollBar = scrollContainer.GetVScrollBar();
        var scrollBarStyle = new StyleBoxFlat();
        scrollBarStyle.BgColor = new Color("2a2a2a");
        scrollBarStyle.SetBorderWidthAll(4);
        vScrollBar.AddThemeStyleboxOverride("scroll", scrollBarStyle);
        vScrollBar.AddThemeStyleboxOverride("grabber", scrollBarStyle);
        vScrollBar.CustomMinimumSize = new Vector2(8, 0);

        logContainer.AddThemeConstantOverride("separation", 2);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    Rect2 titleRect = new Rect2(titleBar.GlobalPosition, titleBar.Size);
                    if (titleRect.HasPoint(mouseButton.GlobalPosition))
                    {
                        isDragging = true;
                        dragOffset = mouseButton.GlobalPosition - GlobalPosition;
                    }

                    Rect2 resizeRect = new Rect2(
                        GlobalPosition + new Vector2(Size.X - 15, Size.Y - 15),
                        new Vector2(15, 15)
                    );
                    if (resizeRect.HasPoint(mouseButton.GlobalPosition))
                    {
                        isResizing = true;
                    }
                }
                else
                {
                    isDragging = false;
                    isResizing = false;
                }
            }
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (isDragging)
            {
                GlobalPosition = mouseMotion.GlobalPosition - dragOffset;
            }
            else if (isResizing)
            {
                Vector2 newSize = mouseMotion.GlobalPosition - GlobalPosition;
                Size = new Vector2(
                    Mathf.Max(MinWidth, newSize.X),
                    Mathf.Max(MinHeight, newSize.Y)
                );
            }
        }
    }

    public void AddLog(string message, LogType logType = LogType.Info)
    {
        var entry = new LogEntry { Message = message, Type = logType };
        allLogs.Add(entry);

        if (allLogs.Count > MaxVisibleLogs * 2)
        {
            TrimLogs();
        }

        CreateLogLabel(entry);
        
        if (autoScrollEnabled)
        {
            CallDeferred(nameof(ScrollToBottom));
        }
    }

    private void CreateLogLabel(LogEntry entry)
    {
        var label = new Label();
        label.Text = entry.Message;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        label.AddThemeColorOverride("font_color", GetColorForLogType(entry.Type));
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeConstantOverride("line_spacing", 6);

        logContainer.AddChild(label);
        visibleLabels.Add(label);
    }

    private Color GetColorForLogType(LogType type)
    {
        return type switch
        {
            LogType.Damage => new Color("ff6b6b"),
            LogType.Heal => new Color("6bff6b"),
            LogType.Shield => new Color("6bb3ff"),
            LogType.Energy => new Color("ffdd6b"),
            LogType.Skill => new Color("dd6bff"),
            LogType.Enemy => new Color("ff9966"),
            LogType.System => new Color("888888"),
            _ => new Color("e0e0e0")
        };
    }

    private void TrimLogs()
    {
        int removeCount = allLogs.Count - MaxVisibleLogs;
        allLogs.RemoveRange(0, removeCount);

        for (int i = 0; i < removeCount && visibleLabels.Count > 0; i++)
        {
            var label = visibleLabels[0];
            visibleLabels.RemoveAt(0);
            label.QueueFree();
        }
    }

    private void ScrollToBottom()
    {
        var vScrollBar = scrollContainer.GetVScrollBar();
        if (vScrollBar.MaxValue > 0)
        {
            vScrollBar.Value = vScrollBar.MaxValue;
        }
    }

    private void OnScrollChanged(double value)
    {
        var vScrollBar = scrollContainer.GetVScrollBar();
        if (vScrollBar.MaxValue <= 0)
        {
            autoScrollEnabled = true;
            return;
        }
        double distanceFromBottom = vScrollBar.MaxValue - value;
        autoScrollEnabled = distanceFromBottom < 50;
    }

    private void OnMinimizePressed()
    {
        isMinimized = !isMinimized;
        
        if (isMinimized)
        {
            scrollContainer.Visible = false;
            clearButton.Visible = false;
            CustomMinimumSize = new Vector2(MinWidth, 60);
            Size = new Vector2(Size.X, 60);
            minimizeButton.Text = "□";
        }
        else
        {
            scrollContainer.Visible = true;
            clearButton.Visible = true;
            CustomMinimumSize = new Vector2(MinWidth, MinHeight);
            Size = new Vector2(Size.X, DefaultHeight);
            minimizeButton.Text = "_";
        }
    }

    private void OnClosePressed()
    {
        Visible = false;
    }

    private void OnClearPressed()
    {
        ClearLogs();
    }

    public void ClearLogs()
    {
        allLogs.Clear();
        
        foreach (var label in visibleLabels)
        {
            label.QueueFree();
        }
        visibleLabels.Clear();
    }

    public void ShowWindow()
    {
        Visible = true;
    }

    public void HideWindow()
    {
        Visible = false;
    }

    public void ToggleWindow()
    {
        Visible = !Visible;
    }

    public int GetLogCount()
    {
        return allLogs.Count;
    }
}

public enum LogType
{
    Info,
    Damage,
    Heal,
    Shield,
    Energy,
    Skill,
    Enemy,
    System
}

public class LogEntry
{
    public string Message { get; set; }
    public LogType Type { get; set; }
}
