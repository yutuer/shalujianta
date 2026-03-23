using Godot;
using System;
using System.Collections.Generic;

public partial class SilverKeyProgressIndicator : Control
{
    [Export]
    public int Diameter = 80;

    [Export]
    public Color BaseProgressColor = new Color("#C0C0C0");

    [Export]
    public Color ExcessProgressColor = new Color("#FFD700");

    [Export]
    public Color BackgroundColor = new Color("#333333");

    [Export]
    public Color DisabledColor = new Color("#666666");

    [Export]
    public Color TextColor = new Color("#FFFFFF");

    private int _currentValue = 0;
    private int _maxValue = 1000;
    private int _maxStackValue = 2000;

    private Tween _progressTween;
    private Tween _glowTween;
    private bool _isMaxed = false;

    private Label _valueLabel;
    private Label _maxLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Diameter, Diameter);

        var vbox = new VBoxContainer();
        vbox.Alignment = VBoxContainer.AlignmentMode.Center;
        AddChild(vbox);

        _valueLabel = new Label();
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _valueLabel.AddThemeColorOverride("font_color", TextColor);
        _valueLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_valueLabel);

        _maxLabel = new Label();
        _maxLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _maxLabel.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        _maxLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_maxLabel);

        UpdateDisplay();
    }

    public void SetValue(int value, bool animate = true)
    {
        int oldValue = _currentValue;
        _currentValue = Mathf.Clamp(value, 0, _maxStackValue);

        if (animate && oldValue != _currentValue)
        {
            AnimateProgress(oldValue, _currentValue);
        }
        else
        {
            UpdateDisplay();
        }
    }

    public int GetValue()
    {
        return _currentValue;
    }

    public bool CanUseKeyOrder()
    {
        return _currentValue >= _maxValue;
    }

    public bool IsMaxed()
    {
        return _currentValue >= _maxStackValue;
    }

    private void UpdateDisplay()
    {
        if (_valueLabel != null)
        {
            _valueLabel.Text = _currentValue.ToString();
        }

        if (_maxLabel != null)
        {
            _maxLabel.Text = _maxValue.ToString();
        }

        bool canUse = CanUseKeyOrder();
        bool isMaxed = IsMaxed();

        if (_valueLabel != null)
        {
            _valueLabel.AddThemeColorOverride("font_color", canUse ? TextColor : DisabledColor);
        }

        QueueRedraw();

        if (isMaxed && !_isMaxed)
        {
            _isMaxed = true;
            StartPulseAnimation();
        }
        else if (!isMaxed && _isMaxed)
        {
            _isMaxed = false;
            StopPulseAnimation();
        }
    }

    private void AnimateProgress(int fromValue, int toValue)
    {
        if (_progressTween != null && _progressTween.IsValid())
        {
            _progressTween.Kill();
        }

        _progressTween = CreateTween();
        _progressTween.SetParallel(false);

        float duration = Mathf.Abs(toValue - fromValue) / 1000f;
        duration = Mathf.Clamp(duration, 0.2f, 0.5f);

        var tweenData = new Dictionary<string, Variant>
        {
            { "from", fromValue },
            { "to", toValue }
        };

        float progress = 0f;
        _progressTween.TweenCallback(new Callable(this, nameof(OnProgressUpdate)));
        _progressTween.TweenMethod(Callable.From<float>(t => {
            progress = t;
            int current = (int)Mathf.Lerp(fromValue, toValue, t);
            _currentValue = current;
            UpdateDisplay();
        }), 0f, 1f, duration);

        _progressTween.Play();
    }

    private void OnProgressUpdate()
    {
        UpdateDisplay();
    }

    private void StartPulseAnimation()
    {
        if (_glowTween != null && _glowTween.IsValid())
        {
            _glowTween.Kill();
        }

        _glowTween = CreateTween();
        _glowTween.SetLoops();

        var labelModulate = Modulate;
        _glowTween.TweenProperty(this, "modulate", new Color(1.2f, 1.2f, 0.8f, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _glowTween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        _glowTween.Play();
    }

    private void StopPulseAnimation()
    {
        if (_glowTween != null && _glowTween.IsValid())
        {
            _glowTween.Kill();
        }
        Modulate = new Color(1f, 1f, 1f, 1f);
    }

    public override void _Draw()
    {
        base._Draw();

        Vector2 center = new Vector2(Diameter / 2f, Diameter / 2f);
        float radius = Diameter / 2f - 4f;
        float lineWidth = 6f;

        DrawCircle(center, radius, BackgroundColor);

        float baseProgress = (float)_currentValue / _maxValue;
        baseProgress = Mathf.Clamp(baseProgress, 0f, 1f);

        if (baseProgress > 0f)
        {
            float startAngle = -90f;
            float endAngle = startAngle + (360f * baseProgress);

            DrawArc(center, radius, Mathf.DegToRad(startAngle), Mathf.DegToRad(endAngle), 32, BaseProgressColor, lineWidth, true);
        }

        if (_currentValue > _maxValue)
        {
            float excessProgress = (float)(_currentValue - _maxValue) / (_maxStackValue - _maxValue);
            excessProgress = Mathf.Clamp(excessProgress, 0f, 1f);

            if (excessProgress > 0f)
            {
                float startAngle = -90f + (360f * 1f);
                float endAngle = startAngle + (360f * excessProgress);

                Color excessColor = ExcessProgressColor;
                if (_isMaxed)
                {
                    excessColor = new Color(1f, 0.85f, 0f, 1f);
                }

                DrawArc(center, radius - 2f, Mathf.DegToRad(startAngle), Mathf.DegToRad(endAngle), 32, excessColor, lineWidth - 2f, true);
            }
        }

        if (!CanUseKeyOrder())
        {
            DrawCircle(center, radius - 4f, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        }
    }

    public void SetMaxValue(int value)
    {
        _maxValue = value;
        _maxStackValue = value * 2;
        UpdateDisplay();
    }

    public void Reset()
    {
        SetValue(0, false);
    }
}
