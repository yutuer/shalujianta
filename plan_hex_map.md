# 六边形地图探索系统

## 一、功能概述

在进入正式战斗前，玩家将进入一个六边形网格地图进行探索。玩家需要从起点移动到终点（BOSS战），途中可能触发各种事件。

### 1.1 核心流程

```
角色选择界面
    ↓
选择4个角色 + 银钥
    ↓
确认进入 → 六边形地图场景
    ↓
玩家在地图上自由移动（无回合限制）
    ↓
触发各种事件（战斗/陷阱/宝箱等）
    ↓
到达终点 → 进入正式BOSS战
```

### 1.2 已确认的设计

| 设计点 | 决策 |
|--------|------|
| 回合系统 | ❌ 无回合概念，自由移动 |
| 跳过选项 | ✅ 提供跳过地图直接BOSS的选项 |
| 返回机制 | ✅ 可返回已访问格子 |
| 难度适配 | ✅ 根据玩家等级自动调整 |

### 1.3 事件分类（按消失行为）

| 分类 | 说明 | 事件类型 |
|------|------|----------|
| **一次性事件** | 触发后消失 | 战斗、获得黑印、治疗、传送 |
| **地形效果** | 触发后不消失，再次踩上仍触发 | 沼泽、单向门 |

---

## 二、技术设计

### 2.1 六边形网格系统

#### 坐标系统
使用轴坐标系统（Axial Coordinates）：

```
            q
           ↙ ↘
       (-1,1) (0,1)
           ↙ ↘
       (-1,0)→(0,0)←(1,0)
           ↙ ↘
       (-1,-1)(0,-1)
           ↙ ↘
            q
```

#### 六边形方向
```
            ↑ North (0, -1)
           ↗ ↖
 West (-1, 0)   East (1, 0)
           ↙ ↘
            ↓ South (0, 1)
```

---

### 2.2 事件系统（完整版）

#### 事件类型一览

| 事件ID | 事件名称 | 触发行为 | 说明 |
|--------|----------|----------|------|
| `battle_normal` | 简单战斗 | 一次性 | 加载随机普通敌人 |
| `battle_elite` | 精英战斗 | 一次性 | 加载精英敌人（更强） |
| `battle_boss` | BOSS战斗 | 一次性 | 加载BOSS敌人（终点） |
| `empty` | 空格子 | - | 无效果 |
| `swamp` | 沼泽陷阱 | **地形效果** | 损失生命值，踩一次扣一次 |
| `gain_black_mark` | 获得黑印 | 一次性 | 获得黑印货币 |
| `shop` | 市场 | **地形效果** | 进入商店消费黑印，可重复访问 |
| `heal` | 回复生命 | 一次性 | 回复指定生命值 |
| `two_way_teleport` | 双向传送门 | **地形效果** | 踩上去可选择传送到配对的传送门 |
| `hole` | **洞穴** | **地形效果** | 踩上去后格子消失，有 >= 2 个相邻格子，进入后无法返回，只能继续前进 |
| `one_way_gate` | **单向门** | **地形效果** | 只能单向通行（如：只能从北到南，不能反向），地形不会消失 |

#### 事件配置示例

```json
{
    "hexEvents": {
        "battle_normal": {
            "displayName": "遭遇敌人",
            "icon": "res://Assets/Icons/enemy.png",
            "type": "one_time",
            "effect": "battle",
            "enemyConfig": "normal_wave_1"
        },
        "battle_elite": {
            "displayName": "精英敌人",
            "icon": "res://Assets/Icons/elite.png",
            "type": "one_time",
            "effect": "battle",
            "enemyConfig": "elite_wave_1"
        },
        "battle_boss": {
            "displayName": "BOSS",
            "icon": "res://Assets/Icons/boss.png",
            "type": "one_time",
            "effect": "battle",
            "enemyConfig": "boss_stage_1"
        },
        "swamp": {
            "displayName": "沼泽",
            "icon": "res://Assets/Icons/swamp.png",
            "type": "terrain",
            "damage": 10,
            "stackable": true
        },
        "gain_black_mark": {
            "displayName": "黑印碎片",
            "icon": "res://Assets/Icons/black_mark.png",
            "type": "one_time",
            "blackMarkGain": 5
        },
        "shop": {
            "displayName": "神秘商店",
            "icon": "res://Assets/Icons/shop.png",
            "type": "one_time",
            "effect": "open_shop"
        },
        "heal": {
            "displayName": "生命之泉",
            "icon": "res://Assets/Icons/heal.png",
            "type": "one_time",
            "healAmount": 20
        },
        "teleport": {
            "displayName": "传送门",
            "icon": "res://Assets/Icons/teleport.png",
            "type": "one_time",
            "effect": "teleport",
            "targetEvent": "teleport_dest"
        },
        "one_way": {
            "displayName": "单向通道",
            "icon": "res://Assets/Icons/one_way.png",
            "type": "terrain",
            "disappearOnTrigger": true,
            "blockReturn": true
        }
    }
}
```

---

### 2.3 单向门（OneWay）实现方案 ⭐

#### 问题分析
```
单向门格子A的特点：
1. 踩上去后A从地图消失
2. 踩上去后无法返回A（因为A消失了）
3. A消失后玩家仍能到达BOSS
```

#### 生成算法（关键）

```csharp
public class HexMapGenerator
{
    public HexMap Generate(int radius, int playerLevel)
    {
        // 1. 生成基础地图
        var tiles = GenerateHexGrid(radius);
        var startCoord = new HexCoord(0, 0);
        var endCoord = FindFarthestTile(tiles, startCoord);

        // 2. 计算最短路径（用于验证可达性）
        var criticalPath = FindShortestPath(tiles, startCoord, endCoord);
        var criticalSet = new HashSet<HexCoord>(criticalPath);

        // 3. 放置单向门（只能在非关键路径的格子上）
        var nonCriticalTiles = tiles.Keys
            .Where(c => !criticalSet.Contains(c))
            .ToList();

        // 4. 验证：移除所有单向门后仍可达
        //    这保证了单向门不是必经之路
        var oneWayTiles = PlaceOneWayTiles(tiles, nonCriticalTiles, playerLevel);

        foreach (var ow in oneWayTiles)
        {
            // 模拟移除此单向门
            var testTiles = tiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            testTiles.Remove(ow.Key);

            // BFS验证可达性
            if (!IsReachable(startCoord, endCoord, testTiles))
            {
                // 如果不可达，说明这个单向门是必需的，取消放置
                continue;
            }
        }

        return new HexMap(tiles, startCoord, endCoord);
    }

    private bool IsReachable(HexCoord start, HexCoord end,
        Dictionary<HexCoord, HexTile> tiles)
    {
        var visited = new HashSet<HexCoord>();
        var queue = new Queue<HexCoord>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == end) return true;

            foreach (var neighbor in current.GetNeighbors())
            {
                if (tiles.ContainsKey(neighbor) &&
                    !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return false;
    }
}
```

#### 单向门触发逻辑

```csharp
public class HexTile
{
    public bool IsOneWay { get; set; }
    public bool IsDisappeared { get; set; }

    public void TriggerOneWay()
    {
        if (!IsOneWay) return;

        // 地形效果：每次触发都执行
        OnEnter?.Invoke();

        // 单向门特殊逻辑：触发后消失
        IsDisappeared = true;
        GD.Print($"[HexTile] 单向门 {Coord} 已消失");
    }
}
```

#### 测试策略

| 测试场景 | 预期结果 |
|----------|----------|
| 生成100张地图，验证都有到BOSS的路径 | 100%通过 |
| 移除所有单向门后验证路径 | 100%通过 |
| 模拟玩家走遍所有单向门 | 都能正常触发和消失 |
| 单向门消失后，验证邻居格子仍可正常访问 | 通过 |

```csharp
// 单元测试示例
[Test]
public void TestOneWayPlacement()
{
    for (int i = 0; i < 100; i++)
    {
        var map = generator.Generate(4, 5);

        // 验证：Boss格子可达
        Assert.IsTrue(map.IsReachable(map.Start, map.End));

        // 验证：移除所有单向门后Boss仍可达
        foreach (var tile in map.GetOneWayTiles())
        {
            map.RemoveTile(tile.Coord);
        }
        Assert.IsTrue(map.IsReachable(map.Start, map.End));
    }
}
```

---

### 2.4 地图生成算法

#### 生成步骤

```
1. 创建中心点作为起点
    ↓
2. 使用波纹扩散生成六边形网格
    ↓
3. 随机选择终点（距起点最远的格子）
    ↓
4. 计算关键路径（保证可达性）
    ↓
5. 在非关键路径上放置单向门（验证移除后仍可达）
    ↓
6. 根据难度等级分布其他事件
    ↓
7. 完成生成
```

#### 难度适配

| 玩家等级 | 地图大小 | 简单战斗 | 精英战斗 | 陷阱数 | 黑印奖励 |
|----------|----------|----------|----------|--------|----------|
| 1-5 | 小型(3层) | 2-3个 | 1个 | 1-2个 | 5-10 |
| 6-10 | 中型(4层) | 3-4个 | 1-2个 | 2-3个 | 10-20 |
| 11+ | 大型(5层) | 4-5个 | 2个 | 3-4个 | 20-30 |

---

## 三、UI设计

### 3.1 完整布局

```
┌────────────────────────────────────────────────────────┐
│ [跳过]                              [死亡抵抗][黑印][⚙]│
│ 跳过地图                              这些按钮下方有文字说明│
├────────────────────────────────────────────────────────┤
│  ┌─────────────┐                                        │
│  │ R1 ● 35    │   ← 怒气圆圈（角色1）                   │
│  │ R2 ● 20    │   ← 怒气圆圈（角色2）                   │
│  │ R3 ● 50    │   ← 怒气圆圈（角色3）                   │
│  │ R4 ● 10    │   ← 怒气圆圈（角色4）                   │
│  └─────────────┘                                        │
│                                                        │
│              ◇──◇──◇──◇                                │
│             ╱              ╲                           │
│            ◇  ●玩家    ◇──◇──◇                          │ ← 六边形地图
│             ╲      ●单向门╱                              │
│              ◇──◇──◇──◇──◇                             │
│               ╲          ╱                              │
│                ◇──◇──◇──◇                              │
│                                                        │
│                                                        │
├────────────────────────────────────────────────────────┤
│  ┌──────────────────┐                                 │
│  │ 👤 头像          │   ← 玩家头像                      │
│  │ ████████░░ 80/100│   ← 血量条                       │
│  └──────────────────┘                                 │
└────────────────────────────────────────────────────────┘
```

### 3.2 UI组件详情

#### 3.2.1 玩家位置显示（气球图标）

```
        ●  ← 气球图标（玩家当前位置）
       ╱  ╲
      ◇    ◇  ← 下方格子
```

- 气球使用 Godot `Sprite2D` 或 `NinePatchRect`
- 跟随当前格子位置移动
- 移动时有平滑插值动画

#### 3.2.2 血量条（左下方）

```
┌──────────────────┐
│ 👤               │  ← 玩家头像
│ ████████░░ 80/100│  ← 血量填充条
└──────────────────┘
```

- 绿色 → 黄色 → 红色（根据血量百分比）
- 显示当前值/最大值

#### 3.2.3 怒气圆圈（左上方）

```
┌─────────┐
│ ●●●○○○○ │  ← 进度圆圈（填充效果）
│   龙    │  ← 角色名字
└─────────┘
```

- 4个圆圈，垂直排列
- 圆圈内显示角色名字
- 圆圈填充表示怒气值百分比

#### 3.2.4 右上方按钮

```
┌───────┐ ┌───────┐ ┌───────┐
│  💀  │ │  ⚫  │ │  ⚙  │
└───────┘ └───────┘ └───────┘
 死亡抵抗   黑印      设置
```

---

## 四、文件结构

```
Scripts/
├── Battle/
│   └── HexMap/
│       ├── HexCoord.cs              # 六边形坐标系统
│       ├── HexTile.cs               # 格子数据
│       ├── HexMap.cs                # 地图数据
│       ├── HexMapGenerator.cs       # 地图生成器
│       ├── HexMapView.cs            # 地图视图(Godot)
│       ├── HexTileView.cs            # 格子视图(Godot)
│       ├── HexMapController.cs       # 地图控制器
│       ├── HexPlayer.cs             # 玩家移动控制
│       ├── HexEvent.cs              # 事件基类
│       ├── HexEventConfig.cs        # 事件配置
│       └── HexEventManager.cs       # 事件管理器
│
├── Scenes/
│   └── HexMap/
│       └── HexMapScene.tscn         # 地图场景
│
├── UI/
│   └── HexMap/
│       ├── HexMapUI.cs              # 地图UI控制
│       ├── HealthBar.cs             # 血量条组件
│       ├── RageCircle.cs            # 怒气圆圈组件
│       └── PlayerIcon.cs            # 玩家气球图标
│
└── Data/
    └── hex_events.json              # 事件配置
```

---

## 五、实现步骤

### Phase 1: 核心基础设施 ✅
1. ✅ 创建 `HexCoord.cs` - 坐标系统
2. ✅ 创建 `HexTile.cs` - 格子数据结构（含传送门）
3. ✅ 创建 `HexMap.cs` - 地图数据结构
4. ✅ 创建 `HexMapGenerator.cs` - 地图生成器（含单向门验证）

### Phase 2: 商店系统 ✅
5. ✅ 创建 `artifacts.json` - 造物表
6. ✅ 创建 `BuffEngraving.json` - Buff刻印表
7. ✅ 创建 `ArtifactData.cs` - 造物数据结构
8. ✅ 创建 `EngravingData.cs` - 刻印数据结构
9. ✅ 创建 `BlackMarkShopManager.cs` - 黑印商店管理器

### Phase 3: UI组件 ✅
10. ✅ 创建 `HexTileView.cs` - 瓦片视图
11. ✅ 创建 `PlayerIcon.cs` - 气球图标
12. ✅ 创建 `HealthBar.cs` - 血量条
13. ✅ 创建 `RageCircle.cs` - 怒气圆圈
14. ✅ 创建 `HexMapUI.cs` - 完整UI（含商店、刻印选择、失败界面）

### Phase 4: 控制器和事件系统 ✅
15. ✅ 创建 `HexMapController.cs` - 地图控制器
16. ✅ 创建 `HexEventManager.cs` - 事件管理器
17. ✅ 实现传送门系统（单向/双向）
18. ✅ 实现黑印商店界面
19. ✅ 实现挑战失败判定机制

### Phase 5: 待完成
20. 创建 `HexMapScene.tscn` - 完整场景（需要Godot编辑器）
21. 与战斗系统集成
22. 单元测试：单向门可达性验证
23. 集成测试：完整探索流程

---

## 六、核心代码预览

### HexTile.cs（含单向门）
```csharp
public enum HexEventType
{
    Empty,
    BattleNormal,
    BattleElite,
    BattleBoss,
    Swamp,
    GainBlackMark,
    Shop,
    Heal,
    Teleport,
    OneWay
}

public enum EventTriggerType
{
    OneTime,    // 触发后消失
    Terrain     // 地形效果，可重复触发
}

public partial class HexTile
{
    public HexCoord Coord { get; }
    public HexEventType EventType { get; set; }
    public EventTriggerType TriggerType { get; set; }
    public bool IsStart { get; set; }
    public bool IsEnd { get; set; }
    public bool IsVisited { get; set; }
    public bool IsDisappeared { get; set; }  // 单向门专用

    // 事件参数
    public int Damage { get; set; }
    public int HealAmount { get; set; }
    public int BlackMarkGain { get; set; }
    public HexCoord TeleportTarget { get; set; }
    public string EnemyConfig { get; set; }

    public bool CanEnter => !IsDisappeared;
    public bool CanTrigger => !IsDisappeared;

    public void Trigger()
    {
        if (!CanTrigger) return;

        // 执行事件逻辑
        EventManager.Instance.TriggerEvent(this);

        // 根据触发类型处理
        if (TriggerType == EventTriggerType.OneTime)
        {
            EventType = HexEventType.Empty;
        }
        else if (IsOneWay)
        {
            // 单向门：触发后消失
            IsDisappeared = true;
            GD.Print($"[HexTile] 单向门 {Coord} 已消失");
        }
    }
}
```

---

## 七、测试方案

### 7.1 单向门专项测试

```csharp
[TestFixture]
public class OneWayTests
{
    private HexMapGenerator generator = new HexMapGenerator();

    [Test]
    public void 所有地图必须可达()
    {
        for (int i = 0; i < 100; i++)
        {
            var map = generator.Generate(4, 5);
            Assert.IsTrue(map.IsReachable(map.Start, map.End),
                $"地图{i}不可达");
        }
    }

    [Test]
    public void 移除所有单向门后仍可达()
    {
        for (int i = 0; i < 100; i++)
        {
            var map = generator.Generate(4, 5);
            var oneWays = map.GetOneWayTiles().ToList();

            foreach (var tile in oneWays)
            {
                map.RemoveTile(tile.Coord);
            }

            Assert.IsTrue(map.IsReachable(map.Start, map.End),
                $"移除{oneWays.Count}个单向门后地图{i}不可达");
        }
    }

    [Test]
    public void 单向门触发后消失()
    {
        var tile = new HexTile(new HexCoord(1, 0), HexEventType.OneWay);
        Assert.IsFalse(tile.IsDisappeared);

        tile.Trigger();
        Assert.IsTrue(tile.IsDisappeared);
        Assert.IsFalse(tile.CanEnter);
    }

    [Test]
    public void 单向门触发后事件消失但可访问邻居()
    {
        var tile = new HexTile(new HexCoord(1, 0), HexEventType.OneWay);
        tile.Trigger();

        // 单向门本身不可进入
        Assert.IsFalse(tile.CanEnter);

        // 但邻居格子应该仍然存在
        var neighbors = tile.Coord.GetNeighbors();
        Assert.IsTrue(neighbors.Length == 6);
    }
}
```

### 7.2 集成测试

```csharp
[Test]
public async Task 完整探索流程()
{
    var map = generator.Generate(3, 1);
    var controller = new HexMapController(map);

    // 模拟玩家移动到BOSS
    var path = controller.FindPath(map.Start, map.End);
    foreach (var coord in path)
    {
        await controller.MoveTo(coord);
        map.GetTile(coord).Trigger();
    }

    // 验证到达终点
    Assert.AreEqual(map.End, controller.CurrentPosition);

    // 验证BOSS战已触发
    Assert.IsTrue(map.GetTile(map.End).IsVisited);
}
```

---

## 八、黑印商店系统 ⭐

### 8.1 商店物品生成规则

每次进入市场时，系统从以下两个数据源随机生成3个可购买选项：

| 选项类型 | 数量 | 来源 |
|----------|------|------|
| 造物 | 2个 | artifacts.json |
| Buff刻印 | 1种 | BuffEngraving.json |

### 8.2 物品数据结构

#### 造物表 (artifacts.json)
```json
{
    "artifacts": [
        {
            "artifactId": "artifact_001",
            "name": "神圣衣碎片",
            "icon": "res://Assets/Icons/artifacts/sacred_cloth.png",
            "description": "装备后攻击力+15%",
            "price": 30,
            "effect": {
                "type": "attack_buff",
                "value": 0.15
            }
        },
        {
            "artifactId": "artifact_002",
            "name": "冥王之剑",
            "icon": "res://Assets/Icons/artifacts/hades_sword.png",
            "description": "攻击时有20%几率造成双倍伤害",
            "price": 50,
            "effect": {
                "type": "crit_chance",
                "value": 0.20
            }
        }
    ]
}
```

#### Buff刻印表 (BuffEngraving.json)
```json
{
    "buffEngravings": [
        {
            "engravingId": "engraving_001",
            "name": "烈火刻印",
            "icon": "res://Assets/Icons/engravings/fire.png",
            "description": "攻击力+10%",
            "price": 25,
            "effect": {
                "type": "attack_buff",
                "value": 0.10
            }
        },
        {
            "engravingId": "engraving_002",
            "name": "磐石刻印",
            "icon": "res://Assets/Icons/engravings/earth.png",
            "description": "防御力+10%",
            "price": 25,
            "effect": {
                "type": "defense_buff",
                "value": 0.10
            }
        },
        {
            "engravingId": "engraving_003",
            "name": "生命刻印",
            "icon": "res://Assets/Icons/engravings/life.png",
            "description": "生命上限+15%",
            "price": 25,
            "effect": {
                "type": "health_buff",
                "value": 0.15
            }
        }
    ]
}
```

### 8.3 价格系统

| 物品类型 | 价格来源 | 固定/浮动 |
|----------|----------|-----------|
| 造物 | 从artifacts.json读取price字段 | 固定 |
| Buff刻印 | 固定25黑印 | 固定 |

### 8.4 商店卡牌UI设计

#### 卡牌外观
- 统一使用**长方形卡牌**展示选项
- 卡片大小：约 200x280 像素

#### 卡牌内容布局（从上到下）
```
┌─────────────────────┐
│     物品名称         │  ← 居中显示
├─────────────────────┤
│                     │
│     [图标]          │  ← 造物/刻印图标
│                     │
├─────────────────────┤
│   作用说明文字       │  ← 描述效果
│                     │
├─────────────────────┤
│   💰 消耗黑印值     │  ← 粗体显示
└─────────────────────┘
```

### 8.5 购买交互流程

#### 造物购买流程
```
点击造物卡牌
    ↓
弹出居中确认界面
    ├─ 顶部中央：物品名称
    ├─ 中间区域：造物图标
    ├─ 图标下方：作用说明
    └─ 底部（从左到右）：
         [取消]    [💰30 确定]
    
    ├─ 黑印不足：确定按钮置灰，不可点击
    ├─ 黑印足够 + 点击取消 → 返回市场界面
    └─ 黑印足够 + 点击确定 → 扣除黑印，物品加入背包，关闭弹窗
```

#### Buff刻印购买流程
```
点击刻印卡牌
    ↓
弹出居中确认界面
    ├─ 顶部中央：刻印名称
    ├─ 中间区域：刻印图标
    ├─ 图标下方：作用说明
    └─ 底部（从左到右）：
         [取消]    [💰25 确定]
    
    ├─ 黑印不足：确定按钮置灰，不可点击
    ├─ 黑印足够 + 点击取消 → 返回市场界面
    └─ 黑印足够 + 点击确定 → 进入卡牌选择界面
```

#### Buff刻印卡牌选择流程
```
刻印购买确认后
    ↓
显示卡牌选择界面
    ├─ 右上角：[X] 关闭按钮
    ├─ 界面内容：4个角色所有未刻印的卡牌（每张卡一个选择框）
    ├─ 点击卡牌：选中该卡牌（高亮显示）
    └─ 底部：
         [返回]    [确定刻印]
    
    ├─ 选择卡牌 + 点击确定 → 扣除黑印，刻印应用到卡牌，关闭界面
    └─ 点击X或返回 → 返回市场界面（不扣除黑印）
```

### 8.6 市场关闭功能
- 玩家可点击界面右上角的 **"X"按钮** 关闭市场界面
- 关闭时不做任何操作，直接返回地图

### 8.7 玩家背包展示

#### 展示位置
- **地图正中间上方区域**

#### 展示内容
- 玩家当前场景拥有造物（artifacts）
- 物品图标清晰可见

#### 交互功能
- 点击图标可查看详细信息（名称、效果）

---

## 九、挑战失败判定机制 ⭐

### 9.1 失败判定条件

| 场景 | 判定结果 |
|------|----------|
| 玩家**跳过地图** → 进入BOSS战 | 正常流程，不算失败 |
| 玩家**未跳过地图**，选择返回 | **立即判定挑战失败** |

### 9.2 失败处理流程

```
玩家点击"返回"按钮
    ↓
系统判定：未执行跳过操作
    ↓
执行失败处理
    ├─ 显示失败界面
    ├─ 记录失败状态
    ├─ 扣除相关惩罚（如有）
    └─ 返回角色选择界面
```

### 9.3 UI按钮布局

```
┌────────────────────────────────────────────────────────┐
│ [跳过地图]                    [死亡抵抗][黑印:XX][⚙]   │
│                                                    [X] │
└────────────────────────────────────────────────────────┘
                                                      ↑
                                                   市场关闭按钮
```

---

## 十、传送门系统 ⭐

### 10.1 传送门类型

| 类型 | 组成 | 传送特性 |
|------|------|----------|
| 单向传送门 | 传入块 + 传出块 | 只能从传入块传送到传出块 |
| 双向传送门 | 传送门A + 传送门B | A↔B可互相传送 |

### 10.2 单向传送门

```
触发机制：
┌─────────────────────────────────────┐
│  玩家进入传入块方格                  │
│       ↓                             │
│  弹出确认提示："是否要开始传送？"    │
│       ↓                             │
│  ┌─────┐        ┌─────┐             │
│  │ 否  │        │ 是  │             │
│  └─────┘        └─────┘             │
│       ↓              ↓              │
│  不传送          传送到传出块        │
│  正常进入格子    立即移动到目标格子   │
└─────────────────────────────────────┘

传出块特性：
- 玩家进入传出块格子时无任何提示
- 传出块可正常进入和离开
```

### 10.3 双向传送门

```
触发机制：
┌─────────────────────────────────────┐
│  玩家进入任意一个传送门格子          │
│       ↓                             │
│  弹出确认提示："是否要开始传送？"    │
│       ↓                             │
│  ┌─────┐        ┌─────┐             │
│  │ 否  │        │ 是  │             │
│  └─────┘        └─────┘             │
│       ↓              ↓              │
│  不传送          传送到另一个传送门  │
└─────────────────────────────────────┘

防重复触发机制：
- 传送完成后，玩家在当前格子内不会再次触发提示
- 必须**离开该格子后重新进入**才会再次触发
```

### 10.4 传送门数据结构

```csharp
public enum TeleportType
{
    OneWay,   // 单向：传入块→传出块
    TwoWay    // 双向：A↔B
}

public class HexTeleportTile : HexTile
{
    public TeleportType Type { get; set; }
    public string TeleportPairId { get; set; }  // 配对ID
    public bool IsEntrance { get; set; }        // 单向传送门的传入块
    public bool HasTriggeredThisVisit { get; set; }  // 防重复触发

    public HexCoord GetTargetCoord(HexMap map)
    {
        if (Type == TeleportType.OneWay)
        {
            return map.GetTeleportExit(Coord);
        }
        else // TwoWay
        {
            return map.GetPairedTeleport(Coord, TeleportPairId);
        }
    }
}
```

---

## 十一、地形移除机制 ⭐

### 11.1 笔直路径场景

当起点与BOSS之间存在直线连接的地形时：
- 系统允许玩家从起点直接走向BOSS
- 移除可破坏地形**不会阻断**这条直线路径

### 11.2 移除行为说明

| 地形类型 | 移除行为 | 说明 |
|----------|----------|------|
| 单向门 | 消失 | 触发后从地图移除，不可恢复 |
| 可破坏地形 | 消失 | 永久性地形改变 |

### 11.3 路径保护规则

```
生成地图时：
1. 计算从起点到终点的最短路径
2. 标记所有关键格子（移除后会导致不可达的格子）
3. 可破坏地形只能放置在**非关键格子**
4. 移除可破坏地形后，系统验证路径仍然连通
```

---

## 十二、功能保留说明

### 12.1 原设计保留

| 功能 | 状态 | 说明 |
|------|------|------|
| 单向门（OneWay） | ✅ 保留 | 游戏内特色机制 |
| 可破坏地形 | ✅ 保留 | 提供策略选择 |

---

## 十三、待确认事项

| 问题 | 状态 | 备注 |
|------|------|------|
| 跳过地图消耗什么资源？ | 待定 | 建议：消耗道具或无消耗 |
| 死亡抵抗效果 | 待定 | 消耗道具或技能？ |
| 传送门配对逻辑 | ✅ 已确认 | 单向/双向传送门 |

---

## 十四、文件结构更新

```
Scripts/
├── Battle/
│   └── HexMap/
│       ├── HexCoord.cs              # 六边形坐标系统
│       ├── HexTile.cs               # 格子数据（含传送门）
│       ├── HexMap.cs                # 地图数据
│       ├── HexMapGenerator.cs       # 地图生成器
│       ├── HexMapView.cs            # 地图视图
│       ├── HexTileView.cs           # 格子视图
│       ├── HexMapController.cs       # 地图控制器
│       ├── HexPlayer.cs             # 玩家移动控制
│       ├── HexEvent.cs              # 事件基类
│       └── HexEventManager.cs       # 事件管理器
│
├── Shop/
│   ├── BlackMarkShopManager.cs      # 黑印商店管理器
│   ├── ArtifactData.cs              # 造物数据结构
│   └── EngravingData.cs             # 刻印数据结构
│
├── Scenes/
│   └── HexMap/
│       ├── HexMapScene.tscn         # 地图场景
│       ├── BlackMarkShopScene.tscn  # 商店场景（可作为子界面）
│       └── EngravingSelectScene.tscn # 刻印选择场景
│
├── UI/
│   └── HexMap/
│       ├── HexMapUI.cs              # 地图UI控制
│       ├── HealthBar.cs             # 血量条组件
│       ├── RageCircle.cs            # 怒气圆圈组件
│       ├── PlayerIcon.cs            # 玩家气球图标
│       ├── PlayerBackpackUI.cs      # 玩家背包UI
│       ├── ShopItemCard.tscn        # 商店物品卡牌
│       ├── ShopConfirmDialog.tscn   # 购买确认弹窗
│       └── EngravingSelectUI.tscn   # 刻印选择界面
│
└── Data/
    ├── hex_events.json              # 事件配置
    ├── artifacts.json               # 造物表
    └── BuffEngraving.json            # Buff刻印表
```

---

## 九、风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 单向门导致死路 | 高 | 生成时验证，移除后测试可达 |
| 六边形渲染性能 | 中 | 使用对象池，批量渲染 |
| 传送门循环 | 低 | 记录传送历史，禁止循环 |
| 随机性过强 | 中 | 设置事件分布规则和最小间距 |
