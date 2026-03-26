# 项目规则

## 六边形地图算法 (重要)

### 坐标系统
- 使用 **axial 坐标** (Q, R)
- 六边形类型：**扁顶 (flat-top)**，顶点左右

### 顶点定义（相对于 Control 左上角）
```
V0 = (90, 0)    - 顶部
V1 = (165, 39)  - 右上
V2 = (165, 117) - 右下
V3 = (90, 156)  - 底部
V4 = (15, 117)  - 左下
V5 = (15, 39)   - 左上
```

### 边的定义
```
a = V5→V0 (左上)
b = V4→V5 (左中，垂直)
c = V3→V4 (左下)
d = V2→V3 (右下)
e = V1→V2 (右中，垂直)
f = V0→V1 (右上)
```

### 坐标转换公式
```
x = 150 * Q + 75 * R
y = 117 * R
```

### HexCoord 方向向量
| 方向 | Q, R 增量 |
|------|----------|
| North | (0, -1) |
| South | (0, 1) |
| NorthEast | (1, -1) |
| NorthWest | (-1, 0) |
| SouthEast | (1, 0) |
| SouthWest | (-1, 1) |

### 六边形尺寸
- 宽：180px
- 高：156px
- 中心：(90, 78)
- 水平间距：150
- 垂直间距：117

### 地图配置
- 默认半径：5（生成 11x11 = 121 个格子）
- 相机跟随：玩家所在格子始终居中
- 移动动画：0.3秒平滑过渡

### 关键文件
- `Scripts/UI/HexMapUI/HexMapUI.cs` - 坐标转换逻辑、相机跟随
- `Scripts/UI/HexMapUI/HexTileView.cs` - 六边形绘制
- `Scripts/Battle/HexMap/HexCoord.cs` - 坐标定义
- `Scripts/Battle/HexMap/HexMapController.cs` - 地图控制器
- `Scripts/Battle/HexMap/HexMapGenerator.cs` - 地图生成器

### 快速使用
```csharp
// Axial坐标转屏幕坐标
Vector2 HexToWorld(HexCoord coord) {
    float x = 150f * coord.Q + 75f * coord.R;
    float y = 117f * coord.R;
    return new Vector2(x, y);
}

// 相机跟随玩家
void CenterOnPlayer(HexCoord playerCoord, bool animate = true) {
    var screenCenter = GetViewportRect().Size / 2;
    var playerWorldPos = HexToWorld(playerCoord) + _hexSize / 2;
    var targetOffset = screenCenter - playerWorldPos;
    
    if (animate) {
        var tween = CreateTween();
        tween.TweenProperty(_tileViewsContainer, "position", targetOffset, 0.3f);
    } else {
        _tileViewsContainer.Position = targetOffset;
    }
}
```
