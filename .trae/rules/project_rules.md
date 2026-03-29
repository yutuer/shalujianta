# 项目规则

## 第一准则：逻辑和页面分离 (最高优先级)

**这是项目开发的第一准则，所有 UI 组件和游戏逻辑都必须遵循此原则。**

### 核心原则

在 Godot 引擎中开发时，**必须将游戏逻辑/业务逻辑与 UI 页面/场景文件严格分离**。

### 什么是"逻辑和页面分离"？

1. **页面（Scene/.tscn 文件）**：负责 UI 的视觉呈现、布局、样式、交互响应
2. **逻辑（Script/.cs 文件）**：负责数据处理、业务规则、状态管理

### 具体要求

#### ✅ 正确的做法

| 职责 | 放在哪里 |
|------|---------|
| UI 布局、尺寸、颜色、字体 | `.tscn` 场景文件 |
| 按钮点击响应、事件绑定 | `.cs` 脚本文件 |
| 动态数据（名称、数值、图标） | `.cs` 脚本文件 |
| 业务逻辑、状态判断 | `.cs` 脚本文件 |
| 动态生成的节点（如列表项） | `.cs` 脚本文件 |

#### ❌ 错误的做法

```csharp
// ❌ 禁止：在脚本中硬编码 UI 布局
public override void _Ready()
{
    var panel = new PanelContainer();
    panel.CustomMinimumSize = new Vector2(400, 300);
    // ... 更多硬编码布局 ...
}
```

```gdscript
# ❌ 禁止：在场景文件中写业务逻辑
[node name="Button"]
script = ExtResource("Logic")
func _ready():
    # 业务逻辑不应该在 _ready() 中写
```

### 为什么要分离？

1. **可维护性**：UI 设计师可以修改 .tscn 不影响逻辑，程序员可以修改逻辑不影响 UI
2. **可测试性**：逻辑代码可以独立单元测试
3. **团队协作**：不同成员可以并行工作
4. **性能优化**：Godot 对场景文件有更好的优化
5. **代码清晰**：每个文件职责单一，易于理解

### 具体实施

#### 对于 UI 组件（如 Dialog、Panel、Card）

1. **创建独立的 .tscn 文件**定义 UI 结构
2. **创建独立的 .cs 文件**处理逻辑
3. **在 .tscn 中引用脚本**：`script = ExtResource("x_script")`
4. **脚本中使用按需加载**：减少初始加载时间

#### 对于动态内容

- **列表项（如商品卡、卡牌）**：基础样式在 .tscn 中，动态数据在代码中设置
- **状态变化**：通过代码修改 .tscn 中定义的节点属性

### 违反此准则的后果

- 代码难以维护和扩展
- UI 和逻辑耦合导致修改困难
- 团队协作效率降低
- 性能可能下降

---

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

## 调试日志规则 (重要)

### UI 问题调试
当出现 UI 显示问题时（如商店空白、组件不显示等），**一次性**在所有相关的 UI 组件中添加完整的调试日志，包括：
- `_Ready()` 方法调用和组件初始化
- 数据设置方法（如 `SetItem`、`SetData` 等）中的每一步
- 子节点的获取结果
- 事件绑定结果
- UI 可见性设置

**不要让用户多次测试，每次都要完整添加所有相关日志。**

### 编译规则
- 修改代码后必须运行 `dotnet build` 编译确认
- 编译成功后再让用户测试

### 事件回调方法日志规则 (重要)
在给组件（Button、Control等）事件实现回调方法时，必须遵循以下规则：

**示例**：
```csharp
private void OnConfirmPressed()
{
    GD.Print($"[ClassName] OnConfirmPressed called");
    // ... 方法逻辑 ...
    GD.Print($"[ClassName] OnConfirmPressed completed");
}

private void OnCancelPressed()
{
    GD.Print($"[ClassName] OnCancelPressed called");
    // ... 方法逻辑 ...
    GD.Print($"[ClassName] OnCancelPressed completed");
}
```

**要求**：
1. 方法的第一行必须是 `GD.Print($"[类名] 方法名 called")`
2. 方法的最后一行必须是 `GD.Print($"[类名] 方法名 completed")`
3. 日志格式：`[类名]` 中使用实际类名（如 `HexMapUI`、`KeyOrderDescriptionUI` 等）
4. 方法名使用驼峰命名（如 `OnConfirmPressed`）

**适用场景**：
- Button 的 `Pressed` 事件
- Control 的 `MouseEntered`、`MouseExited` 事件
- 其他任何组件事件回调

**这样做的目的**：便于调试，当出现问题时可以快速追踪方法的调用链。

## UI 组件编写规则 (重要)

### 优先使用 .tscn 场景文件

编写页面组件（Panel、Dialog、Card Selection 等 UI 组件）时，**必须优先使用 .tscn 场景文件**，而不是代码硬编码。

**原因**：
1. **可视化编辑**：可以在 Godot 编辑器中直观地布局和调整 UI
2. **易于维护**：设计师和开发者可以独立修改 UI 外观而不影响逻辑
3. **减少硬编码**：避免代码中充斥着大量布局属性设置
4. **性能更好**：Godot 对场景文件有更好的优化

**正确做法**：
1. 在 `Scenes/UI/` 目录下创建 `.tscn` 场景文件
2. 在编辑器中设计 UI 布局、样式、尺寸等
3. 代码只负责：
   - 获取节点引用
   - 绑定事件回调
   - 设置动态数据
   - 处理业务逻辑

**错误做法**：
```csharp
// ❌ 不推荐：使用代码硬编码所有 UI 元素
var panel = new PanelContainer();
panel.CustomMinimumSize = new Vector2(800, 500);
// ... 几十行布局代码 ...
```

**正确做法**：
```csharp
// ✅ 推荐：将 UI 布局放在 .tscn 文件中
private PanelContainer _panel;

public override void _Ready()
{
    _panel = GetNode<PanelContainer>("Panel");
    _confirmButton = GetNode<Button>("Panel/ButtonContainer/ConfirmButton");
    _confirmButton.Pressed += OnConfirmPressed;
}
```

**何时可以使用代码创建 UI**：
1. 动态生成的列表项（如卡牌列表、商品列表）
2. 需要根据数据动态创建的可变数量元素
3. 简单的内联提示框

**对于卡牌、物品等动态元素**：
- 基础容器/样式可以在 .tscn 中定义
- 动态内容（如卡牌名称、数值）通过代码填充

