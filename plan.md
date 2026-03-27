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
**现状**：当前卡牌布局简单，缺少视觉效果和详细信息展示
**解决方案**：

#### 造物卡片 (ShopItemCard) 布局优化
```
┌─────────────────────────────┐
│        [图标/表情]           │  <- 图标区域 80x80
│                             │
│         造物名称             │  <- 名称标签
│                             │
│   效果类型图标  + 效果描述    │  <- 效果说明区
│                             │
│    💰 价格    [购买按钮]      │  <- 价格和按钮行
└─────────────────────────────┘
```

#### 刻印卡片布局优化
```
┌─────────────────────────────┐
│        [图标/表情]           │  <- 图标区域
│                             │
│         刻印名称             │  <- 名称标签
│                             │
│    效果类型图标  + 效果描述   │  <- 效果说明区
│                             │
│    💰 价格    [选择刻印]      │  <- 价格和选择按钮
└─────────────────────────────┘
```

#### 具体实现
- 添加效果类型图标显示
- 添加效果数值展示
- 添加稀有度标识（颜色边框）
- 添加悬停效果（鼠标悬停时高亮）

### 8. 商店卡牌交互逻辑优化
**现状**：点击购买后没有确认反馈
**解决方案**：

#### 交互流程
1. **悬停效果**：鼠标悬停在卡片上时，卡片轻微放大/高亮
2. **点击购买**：
   - 检查黑印是否足够
   - 黑印足够：弹出确认对话框
   - 黑印不足：显示"黑印不足"提示，卡片闪烁
3. **购买确认**：
   - 扣除黑印
   - 更新物品状态为已购买
   - 播放购买成功动画
   - 关闭商店时刷新背包

#### 具体实现
- 添加 `OnMouseEntered` 和 `OnMouseExited` 事件
- 添加购买确认对话框
- 添加黑印不足的视觉反馈（卡片变灰、按钮禁用）
- 添加购买成功/失败的音效和动画

## 实施步骤

### Step 1: 修复商店物品持久化
- 修改 `BlackMarkShopManager.cs`
  - 添加 `_purchasedItemIds` 列表存储已购买物品ID
  - 修改 `GenerateShopItems()` 逻辑，过滤已购买物品
  - 修改购买逻辑，将物品ID添加到 `_purchasedItemIds`

### Step 2: 检查背包显示
- 修改 `BuyArtifact()` 确保调用 `RefreshBackpack()`
- 检查 `RefreshBackpack()` 的实现

### Step 3: 添加造物图标
- 修改 `artifacts.json` 添加 `icon` 字段
- 或在 `BlackMarkShopManager` 中为没有图标的造物设置默认图标

### Step 4: 调整卡牌布局
- 修改 `HexMapUI.tscn` 中 `ShopItems` 的 GridContainer 属性
- 设置 `custom_minimum_size` 或调整 `columns`

### Step 5: 修复背景高度问题
- 在 `RefreshShopItems()` 中添加调试日志
- 检查 `QueueFree()` 是否正常工作
- 可能需要添加清理前的检查

### Step 6: 购买后占位但不可购买
- 修改购买逻辑，设置 `Purchased = true`
- 修改 UI 显示已售出状态

### Step 7: 优化卡牌内部布局
- 修改 `ShopItemCard.tscn`，添加效果图标和效果数值显示
- 修改 `ShopItemCard.cs`，更新布局逻辑
- 为造物和刻印分别设计效果图标

### Step 8: 添加交互效果
- 添加鼠标悬停效果
- 添加购买确认对话框
- 添加黑印不足的视觉反馈
- 添加购买成功动画

## 关键文件
- `scripts/Shop/BlackMarkShopManager.cs`
- `scripts/UI/HexMapUI/HexMapUI.cs`
- `scripts/UI/HexMapUI/ShopItemCard.cs`
- `Scenes/UI/HexMapUI.tscn`
- `Scenes/UI/ShopItemCard.tscn`
- `Data/artifacts.json`
- `Data/BuffEngraving.json`
