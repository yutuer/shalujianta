using Godot;
using FishEatFish.UI.HexMap;
using FishEatFish.Battle.HexMap;
using FishEatFish.UI;

namespace FishEatFish.Scenes
{
    public partial class HexMapScene : Control
    {
        private HexMapUI _hexMapUI;
        private HexMapController _controller;
        private CommonHUD _commonHUD;

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

            // 顶部公共 HUD（地图 / 战斗 / 商店共用）
            var commonHudScene = GD.Load<PackedScene>("res://Scenes/UI/CommonHUD.tscn");
            if (commonHudScene != null)
            {
                _commonHUD = (CommonHUD)commonHudScene.Instantiate();
                // 地图左上角已有自己的怒气圈显示，这里不重复显示角色圆环
                _commonHUD.ShowTeamSection = false;
				// 地图场景已有既有的 BackpackUI 造物栏（常驻顶部、支持点击查看详情），
				// HUD 再显示一套 8 格收集品会和它重叠，所以关掉 HUD 这一套。
				_commonHUD.ShowCollectionSection = false;
                AddChild(_commonHUD);
            }
            else
            {
                GD.PrintErr("[HexMapScene] Failed to load CommonHUD scene!");
            }
        }
    }
}
