# HexMapUI 重构计划

## 目标
将 HexMapUI 中的复杂 UI 组件拆分到独立的场景文件中，遵循 Godot 的模块化设计原则。

## 拆分结果

### 已完成 (Stage 1-5)

#### 阶段 1: 基础面板 (已 ✅)
1. ✅ **FailurePanel** - 失败面板
   - 脚本: `Scripts/UI/FailurePanel/FailurePanel.cs`
   - 场景: `Scenes/UI/FailurePanel.tscn`
   - 功能: 显示游戏失败信息

2. ✅ **BackpackUI** - 背包界面
   - 脚本: `Scripts/UI/BackpackUI/BackpackUI.cs`
   - 场景: `Scenes/UI/BackpackUI.tscn`
   - 功能: 显示玩家拥有的神器

3. ✅ **TeleportDialog** - 传送对话框
   - 脚本: `Scripts/UI/TeleportDialog/TeleportDialog.cs`
   - 场景: `Scenes/UI/TeleportDialog.tscn`
   - 功能: 传送门确认/取消

#### 阶段 2: 描述面板 (已 ✅)
4. ✅ **EngravingDescriptionUI** (原名 KeyOrderDescriptionUI) - 刻印描述面板
   - 脚本: `Scripts/UI/EngravingDescriptionUI/EngravingDescriptionUI.cs`
   - 场景: `Scenes/UI/EngravingDescriptionUI.tscn`
   - 功能: 显示刻印道具详情

5. ✅ **ArtifactDescriptionUI** - 神器描述面板
   - 脚本: `Scripts/UI/ArtifactDescriptionUI/ArtifactDescriptionUI.cs`
   - 场景: `Scenes/UI/ArtifactDescriptionUI.tscn`
   - 功能: 显示神器购买详情

#### 阶段 3: 卡牌选择组件 (已 ✅)
6. ✅ **CardSelectionItem** - 卡牌选择项
   - 脚本: `Scripts/UI/CardSelectionItem/CardSelectionItem.cs`
   - 场景: `Scenes/UI/CardSelectionItem.tscn`
   - 功能: 刻印卡牌选择列表项

7. ✅ **EngravingCardSelectionUI** - 刻印卡牌选择界面
   - 脚本: `Scripts/UI/EngravingCardSelectionUI/EngravingCardSelectionUI.cs`
   - 场景: `Scenes/UI/EngravingCardSelectionUI.tscn`
   - 功能: 显示可刻印的卡牌列表

#### 阶段 4: 商店界面 (已 ✅)
8. ✅ **ShopUI** - 商店界面
   - 脚本: `Scripts/UI/ShopUI/ShopUI.cs`
   - 场景: `Scenes/UI/ShopUI.tscn`
   - 功能: 商店商品展示和购买

#### 阶段 5: HexMapUI 清理 (已 ✅)
9. ✅ 删除内联组件节点
   - 从 HexMapUI.tscn 移除 ShopContainer, EngravingSelectContainer, KeyOrderDescriptionUI, ArtifactDescriptionUI 等节点
   - HexMapUI.tscn 从 550 行减少到 119 行

10. ✅ 动态加载组件
    - 在 InitializeComponents() 中动态加载各组件场景
    - 使用 PackedScene.Instantiate() 和 AddChild()

11. ✅ 清理旧代码
    - 删除不再使用的字段和方法
    - 删除不再需要的 Export 属性
    - HexMapUI.cs 从 1175 行减少到 792 行

## 统计

### 文件行数变化
| 文件 | 重构前 | 重构后 | 减少 |
|------|--------|--------|------|
| HexMapUI.tscn | 550 | 119 | -431 行 |
| HexMapUI.cs | 1175 | 792 | -383 行 |

### 新增文件
- 脚本文件: 8 个
- 场景文件: 7 个

## 组件依赖关系

```
HexMapUI
├── FailurePanel (动态加载)
├── BackpackUI (动态加载)
├── TeleportDialog (动态加载)
├── ShopUI (动态加载)
│   └── ShopItemCard (动态创建)
├── ArtifactDescriptionUI (动态加载)
├── EngravingDescriptionUI (动态加载)
└── EngravingCardSelectionUI (动态加载)
    └── CardSelectionItem (动态创建)
```

## 命名空间规范

遵循 `FishEatFish.UI.{ComponentName}` 命名空间规范：
- `FishEatFish.UI.FailurePanel.FailurePanel`
- `FishEatFish.UI.BackpackUI.BackpackUI`
- `FishEatFish.UI.ShopUI.ShopUI`
- `FishEatFish.UI.EngravingCardSelectionUI.EngravingCardSelectionUI`

## 动态加载模式

```csharp
var scene = GD.Load<PackedScene>("res://Scenes/UI/{Component}.tscn");
var node = scene.Instantiate();
AddChild(node);
_component = node as FishEatFish.UI.{Component}.{Component};
```

## 注意事项

1. **EngravingSelectContainer 废弃**: 旧的刻印选择界面已被 EngravingCardSelectionUI 取代
2. **ShopItemCard 子场景**: ShopUI 内部会动态创建 ShopItemCard 实例
3. **事件委托**: 组件通过事件与 HexMapUI 通信

## 验收标准

- [x] 编译无错误
- [x] HexMapUI.tscn 行数 < 150
- [x] 所有组件独立可加载
- [x] 功能与重构前一致

## 执行日期
2026-03-28
