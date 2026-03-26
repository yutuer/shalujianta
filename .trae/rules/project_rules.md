# 项目规则

## 六边形地图算法 (重要)

### 坐标系统
- 使用 **axial 坐标** (Q, R)
- 六边形类型：**扁顶 (flat-top)**，顶点左右

### 顶点定义（相对于 Control 左上角）
```
V0 = (60, 0)   - 顶部
V1 = (110, 26) - 右上
V2 = (110, 78) - 右下
V3 = (60, 104) - 底部
V4 = (10, 78)  - 左下
V5 = (10, 26)  - 左上
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
x = 100 * Q + 50 * R
y = 78 * R
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
- 宽：120px
- 高：104px
- 中心：(60, 52)
- 水平间距：100
- 垂直间距：78

### 关键文件
- `Scripts/UI/HexMapUI/HexMapUI.cs` - 坐标转换逻辑
- `Scripts/UI/HexMapUI/HexTileView.cs` - 六边形绘制
- `Scripts/Battle/HexMap/HexCoord.cs` - 坐标定义
- `Scenes/HexMapTest.tscn` - 测试场景

### 快速使用
```csharp
// Axial坐标转屏幕坐标
Vector2 HexToWorld(HexCoord coord) {
    float x = 100f * coord.Q + 50f * coord.R;
    float y = 78f * coord.R;
    return new Vector2(x, y);
}
```
