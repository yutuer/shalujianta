# HexMapUI 重构计划

## 目标
将 HexMapUI.tscn 中臃肿的 UI 组件拆分到独立的场景文件中，实现代码模块化。

## 当前状态
HexMapUI.tscn 共 644 行，包含多个复杂 UI 组件，不符合项目规范。

## 组件层次关系

```
HexMapUI
├── HealthBar (生命条)
├── RageCircles (怒气圈)
├── TopRightButtons (右上按钮组)
│   └── BlackMarkButtonContainer (黑印按钮容器)
├── MapContainer (地图容器)
│   ├── TileViews (瓦片视图)
│   └── PlayerIcon (玩家图标)
├── TeleportDialog (传送确认弹窗) - [独立弹窗]
├── FailurePanel (失败面板) - [独立面板]
├── BackpackUI (背包界面) - [独立面板]
└── ShopUI (商店界面)
    ├── EngravingSelectUI (刻印物品选择面板) ← ShopUI 子面板
    │   └── EngravingCardSelectionUI (卡牌选择界面) ← 点击刻印后弹出
    │       └── CardSelectionItem (卡牌项)
 │   └── ShopItemCard (商店物品卡)
│       ├── ArtifactDescriptionUI (造物详情弹窗)
│       └── EngravingDescriptionUI (刻印详情弹窗)
```

## 待拆分组件清单

### 层级 1: 独立面板/弹窗

#### 1. TeleportDialog (传送确认弹窗)
| 项目 | 内容 |
|------|------|
| 当前行数 | 148-185 |
| 节点路径 | `TeleportDialog` |
| 脚本 | 无独立脚本 (内联在 HexMapUI.cs) |
| 目标目录 | `Scripts/UI/TeleportDialog/` |
| 目标场景 | `Scenes/UI/TeleportDialog.tscn` |
| 依赖 | 无 |

#### 2. FailurePanel (失败面板)
| 项目 | 内容 |
|------|------|
| 当前行数 | 319-348 |
| 节点路径 | `FailurePanel` |
| 脚本 | 无独立脚本 (内联在 HexMapUI.cs) |
| 目标目录 | `Scripts/UI/FailurePanel/` |
| 目标场景 | `Scenes/UI/FailurePanel.tscn` |
| 依赖 | 无 |

#### 3. BackpackUI (背包界面)
| 项目 | 内容 |
|------|------|
| 当前行数 | 350-372 |
| 节点路径 | `BackpackContainer` |
| 脚本 | 无独立脚本 (内联在 HexMapUI.cs) |
| 目标目录 | `Scripts/UI/BackpackUI/` |
| 目标场景 | `Scenes/UI/BackpackUI.tscn` |
| 依赖 | 无 |

### 层级 2: 详情弹窗 (ShopItemCard 的子组件)

#### 4. EngravingDescriptionUI (刻印详情界面)
| 项目 | 内容 |
|------|------|
| 当前行数 | 374-464 |
| 节点路径 | `KeyOrderDescriptionUI` |
| 脚本 | `Scripts/UI/KeyOrderDescriptionUI.cs` (已有，需重命名) |
| 场景 | 需创建 `Scenes/UI/EngravingDescriptionUI.tscn` |
| 父组件 | ShopItemCard |

#### 5. ArtifactDescriptionUI (圣物详情界面)
| 项目 | 内容 |
|------|------|
| 当前行数 | 466-561 |
| 节点路径 | `ArtifactDescriptionUI` |
| 脚本 | `Scripts/UI/HexMapUI/ArtifactDescriptionUI.cs` (需移动) |
| 目标目录 | `Scripts/UI/ArtifactDescriptionUI/` |
| 目标场景 | `Scenes/UI/ArtifactDescriptionUI.tscn` |
| 父组件 | ShopItemCard |

### 层级 3: 商店相关 (ShopUI 体系)

#### 6. ShopUI (商店界面)
| 项目 | 内容 |
|------|------|
| 当前行数 | 187-267 |
| 节点路径 | `ShopContainer` |
| 脚本 | 无独立脚本 (内联在 HexMapUI.cs) |
| 目标目录 | `Scripts/UI/ShopUI/` |
| 目标场景 | `Scenes/UI/ShopUI.tscn` |
| 子组件 | EngravingSelectUI, ShopItemCard |
| **注意** | EngravingSelectUI 和 ShopItemCard 应该作为 ShopUI 的子节点，或独立后通过引用关联 |

#### 7. EngravingSelectUI (刻印选择界面)
| 项目 | 内容 |
|------|------|
| 当前行数 | 268-318 |
| 节点路径 | `EngravingSelectContainer` |
| 脚本 | 无独立脚本 (内联在 HexMapUI.cs) |
| 目标目录 | `Scripts/UI/EngravingSelectUI/` |
| 目标场景 | `Scenes/UI/EngravingSelectUI.tscn` |
| 父组件 | ShopUI |
| 子组件 | EngravingCardSelectionUI |

#### 8. EngravingCardSelectionUI (刻印卡牌选择界面)
| 项目 | 内容 |
|------|------|
| 状态 | ✅ 场景已完成 |
| 脚本 | `Scripts/UI/HexMapUI/EngravingCardSelectionUI.cs` (需移动) |
| 目标目录 | `Scripts/UI/EngravingCardSelectionUI/` |
| 场景 | `Scenes/UI/EngravingCardSelectionUI.tscn` |
| 父组件 | EngravingSelectUI |
| 子组件 | CardSelectionItem |

### 层级 4: 卡牌组件

#### 9. CardSelectionItem (刻印卡牌项)
| 项目 | 内容 |
|------|------|
| 状态 | ✅ 场景已完成 |
| 脚本 | `Scripts/UI/HexMapUI/CardSelectionItem.cs` (需移动) |
| 目标目录 | `Scripts/UI/CardSelectionItem/` |
| 场景 | `Scenes/UI/CardSelectionItem.tscn` |
| 父组件 | EngravingCardSelectionUI |

## 验收标准

### 通用验收标准
- [ ] 每个组件有独立的 `.cs` 脚本文件
- [ ] 每个组件有独立的 `.tscn` 场景文件
- [ ] 场景文件中正确引用脚本: `script = ExtResource("1")`
- [ ] HexMapUI.cs 中不再内联 UI 组件的布局代码
- [ ] HexMapUI.cs 通过 `PackedScene.Instantiate()` 动态加载组件
- [ ] 所有 `using` 命名空间正确
- [ ] `dotnet build` 编译成功

### 组件功能验收

#### TeleportDialog
- [ ] 显示传送确认对话框
- [ ] 取消/确认按钮正常响应

#### ShopUI
- [ ] 显示商店物品列表
- [ ] 刷新按钮正常响应
- [ ] 关闭按钮正常响应

#### EngravingSelectUI
- [ ] 显示刻印物品网格
- [ ] 选择刻印后弹出 EngravingCardSelectionUI
- [ ] 返回按钮正常响应

#### FailurePanel
- [ ] 显示失败信息
- [ ] 确定按钮正常响应

#### BackpackUI
- [ ] 显示背包物品
- [ ] 物品点击正常响应

#### EngravingDescriptionUI
- [ ] 显示刻印详情
- [ ] 使用/取消按钮正常响应

#### ArtifactDescriptionUI
- [ ] 显示造物详情
- [ ] 购买/取消按钮正常响应

#### EngravingCardSelectionUI
- [ ] 显示未刻印卡牌列表
- [ ] 卡牌选择正常响应
- [ ] 确定/取消按钮正常响应

### 最终验收
- [ ] HexMapUI.tscn 精简到 150 行以内
- [ ] 所有组件功能测试通过
- [ ] 代码风格一致

## 执行顺序

### 阶段 1: 独立组件 (无依赖)
1. **FailurePanel** - 创建脚本 + 创建场景
2. **BackpackUI** - 创建脚本 + 创建场景
3. **TeleportDialog** - 创建脚本 + 创建场景

### 阶段 2: 详情弹窗 (依赖 ShopItemCard)
4. **EngravingDescriptionUI** - 脚本重命名 + 创建场景文件
5. **ArtifactDescriptionUI** - 移动脚本到独立目录 + 创建场景

### 阶段 3: 卡片组件 (Leaf 组件)
6. **CardSelectionItem** - 移动脚本到独立目录 + 验证场景
7. **EngravingCardSelectionUI** - 移动脚本到独立目录 + 验证场景

### 阶段 4: 商店体系 (复杂的父子关系)
8. **EngravingSelectUI** - 创建脚本 + 创建场景 (包含 EngravingCardSelectionUI 引用)
9. **ShopUI** - 创建脚本 + 创建场景 (包含 EngravingSelectUI 和 ShopItemCard 引用)

### 阶段 5: 更新主界面
10. **HexMapUI** - 移除内联代码，改为动态加载所有组件
11. **完整测试** - 验收所有功能
