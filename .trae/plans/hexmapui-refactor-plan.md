# HexMapUI 重构计划

## 目标
将 HexMapUI 中的复杂 UI 组件拆分到独立的场景文件中，遵循 Godot 的模块化设计原则。

## 重构进度

### ✅ 阶段 1: 基础面板 (已完成)
1. ✅ **FailurePanel** - 失败面板
   - 脚本: `Scripts/UI/FailurePanel/FailurePanel.cs`
   - 场景: `Scenes/UI/FailurePanel.tscn`

2. ✅ **BackpackUI** - 背包界面
   - 脚本: `Scripts/UI/BackpackUI/BackpackUI.cs`
   - 场景: `Scenes/UI/BackpackUI.tscn`

3. ✅ **TeleportDialog** - 传送对话框
   - 脚本: `Scripts/UI/TeleportDialog/TeleportDialog.cs`
   - 场景: `Scenes/UI/TeleportDialog.tscn`

### ✅ 阶段 2: 描述面板 (已完成)
4. ✅ **EngravingDescriptionUI** - 刻印描述面板
   - 脚本: `Scripts/UI/EngravingDescriptionUI/EngravingDescriptionUI.cs`
   - 场景: `Scenes/UI/EngravingDescriptionUI.tscn`

5. ✅ **ArtifactDescriptionUI** - 神器描述面板
   - 脚本: `Scripts/UI/ArtifactDescriptionUI/ArtifactDescriptionUI.cs`
   - 场景: `Scenes/UI/ArtifactDescriptionUI.tscn`

### ✅ 阶段 3: 卡牌选择组件 (已完成)
6. ✅ **CardSelectionItem** - 卡牌选择项
   - 脚本: `Scripts/UI/CardSelectionItem/CardSelectionItem.cs`
   - 场景: `Scenes/UI/CardSelectionItem.tscn`

7. ✅ **EngravingCardSelectionUI** - 刻印卡牌选择界面
   - 脚本: `Scripts/UI/EngravingCardSelectionUI/EngravingCardSelectionUI.cs`
   - 场景: `Scenes/UI/EngravingCardSelectionUI.tscn`

### ✅ 阶段 4: 商店界面整合 (已完成)
8. ✅ **ShopUI** - 商店界面 (整合所有子组件)
   - 脚本: `Scripts/UI/ShopUI/ShopUI.cs`
   - 场景: `Scenes/UI/ShopUI.tscn` (包含子组件节点树)

### ✅ 阶段 5: HexMapUI 清理 (已完成)
9. ✅ 删除内联组件节点
   - HexMapUI.tscn 从 550 行减少到 119 行

10. ✅ 动态加载组件
    - 在 InitializeComponents() 中动态加载各组件场景

11. ✅ 清理旧代码
    - HexMapUI.cs 从 1175 行减少到 655 行

## 统计

### 文件行数变化
| 文件 | 重构前 | 重构后 | 减少 |
|------|--------|--------|------|
| HexMapUI.tscn | 550 | 119 | **-431 行** |
| HexMapUI.cs | 1175 | 655 | **-520 行** |
| ShopUI.tscn (新增) | 0 | 307 | **+307 行** |

### 新增文件
- 脚本文件: 8 个
- 场景文件: 7 个

## 组件依赖关系

```
HexMapUI
├── FailurePanel (动态加载)
├── BackpackUI (动态加载)
├── TeleportDialog (动态加载)
└── ShopUI (动态加载) ← 整合所有商店相关组件
    ├── ShopItemCard (动态创建)
    ├── ArtifactDescriptionUI (节点树)
    ├── EngravingDescriptionUI (节点树)
    └── EngravingCardSelectionUI (节点树)
        └── CardSelectionItem (动态创建)
```

## 命名空间规范

遵循 `FishEatFish.UI.{ComponentName}` 命名空间规范：
- `FishEatFish.UI.FailurePanel.FailurePanel`
- `FishEatFish.UI.BackpackUI.BackpackUI`
- `FishEatFish.UI.ShopUI.ShopUI`
- `FishEatFish.UI.EngravingCardSelectionUI.EngravingCardSelectionUI`

## 事件通信模式

```
HexMapUI
    └── ShopUI (管理所有商店子组件)
        ├── OnShopItemClicked → 点击商店商品
        ├── OnEngravingItemConfirmed → 刻印道具确认
        ├── OnArtifactItemConfirmed → 神器购买确认
        ├── OnEngravingCompleted → 刻印完成
        ├── OnCloseClicked → 关闭商店
        └── OnRefreshClicked → 刷新商店
```

## ShopUI 公共接口

```csharp
// 刷新商店商品
public void RefreshShopItems(List<ShopItem> items)

// 显示/隐藏
public void ShowShop()
public void HideShop()

// 子组件显示
public void ShowArtifactDescription(ShopItem item)
public void ShowEngravingDescription(ShopItem item)
public void ShowEngravingCardSelection(ShopItem item, List<Card> cards)

// 子组件隐藏
public void HideArtifactDescription()
public void HideEngravingDescription()
public void HideEngravingCardSelection()

// 交互控制
public void SetInteractionEnabled(bool enabled)
public void SetRefreshCount(int count)
public int GetRefreshCount()
public bool TrySpendRefresh()
```

## 验收标准

- [x] 编译无错误
- [x] HexMapUI.tscn 行数 < 150
- [x] 所有组件独立可加载
- [x] 功能与重构前一致

## 执行日期
2026-03-28
