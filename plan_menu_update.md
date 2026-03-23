# 游戏界面流程修改计划

## 新的游戏流程

```
主菜单
    ↓
地图选择 (LevelMap.tscn)
    ↓ 选择地图
角色选择 (CharacterSelection.tscn) [新建]
    ↓ 选择4角色+钥令
战斗场景 (BattleScene.tscn)
    ↓ 战斗胜利
造物/刻印选择面板 [可选整合到战斗结算]
    ↓
继续下一场 或 地图胜利结算
    ↓
返回主菜单
```

## 需要修改的文件

### 1. MainMenu.tscn / MainMenu.cs
**修改内容：**
- 开始游戏按钮文字：`"开始冒险"` → 直接进入地图选择
- 删除或隐藏"直路战斗测试"按钮（保留用于调试）
- "继续游戏"按钮保持禁用状态

**目标：** 简化主菜单，聚焦"开始冒险"

### 2. LevelMap.tscn / LevelMap.cs
**修改内容：**
- 更新标题：`"关卡地图"` → `"选择地图"`
- 地图选择逻辑：点击地图节点进入角色选择
- 显示地图信息：难度、战斗场数、奖励预览
- 按钮文字：`"继续推进"` → `"选择此地图"`
- 战斗测试按钮保持用于调试

**目标：** 让玩家能直观选择想要挑战的地图

### 3. CharacterSelection.tscn / CharacterSelection.cs [新建]
**界面元素：**
- 12个角色按钮（3x4网格或滚动列表）
- 已选择角色显示区（4个槽位）
- 角色详情预览（属性、大招预览）
- 钥令选择区
- 攻击者属性预览（总生命/攻击/防御/能量）
- 开始战斗按钮

**功能：**
- 选择4个角色组成攻击者
- 选择装备的钥令
- 预览组合后的总属性
- 点击开始进入战斗

**目标：** 让玩家完成队伍组建

### 4. BattleManager.cs
**修改内容：**
- 支持接收 Attacker 数据（4角色组合）
- 集成 RageManager（怒气系统）
- 集成 EngravingManager（刻印系统）
- 集成 KeyOrderManager（钥令系统）
- 战斗胜利判定后触发奖励选择
- 支持多场战斗流程

**目标：** 完整支持新战斗流程

## 实施步骤

### Phase A: 主菜单优化
1. [ ] 更新 MainMenu.tscn 按钮文字
2. [ ] 更新 MainMenu.cs 开始游戏逻辑

### Phase B: 地图选择优化
1. [ ] 更新 LevelMap.tscn 显示地图信息
2. [ ] 更新 LevelMap.cs 选择地图逻辑

### Phase C: 角色选择界面
1. [ ] 创建 CharacterSelection.tscn
2. [ ] 创建 CharacterSelection.cs
3. [ ] 实现角色选择逻辑
4. [ ] 实现钥令选择逻辑
5. [ ] 实现攻击者属性预览

### Phase D: 战斗流程集成
1. [ ] 修改 BattleManager.cs 支持新流程
2. [ ] 集成怒气系统
3. [ ] 集成刻印系统
4. [ ] 集成钥令系统
5. [ ] 实现战斗奖励逻辑

### Phase E: 测试验证
1. [ ] 主菜单 → 地图选择流程
2. [ ] 地图选择 → 角色选择流程
3. [ ] 角色选择 → 战斗流程
4. [ ] 战斗胜利 → 奖励选择
5. [ ] 整体流程贯通

## 文件清单

### 需要修改
```
Scenes/MainMenu.tscn
Scripts/MainMenu.cs
Scenes/LevelMap.tscn
Scripts/LevelMap.cs
Scripts/BattleManager.cs
```

### 需要新建
```
Scenes/UI/CharacterSelection.tscn
Scripts/CharacterSelection.cs
```

### 依赖系统
```
Scripts/Battle/CharacterSystem/CharacterDefinition.cs (已存在)
Scripts/Battle/CharacterSystem/Attacker.cs (已存在)
Scripts/Battle/SilverKeySystem/KeyOrder.cs (已存在)
Scripts/Battle/SilverKeySystem/KeyOrderManager.cs (已存在)
Scripts/Battle/RageManager.cs (已存在)
Scripts/Battle/EngravingSystem/EngravingManager.cs (已存在)
```

## 需要清理的旧逻辑

### LevelMap.cs 旧代码清理
| 旧内容 | 操作 | 原因 |
|--------|------|------|
| `SetupDeckOptions()` | **删除** | 旧流派选择系统不适用于角色系统 |
| `deckDropdown` | **删除** | 同上 |
| `nodeTypes[]` 节点系统 | **重构** | 旧杀戮尖塔风格节点改为地图选择 |
| `nodeDescriptions[]` | **删除** | 节点描述不再需要 |
| `BuildMapNodes()` | **重构** | 改为显示地图列表 |
| `BuildLegend()` | **删除** | 地图选择不需要图例 |
| `UpdateSelection()` | **重构** | 改为显示地图详情 |

### CardConfigLoader.cs 调整
| 旧内容 | 操作 | 原因 |
|--------|------|------|
| `deckDatabase` | **保留但简化** | 仍可用于加载默认卡组 |
| 卡组选择逻辑 | **删除/替换** | 由Attacker角色组合替代 |

### Player.cs 调整
| 旧内容 | 操作 | 原因 |
|--------|------|------|
| `Class` 属性 | **保留/重新定义** | 可用于显示当前攻击者名称 |

### 需要保留的功能
- Player 通用方法（抽卡、掉血、加护盾、Buff/Debuff）
- Card 数据结构
- CardConfigLoader 卡牌加载
