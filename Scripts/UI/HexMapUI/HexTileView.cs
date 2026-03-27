using System.Collections.Generic;
using Godot;
using FishEatFish.Battle.HexMap;

namespace FishEatFish.UI.HexMap
{
    public partial class HexTileView : Control
    {
        private Color _normalColor = new Color(0.75f, 0.75f, 0.75f);
        private Color _startColor = new Color(0.2f, 0.8f, 0.2f);
        private Color _endColor = new Color(0.8f, 0.2f, 0.2f);
        private Color _visitedColor = new Color(0.6f, 0.6f, 0.6f);
        private Color _pathColor = new Color(0.6f, 0.6f, 0.2f);
        private Color _disappearedColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);

        private static readonly Dictionary<HexEventType, Color> EventColors = new Dictionary<HexEventType, Color>
        {
            { HexEventType.Empty, new Color(0.75f, 0.75f, 0.75f) },
            { HexEventType.BattleNormal, new Color(0.9f, 0.3f, 0.3f) },
            { HexEventType.BattleElite, new Color(0.8f, 0.2f, 0.6f) },
            { HexEventType.BattleBoss, new Color(0.6f, 0.1f, 0.1f) },
            { HexEventType.Shop, new Color(1.0f, 0.8f, 0.2f) },
            { HexEventType.Heal, new Color(0.3f, 0.9f, 0.5f) },
            { HexEventType.Hole, new Color(0.3f, 0.3f, 0.3f) },
            { HexEventType.GainBlackMark, new Color(0.2f, 0.2f, 0.5f) },
            { HexEventType.Swamp, new Color(0.5f, 0.4f, 0.2f) },
            { HexEventType.TwoWayTeleport, new Color(0.3f, 0.6f, 0.9f) },
            { HexEventType.OneDirectionTele, new Color(0.5f, 0.3f, 0.8f) }
        };

        private Polygon2D _hexShape;
        private ColorRect _background;
        private Label _iconLabel;
        private Label _debugLabel;

        private HexTile _tile;
        public HexTile Tile => _tile;

        private bool _isHovered;
        private bool _isPath;
        private bool _isClickable = true;
        private Vector2 _hexWorldPosition;

        public System.Action<HexTileView> OnTileClicked;
        public System.Action<HexTileView> OnTileHovered;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;

            _hexShape = GetNode<Polygon2D>("HexShape");
            _background = GetNode<ColorRect>("Background");
            _iconLabel = GetNode<Label>("IconLabel");
            _debugLabel = GetNode<Label>("DebugLabel");

            UpdateHexShape();

            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        private void UpdateHexShape()
        {
            if (_hexShape == null) return;

            var points = new Vector2[6];
            points[0] = new Vector2(90, 0);
            points[1] = new Vector2(165, 39);
            points[2] = new Vector2(165, 117);
            points[3] = new Vector2(90, 156);
            points[4] = new Vector2(15, 117);
            points[5] = new Vector2(15, 39);

            _hexShape.Polygon = points;
            _background.Color = Colors.Transparent;
        }

        public void SetTile(HexTile tile)
        {
            _tile = tile;
            if (_background != null && _iconLabel != null && _debugLabel != null)
            {
                UpdateHexShape();
                UpdateVisuals();
            }
        }

        public void SetHexWorldPosition(Vector2 worldPos)
        {
            _hexWorldPosition = worldPos;
        }

        public Vector2 GetHexWorldPosition()
        {
            return GetGlobalPosition() + Size / 2;
        }

        public void SetAsPath(bool isPath)
        {
            _isPath = isPath;
            UpdateVisuals();
        }

        public void SetClickable(bool clickable)
        {
            _isClickable = clickable;
        }

        private void UpdateVisuals()
        {
            if (_tile == null || _hexShape == null) return;

            Color tileColor;
            string icon = "";

            if (_tile.IsStart)
            {
                tileColor = _startColor;
                icon = "S";
            }
            else if (_tile.IsEnd)
            {
                tileColor = _endColor;
                icon = "B";
            }
            else if (_tile.IsDisappeared)
            {
                tileColor = _disappearedColor;
                icon = "X";
            }
            else
            {
                tileColor = EventColors.GetValueOrDefault(_tile.EventType, _normalColor);

                if (_tile.IsVisited)
                {
                    tileColor = new Color(tileColor.R * 0.7f, tileColor.G * 0.7f, tileColor.B * 0.7f);
                }

                if (_isPath && !_tile.IsVisited)
                {
                    tileColor = _pathColor;
                }
            }

            switch (_tile.EventType)
            {
                case HexEventType.BattleNormal:
                    icon = "⚔";
                    break;
                case HexEventType.BattleElite:
                    icon = "⚔!";
                    break;
                case HexEventType.BattleBoss:
                    icon = "👹";
                    break;
                case HexEventType.Shop:
                    icon = "$";
                    break;
                case HexEventType.Heal:
                    icon = "+";
                    break;
                case HexEventType.Hole:
                    icon = "O";
                    break;
                case HexEventType.GainBlackMark:
                    icon = "💎";
                    break;
                case HexEventType.Swamp:
                    icon = "~";
                    break;
                case HexEventType.TwoWayTeleport:
                    icon = "⇄";
                    break;
                case HexEventType.OneDirectionTele:
                    icon = "→";
                    break;
                case HexEventType.Empty:
                    icon = "";
                    break;
            }

            _hexShape.Color = tileColor;
            _iconLabel.Text = icon;
            _debugLabel.Text = $"{_tile.Coord}";
        }

        public void AnimateDisappear()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 0.3f, 0.5f);
            tween.TweenCallback(new Callable(this, nameof(OnDisappearComplete)));
        }

        private void OnDisappearComplete()
        {
            if (_tile != null)
            {
                _tile.IsDisappeared = true;
            }
            Modulate = new Color(1, 1, 1, 0.3f);
        }

        private void OnMouseEntered()
        {
            _isHovered = true;
            OnTileHovered?.Invoke(this);
        }

        private void OnMouseExited()
        {
            _isHovered = false;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                var localPos = GetLocalMousePosition();
                var rect = new Rect2(Vector2.Zero, Size);
                if (rect.HasPoint(localPos))
                {
                    if (_isClickable && _tile != null && _tile.CanEnter)
                    {
                        OnTileClicked?.Invoke(this);
                    }
                }
            }
        }
    }
}
