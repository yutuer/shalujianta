using Godot;
using FishEatFish.Shop;

namespace FishEatFish.UI.BackpackUI
{
    public partial class BackpackUI : PanelContainer
    {
        private HBoxContainer _itemsContainer;

        public override void _Ready()
        {
            _itemsContainer = GetNodeOrNull<HBoxContainer>("HBoxContainer/BackpackItems");
        }

        public void Refresh()
        {
            if (_itemsContainer == null)
            {
                GD.PrintErr("[BackpackUI] _itemsContainer is null!");
                return;
            }

            foreach (var child in _itemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (FishEatFish.Shop.BlackMarkShopManager.Instance == null)
            {
                GD.PrintErr("[BackpackUI] BlackMarkShopManager.Instance is null!");
                return;
            }

            var ownedArtifacts = FishEatFish.Shop.BlackMarkShopManager.Instance.GetOwnedArtifacts();

            foreach (var artifact in ownedArtifacts)
            {
                TextureRect icon = null;
                bool hasValidTexture = false;

                if (!string.IsNullOrEmpty(artifact.icon))
                {
                    var texture = GD.Load<Texture2D>(artifact.icon);
                    if (texture != null)
                    {
                        icon = new TextureRect();
                        icon.Name = $"Icon_{artifact.artifactId}";
                        icon.CustomMinimumSize = new Vector2(40, 40);
                        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        icon.Texture = texture;
                        icon.TooltipText = $"{artifact.name}: {artifact.description}";
                        hasValidTexture = true;
                    }
                }

                if (!hasValidTexture)
                {
                    icon = CreateEmojiBackpackItem(artifact);
                }

                _itemsContainer.AddChild(icon);
            }
        }

        private TextureRect CreateEmojiBackpackItem(ArtifactData artifact)
        {
            var container = new TextureRect();
            container.Name = $"Icon_{artifact.artifactId}";
            container.CustomMinimumSize = new Vector2(40, 40);
            container.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            container.TooltipText = $"{artifact.name}: {artifact.description}";

            if (!string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    container.Texture = texture;
                    return container;
                }
            }

            var label = new Label();
            label.Text = "💎";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            container.AddChild(label);
            return container;
        }
    }
}
