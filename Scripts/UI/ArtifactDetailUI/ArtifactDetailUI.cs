using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.ArtifactDetailPanel
{
    public partial class ArtifactDetailUI : Control
    {
        private TextureRect _iconRect;
        private Label _emojiLabel;
        private Label _nameLabel;
        private Label _typeLabel;
        private Label _descriptionLabel;
        private Button _closeButton;
        private PanelContainer _backgroundPanel;

        private ArtifactData _currentArtifact;
        private ulong _shownAtFrame;

        public override void _Ready()
        {
            GD.Print($"[ArtifactDetailUI] _Ready called");

            _backgroundPanel = GetNodeOrNull<PanelContainer>("BackgroundPanel");
            GD.Print($"[ArtifactDetailUI] _backgroundPanel: {_backgroundPanel != null}");
            if (_backgroundPanel != null)
            {
                GD.Print($"[ArtifactDetailUI] _backgroundPanel Visible: {_backgroundPanel.Visible}");
                GD.Print($"[ArtifactDetailUI] _backgroundPanel Size: {_backgroundPanel.Size}");
                GD.Print($"[ArtifactDetailUI] _backgroundPanel GlobalPosition: {_backgroundPanel.GlobalPosition}");
            }

            _iconRect = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/IconRect");
            _emojiLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/EmojiLabel");
            _nameLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/NameLabel");
            _typeLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/TypeLabel");
            _descriptionLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/DescriptionLabel");

            Visible = false;

            GD.Print($"[ArtifactDetailUI] _Ready completed");
        }

        public override void _Input(InputEvent @event)
        {
            if (!Visible)
            {
                return;
            }

            if (@event is InputEventMouseButton mouseEvent)
            {
                if (_backgroundPanel == null) return;

                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && Engine.GetProcessFrames() == _shownAtFrame)
                {
                    GD.Print("[ArtifactDetailUI] Ignore same-frame click right after opening");
                    return;
                }

                var localPos = GetLocalMousePosition();
                var backgroundLocalPos = _backgroundPanel.GetLocalMousePosition();
                var backgroundRect = new Rect2(Vector2.Zero, _backgroundPanel.Size);
                var isInBackground = backgroundRect.HasPoint(backgroundLocalPos);
                GD.Print($"[ArtifactDetailUI] _Input: localPos=({localPos.X:F1}, {localPos.Y:F1}), backgroundLocalPos=({backgroundLocalPos.X:F1}, {backgroundLocalPos.Y:F1}), backgroundSize=({_backgroundPanel.Size.X:F1}, {_backgroundPanel.Size.Y:F1}), InBackground={isInBackground}, Pressed={mouseEvent.Pressed}, Button={mouseEvent.ButtonIndex}");

                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && !isInBackground)
                {
                    GD.Print($"[ArtifactDetailUI] Clicked OUTSIDE background, hiding");
                    HideArtifact();
                }
            }
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            MouseFilter = MouseFilterEnum.Stop;
            GD.Print($"[ArtifactDetailUI] _EnterTree: set MouseFilter to Stop");
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            GD.Print($"[ArtifactDetailUI] _ExitTree: set MouseFilter to Pass");
            MouseFilter = MouseFilterEnum.Pass;
        }

        public void ShowArtifact(ArtifactData artifact)
        {
            GD.Print($"[ArtifactDetailUI] ShowArtifact called: artifact={artifact?.name}");
            GD.Print($"[ArtifactDetailUI] Current Visible state before: {Visible}");

            if (artifact == null)
            {
                GD.PrintErr("[ArtifactDetailUI] ShowArtifact: artifact is null!");
                return;
            }

            _currentArtifact = artifact;

            if (_nameLabel != null)
                _nameLabel.Text = artifact.name;

            if (_descriptionLabel != null)
                _descriptionLabel.Text = artifact.description;

            bool hasTexture = false;
            if (_iconRect != null && !string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    _iconRect.Texture = texture;
                    if (_emojiLabel != null) _emojiLabel.Visible = false;
                    hasTexture = true;
                }
            }

            if (!hasTexture)
            {
                if (_emojiLabel != null)
                {
                    _emojiLabel.Text = "💎";
                    _emojiLabel.Visible = true;
                }
                if (_iconRect != null)
                {
                    _iconRect.Visible = false;
                }
            }
            else if (_iconRect != null)
            {
                _iconRect.Visible = true;
            }

            _shownAtFrame = Engine.GetProcessFrames();
            Visible = true;
            GD.Print($"[ArtifactDetailUI] Set Visible to true");
            GD.Print($"[ArtifactDetailUI] Current Visible state after: {Visible}");
            GD.Print($"[ArtifactDetailUI] GlobalPosition: {GlobalPosition}");
            GD.Print($"[ArtifactDetailUI] Size: {Size}");
            GD.Print($"[ArtifactDetailUI] _backgroundPanel Visible: {_backgroundPanel?.Visible}");
            GD.Print($"[ArtifactDetailUI] _backgroundPanel GlobalPosition: {_backgroundPanel?.GlobalPosition}");
            GD.Print($"[ArtifactDetailUI] _backgroundPanel Size: {_backgroundPanel?.Size}");
            GD.Print($"[ArtifactDetailUI] ShowArtifact completed");
        }

        public void UpdateArtifact(ArtifactData artifact)
        {
            GD.Print($"[ArtifactDetailUI] UpdateArtifact called: artifact={artifact?.name}");

            if (artifact == null)
            {
                GD.PrintErr("[ArtifactDetailUI] UpdateArtifact: artifact is null!");
                return;
            }

            _currentArtifact = artifact;

            if (_nameLabel != null)
                _nameLabel.Text = artifact.name;

            if (_descriptionLabel != null)
                _descriptionLabel.Text = artifact.description;

            bool hasTexture = false;
            if (_iconRect != null && !string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    _iconRect.Texture = texture;
                    if (_emojiLabel != null) _emojiLabel.Visible = false;
                    hasTexture = true;
                }
            }

            if (!hasTexture)
            {
                if (_emojiLabel != null)
                {
                    _emojiLabel.Text = "💎";
                    _emojiLabel.Visible = true;
                }
                if (_iconRect != null)
                {
                    _iconRect.Visible = false;
                }
            }
            else if (_iconRect != null)
            {
                _iconRect.Visible = true;
            }

            GD.Print($"[ArtifactDetailUI] UpdateArtifact completed");
        }

        public void HideArtifact()
        {
            GD.Print($"[ArtifactDetailUI] HideArtifact called");

            Visible = false;
            _currentArtifact = null;

            GD.Print($"[ArtifactDetailUI] HideArtifact completed");
        }

        private void OnClosePressed()
        {
            GD.Print($"[ArtifactDetailUI] OnClosePressed called");

            HideArtifact();

            GD.Print($"[ArtifactDetailUI] OnClosePressed completed");
        }
    }
}
