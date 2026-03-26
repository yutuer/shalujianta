using Godot;
using FishEatFish.Battle.HexMap;

namespace FishEatFish.UI.HexMap
{
    public partial class DraggableHexView : Control
    {
        private Polygon2D _hexShape;
        private ColorRect _background;
        private Label _iconLabel;
        private Label _debugLabel;

        private bool _isDragging = false;
        private Vector2 _dragOffset;
        private Vector2 _initialPosition;

        private Color _tileColor = new Color(0.75f, 0.75f, 0.75f);

        public override void _Ready()
        {
            _hexShape = GetNodeOrNull<Polygon2D>("HexShape");
            _background = GetNodeOrNull<ColorRect>("Background");
            _iconLabel = GetNodeOrNull<Label>("IconLabel");
            _debugLabel = GetNodeOrNull<Label>("DebugLabel");

            UpdateHexShape();
        }

        public void SetColor(Color color)
        {
            _tileColor = color;
            UpdateVisuals();
        }

        public void SetLabel(string text)
        {
            if (_iconLabel != null)
            {
                _iconLabel.Text = text;
            }
        }

        public void SetDebugText(string text)
        {
            if (_debugLabel != null)
            {
                _debugLabel.Text = text;
            }
        }

        private void UpdateHexShape()
        {
            if (_hexShape == null) return;

            var center = Size / 2;
            var points = new Vector2[6];
            float radius = Mathf.Min(center.X, center.Y);

            for (int i = 0; i < 6; i++)
            {
                float angleDeg = 60 * i - 30;
                float angleRad = Mathf.DegToRad(angleDeg);
                points[i] = new Vector2(
                    center.X + radius * Mathf.Cos(angleRad),
                    center.Y + radius * Mathf.Sin(angleRad)
                );
            }

            _hexShape.Polygon = points;
            if (_background != null)
            {
                _background.Color = Colors.Transparent;
            }
        }

        private void UpdateVisuals()
        {
            if (_hexShape == null) return;

            _hexShape.Color = _tileColor;

            if (_debugLabel != null)
            {
                var hexPos = _hexShape.Position;
                _debugLabel.Text = $"Control: ({Position.X:F0}, {Position.Y:F0})\nHexShape: ({hexPos.X:F0}, {hexPos.Y:F0})";
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (mouseEvent.Pressed)
                    {
                        _isDragging = true;
                        _dragOffset = mouseEvent.Position;
                        _initialPosition = Position;
                        GD.Print($"{Name}: 开始拖动，初始位置 ({_initialPosition.X:F0}, {_initialPosition.Y:F0})");
                    }
                    else if (_isDragging)
                    {
                        _isDragging = false;
                        GD.Print($"{Name}: 拖动结束，最终位置 ({Position.X:F0}, {Position.Y:F0})");
                    }
                }
            }
            else if (@event is InputEventMouseMotion && _isDragging)
            {
                Position = GlobalPosition + ((InputEventMouseMotion)@event).Position - _dragOffset;
                UpdateVisuals();
            }
        }
    }
}
