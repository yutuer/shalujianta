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

        private ArtifactData _currentArtifact;

        public override void _Ready()
        {
            GD.Print($"[ArtifactDetailUI] _Ready called");

            _iconRect = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/IconRect");
            _emojiLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/IconContainer/EmojiLabel");
            _nameLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/NameLabel");
            _typeLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/HeaderContainer/TitleContainer/TypeLabel");
            _descriptionLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/DescriptionLabel");
            _closeButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/CloseButton");

            if (_closeButton != null)
            {
                _closeButton.Pressed += OnClosePressed;
            }

            Visible = false;

            GD.Print($"[ArtifactDetailUI] _Ready completed");
        }

        public void ShowArtifact(ArtifactData artifact)
        {
            GD.Print($"[ArtifactDetailUI] ShowArtifact called: artifact={artifact?.name}");

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

            Visible = true;

            GD.Print($"[ArtifactDetailUI] ShowArtifact completed");
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
