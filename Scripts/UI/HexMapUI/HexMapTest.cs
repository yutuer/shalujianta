using Godot;

namespace FishEatFish.UI.HexMap
{
    public partial class HexMapTest : Control
    {
        public override void _Ready()
        {
            GD.Print("[HexMapTest] 场景已加载");
            GD.Print("六边形布局说明:");
            GD.Print("  左上=a, 左中=b, 左下=c, 右下=d, 右中=e, 右上=f");
            GD.Print("  扁顶六边形(扁平面上下)，顶点左右");
            GD.Print("  关键公式: 水平间距 = √3 × r, 垂直间距 = 1.5 × r");
            GD.Print("  当3和4在同y时，中心距 = 2r");
        }

        public override void _Process(double delta)
        {
            if (Input.IsKeyPressed(Key.Space))
            {
                GD.Print("=== HexShape位置坐标 ===");
                foreach (Node child in GetChildren())
                {
                    if (child.Name.ToString().StartsWith("Hex"))
                    {
                        var hexShape = child.GetNodeOrNull("HexShape");
                        if (hexShape != null)
                        {
                            var pos = hexShape.Get("position");
                            var parentPos = child.Get("position");
                            GD.Print($"{child.Name}: Parent({parentPos}), HexShape相对位置({pos})");
                        }
                    }
                }
                GD.Print("=========================");
            }
        }
    }
}
