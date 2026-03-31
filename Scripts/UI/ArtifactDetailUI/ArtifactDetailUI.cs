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
        private PanelContainer _backgroundPanel;

        private ArtifactData _currentArtifact;

        public override void _Ready()
        {
            _backgroundPanel = GetNodeOrNull<PanelContainer>("BackgroundPanel");
            _iconRect = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/IconRect");
            _emojiLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/EmojiLabel");
            _nameLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/NameLabel");
            _typeLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/TypeLabel");
            _descriptionLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/DescriptionLabel");

            Visible = false;
        }

        public override void _Input(InputEvent @event)
        {
            if (!Visible)
            {
                return;
            }

            if (@event is InputEventMouseButton mouseEvent &&
                mouseEvent.Pressed &&
                mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (_backgroundPanel == null)
                {
                    return;
                }

                var mousePos = GetViewport().GetMousePosition();
                var bgRect = _backgroundPanel.GetGlobalRect();

                if (bgRect.HasPoint(mousePos))
                {
                    HideArtifact();
                }
            }
        }

        public override void _EnterTree()
        {
            base._EnterTree();
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }

        public void ShowArtifact(ArtifactData artifact)
        {
            if (artifact == null)
            {
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

            Visible = true;
        }

        public void UpdateArtifact(ArtifactData artifact)
        {
            if (artifact == null)
            {
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
        }

        public void HideArtifact()
        {
            Visible = false;
            _currentArtifact = null;
        }

        public bool IsPointInsideDetail(Vector2 globalMousePos)
        {
            if (!Visible || _backgroundPanel == null)
            {
                return false;
            }

            return _backgroundPanel.GetGlobalRect().HasPoint(globalMousePos);
        }

    }
}
