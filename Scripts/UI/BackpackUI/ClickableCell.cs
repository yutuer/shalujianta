using Godot;
using FishEatFish.Shop;
using System.Linq;

namespace FishEatFish.UI.BackpackUI
{
    public partial class ClickableCell : Control
    {
        private TextureRect _icon;
        private Label _emoji;
        private ArtifactData _artifact;
        private bool _hasValidTexture = false;

        public System.Action<ArtifactData> OnCellClicked;

        public override void _Ready()
        {
            GD.Print($"[ClickableCell] _Ready called");
            MouseFilter = MouseFilterEnum.Stop;
            GD.Print($"[ClickableCell] MouseFilter set to Stop");
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            GD.Print($"[ClickableCell] _EnterTree called");
        }

        public void SetArtifact(ArtifactData artifact)
        {
            _artifact = artifact;

            foreach (var child in GetChildren().ToList())
            {
                child.QueueFree();
            }

            if (artifact == null) return;

            if (!string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    _icon = new TextureRect();
                    _icon.Name = "Icon";
                    _icon.CustomMinimumSize = new Vector2(40, 40);
                    _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    _icon.Texture = texture;
                    _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    _icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                    _icon.OffsetLeft = 5;
                    _icon.OffsetTop = 5;
                    _icon.OffsetRight = -5;
                    _icon.OffsetBottom = -5;
                    _hasValidTexture = true;
                    AddChild(_icon);
                    return;
                }
            }

            _icon = new TextureRect();
            _icon.Name = "Icon";
            _icon.CustomMinimumSize = new Vector2(40, 40);
            _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _icon.OffsetLeft = 5;
            _icon.OffsetTop = 5;
            _icon.OffsetRight = -5;
            _icon.OffsetBottom = -5;

            _emoji = new Label();
            _emoji.Name = "Emoji";
            _emoji.Text = "💎";
            _emoji.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _emoji.HorizontalAlignment = HorizontalAlignment.Center;
            _emoji.VerticalAlignment = VerticalAlignment.Center;
            _emoji.MouseFilter = Control.MouseFilterEnum.Ignore;
            _icon.AddChild(_emoji);

            AddChild(_icon);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (_artifact != null)
                    {
                        GD.Print($"[ClickableCell] Clicked on artifact: {_artifact.name}");
                        OnCellClicked?.Invoke(_artifact);
                        AcceptEvent();
                    }
                }
            }
        }
    }
}
