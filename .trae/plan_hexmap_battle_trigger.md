# HexMap战斗格子触发系统 - 实现计划

## 一、需求分析

当玩家在六边形地图上移动到 **普通战斗格子 (BattleNormal)** 或 **精英战斗格子 (BattleElite)** 时，需要：
1. 触发战斗场景切换
2. 根据格子类型加载对应的敌人配置
3. 战斗结束后返回地图场景
4. 保持地图状态（玩家位置、已访问格子等）

## 二、现有架构分析

### 关键文件
| 文件 | 职责 |
|------|------|
| `Scripts/Battle/HexMap/HexTile.cs` | 格子类型定义，包含 `HexEventType.BattleNormal/BattleElite/BattleBoss` |
| `Scripts/Battle/HexMap/HexMapController.cs` | 地图控制器，管理玩家移动和状态 |
| `Scripts/Battle/HexMap/HexEventManager.cs` | 事件处理器，`ProcessBattle()` 目前只扣血 |
| `Scripts/UI/HexMapUI/HexMapUI.cs` | 地图 UI，处理事件通知 |
| `Scripts/Scenes/BattleManager.cs` | 战斗管理器，完整的战斗系统 |
| `Scenes/BattleScene.tscn` | 战斗场景 |

### 当前问题
- `HexEventManager.ProcessBattle()` 只是模拟伤害，没有进入真实战斗
- 缺少地图状态保存/恢复机制
- 缺少战斗结果回调处理

## 三、实现方案

### 3.1 新增全局状态管理器

创建 `BattleTransitionManager` 管理地图与战斗场景之间的切换：

```
Scripts/Battle/BattleTransitionManager.cs (新建)
```

**职责**：
- 保存当前地图状态（玩家位置、地图数据、格子状态）
- 存储战斗类型（普通/精英/Boss）
- 战斗结束后恢复地图状态
- 提供战斗结果（胜利/失败）处理

### 3.2 修改 HexEventManager

修改 `ProcessBattle()` 方法：
- 不再直接扣除玩家生命值
- 触发场景切换到战斗场景
- 传递敌人配置信息

### 3.3 修改 HexMapController

新增功能：
- `SaveMapState()` - 保存地图状态
- `LoadMapState()` - 恢复地图状态
- `OnBattleStart()` - 进入战斗前的处理
- `OnBattleEnd()` - 战斗结束后的处理

### 3.4 修改 BattleManager

新增功能：
- 接收战斗类型参数（普通/精英/Boss）
- 根据战斗类型加载对应敌人配置
- 战斗结束后通知 `BattleTransitionManager`

### 3.5 敌人配置系统

创建敌人配置文件或数据结构：
- 普通战斗：基础敌人组合
- 精英战斗：更强的敌人组合
- Boss 战斗：Boss 敌人

## 四、详细任务清单

### 阶段一：状态管理基础设施
1. [ ] 创建 `BattleTransitionManager.cs` - 全局战斗切换管理器
2. [ ] 创建 `MapStateData.cs` - 地图状态数据结构

### 阶段二：修改现有系统
3. [ ] 修改 `HexEventManager.ProcessBattle()` - 触发场景切换
4. [ ] 修改 `HexMapController` - 添加状态保存/恢复方法
5. [ ] 修改 `HexMapUI` - 处理战斗开始/结束的 UI 状态

### 阶段三：战斗系统集成
6. [ ] 修改 `BattleManager` - 支持不同战斗类型
7. [ ] 创建/完善敌人配置系统
8. [ ] 实现战斗结果处理（胜利/失败）

### 阶段四：测试与完善
9. [ ] 测试普通战斗格子触发
10. [ ] 测试精英战斗格子触发
11. [ ] 测试战斗结束后返回地图
12. [ ] 测试战斗失败处理

## 五、数据流设计

```
玩家移动到战斗格子
       ↓
HexMapController.ProcessTile()
       ↓
HexEventManager.ProcessBattle()
       ↓
BattleTransitionManager.StartBattle()
       ↓
保存地图状态 → 切换到 BattleScene
       ↓
BattleManager 初始化战斗
       ↓
战斗结束 → BattleTransitionManager.EndBattle()
       ↓
恢复地图状态 → 切换回 HexMapScene
```

## 六、敌人配置设计

### 普通战斗 (BattleNormal)
```csharp
// 敌人数量：2-3个
// 敌人类型：基础敌人
// 奖励：少量黑印
```

### 精英战斗 (BattleElite)
```csharp
// 敌人数量：1-2个
// 敌人类型：精英敌人（更高血量、攻击）
// 奖励：中量黑印 + 可能的道具
```

### Boss 战斗 (BattleBoss)
```csharp
// 敌人数量：1个 Boss
// 敌人类型：Boss（特殊技能、高血量）
// 奖励：大量黑印 + 稀有道具
```

## 七、注意事项

1. **状态持久化**：确保玩家生命值、黑印数量等在战斗前后保持一致
2. **格子状态**：战斗胜利后格子应变为已访问状态
3. **失败处理**：战斗失败后可能需要返回起点或显示失败界面
4. **性能优化**：避免频繁的场景加载/卸载

## 八、预计工作量

| 任务 | 预计时间 |
|------|----------|
| 状态管理基础设施 | 30分钟 |
| 修改现有系统 | 45分钟 |
| 战斗系统集成 | 45分钟 |
| 测试与完善 | 30分钟 |

**总计：约 2.5 小时**
