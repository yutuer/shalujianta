using Godot;
using FishEatFish.Shop;
using System.Linq;

namespace FishEatFish.UI.BackpackUI
{
    public partial class ClickableIcon : TextureRect
    {
        private ArtifactData _artifact;

        public System.Action<ArtifactData> OnIconClicked;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
        }

        public void SetArtifact(ArtifactData artifact)
        {
            _artifact = artifact;

            foreach (var child in GetChildren().ToList())
            {
                child.QueueFree();
            }

            bool hasTexture = false;
            if (!string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    Texture = texture;
                    hasTexture = true;
                }
            }

            if (!hasTexture)
            {
                var emoji = new Label();
                emoji.Name = "Emoji";
                emoji.Text = "💎";
                emoji.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                emoji.HorizontalAlignment = HorizontalAlignment.Center;
                emoji.VerticalAlignment = VerticalAlignment.Center;
                emoji.MouseFilter = Control.MouseFilterEnum.Ignore;
                AddChild(emoji);
            }
        }

        public string GetArtifactName()
        {
            return _artifact?.name ?? "null";
        }

        public ArtifactData GetArtifact()
        {
            return _artifact;
        }
    }
}
