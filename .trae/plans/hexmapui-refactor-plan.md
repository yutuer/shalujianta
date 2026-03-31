# HexMapUI 重构计划

## 概述

HexMapUI 是游戏地图界面的核心组件，负责显示六边形地图、玩家状态、怒气槽、银钥进度等信息。本计划旨在将 HexMapUI 中的复杂 UI 组件拆分到独立的场景文件中，遵循 Godot 的模块化设计原则。

## 当前项目结构分析

### 目录结构

```
Scripts/UI/
├── HexMapUI/                    # HexMapUI 相关组件
│   ├── HexMapUI.cs             # 主控制器 (899行)
│   ├── HealthBar.cs            # 血条组件
│   ├── RageCircle.cs           # 怒气圈组件
│   ├── PlayerIcon.cs           # 玩家图标组件
│   ├── HexTileView.cs          # 地图格子视图组件
│   ├── BlackMarkPopup.cs       # 黑印弹窗组件
│   ├── DraggableHexView.cs     # 可拖拽地图视图
│   └── EngravingCardSlot.cs    # 刻印卡槽组件
├── SilverKeyProgressIndicator.cs  # 银钥进度条组件
├── ShopUI/
│   └── ShopUI.cs               # 商店界面
├── BackpackUI/
│   └── BackpackUI.cs           # 背包界面
├── FailurePanel/
│   └── FailurePanel.cs         # 失败面板
├── TeleportDialog/
│   └── TeleportDialog.cs       # 传送对话框
├── EngravingCardSelectionUI/
│   └── EngravingCardSelectionUI.cs
├── EngravingDescriptionUI/
│   └── EngravingDescriptionUI.cs
├── ArtifactDescriptionUI/
│   └── ArtifactDescriptionUI.cs
└── CardSelectionItem/
    └── CardSelectionItem.cs

Scripts/Battle/HexMap/
├── HexMapController.cs         # 地图控制器
├── HexMap.cs                   # 地图数据模型
├── HexMapGenerator.cs          # 地图生成器
├── HexCoord.cs                 # 六边形坐标
├── HexTile.cs                  # 地图格子数据
└── HexEventManager.cs          # 事件管理器

Scripts/Battle/SilverKeySystem/
├── SilverKeyConfig.cs          # 银钥配置
├── KeyOrder.cs                 # 钥匙指令
└── KeyOrderManager.cs          # 钥匙指令管理器
```

### HexMapUI.tscn 当前节点结构 (177行)

```
HexMapUI (Control)
├── MapContainer (Control)
│   └── TileViews (Control)
│       └── PlayerIcon (PlayerIcon)
│           ├── Background (ColorRect)
│           └── IconLabel (Label)
├── HealthBar (HealthBar)
│   └── Background (PanelContainer)
│       ├── DamageBar (ColorRect)
│       ├── HealthBarFill (ColorRect)
│       └── HealthLabel (Label)
├── RageCircles (HBoxContainer) [510×120]
│   ├── RageCircle0 (RageCircle)
│   ├── RageCircle1 (RageCircle)
│   ├── RageCircle2 (RageCircle)
│   └── RageCircle3 (RageCircle)
├── SilverKeyProgressIndicator (PackedScene) [120×120]
├── TopRightButtons (HBoxContainer)
│   ├── DeathResistanceButtonContainer (VBoxContainer)
│   │   ├── DeathResistanceButton (Button)
│   │   └── DeathResistanceLabel (Label)
│   ├── BlackMarkButtonContainer (VBoxContainer)
│   │   ├── BlackMarkButton (Button)
│   │   └── BlackMarkLabel (Label)
│   └── SettingsButtonContainer (VBoxContainer)
│       └── SettingsButton (Button)
├── TeleportDialog (动态加载)
├── FailurePanel (动态加载)
└── BackpackContainer (动态加载)
```

### HexMapUI.cs 核心职责

| 类别 | 方法/事件 | 说明 |
|------|-----------|------|
| **初始化** | _Ready(), InitializeComponents() | 组件初始化和查找 |
| **定位** | PositionHealthBar(), PositionSilverKeyProgressIndicator(), PositionRageCircles(), PositionTopRightButtons() | UI元素动态定位 |
| **地图** | RefreshMap(), CreateTileView(), ClearTileViews(), CenterOnPlayer() | 地图视图管理 |
| **坐标转换** | HexToWorld(), WorldToHex() | 六边形坐标与屏幕坐标转换 |
| **事件处理** | OnTileClicked(), OnTileHovered(), OnPlayerMoved(), OnTileTriggered() | 用户交互和状态变化 |
| **路径显示** | ClearPathHighlights(), HighlightPath() | 路径高亮 |
| **传送** | OnTeleportTriggered(), OnTeleportConfirmPressed(), OnTeleportCancelPressed() | 传送门交互 |
| **商店** | OnShopOpened(), OnShopClosed(), OnEngravingItemConfirmed(), OnArtifactItemConfirmed() | 商店系统集成 |
| **显示更新** | UpdateBlackMarkDisplay(), UpdateDeathResistanceDisplay(), OnHealthChanged(), OnSilverKeyChanged() | 状态显示更新 |
| **怒气系统** | SetCharacterRages(), AddRageToCharacter() | 角色怒气管理 |

## 重构进度

### ✅ 阶段 1: 基础面板 (已完成)

| 组件 | 脚本路径 | 场景路径 | 状态 |
|------|----------|----------|------|
| FailurePanel | Scripts/UI/FailurePanel/FailurePanel.cs | Scenes/UI/FailurePanel.tscn | ✅ 完成 |
| BackpackUI | Scripts/UI/BackpackUI/BackpackUI.cs | Scenes/UI/BackpackUI.tscn | ✅ 完成 |
| TeleportDialog | Scripts/UI/TeleportDialog/TeleportDialog.cs | Scenes/UI/TeleportDialog.tscn | ✅ 完成 |

### ✅ 阶段 2: 描述面板 (已完成)

| 组件 | 脚本路径 | 场景路径 | 状态 |
|------|----------|----------|------|
| EngravingDescriptionUI | Scripts/UI/EngravingDescriptionUI/EngravingDescriptionUI.cs | Scenes/UI/EngravingDescriptionUI.tscn | ✅ 完成 |
| ArtifactDescriptionUI | Scripts/UI/ArtifactDescriptionUI/ArtifactDescriptionUI.cs | Scenes/UI/ArtifactDescriptionUI.tscn | ✅ 完成 |

### ✅ 阶段 3: 卡牌选择组件 (已完成)

| 组件 | 脚本路径 | 场景路径 | 状态 |
|------|----------|----------|------|
| CardSelectionItem | Scripts/UI/CardSelectionItem/CardSelectionItem.cs | Scenes/UI/CardSelectionItem.tscn | ✅ 完成 |
| EngravingCardSelectionUI | Scripts/UI/EngravingCardSelectionUI/EngravingCardSelectionUI.cs | Scenes/UI/EngravingCardSelectionUI.tscn | ✅ 完成 |

### ✅ 阶段 4: 商店界面整合 (已完成)

| 组件 | 脚本路径 | 场景路径 | 状态 |
|------|----------|----------|------|
| ShopUI | Scripts/UI/ShopUI/ShopUI.cs | Scenes/UI/ShopUI.tscn | ✅ 完成 |
| ShopItemCard | Scripts/UI/ShopItemCard/ShopItemCard.cs | Scenes/UI/ShopItemCard.tscn | ✅ 完成 |

### ✅ 阶段 5: 银钥进度条 (已完成)

| 组件 | 脚本路径 | 场景路径 | 状态 |
|------|----------|----------|------|
| SilverKeyProgressIndicator | Scripts/UI/SilverKeyProgressIndicator.cs | Scenes/UI/SilverKeyProgressIndicator.tscn | ✅ 完成 |

### ⏳ 阶段 6: HexMapUI 子组件拆分 (待进行)

#### 6.1 TopRightButtons 容器拆分

**问题**: TopRightButtons HBoxContainer 包含死亡抵抗、黑印、设置三个按钮及标签，逻辑和样式耦合在一起。

**计划**:
- [ ] 创建 TopRightButtonsPanel.tscn 和 TopRightButtonsPanel.cs
- [ ] 将 DeathResistanceButton、DeathResistanceLabel 移入
- [ ] 将 BlackMarkButton、BlackMarkLabel 移入
- [ ] 将 SettingsButton 移入
- [ ] 在 HexMapUI 中动态加载

**目标结构**:
```
TopRightButtonsPanel (Control)
├── DeathResistanceSection (VBoxContainer)
│   ├── DeathResistanceButton (Button)
│   └── DeathResistanceLabel (Label)
├── BlackMarkSection (VBoxContainer)
│   ├── BlackMarkButton (Button)
│   └── BlackMarkLabel (Label)
└── SettingsSection (VBoxContainer)
    └── SettingsButton (Button)
```

**事件接口**:
```csharp
public class TopRightButtonsPanel : Control
{
    public System.Action OnDeathResistanceClicked;
    public System.Action OnBlackMarkClicked;
    public System.Action OnSettingsClicked;

    public void UpdateBlackMarkDisplay(int count);
    public void UpdateDeathResistanceDisplay(int percentage);
}
```

#### 6.2 PlayerStatusPanel 整合

**问题**: 玩家状态相关组件分散在多个位置，包括血条、银钥进度条、怒气槽。

**计划**:
- [ ] 创建 PlayerStatusPanel.tscn 和 PlayerStatusPanel.cs
- [ ] 整合 HealthBar
- [ ] 整合 SilverKeyProgressIndicator
- [ ] 整合 RageCircles
- [ ] 统一管理布局和定位

**目标结构**:
```
PlayerStatusPanel (Control)
├── LeftColumn (VBoxContainer)
│   ├── SilverKeyProgressIndicator (instance)
│   └── HealthBar (instance)
└── RageCircles (HBoxContainer)
    ├── RageCircle0 (instance)
    ├── RageCircle1 (instance)
    ├── RageCircle2 (instance)
    └── RageCircle3 (instance)
```

#### 6.3 RageCircles 容器独立化

**问题**: 怒气槽组件虽然使用独立场景实例，但容器布局在 HexMapUI.tscn 中定义。

**计划**:
- [ ] 创建 RageCirclesPanel.tscn 和 RageCirclesPanel.cs
- [ ] 将 RageCircles 容器和 RageCircle 实例整合
- [ ] 提供统一的怒气值设置接口

**事件接口**:
```csharp
public class RageCirclesPanel : Control
{
    public void SetCharacterRages(List<int> rageValues, int maxRage);
    public void AddRageToCharacter(int characterIndex, int rageAmount);
    public void ResetAllRages();
}
```

#### 6.4 HexMapView 地图视图组件

**问题**: HexMapUI 包含大量地图视图相关逻辑，包括格子创建、坐标转换、路径显示等。

**计划**:
- [ ] 创建 HexMapView.tscn 和 HexMapView.cs
- [ ] 提取 HexTileView 创建和管理逻辑
- [ ] 提取坐标转换方法
- [ ] 提取路径高亮逻辑

**目标结构**:
```
HexMapView (Control)
├── TileViewsContainer (Control)
│   └── [Dynamic HexTileView instances]
└── PlayerIcon (PlayerIcon)
```

**事件接口**:
```csharp
public class HexMapView : Control
{
    public System.Action<HexTile> OnTileClicked;
    public System.Action<HexTile> OnTileHovered;

    public void SetHexSize(Vector2 size);
    public void RefreshTiles(HexMap map);
    public void CenterOn(HexCoord coord);
    public void HighlightPath(List<HexCoord> path);
    public void ClearHighlights();
}
```

## 组件依赖关系图

### 当前状态

```
HexMapUI
├── MapContainer/TileViews
│   └── PlayerIcon
├── HealthBar
├── RageCircles
│   └── RageCircle0-3
├── SilverKeyProgressIndicator
├── TopRightButtons
│   ├── DeathResistanceSection
│   ├── BlackMarkSection
│   └── SettingsSection
├── TeleportDialog (动态加载)
├── FailurePanel (动态加载)
└── BackpackContainer (动态加载)

ShopUI (独立加载)
├── ShopItemCard (动态创建)
├── ArtifactDescriptionUI
├── EngravingDescriptionUI
└── EngravingCardSelectionUI
    └── CardSelectionItem (动态创建)
```

### 重构后目标状态

```
HexMapUI (精简后)
├── HexMapView (独立组件)
│   ├── TileViewsContainer
│   └── PlayerIcon
├── PlayerStatusPanel (独立组件)
│   ├── SilverKeyProgressIndicator
│   ├── HealthBar
│   └── RageCirclesPanel
│       └── RageCircle0-3
├── TopRightButtonsPanel (独立组件)
│   ├── DeathResistanceSection
│   ├── BlackMarkSection
│   └── SettingsSection
├── TeleportDialog (动态加载)
├── FailurePanel (动态加载)
└── BackpackContainer (动态加载)

ShopUI (独立加载)
├── ShopItemCard (动态创建)
├── ArtifactDescriptionUI
├── EngravingDescriptionUI
└── EngravingCardSelectionUI
    └── CardSelectionItem (动态创建)
```

## 命名空间规范

遵循 `FishEatFish.UI.{ComponentName}` 命名空间规范：

| 组件 | 命名空间 |
|------|----------|
| HexMapUI | FishEatFish.UI.HexMap |
| HealthBar | FishEatFish.UI.HexMap |
| RageCircle | FishEatFish.UI.HexMap |
| PlayerIcon | FishEatFish.UI.HexMap |
| HexTileView | FishEatFish.UI.HexMap |
| BlackMarkPopup | FishEatFish.UI.HexMap |
| DraggableHexView | FishEatFish.UI.HexMap |
| EngravingCardSlot | FishEatFish.UI.HexMap |
| SilverKeyProgressIndicator | FishEatFish.UI |
| ShopUI | FishEatFish.UI.ShopUI |
| BackpackUI | FishEatFish.UI.BackpackUI |
| FailurePanel | FishEatFish.UI.FailurePanel |
| TeleportDialog | FishEatFish.UI.TeleportDialog |

## 事件通信模式

### HexMapUI 事件流

```
HexMapController (业务逻辑)
    │
    ├── OnPlayerMoved ──────────────→ HexMapUI.OnPlayerMoved()
    ├── OnTileTriggered ────────────→ HexMapUI.OnTileTriggered()
    ├── OnTeleportTriggered ────────→ HexMapUI → TeleportDialog.Show()
    ├── OnHealthChanged ────────────→ HexMapUI.OnHealthChanged() → HealthBar
    ├── OnBlackMarkChanged ─────────→ HexMapUI.OnBlackMarkChanged()
    ├── OnSilverKeyChanged ─────────→ HexMapUI.OnSilverKeyChanged() → SilverKeyProgressIndicator
    ├── OnMapCompleted ─────────────→ HexMapUI.OnMapCompleted()
    ├── OnChallengeFailed ──────────→ HexMapUI.OnChallengeFailed() → FailurePanel
    ├── OnShopOpened ───────────────→ HexMapUI.OnShopOpened() → ShopUI.ShowShop()
    └── OnShopClosed ────────────────→ HexMapUI.OnShopClosed() → ShopUI.HideShop()
```

### 重构后接口设计

```csharp
// HexMapUI 公共接口
public partial class HexMapUI : Control
{
    // 地图操作
    public void RefreshMap();
    public void CenterOnPlayer();

    // 状态更新
    public void SetCharacterRages(List<int> rageValues, int maxRage);
    public void AddRageToCharacter(int characterIndex, int rageAmount);

    // 交互控制
    public void SetInteractionBlocked(bool blocked);
    public void SetHexMapController(HexMapController controller);
}

// PlayerStatusPanel 公共接口
public partial class PlayerStatusPanel : Control
{
    public void UpdateHealth(float current, float max);
    public void UpdateSilverKey(int current, int max);
    public void SetCharacterRages(List<int> rageValues, int maxRage);
}

// TopRightButtonsPanel 公共接口
public partial class TopRightButtonsPanel : Control
{
    public void UpdateBlackMarkDisplay(int count);
    public void UpdateDeathResistanceDisplay(int percentage);
    public System.Action OnDeathResistanceClicked;
    public System.Action OnBlackMarkClicked;
    public System.Action OnSettingsClicked;
}

// HexMapView 公共接口
public partial class HexMapView : Control
{
    public System.Action<HexTile> OnTileClicked;
    public System.Action<HexTile> OnTileHovered;

    public void SetHexSize(Vector2 size);
    public void RefreshTiles(HexMap map);
    public void CenterOn(HexCoord coord);
    public void HighlightPath(List<HexCoord> path);
    public void ClearHighlights();
    public void UpdatePlayerPosition(HexCoord coord);
}
```

## 文件统计

### 当前状态

| 文件 | 行数 | 状态 |
|------|------|------|
| HexMapUI.tscn | 177 | 待优化 |
| HexMapUI.cs | 899 | 待优化 |
| HealthBar.cs | 73 | 可复用 |
| RageCircle.cs | 86 | 可复用 |
| PlayerIcon.cs | - | 待检查 |
| HexTileView.cs | - | 待检查 |
| SilverKeyProgressIndicator.cs | 242 | ✅ 已独立 |

### 重构目标

| 文件 | 目标行数 | 减少 |
|------|----------|------|
| HexMapUI.tscn | < 100 | -77+ |
| HexMapUI.cs | < 400 | -499+ |

### 新增文件计划

| 组件 | 脚本文件 | 场景文件 |
|------|----------|----------|
| PlayerStatusPanel | 1 | 1 |
| TopRightButtonsPanel | 1 | 1 |
| RageCirclesPanel | 1 | 1 |
| HexMapView | 1 | 1 |
| **总计** | **4** | **4** |

## 验收标准

- [ ] 编译无错误
- [ ] HexMapUI.tscn 行数 < 100
- [ ] HexMapUI.cs 行数 < 400
- [ ] 所有组件独立可加载
- [ ] 功能与重构前一致
- [ ] 屏幕尺寸变化时布局正确响应
- [ ] 事件通信正常工作

## 执行顺序

1. **阶段 6.3**: RageCircles 容器独立化 (隔离性最强)
2. **阶段 6.1**: TopRightButtons 容器拆分 (相对独立)
3. **阶段 6.2**: PlayerStatusPanel 整合 (整合现有组件)
4. **阶段 6.4**: HexMapView 地图视图组件 (最大改动)

## 风险评估

| 阶段 | 风险等级 | 说明 | 缓解措施 |
|------|----------|------|----------|
| 6.1 | 低 | 按钮组件结构简单 | 先完成其他阶段积累经验 |
| 6.2 | 中 | 涉及多个组件整合 | 定义清晰接口，先小范围测试 |
| 6.3 | 低 | 只是容器化 | 现有 RageCircle 已独立 |
| 6.4 | 高 | 地图逻辑复杂 | 最后执行，充分测试 |

## 备注

- 本计划假设使用 Godot 4.x 和 C# 脚本
- SilverKeyProgressIndicator 已于 2026-03-31 完成集成
- 怒气槽尺寸已调整为 120×120，与银钥进度条一致
- 银钥进度条位于左下角，血条位于银钥圈右侧
- RageCircles 容器尺寸已调整为 510×120

## 更新历史

| 日期 | 版本 | 更新内容 |
|------|------|----------|
| 2026-03-28 | 1.0 | 初始版本，完成阶段1-5 |
| 2026-03-31 | 1.1 | 添加阶段6，添加银钥进度条完成记录，添加怒气槽尺寸调整 |
| 2026-03-31 | 1.2 | 文档结构优化，添加当前项目结构分析 |
