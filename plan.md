# 商店Bug修复计划

## 问题分析

### 1. 商店物品刷新问题
**现状**：每次进入商店都重新生成3个物品，已购买的物品在下次进入时又出现
**原因**：`GenerateShopItems()` 每次都清空并重新生成
**解决方案**：
- 修改 `BlackMarkShopManager`，将生成的物品和已购买的物品持久化
- `CurrentShopItems` 只在首次进入时生成，之后保持不变
- 购买后从 `CurrentShopItems` 移除，不重新生成

### 2. 造物没有显示在背包
**现状**：购买造物后，背包里没有显示
**原因**：需要检查 `BackpackContainer` 的显示逻辑
**解决方案**：
- 检查 `HexMapUI.cs` 中 `RefreshBackpack()` 的实现
- 确保购买造物时调用了 `RefreshBackpack()`

### 3. 造物增加图标
**现状**：造物没有图标
**解决方案**：
- 为每个造物配置图标路径
- 或者使用表情符号作为临时图标

### 4. 卡牌位置摆放
**现状**：3个卡牌位置不好看
**解决方案**：
- 调整 `ShopItemCard.tscn` 或 `HexMapUI.tscn` 中 `ShopItems` (GridContainer) 的布局
- 设置 `columns = 3`
- 让卡牌平均分布

### 5. 商店背景高度问题
**现状**：第二次进入商店，背景高度增加
**原因**：可能是在 `RefreshShopItems()` 中 `QueueFree()` 没有正确执行
**解决方案**：
- 检查清理逻辑
- 可能需要用 `Visible = false` 替代 `QueueFree()`

### 6. 购买后占位但不可购买
**现状**：购买后物品消失，下次进入可能又出现
**解决方案**：
- 购买后物品依然保留在 `CurrentShopItems` 中
- 设置物品的 `Purchased = true` 标志
- UI 显示时检查此标志，按钮禁用，显示"已售出"状态
- 下次进入时物品依然存在但不可购买

### 7. 商店卡牌内部组件布局优化

#### 7.1 卡牌布局规范

**统一布局样式**：造物和刻印类卡牌采用完全一致的布局结构

**整体居中对齐**：所有卡牌元素水平垂直居中

**布局层次结构**：
```
┌─────────────────────────────┐
│                             │
│        [图标/表情]           │  <- 中间区域：图标区域 80x80
│                             │
│         物品名称             │  <- 顶部：名称标签
│                             │
│    💰 黑印数量               │  <- 底部：价格显示
│                             │
└─────────────────────────────┘
```

**重要修改**：移除所有卡牌上的"购买"按钮，改为点击卡牌跳转详情页

#### 7.2 卡牌交互功能

**点击事件**：
- 点击任意卡牌（造物/刻印）→ 跳转到对应的详情页面
- 刻印卡牌详情页面：复用现有 `KeyOrderDescriptionUI.tscn` 界面
- 造物卡牌详情页面：新建 `ArtifactDescriptionUI.tscn`，布局与刻印详情页完全一致

#### 7.3 详情页面交互逻辑

**造物详情页面 (ArtifactDescriptionUI)**：

| 按钮 | 功能 |
| :--- | :--- |
| 取消 | 点击后直接返回商店初始页面 |
| 确定 | 扣除对应数量黑印 → 执行购买逻辑 → 返回商店页面 |

**刻印详情页面 (KeyOrderDescriptionUI)**：

| 按钮 | 功能 |
| :--- | :--- |
| 取消 | 点击后直接返回商店页面 |
| 确定 | 弹出新界面，显示玩家卡牌组合中所有未被刻印的卡牌 |

**新增卡牌选择界面 (EngravingCardSelectionUI)**：

```
┌─────────────────────────────────────┐
│         选择要刻印的卡牌             │
├─────────────────────────────────────┤
│                                     │
│   [卡牌1]  [卡牌2]  [卡牌3]  ...    │  <- 可滚动画廊
│                                     │
├─────────────────────────────────────┤
│                            [确定]   │  <- 右下角，初始禁用
└─────────────────────────────────────┘
```

**交互规则**：
- "确定"按钮：初始状态为置灰不可点击
- 玩家选择一张卡牌后，"确定"按钮变为可点击状态
- 点击"确定"后：为选中卡牌执行刻印操作 → 返回商店页面

#### 7.4 卡牌显示效果

**已刻印卡牌标识**：
- 在已刻印的卡牌 NameLabel 下方显示刻印 buff 的图标
- 刻印图标样式与 `BuffEngraving.json` 中定义的图标一致

#### 7.5 具体实现文件

| 文件 | 用途 |
| :--- | :--- |
| `ShopItemCard.tscn` | 移除购买按钮，修改为可点击 |
| `ShopItemCard.cs` | 添加点击事件，跳转到详情页 |
| `ArtifactDescriptionUI.tscn` | 新建造物详情页 |
| `ArtifactDescriptionUI.cs` | 造物详情页逻辑 |
| `KeyOrderDescriptionUI.tscn` | 复用刻印详情页 |
| `KeyOrderDescriptionUI.cs` | 刻印详情页逻辑 |
| `EngravingCardSelectionUI.tscn` | 新建刻印卡牌选择页 |
| `EngravingCardSelectionUI.cs` | 刻印卡牌选择页逻辑 |
| `HexMapUI.cs` | 添加详情页显示/隐藏逻辑 |

### 8. 商店卡牌交互逻辑优化

#### 8.1 悬停效果

- 鼠标悬停在卡片上时，卡片轻微放大或高亮
- 鼠标移出时恢复正常状态

#### 8.2 购买流程

1. 点击卡牌 → 进入详情页
2. 检查黑印是否足够
3. 黑印足够 → 点击"确定"购买
4. 黑印不足 → 显示"黑印不足"提示
5. 购买成功 → 播放成功动画
6. 关闭商店时刷新背包

#### 8.3 具体实现

- 添加 `OnMouseEntered` 和 `OnMouseExited` 事件
- 添加黑印不足的视觉反馈
- 添加购买成功/失败的动画

## 实施步骤

### Step 1: 修复商店物品持久化 ✅
- 修改 `BlackMarkShopManager.cs`
  - 添加 `_purchasedItemIds` 列表存储已购买物品ID
  - 修改 `GenerateShopItems()` 逻辑，过滤已购买物品
  - 修改购买逻辑，将物品ID添加到 `_purchasedItemIds`

### Step 2: 检查背包显示 ✅
- 修改 `BuyArtifact()` 确保调用 `RefreshBackpack()`
- 检查 `RefreshBackpack()` 的实现

### Step 3: 添加造物图标 ✅
- 修改 `artifacts.json` 添加 `icon` 字段
- 或在 `BlackMarkShopManager` 中为没有图标的造物设置默认图标

### Step 4: 调整卡牌布局 ✅
- 修改 `HexMapUI.tscn` 中 `ShopItems` 的 GridContainer 属性
- 设置 `custom_minimum_size` 或调整 `columns`

### Step 5: 修复背景高度问题 🔄
- 在 `RefreshShopItems()` 中添加调试日志
- 检查 `RemoveChild()` 是否正常工作
- 待测试验证

### Step 6: 购买后占位但不可购买 ✅
- 修改购买逻辑，设置 `Purchased = true`
- 修改 UI 显示已售出状态

### Step 7: 优化卡牌内部布局
**New Implementation Required**

| 子任务 | 状态 |
| :--- | :-- |
| 7.1 修改 ShopItemCard 布局：移除购买按钮，改为顶部名称-中间图标-底部价格 | ⬜ |
| 7.2 修改 ShopItemCard.cs：添加点击事件 | ⬜ |
| 7.3 创建 ArtifactDescriptionUI.tscn（与刻印详情页布局一致） | ⬜ |
| 7.4 创建 ArtifactDescriptionUI.cs：实现确定/取消逻辑 | ⬜ |
| 7.5 复用 KeyOrderDescriptionUI.tscn 作为刻印详情页 | ⬜ |
| 7.6 修改 KeyOrderDescriptionUI.cs：添加确定按钮跳转逻辑 | ⬜ |
| 7.7 创建 EngravingCardSelectionUI.tscn | ⬜ |
| 7.8 创建 EngravingCardSelectionUI.cs：实现卡牌选择逻辑 | ⬜ |
| 7.9 修改 HexMapUI.cs：添加详情页显示/隐藏逻辑 | ⬜ |
| 7.10 修改卡牌显示：已刻印卡牌显示刻印图标 | ⬜ |

### Step 8: 添加交互效果
- 添加鼠标悬停效果
- 添加黑印不足的视觉反馈
- 添加购买成功动画

## 关键文件

### 现有文件
- `scripts/Shop/BlackMarkShopManager.cs`
- `scripts/UI/HexMapUI/HexMapUI.cs`
- `scripts/UI/HexMapUI/ShopItemCard.cs`
- `Scenes/UI/HexMapUI.tscn`
- `Scenes/UI/ShopItemCard.tscn`
- `Data/artifacts.json`
- `Data/BuffEngraving.json`

### 需要新建/修改的文件
- `Scenes/UI/ArtifactDescriptionUI.tscn` ⬜ (新建)
- `Scripts/UI/HexMapUI/ArtifactDescriptionUI.cs` ⬜ (新建)
- `Scenes/UI/EngravingCardSelectionUI.tscn` ⬜ (新建)
- `Scripts/UI/HexMapUI/EngravingCardSelectionUI.cs` ⬜ (新建)
- `Scripts/UI/HexMapUI/KeyOrderDescriptionUI.cs` ⬜ (修改)

### 复用文件
- `Scenes/UI/KeyOrderDescriptionUI.tscn` (复用现有)
