using Godot;
using FishEatFish.Battle.HexMap;

namespace FishEatFish.UI.HexMap
{
    public partial class HexTileView : Control
    {
        [Export]
        private Color _normalColor = new Color(0.3f, 0.3f, 0.3f);

        [Export]
        private Color _startColor = new Color(0.2f, 0.8f, 0.2f);

        [Export]
        private Color _endColor = new Color(0.8f, 0.2f, 0.2f);

        [Export]
        private Color _visitedColor = new Color(0.4f, 0.4f, 0.4f);

        [Export]
        private Color _disappearedColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);

        [Export]
        private Color _hoverColor = new Color(0.5f, 0.5f, 0.5f);

        [Export]
        private Color _pathColor = new Color(0.6f, 0.6f, 0.2f);

        private ColorRect _background;
        private TextureRect _icon;
        private Label _label;
        private Polygon2D _hexShape;

        private HexTile _tile;
        public HexTile Tile => _tile;

        private bool _isHovered;
        private bool _isPath;
        private bool _isClickable = true;

        public System.Action<HexTileView> OnTileClicked;
        public System.Action<HexTileView> OnTileHovered;

        public override void _Ready()
        {
            InitializeComponents();
            UpdateVisuals();
        }

        private void InitializeComponents()
        {
            _hexShape = new Polygon2D();
            AddChild(_hexShape);
            _hexShape.ZIndex = 0;

            _background = new ColorRect();
            AddChild(_background);
            _background.ZIndex = 1;

            _icon = new TextureRect();
            AddChild(_icon);
            _icon.ZIndex = 2;
            _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _icon.Modulate = new Color(1, 1, 1, 0.8f);

            _label = new Label();
            AddChild(_label);
            _label.ZIndex = 3;
            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.VerticalAlignment = VerticalAlignment.Bottom;
            _label.Modulate = Colors.White;

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        public void SetTile(HexTile tile)
        {
            _tile = tile;
            UpdateVisuals();
        }

        public void SetClickable(bool clickable)
        {
            _isClickable = clickable;
        }

        public void SetAsPath(bool isPath)
        {
            _isPath = isPath;
            UpdateVisuals();
        }

        public override void _Draw()
        {
            base._Draw();

            var hexPoints = GetHexPoints(Size / 2);
            _hexShape.Polygon = hexPoints;

            var iconSize = Mathf.Min(Size.X, Size.Y) * 0.5f;
            _icon.Size = new Vector2(iconSize, iconSize);
            _icon.Position = (Size - _icon.Size) / 2;

            _label.Size = new Vector2(Size.X, 20);
            _label.Position = new Vector2(0, Size.Y - 25);
        }

        private Vector2[] GetHexPoints(Vector2 center)
        {
            var points = new Vector2[6];
            var radius = Mathf.Min(center.X, center.Y);

            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.DegToRad(60 * i - 30);
                points[i] = new Vector2(
                    center.X + radius * Mathf.Cos(angle),
                    center.Y + radius * Mathf.Sin(angle)
                );
            }

            return points;
        }

        private void UpdateVisuals()
        {
            if (_tile == null) return;

            Color bgColor;

            if (_tile.IsDisappeared)
            {
                bgColor = _disappearedColor;
            }
            else if (_tile.IsEnd)
            {
                bgColor = _endColor;
            }
            else if (_tile.IsStart)
            {
                bgColor = _startColor;
            }
            else if (_isPath)
            {
                bgColor = _pathColor;
            }
            else if (_isHovered)
            {
                bgColor = _hoverColor;
            }
            else if (_tile.IsVisited)
            {
                bgColor = _visitedColor;
            }
            else
            {
                bgColor = _normalColor;
            }

            _background.Color = bgColor;

            if (!string.IsNullOrEmpty(_tile.IconPath) && !_tile.IsDisappeared)
            {
                var texture = GD.Load<Texture2D>(_tile.IconPath);
                _icon.Texture = texture;
                _icon.Visible = true;
            }
            else
            {
                _icon.Visible = false;
            }

            if (!string.IsNullOrEmpty(_tile.DisplayName) && !_tile.IsDisappeared)
            {
                _label.Text = _tile.DisplayName;
                _label.Visible = true;
            }
            else
            {
                _label.Visible = false;
            }

            _hexShape.Modulate = _tile.IsDisappeared ? new Color(1, 1, 1, 0.2f) : Colors.White;
        }

        private void OnMouseEntered()
        {
            if (!_isClickable || (_tile != null && !_tile.CanEnter)) return;

            _isHovered = true;
            UpdateVisuals();
            OnTileHovered?.Invoke(this);
        }

        private void OnMouseExited()
        {
            _isHovered = false;
            UpdateVisuals();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (_isClickable && _tile != null && _tile.CanEnter)
                    {
                        OnTileClicked?.Invoke(this);
                    }
                }
            }
        }

        public Vector2 GetHexWorldPosition()
        {
            return GlobalPosition + Size / 2;
        }

        public void AnimateDisappear()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
            tween.TweenCallback(Callable.From(() =>
            {
                Visible = false;
                QueueFree();
            }));
        }

        public void AnimateAppear()
        {
            Modulate = new Color(1, 1, 1, 0);
            Visible = true;

            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 1f, 0.3f);
        }
    }
}
