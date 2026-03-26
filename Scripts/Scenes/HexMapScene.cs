using Godot;
using FishEatFish.UI.HexMap;
using FishEatFish.Battle.HexMap;

namespace FishEatFish.Scenes
{
    public partial class HexMapScene : Control
    {
        private HexMapUI _hexMapUI;
        private HexMapController _controller;

        public override void _Ready()
        {
            _controller = new HexMapController();
            AddChild(_controller);

            _controller.SetPlayerLevel(7);

            var hexMapUIScene = GD.Load<PackedScene>("res://Scenes/UI/HexMapUI.tscn");
            if (hexMapUIScene != null)
            {
                _hexMapUI = (HexMapUI)hexMapUIScene.Instantiate();
                AddChild(_hexMapUI);
            }
            else
            {
                GD.PrintErr("[HexMapScene] Failed to load HexMapUI scene!");
            }
        }
    }
}
