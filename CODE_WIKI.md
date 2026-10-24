# FishEatFish 游戏项目 Code Wiki

> 本文档由代码分析工具自动生成，提供项目的完整技术架构说明。
> 最后更新：2026-06-12

---

## 📋 目录

1. [项目概述](#1-项目概述)
2. [项目架构](#2-项目架构)
3. [核心模块详解](#3-核心模块详解)
4. [关键类与函数说明](#4-关键类与函数说明)
5. [数据配置系统](#5-数据配置系统)
6. [依赖关系](#6-依赖关系)
7. [项目运行方式](#7-项目运行方式)
8. [开发规范](#8-开发规范)

---

## 1. 项目概述

### 1.1 项目简介

**FishEatFish** 是一款基于 Godot 4.6 引擎开发的回合制卡牌roguelike游戏，采用 C# 语言开发。项目模仿《杀戮尖塔》(Slay the Spire)的核心玩法，玩家通过选择角色、构建卡组、探索六边形地图来与敌人战斗。

### 1.2 核心技术栈

| 技术 | 版本 | 说明 |
|:---|:---|:---|
| Godot Engine | 4.6.1 | 游戏引擎 |
| C# (.NET) | 8.0 | 编程语言 |
| Godot.NET.Sdk | 4.6.1 | Godot C# SDK |
| 项目模板 | PackedStringArray("4.6", "C#", "Forward Plus") | Godot项目配置 |

### 1.3 项目目标

- 实现四人组队战斗系统
- 支持怒气系统和必杀技释放
- 实现银钥/钥令系统
- 支持刻印系统
- 实现造物（Artifact）系统
- 六边形地图探索

### 1.4 项目配置

**项目文件**: `FishEatFish.csproj`
```xml
<Project Sdk="Godot.NET.Sdk/4.6.1">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
</Project>
```

**引擎配置**: `project.godot`
- 主场景：`res://Scenes/MainMenu.tscn`
- 分辨率：1920×1080
- 拉伸模式：canvas_items

---

## 2. 项目架构

### 2.1 整体架构图

```
FishEatFish (Godot 4.6 C# Project)
│
├── 📁 Assets/                    # 资源目录
│   ├── 📁 Cards/                 # 卡牌图片资源
│   └── 📁 Icons/                 # 图标资源
│
├── 📁 Data/                      # 数据配置目录
│   ├── cards.json               # 卡牌数据
│   ├── characters.json           # 角色数据
│   ├── buffs.json               # Buff数据
│   ├── debuffs.json             # Debuff数据
│   ├── artifacts.json           # 造物数据
│   └── BuffEngraving.json       # 刻印数据
│
├── 📁 Scenes/                    # Godot场景文件
│   ├── MainMenu.tscn            # 主菜单
│   ├── CharacterSelection.tscn   # 角色选择
│   ├── BattleScene.tscn          # 战斗场景
│   ├── HexMapScene.tscn          # 六边形地图
│   ├── LevelMap.tscn             # 关卡地图
│   └── 📁 UI/                    # UI组件场景
│
├── 📁 Scripts/                    # C#脚本目录
│   ├── 📁 AI/                    # AI系统
│   ├── 📁 Battle/                # 战斗系统核心
│   │   ├── 📁 ArtifactSystem/    # 造物系统
│   │   ├── 📁 Card/              # 卡牌系统
│   │   ├── 📁 CharacterSystem/   # 角色系统
│   │   ├── 📁 Core/              # 核心战斗类
│   │   ├── 📁 Effects/           # 效果系统
│   │   ├── 📁 EngravingSystem/    # 刻印系统
│   │   ├── 📁 HexMap/            # 六边形地图
│   │   ├── 📁 MapSystem/         # 地图系统
│   │   └── 📁 SilverKeySystem/   # 银钥系统
│   ├── 📁 Scenes/                # 场景脚本
│   ├── 📁 Shop/                  # 商店系统
│   └── 📁 UI/                    # UI组件脚本
│
├── FishEatFish.csproj           # C#项目文件
├── project.godot                # Godot项目配置
└── CODE_WIKI.md                 # 本文档
```

### 2.2 模块职责划分

| 模块 | 目录 | 主要职责 |
|:---|:---|:---|
| **场景系统** | `Scripts/Scenes/` | 场景切换、场景初始化 |
| **战斗系统** | `Scripts/Battle/` | 回合制战斗、伤害计算 |
| **卡牌系统** | `Scripts/Battle/Card/` | 卡牌数据、效果解析 |
| **角色系统** | `Scripts/Battle/CharacterSystem/` | 角色定义、属性计算 |
| **UI系统** | `Scripts/UI/` | 界面展示、用户交互 |
| **AI系统** | `Scripts/AI/` | 敌人行为决策 |
| **地图系统** | `Scripts/Battle/HexMap/` | 六边形地图生成与导航 |
| **商店系统** | `Scripts/Shop/` | 商店逻辑、黑印交易 |

### 2.3 核心设计原则

> **第一准则：逻辑和页面分离**
> 
> 所有 UI 组件和游戏逻辑必须遵循此原则：
> - `.tscn` 场景文件：负责 UI 的视觉呈现、布局、样式
> - `.cs` 脚本文件：负责数据处理、业务规则、状态管理

---

## 3. 核心模块详解

### 3.1 场景模块 (Scenes)

#### 3.1.1 主菜单场景 (`MainMenu.cs`)

**文件路径**: `Scripts/Scenes/MainMenu.cs`

**功能说明**: 游戏主入口界面，提供游戏开始、地图测试等入口按钮。

**核心属性**:
```csharp
private Button _startButton;      // 开始游戏按钮
private Button _hexMapButton;     // 六边形地图按钮
private Button _levelSelectButton; // 关卡选择按钮
private Button _testButton;       // 战斗测试按钮
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `OnStartPressed()` | private | 点击开始按钮，切换到关卡地图 |
| `OnHexMapPressed()` | private | 点击地图按钮，进入六边形地图模式 |
| `OnLevelSelectPressed()` | private | 点击关卡选择，切换到关卡地图 |
| `OnTestPressed()` | private | 点击测试按钮，进入战斗场景 |
| `OnHoleTestPressed()` | private | 执行洞穴地图验证测试 |
| `FindConnectedHoleGroups()` | private | 查找相邻的洞穴组 |

**场景切换**:
```
MainMenu → LevelMap (关卡地图)
        → CharacterSelection (角色选择)
        → BattleScene (战斗场景)
        → HexMapScene (六边形地图)
```

#### 3.1.2 角色选择场景 (`CharacterSelection.cs`)

**文件路径**: `Scripts/Scenes/CharacterSelection.cs`

**功能说明**: 玩家选择4个角色组成攻击者，并选择钥令。

**核心属性**:
```csharp
private GridContainer _characterContainer;  // 角色网格容器
private CharacterDefinition[] _allCharacters; // 所有可用角色
private List<CharacterDefinition> _selectedCharacters; // 已选角色
private KeyOrder _selectedKeyOrder;          // 已选钥令
private const int MaxSelection = 4;          // 最大选择数
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `BuildCharacterGrid()` | private | 构建角色选择网格 |
| `OnCharacterSelected()` | private | 处理角色选择逻辑 |
| `BuildSelectedSlots()` | private | 构建已选角色槽位 |
| `BuildKeyOrderSelection()` | private | 构建钥令选择界面 |
| `OnKeyOrderSelected()` | private | 处理钥令选择 |
| `UpdateStats()` | private | 更新总属性显示 |
| `OnStartPressed()` | private | 确认选择，切换到战斗 |

**角色选择流程**:
```
角色选择 → 选择4个角色 → 选择钥令 → 开始游戏
         ↓
    GlobalData.SelectedCharacters 存储
    GlobalData.EquippedKeyOrder 存储
         ↓
    进入 HexMapScene 或 BattleScene
```

#### 3.1.3 战斗管理器 (`BattleManager.cs`)

**文件路径**: `Scripts/Scenes/BattleManager.cs`

**功能说明**: 核心战斗系统，管理战斗流程、玩家/敌人状态、卡牌系统、银钥/怒气系统。

**初始化顺序**: `Attacker → Managers → UI → Player → Enemies → Cards → StartBattle`

**核心属性**:
```csharp
// 核心战斗数据
private Player player;           // 玩家对象
private List<Enemy> enemies;     // 敌人列表
private int turnCount;           // 当前回合数
private bool isPlayerTurn;       // 是否玩家回合

// UI节点引用
private HBoxContainer handContainer;     // 手牌容器
private HBoxContainer enemyContainer;    // 敌人容器
private Button endTurnButton;            // 结束回合按钮

// UI组件
private List<EnemyUI> enemyUIs;         // 敌人UI列表
private List<CardUI> cardUIs;            // 卡牌UI列表
private PlayerStatsUI playerStatsUI;     // 玩家状态UI
private CardLayoutManager cardLayoutManager; // 卡牌布局管理器

// 银钥系统
private SilverKeyConfig silverKeyConfig; // 银钥配置
private KeyOrderManager keyOrderManager;  // 钥令管理器
private int currentSilverKey;            // 当前银钥值

// 怒气系统
private RageManager rageManager;         // 怒气管理器
private List<CharacterPosition> characterPositions; // 角色位置列表
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `InitializeAttacker()` | private | 初始化攻击体（4角色组合） |
| `InitializeManagers()` | private | 初始化各类管理器 |
| `InitializeUI()` | private | 初始化UI节点引用 |
| `InitializePlayer()` | private | 初始化玩家属性 |
| `InitializeEnemies()` | private | 初始化敌人 |
| `InitializeCards()` | private | 初始化卡组 |
| `StartBattle()` | private | 开始战斗 |
| `StartTurn()` | private | 开始回合（抽牌、重置状态） |
| `EndTurn()` | private | 结束玩家回合 |
| `EnemyTurn()` | private | 执行敌人回合 |
| `OnCardPlayed()` | private | 处理卡牌使用 |
| `ApplyCardEffects()` | private | 应用卡牌效果 |
| `ApplyCardDamage()` | private | 应用卡牌伤害 |
| `UpdateRageDisplay()` | private | 更新怒气显示 |
| `ProcessPostBattleRage()` | private | 处理战斗后怒气 |

**战斗流程**:
```
战斗开始
  ↓
回合开始（StartTurn）
  ├── 玩家抽牌
  ├── 重置护盾
  └── 更新UI
  ↓
玩家出牌（OnCardPlayed）
  ├── 扣除能量
  ├── 增加银钥
  ├── 追踪怒气
  ├── 应用效果
  └── 应用伤害
  ↓
玩家结束回合（EndTurn）
  ↓
敌人回合（EnemyTurn）
  ├── 敌人决策
  └── 执行行动
  ↓
回合结束
  ↓
检查游戏结束？
  ├── 玩家血量<=0 → 游戏失败
  └── 敌人全死 → 游戏胜利
  ↓
下一回合 或 战斗结算
```

#### 3.1.4 六边形地图场景 (`HexMapScene.cs`)

**文件路径**: `Scripts/Scenes/HexMapScene.cs`

**功能说明**: 六边形地图探索场景，玩家在地图上移动并触发各种事件。

**核心代码**:
```csharp
public partial class HexMapScene : Control
{
    private HexMapUI _hexMapUI;       // 地图UI
    private HexMapController _controller; // 地图控制器

    public override void _Ready()
    {
        _controller = new HexMapController();
        AddChild(_controller);
        _controller.SetPlayerLevel(7);

        var hexMapUIScene = GD.Load<PackedScene>("res://Scenes/UI/HexMapUI.tscn");
        _hexMapUI = (HexMapUI)hexMapUIScene.Instantiate();
        AddChild(_hexMapUI);
    }
}
```

---

### 3.2 战斗系统模块 (Battle)

#### 3.2.1 核心战斗类 (Core)

##### Player (`Scripts/Battle/Core/Player.cs`)

**功能说明**: 玩家对象，管理玩家属性、卡牌、生命值等。

**核心属性**:
```csharp
public int MaxHealth { get; set; }          // 最大生命值
public int CurrentHealth { get; set; }       // 当前生命值
public int MaxEnergy { get; set; }          // 最大能量
public int CurrentEnergy { get; set; }      // 当前能量
public int DrawCount { get; set; }          // 抽卡数量
public int Shield { get; set; }             // 当前护盾
public int MaxHandSize { get; set; }        // 最大手牌数

public List<Card> Deck { get; set; }        // 抽牌堆
public List<Card> Hand { get; set; }        // 手牌
public List<Card> DiscardPile { get; set; } // 弃牌堆
```

##### Enemy (`Scripts/Battle/Core/Enemy.cs`)

**功能说明**: 敌人对象，管理敌人属性、AI行为等。

**核心属性**:
```csharp
public string EnemyName { get; set; }       // 敌人名称
public int MaxHealth { get; set; }           // 最大生命值
public int CurrentHealth { get; set; }       // 当前生命值
public int Attack { get; set; }             // 攻击力
public int Position { get; set; }           // 位置
public string Behavior { get; set; }         // AI行为模式

public List<StatusEffect> StatusEffects { get; set; } // 状态效果
```

##### IUnit (`Scripts/Battle/Core/IUnit.cs`)

**功能说明**: 战斗单位接口，定义玩家和敌人的通用行为。

```csharp
public interface IUnit
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    void TakeDamage(int damage);
    void Heal(int amount);
    void AddShield(int amount);
    bool IsDead { get; }
}
```

#### 3.2.2 卡牌系统 (Card)

##### Card (`Scripts/Battle/Card/Card.cs`)

**功能说明**: 卡牌数据类，定义卡牌的所有属性和行为。

**核心属性**:
```csharp
public string CardId { get; set; }          // 卡牌ID
public string Name { get; set; }            // 卡牌名称
public string Description { get; set; }     // 卡牌描述
public int Level { get; set; }              // 卡牌等级
public int MaxLevel { get; set; }           // 最大等级

public int Cost { get; set; }               // 能量消耗
public int BaseCost { get; set; }           // 基础能量消耗

public bool IsAttack { get; set; }          // 是否攻击卡
public bool IsDefense { get; set; }         // 是否防御卡
public bool IsAreaAttack { get; set; }      // 是否AOE攻击
public TargetType Target { get; set; }      // 目标类型
public int TargetPosition { get; set; }    // 目标位置

public int Damage { get; set; }             // 伤害值
public int ShieldGain { get; set; }         // 护盾增益
public int EnergyGain { get; set; }         // 能量增益
public int HealAmount { get; set; }        // 治疗量
public int DrawCount { get; set; }          // 抽牌数

public string ApplyBuffName { get; set; }   // 附加Buff名称
public int ApplyBuffDuration { get; set; }  // Buff持续回合
public string ApplyDebuffName { get; set; } // 附加Debuff名称
public int ApplyDebuffDuration { get; set; } // Debuff持续回合

public bool IsEngraved { get; set; }        // 是否已刻印
public EngravingType EngravingType { get; set; } // 刻印类型
```

**目标类型枚举**:
```csharp
public enum TargetType
{
    None = 0,     // 无目标
    Front = 1,    // 前方目标
    Rear = 2,     // 后方目标
    Position = 3  // 指定位置
}
```

##### CardData (`Scripts/Battle/Card/CardData.cs`)

**功能说明**: 卡牌配置数据结构，用于从JSON加载卡牌数据。

##### CardConfigLoader (`Scripts/Battle/Card/CardConfigLoader.cs`)

**功能说明**: 卡牌配置加载器，从JSON文件加载卡牌数据。

**核心方法**:
```csharp
public static void LoadCards()        // 加载所有卡牌配置
public static List<Card> CreateDeck() // 创建卡组
```

##### CardLevelSystem (`Scripts/Battle/Card/CardLevelSystem.cs`)

**功能说明**: 卡牌等级系统，管理卡牌的升级和属性计算。

##### DescriptionGenerator (`Scripts/Battle/Card/DescriptionGenerator.cs`)

**功能说明**: 卡牌描述生成器，动态生成卡牌的描述文本。

#### 3.2.3 角色系统 (CharacterSystem)

##### Attacker (`Scripts/Battle/CharacterSystem/Attacker.cs`)

**功能说明**: 攻击者实体，由4个角色组成的队伍。

**核心属性**:
```csharp
public CharacterDefinition[] Characters { get; set; } // 角色数组（固定4个）
private List<Card> _teamDeck;                           // 队伍卡组
private Dictionary<Card, CharacterDefinition> _cardToCharacter; // 卡牌与角色映射

// 总属性（4角色属性之和）
public int TotalHealth => Characters.Sum(c => c.BaseHealth);
public int TotalEnergy => Characters.Sum(c => c.BaseEnergy);
public int TotalAttack => Characters.Sum(c => c.BaseAttack);
public int TotalDefense => Characters.Sum(c => c.BaseDefense);
public int TotalDrawCount => Characters.Sum(c => c.BaseDrawCount);
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `SetCharacters()` | public | 设置4个角色 |
| `BuildTeamDeck()` | private | 构建队伍卡组（每角色5张卡） |
| `CreateCardFromId()` | private | 根据卡ID创建卡牌 |
| `GetTeamDeck()` | public | 获取队伍卡组 |
| `GetCharacterByCard()` | public | 根据卡牌获取对应角色 |
| `GetCharacterAt()` | public | 获取指定位置的角色 |

**卡组构建逻辑**:
```
每个角色拥有5张卡牌：
  - 1张攻击卡 (AttackCardId)
  - 1张防御卡 (DefenseCardId)
  - 3张特殊卡 (SpecialCardIds)

Attacker卡组总数：4 × 5 = 20张卡
```

##### CharacterDefinition (`Scripts/Battle/CharacterSystem/CharacterDefinition.cs`)

**功能说明**: 角色定义类，包含角色的所有属性和卡牌信息。

**核心属性**:
```csharp
public string CharacterId { get; set; }     // 角色ID
public string Name { get; set; }            // 角色名称
public string Emoji { get; set; }           // 角色表情
public CharacterArchetype Archetype { get; set; } // 角色原型
public CharacterRace Race { get; set; }     // 角色种族

// 基础属性
public int BaseHealth { get; set; }         // 生命值
public int BaseEnergy { get; set; }         // 能量
public int BaseAttack { get; set; }         // 攻击力
public int BaseDefense { get; set; }       // 防御力
public int BaseDrawCount { get; set; }     // 抽牌数

public int BaseDeathResistance { get; set; } // 死亡抵抗

// 卡牌ID
public string AttackCardId { get; set; }    // 攻击卡ID
public string DefenseCardId { get; set; }   // 防御卡ID
public string[] SpecialCardIds { get; set; } // 特殊卡ID数组

// 必杀技
public UltimateSkill Ultimate { get; set; } // 终极技能
```

**角色创建工厂方法**:
```csharp
// 12个生肖角色工厂方法
CharacterDefinition.CreateRat()     // 鼠
CharacterDefinition.CreateOx()      // 牛
CharacterDefinition.CreateTiger()    // 虎
CharacterDefinition.CreateRabbit()   // 兔
CharacterDefinition.CreateDragon()  // 龙
CharacterDefinition.CreateSnake()   // 蛇
CharacterDefinition.CreateHorse()    // 马
CharacterDefinition.CreateGoat()    // 羊
CharacterDefinition.CreateMonkey()   // 猴
CharacterDefinition.CreateRooster()  // 鸡
CharacterDefinition.CreateDog()      // 狗
CharacterDefinition.CreatePig()      // 猪

// 获取所有角色
CharacterDefinition.GetAllCharacters()
```

##### CharacterPosition (`Scripts/Battle/CharacterSystem/CharacterPosition.cs`)

**功能说明**: 角色位置管理器，管理角色在攻击者中的位置和怒气。

**核心属性**:
```csharp
public int PositionIndex { get; set; }     // 位置索引
public CharacterDefinition Character { get; set; } // 对应角色
public int CurrentRage { get; set; }       // 当前怒气
```

##### UltimateSkill (`Scripts/Battle/CharacterSystem/UltimateSkill.cs`)

**功能说明**: 终极技能（必杀技）定义。

**怒气释放条件**: 怒气值达到100

##### DeathResistanceSystem (`Scripts/Battle/CharacterSystem/DeathResistanceSystem.cs`)

**功能说明**: 死亡抵抗系统，处理角色免于死亡的机制。

#### 3.2.4 怒气系统 (RageManager)

##### RageManager (`Scripts/Battle/RageManager.cs`)

**功能说明**: 怒气管理器，管理角色的怒气值和必杀技释放。

**怒气获取规则**:
| 触发条件 | 怒气增加 |
|:---|:---|
| 使用攻击卡 | +15 |
| 使用防御卡 | +10 |
| 被敌人攻击扣血 | +5 |
| 回合结束 | +5 |

**核心方法**:
```csharp
public void OnAttackCardPlayed(CharacterPosition position)  // 攻击卡触发
public void OnDefenseCardPlayed(CharacterPosition position)  // 防御卡触发
public void OnPlayerDamaged(int damage)                      // 受伤触发
public void OnTurnEnd()                                      // 回合结束触发
public void AddRage(CharacterPosition position, int amount)  // 增加怒气
public void UseUltimate(CharacterPosition position)          // 使用必杀技
public bool CanUseUltimate(CharacterPosition position)        // 是否可使用必杀技
```

##### BattleEndRageHandler (`Scripts/Battle/BattleEndRageHandler.cs`)

**功能说明**: 战斗结束怒气处理器，计算战斗后怒气获取。

---

### 3.3 六边形地图系统 (HexMap)

#### 3.3.1 坐标系统 (HexCoord)

**文件路径**: `Scripts/Battle/HexMap/HexCoord.cs`

**功能说明**: 六边形地图坐标系统，使用axial坐标（Q, R）。

**坐标定义**:
```csharp
public readonly struct HexCoord : System.IEquatable<HexCoord>
{
    public readonly int Q;  // 轴向Q坐标
    public readonly int R;  // 轴向R坐标
    public int S => -Q - R; // 轴向S坐标（冗余）
}
```

**方向向量**:
| 方向 | Q, R 增量 | 说明 |
|:---|:---|:---|
| North | (0, -1) | 正北 |
| South | (0, 1) | 正南 |
| NorthEast | (1, -1) | 东北 |
| NorthWest | (-1, 0) | 西北 |
| SouthEast | (1, 0) | 东南 |
| SouthWest | (-1, 1) | 西南 |

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `GetNeighbors()` | public | 获取6个相邻格子坐标 |
| `GetNeighbor(int direction)` | public | 获取指定方向的邻居 |
| `DistanceTo()` | public | 计算到另一格子的距离 |
| `IsWithinRadius()` | public | 是否在指定半径内 |
| `GetDirectionTo()` | public | 获取到目标的方向 |
| `ToOffset()` | public | 转换为offset坐标 |
| `FromOffset()` | public static | 从offset坐标创建 |

**坐标转换公式**:
```
Axial → World (屏幕坐标)
x = 150 * Q + 75 * R
y = 117 * R

World → Axial
r = round(y / 117)
q = round((x - 75 * r) / 150)
```

#### 3.3.2 地图格子 (HexTile)

**文件路径**: `Scripts/Battle/HexMap/HexTile.cs`

**功能说明**: 六边形地图格子数据类。

**核心属性**:
```csharp
public HexCoord Coord { get; set; }        // 格子坐标
public HexEventType EventType { get; set; } // 事件类型
public string DisplayName { get; set; }    // 显示名称
public string IconPath { get; set; }        // 图标路径
public bool CanEnter { get; set; }          // 是否可进入
public bool IsVisited { get; set; }         // 是否已访问
public bool IsDisappeared { get; set; }    // 是否消失（洞穴）
public TeleportRole TeleportRole { get; set; } // 传送门角色

// 商店刷新次数
public int ShopRefreshCount { get; set; }
```

**事件类型枚举**:
```csharp
public enum HexEventType
{
    None,           // 普通格子
    BattleNormal,   // 普通战斗
    BattleElite,    // 精英战斗
    BattleBoss,     // Boss战斗
    Swamp,          // 沼泽
    GainBlackMark,  // 获得黑印
    Shop,           // 商店
    Heal,           // 生命之泉
    TwoWayTeleport, // 双向传送门
    OneDirectionTele, // 单向传送门
    Hole            // 洞穴
}
```

#### 3.3.3 地图生成器 (HexMapGenerator)

**文件路径**: `Scripts/Battle/HexMap/HexMapGenerator.cs`

**功能说明**: 六边形地图生成器。

**核心方法**:
```csharp
public HexMap Generate(int radius, int playerLevel)  // 生成地图
private void PlaceEvents(HexMap map, int difficulty) // 放置事件
private void PlaceHoles(HexMap map, int holeCount)   // 放置洞穴
private void PlaceTeleports(HexMap map)              // 放置传送门
```

**生成参数**:
- `radius`: 地图半径（默认5，生成11×11=121个格子）
- `playerLevel`: 玩家等级（影响敌人强度）

#### 3.3.4 地图控制器 (HexMapController)

**文件路径**: `Scripts/Battle/HexMap/HexMapController.cs`

**功能说明**: 六边形地图控制器，管理玩家移动、事件触发等。

**核心属性**:
```csharp
public HexMap CurrentMap { get; }              // 当前地图
public HexCoord CurrentPosition { get; }      // 玩家当前位置
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `SetPlayerLevel()` | public | 设置玩家等级 |
| `MoveTo()` | public | 移动到指定格子 |
| `QuickMoveTo()` | public | 快速移动（直达终点） |
| `TeleportPlayerTo()` | public | 传送玩家 |
| `GetPathTo()` | public | 获取到目标的路径 |
| `OpenShop()` | public | 打开商店 |
| `CloseShop()` | public | 关闭商店 |

#### 3.3.5 事件管理器 (HexEventManager)

**文件路径**: `Scripts/Battle/HexMap/HexEventManager.cs`

**功能说明**: 六边形地图事件管理器，处理格子事件触发。

**事件类型处理**:
- 战斗事件 → 进入战斗
- 商店事件 → 打开商店
- 传送门 → 执行传送
- 黑印格子 → 增加黑印

---

### 3.4 AI系统 (AI)

#### 3.4.1 敌人AI接口 (IEnemyAI)

**文件路径**: `Scripts/AI/IEnemyAI.cs`

**功能说明**: 敌人AI接口定义。

```csharp
public interface IEnemyAI
{
    AIAction ChooseAction(Enemy enemy, Player player, List<Enemy> allEnemies);
    float CalculateActionPriority(Enemy enemy, Player player, AIActionType actionType);
}
```

**AI动作类型**:
```csharp
public enum AIActionType
{
    Attack,  // 攻击
    Defend,  // 防御
    Buff,    // 增益
    Heal     // 治疗
}
```

#### 3.4.2 AI实现类

| 类名 | 文件路径 | 行为特点 |
|:---|:---|:---|
| `AggressiveAI` | `Scripts/AI/AggressiveAI.cs` | 激进型，优先攻击 |
| `BalancedAI` | `Scripts/AI/BalancedAI.cs` | 均衡型，攻防兼备 |
| `DefensiveAI` | `Scripts/AI/DefensiveAI.cs` | 防守型，优先防御 |

---

### 3.5 UI系统 (UI)

#### 3.5.1 六边形地图UI (HexMapUI)

**文件路径**: `Scripts/UI/HexMapUI/HexMapUI.cs`

**功能说明**: 六边形地图界面UI，管理地图显示、玩家移动、事件显示。

**核心属性**:
```csharp
private HexMapController _controller;           // 地图控制器
private Control _tileViewsContainer;          // 格子视图容器
private PlayerIcon _playerIcon;               // 玩家图标
private HealthBar _healthBar;                 // 生命条
private SilverKeyProgressIndicator _silverKeyProgressIndicator; // 银钥进度条
private Dictionary<HexCoord, HexTileView> _tileViews; // 格子视图字典
private bool _isInteractionBlocked;           // 是否阻止交互
```

**核心方法**:

| 方法名 | 访问级别 | 功能描述 |
|:---|:---|:---|
| `HexToWorld()` | private | Axial坐标转屏幕坐标 |
| `WorldToHex()` | private | 屏幕坐标转Axial坐标 |
| `CenterOnPlayer()` | private | 相机跟随玩家 |
| `RefreshMap()` | private | 刷新地图显示 |
| `CreateTileView()` | private | 创建格子视图 |
| `OnTileClicked()` | private | 处理格子点击 |
| `OnTileHovered()` | private | 处理格子悬停 |
| `OnPlayerMoved()` | private | 处理玩家移动 |
| `ShowEventNotification()` | private | 显示事件通知 |

#### 3.5.2 六边形格子视图 (HexTileView)

**文件路径**: `Scripts/UI/HexMapUI/HexTileView.cs`

**功能说明**: 单个六边形格子的可视化组件。

**核心方法**:
```csharp
public void SetTile(HexTile tile)        // 设置格子数据
public void SetAsPath(bool isPath)      // 设置路径高亮
public void SetPlayerCurrent(bool current) // 设置玩家当前位置
public void AnimateDisappear()          // 消失动画
```

#### 3.5.3 银钥进度指示器 (SilverKeyProgressIndicator)

**文件路径**: `Scripts/UI/SilverKeyProgressIndicator.cs`

**功能说明**: 显示银钥进度条，圆形进度条设计。

**显示规则**:
| 银钥值 | 显示效果 |
|:---|:---|
| 0-999 | 银色进度弧线 |
| 1000 | 满值，金色呼吸灯特效 |
| 1001+ | 银色满格 + 金色超额 |

#### 3.5.4 卡牌UI (CardUI)

**文件路径**: `Scripts/UI/CardUI.cs`

**功能说明**: 卡牌可视化组件。

**核心方法**:
```csharp
public void Setup(Card card, int cardIndex, bool canPlay)  // 设置卡牌数据
public void SetTexture(Texture2D texture)                   // 设置卡牌图片
public void SetPlayCallback(Action callback)               // 设置使用回调
```

#### 3.5.5 其他UI组件

| 组件名 | 场景文件 | 功能描述 |
|:---|:---|:---|
| `EnemyUI` | `Enemy.tscn` | 敌人显示组件 |
| `PlayerStatsUI` | `PlayerStats.tscn` | 玩家状态显示 |
| `RageCircle` | `RageCircle.tscn` | 怒气圆圈显示 |
| `ShopUI` | `ShopUI.tscn` | 商店界面 |
| `BackpackUI` | `BackpackUI.tscn` | 背包界面 |
| `EngravingCardSelectionUI` | `EngravingCardSelectionUI.tscn` | 刻印卡牌选择 |
| `BlackMarkPopup` | `BlackMarkPopup.tscn` | 黑印获得弹窗 |
| `FailurePanel` | `FailurePanel.tscn` | 失败面板 |
| `TeleportDialog` | `TeleportDialog.tscn` | 传送门对话框 |

---

### 3.6 银钥/钥令系统 (SilverKeySystem)

#### 3.6.1 银钥配置 (SilverKeyConfig)

**文件路径**: `Scripts/Battle/SilverKeySystem/SilverKeyConfig.cs`

**功能说明**: 银钥系统配置。

**核心属性**:
```csharp
public int SilverPerEnergy { get; set; }     // 每点能量获得银钥
public int BaseMaxSilverKey { get; set; }   // 基础最大银钥
public int MaxStackSilverKey { get; set; }  // 最大累积银钥
```

**默认值**:
- 每消耗1能量 → 获得5银钥
- 最大银钥值：1000
- 最大累积：2000

#### 3.6.2 钥令 (KeyOrder)

**文件路径**: `Scripts/Battle/SilverKeySystem/KeyOrder.cs`

**功能说明**: 钥令数据结构，定义钥令的名称、消耗、效果。

**核心属性**:
```csharp
public string Name { get; set; }            // 钥令名称
public string Description { get; set; }    // 效果描述
public int SilverKeyCost { get; set; }     // 银钥消耗
public KeyOrderEffect Effect { get; set; } // 效果类型
```

**效果类型**:
```csharp
public enum KeyOrderEffect
{
    SilverSlash,   // 银斩：造成15伤害
    KeyShield,     // 钥盾：获得15护盾
    EnergyInfusion, // 能量灌注：+2能量
    ArmorBreak,    // 破甲：敌人防御-5
    LifeKey        // 生命钥：回复15生命
}
```

#### 3.6.3 钥令管理器 (KeyOrderManager)

**文件路径**: `Scripts/Battle/SilverKeySystem/KeyOrderManager.cs`

**功能说明**: 钥令管理器，处理钥令的使用和效果应用。

**核心方法**:
```csharp
public void EquipKeyOrder(KeyOrder order)                          // 装备钥令
public bool CanUseKeyOrder()                                      // 是否可使用
public void RecordKeyOrderUse()                                   // 记录使用
public void ResetTurnUsage()                                      // 重置回合使用
public void ApplyKeyOrderEffect(KeyOrder order, Player player, Enemy[] enemies) // 应用效果
public List<KeyOrder> GetRandomKeyOrders(int count)               // 获取随机钥令
```

---

### 3.7 刻印系统 (EngravingSystem)

#### 3.7.1 刻印类型 (EngravingType)

**文件路径**: `Scripts/Battle/EngravingSystem/EngravingType.cs`

**功能说明**: 刻印类型枚举。

**刻印列表**:
| 刻印编号 | 刻印名称 | 效果 |
|:---|:---|:---|
| E1 | 护盾刻印 | 获得护盾时额外+3 |
| E2 | 毒伤刻印 | 攻击附加3层中毒 |
| E3 | 复制刻印 | 30%概率复制卡牌 |
| E4 | 团队怒气 | 怒气获得+20% |
| E5 | 自身怒气 | 攻击获得+5怒气 |
| E6 | 易伤刻印 | 攻击附加易伤效果 |

#### 3.7.2 刻印管理器 (EngravingManager)

**文件路径**: `Scripts/Battle/EngravingSystem/EngravingManager.cs`

**功能说明**: 刻印管理器，处理刻印的加载和应用。

---

### 3.8 造物系统 (ArtifactSystem)

#### 3.8.1 造物 (Artifact)

**文件路径**: `Scripts/Battle/ArtifactSystem/Artifact.cs`

**功能说明**: 造物数据类。

**核心属性**:
```csharp
public string ArtifactId { get; set; }      // 造物ID
public string Name { get; set; }           // 名称
public string Description { get; set; }    // 效果描述
public string Type { get; set; }           // 类型（正面/负面）
public string Effect { get; set; }          // 效果类型
public int EffectValue { get; set; }        // 效果值
public int Cost { get; set; }              // 价格
```

#### 3.8.2 造物管理器 (ArtifactManager)

**文件路径**: `Scripts/Battle/ArtifactSystem/ArtifactManager.cs`

**功能说明**: 造物管理器，处理造物的购买和效果应用。

---

### 3.9 效果系统 (Effects)

#### 3.9.1 效果基类 (Effect)

**文件路径**: `Scripts/Battle/Effects/Effect.cs`

**功能说明**: 效果基类。

#### 3.9.2 Buff/Debuff系统

**文件路径**: `Scripts/Battle/Effects/Buffs/`

| 类名 | 功能描述 |
|:---|:---|
| `Buff.cs` | Buff基类 |
| `Debuff.cs` | Debuff基类 |
| `StatusEffect.cs` | 状态效果类 |

**Buff类型**:
- `StrengthBuff` - 力量增强
- `DefenseBuff` - 防御增强
- `RegenerationBuff` - 回复生命
- `ThornsBuff` - 反伤
- `FuryBuff` - 狂暴

**Debuff类型**:
- `WeakDebuff` - 虚弱
- `VulnerableDebuff` - 易伤
- `PoisonDebuff` - 中毒
- `SlowDebuff` - 减速
- `SilenceDebuff` - 沉默

---

## 4. 关键类与函数说明

### 4.1 全局数据管理 (GlobalData)

**文件路径**: `Scripts/Scenes/GlobalData.cs` (推测)

**功能说明**: 全局数据存储类，保存游戏状态。

**核心属性**:
```csharp
public static CharacterDefinition[] SelectedCharacters { get; set; } // 已选角色
public static KeyOrder EquippedKeyOrder { get; set; }                  // 已装备钥令
public static bool EnterHexMapMode { get; set; }                      // 是否进入地图模式
```

### 4.2 资源管理器 (ResourceManager)

**文件路径**: `Scripts/Battle/ResourceManager.cs`

**功能说明**: 管理游戏资源加载。

**核心属性**:
```csharp
public PackedScene EnemyScene { get; }           // 敌人场景
public PackedScene CardScene { get; }           // 卡牌场景
public PackedScene PlayerStatsScene { get; }    // 玩家状态场景
public PackedScene BattleLogScene { get; }      // 战斗日志场景
public Dictionary<string, Texture2D> CardTextures { get; } // 卡牌图片字典
public Texture2D EnemyTexture { get; }          // 敌人纹理
```

### 4.3 卡牌布局管理器 (CardLayoutManager)

**文件路径**: `Scripts/Battle/CardLayoutManager.cs`

**功能说明**: 管理手牌的扇形布局。

**核心属性**:
```csharp
public float CardWidth { get; }                // 卡牌宽度
public float CardHeight { get; }               // 卡牌高度
```

**核心方法**:
```csharp
public (float x, float y) GetCardDrawTargetPosition(Control container, int index, int total)
public Vector2 GetCardDrawStartPosition(Control container)
public void UpdateCardPositions(List<CardUI> cards, Control container, int hoveredIndex)
```

### 4.4 回合管理器 (TurnManager)

**文件路径**: `Scripts/Battle/TurnManager.cs`

**功能说明**: 管理回合流程。

---

## 5. 数据配置系统

### 5.1 数据文件结构

**卡牌数据** (`Data/cards.json`):
```json
{
  "cardId": "strike",
  "name": "打击",
  "cost": 1,
  "damage": 6,
  "isAttack": true,
  "target": "Front"
}
```

**角色数据** (`Data/characters.json`):
```json
{
  "characterId": "rat",
  "name": "鼠",
  "baseHealth": 25,
  "baseEnergy": 2,
  "baseAttack": 4,
  "baseDefense": 2,
  "baseDrawCount": 2
}
```

**造物数据** (`Data/artifacts.json`):
```json
{
  "artifactId": "life_stem",
  "name": "生命圣杯",
  "description": "战斗开始时恢复15点生命",
  "type": "positive",
  "effect": "heal_on_battle_start",
  "effectValue": 15,
  "cost": 30
}
```

**刻印数据** (`Data/BuffEngraving.json`):
```json
{
  "engravingId": "shield",
  "name": "护盾刻印",
  "description": "获得护盾时额外+3",
  "icon": "🛡️",
  "effect": "extra_shield",
  "effectValue": 3
}
```

### 5.2 配置加载器

| 加载器 | 文件路径 | 加载内容 |
|:---|:---|:---|
| `CardConfigLoader` | `Scripts/Battle/Card/CardConfigLoader.cs` | 卡牌数据 |
| `CharacterConfigLoader` | `Scripts/Battle/CharacterSystem/CharacterConfigLoader.cs` | 角色数据 |
| `ArtifactManager` | `Scripts/Battle/ArtifactSystem/ArtifactManager.cs` | 造物数据 |
| `EngravingManager` | `Scripts/Battle/EngravingSystem/EngravingManager.cs` | 刻印数据 |
| `EffectConfigLoader` | `Scripts/Battle/Effects/EffectConfigLoader.cs` | 效果数据 |

---

## 6. 依赖关系

### 6.1 命名空间结构

```
FishEatFish
├── AI                              # AI系统
├── Battle                          # 战斗系统
│   ├── ArtifactSystem             # 造物系统
│   ├── Card                        # 卡牌系统
│   ├── CharacterSystem            # 角色系统
│   ├── Core                        # 核心类
│   ├── Effects                     # 效果系统
│   │   ├── Buffs                   # Buff/Debuff
│   │   └── Effects                 # 具体效果
│   ├── EngravingSystem            # 刻印系统
│   ├── HexMap                      # 六边形地图
│   ├── MapSystem                  # 地图系统
│   └── SilverKeySystem            # 银钥系统
├── Scenes                          # 场景脚本
├── Shop                            # 商店系统
└── UI                              # UI组件
    ├── ArtifactDescriptionUI
    ├── ArtifactDetailUI
    ├── BackpackUI
    ├── CardSelectionItem
    ├── EngravingCardSelectionUI
    ├── EngravingDescriptionUI
    ├── FailurePanel
    ├── HexMapUI
    ├── ShopUI
    ├── ShopItemCard
    └── TeleportDialog
```

### 6.2 类依赖关系图

```
BattleManager (场景脚本)
  ├── Attacker (角色组合)
  │   ├── CharacterDefinition[] (角色定义数组)
  │   └── Card[] (卡牌数组)
  ├── Player (玩家)
  ├── Enemy[] (敌人数组)
  ├── RageManager (怒气管理)
  ├── KeyOrderManager (钥令管理)
  ├── ResourceManager (资源管理)
  ├── CardLayoutManager (卡牌布局)
  └── EnemyManager (敌人管理)

HexMapScene (场景脚本)
  ├── HexMapController (地图控制器)
  │   ├── HexMap (地图)
  │   │   └── HexTile[] (格子数组)
  │   └── HexMapGenerator (地图生成器)
  └── HexMapUI (地图界面)
      ├── HexTileView[] (格子视图)
      ├── PlayerIcon (玩家图标)
      └── HealthBar (生命条)
```

### 6.3 场景依赖关系

```
MainMenu.tscn
  └── MainMenu.cs
        ├── LevelMap.tscn → CharacterSelection.tscn → BattleScene.tscn
        └── HexMapScene.tscn

CharacterSelection.tscn
  └── CharacterSelection.cs
        ├── GlobalData (存储选择)
        └── BattleScene.tscn / HexMapScene.tscn

BattleScene.tscn
  └── BattleManager.cs
        ├── Player.cs
        ├── Enemy.cs
        ├── Attacker.cs
        ├── RageManager.cs
        ├── KeyOrderManager.cs
        ├── CardUI.cs
        └── EnemyUI.cs

HexMapScene.tscn
  └── HexMapScene.cs
        ├── HexMapUI.cs
        └── HexMapController.cs
              ├── HexMap.cs
              ├── HexMapGenerator.cs
              └── HexEventManager.cs
```

---

## 7. 项目运行方式

### 7.1 开发环境要求

| 要求 | 版本 |
|:---|:---|
| Godot Engine | 4.6.1+ |
| .NET SDK | 8.0+ |
| 操作系统 | Windows (当前) |

### 7.2 运行步骤

1. **打开项目**
   ```
   使用 Godot 4.6.1 打开项目根目录
   ```

2. **导入项目**
   ```
   Godot 会自动导入 .tscn 和 .cs 文件
   ```

3. **运行游戏**
   ```
   按 F5 或点击运行按钮
   主场景：res://Scenes/MainMenu.tscn
   ```

### 7.3 测试场景入口

| 入口 | 场景文件 | 说明 |
|:---|:---|:---|
| 主菜单 | `MainMenu.tscn` | 游戏入口 |
| 战斗测试 | `BattleScene.tscn` | 直接进入战斗 |
| 角色选择 | `CharacterSelection.tscn` | 选择角色后进入战斗 |
| 六边形地图 | `HexMapScene.tscn` | 地图探索模式 |
| 关卡地图 | `LevelMap.tscn` | 关卡选择界面 |

### 7.4 编译命令

```bash
# 在项目目录执行
dotnet build

# 清理后重新编译
dotnet clean
dotnet build
```

### 7.5 调试技巧

1. **打印调试信息**
   ```csharp
   GD.Print($"[ClassName] MethodName: {variable}");
   GD.PrintErr($"[ClassName] Error: {message}");
   ```

2. **快速测试战斗**
   ```
   MainMenu → TestButton → 进入BattleScene
   ```

3. **测试地图生成**
   ```
   MainMenu → HoleTestButton → 验证洞穴生成
   ```

---

## 8. 开发规范

### 8.1 代码规范

> **第一准则：逻辑和页面分离**

| 职责 | 放在哪里 |
|:---|:---|
| UI 布局、尺寸、颜色、字体 | `.tscn` 场景文件 |
| 按钮点击响应、事件绑定 | `.cs` 脚本文件 |
| 动态数据（名称、数值、图标） | `.cs` 脚本文件 |
| 业务逻辑、状态判断 | `.cs` 脚本文件 |

### 8.2 日志规范

事件回调方法必须遵循以下格式：
```csharp
private void OnConfirmPressed()
{
    GD.Print($"[ClassName] OnConfirmPressed called");
    // ... 方法逻辑 ...
    GD.Print($"[ClassName] OnConfirmPressed completed");
}
```

**规则**：
- 方法第一行：`GD.Print($"[类名] 方法名 called")`
- 方法最后一行：`GD.Print($"[类名] 方法名 completed")`
- 日志格式：`[类名]` 使用实际类名

### 8.3 组件编写规范

**正确做法**：
```csharp
// ✅ 推荐：在.tscn中定义UI布局，代码只负责逻辑
public partial class ShopUI : Control
{
    private Button _closeButton;

    public override void _Ready()
    {
        _closeButton = GetNode<Button>("Panel/CloseButton");
        _closeButton.Pressed += OnClosePressed;
    }
}
```

**错误做法**：
```csharp
// ❌ 不推荐：代码硬编码所有UI元素
public override void _Ready()
{
    var panel = new PanelContainer();
    panel.CustomMinimumSize = new Vector2(400, 300);
    // ... 大量布局代码 ...
}
```

### 8.4 六边形地图规范

**坐标系统**：使用axial坐标（Q, R）

**坐标转换公式**：
```csharp
// Axial → World
Vector2 HexToWorld(HexCoord coord) {
    float x = 150f * coord.Q + 75f * coord.R;
    float y = 117f * coord.R;
    return new Vector2(x, y);
}
```

**关键参数**：
- 六边形宽度：180px
- 六边形高度：156px
- 水平间距：150
- 垂直间距：117

### 8.5 命名规范

| 类型 | 规范 | 示例 |
|:---|:---|:---|
| 命名空间 | PascalCase | `FishEatFish.Battle.Card` |
| 类名 | PascalCase | `BattleManager`, `HexMapUI` |
| 方法名 | PascalCase | `OnCardPlayed`, `InitializeAttacker` |
| 私有字段 | _camelCase | `_playerIcon`, `_hexSize` |
| 常量 | PascalCase | `MaxSelection`, `DefaultRadius` |

---

## 📊 项目统计

| 统计项 | 数量 |
|:---|:---|
| C# 脚本文件 | ~90+ |
| 场景文件 (.tscn) | ~30 |
| 数据配置文件 | ~10 |
| 卡牌数量 | ~60+ |
| 角色数量 | 12 (十二生肖) |
| AI行为模式 | 3 (激进/均衡/防守) |

---

## 🎯 核心功能清单

| 功能 | 状态 | 文件路径 |
|:---|:---|:---|
| 四人组队战斗 | ✅ 已实现 | `Attacker.cs` |
| 怒气系统 | ✅ 已实现 | `RageManager.cs` |
| 银钥/钥令系统 | ✅ 已实现 | `SilverKeySystem/` |
| 刻印系统 | ✅ 已实现 | `EngravingSystem/` |
| 造物系统 | ✅ 已实现 | `ArtifactSystem/` |
| 六边形地图 | ✅ 已实现 | `HexMap/` |
| 商店系统 | ✅ 已实现 | `ShopUI.cs`, `BlackMarkShopManager.cs` |
| Buff/Debuff | ✅ 已实现 | `Effects/Buffs/` |
| 敌人AI | ✅ 已实现 | `AI/` |
| 卡牌升级 | ✅ 已实现 | `CardLevelSystem.cs` |

---

## 📝 文档更新记录

| 日期 | 版本 | 更新内容 |
|:---|:---|:---|
| 2026-06-12 | 1.0 | 初始版本生成 |

---

> 本文档由代码分析工具自动生成，如有问题请联系开发团队。