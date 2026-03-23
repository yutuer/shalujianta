# 角色系统改进规格文档

## 1. 概念与愿景

将现有的单人玩家系统改为**四人组队战斗系统**。玩家在战斗前选择4个角色组成队伍，每个角色拥有独立的属性和5张卡牌（共20张）。

游戏以**地图/关卡**为单位进行。玩家选择一张地图后，需要选择4名角色组成攻击者，然后进入该地图的一系列战斗。

**核心设计**：

- 战斗前：选择4个角色组队
- 战斗中：4角色组合成一个**攻击者(Attacker)**，拥有总属性
- 伤害机制：敌人攻击由4角色总生命承担
- 角色动作：出牌时对应角色有出手动画
- 怒气系统：使用攻击/防御卡、受击、回合结束增加怒气，满100可释放大招
- **怒气跨战斗继承**
- **地图系统：每张地图包含1-3场战斗，胜利后可获得奖励**

***

## 2. 怒气系统

### 怒气获取

| 触发条件    | 怒气增加 |
| ------- | ---- |
| 使用攻击卡   | +15  |
| 使用防御卡   | +10  |
| 被敌人攻击扣血 | +5   |
| 回合结束    | +5   |

### 怒气显示

- 怒气获取值显示在角色对应的**攻击卡和防御卡**上
- 卡牌上怒气显示格式：`造成X点伤害，获得Y点怒气`
- 示例：`造成6点伤害，获得15点怒气` 或 `获得10点怒气`

### 怒气特效

当角色怒气达到100时：

- 该角色显示**发光特效**（在角色位置UI上）
- 特效持续直到大招释放
- 角色位置显示"可释放大招"提示
- 点击该角色释放大招

### 怒气继承

- **怒气跨战斗继承**，每场战斗后怒气不清零
- 战斗开始时继承上场的怒气值
- 释放大招后怒气清零

### 怒气释放

- 当角色怒气达到100时，点击该角色释放专属大招
- 大招效果在角色选择界面提前提示给玩家
- 释放大招后怒气清零

### 大招提示

在角色选择界面，显示每个角色的大招名称和效果预览：

```
[鼠] 鼠群来袭 - 全体敌人受20伤害，抽3张
[牛] 震天蛮牛冲 - 对最前方敌人造成50伤害
...
```

***

### 角色大招

| 角色 | 大招名称    | 效果                |
| -- | ------- | ----------------- |
| 鼠  | 鼠群来袭    | 全体敌人受20伤害，抽3张     |
| 牛  | 震天蛮牛冲   | 对最前方敌人造成50伤害      |
| 虎  | 猛虎灭世斩   | 全体敌人受30伤害         |
| 兔  | 疾风连蹬    | 随机敌人攻击4次，各10伤害    |
| 龙  | 龙王咆哮    | 全体敌人受25伤害，附加3层中毒  |
| 蛇  | 万蛇噬咬    | 全体敌人受15伤害，附加5层中毒  |
| 马  | 踏雪飞驹    | 对敌人造成20伤害，+2能量    |
| 羊  | 羊灵祝福    | 全体队友回复15生命，加10护盾  |
| 猴  | 齐天大圣    | 随机抽3张牌，本回合能量不限    |
| 鸡  | 雄鸡一唱天下白 | 敌人下回合无法行动         |
| 狗  | 忠犬护主    | 全体队友加25护盾         |
| 猪  | 猪刚鬣冲击   | 对最前方敌人造成35伤害，回10血 |

***

## 3. 攻击者(Attacker)概念

### 攻击者定义

4个角色组队后，组合成一个\*\*攻击者(Attacker)\*\*实体：

```
Attacker = 角色1 + 角色2 + 角色3 + 角色4
```

### 攻击者属性计算

```
AttackerHealth = 角色1.生命 + 角色2.生命 + 角色3.生命 + 角色4.生命
AttackerEnergy = 角色1.能量 + 角色2.能量 + 角色3.能量 + 角色4.能量
AttackerAttack = 角色1.攻击 + 角色2.攻击 + 角色3.攻击 + 角色4.攻击
AttackerDefense = 角色1.防御 + 角色2.防御 + 角色3.防御 + 角色4.防御
AttackerDrawCount = 角色1.抽牌 + 角色2.抽牌 + 角色3.抽牌 + 角色4.抽牌
```

### 攻击者UI显示

战斗中，攻击者以4个角色并列显示，统一展示总属性：

```
        怒气: 80/100        怒气: 45/100        怒气: 100/100        怒气: 20/100
    [角色1:牛]        [角色2:虎]        [角色3:龙⚡]        [角色4:鼠]
       待机             待机           发光特效           待机

总生命: 135  总攻击: 25  总防御: 15
```

怒气显示规则：

- 怒气值显示在**角色位置UI正上方**
- 显示格式：`怒气: X/100`
- 怒气满100时，该角色位置**发光特效**并显示⚡图标

### Buff/Debuff 显示

攻击者拥有独立的 Buff（增益）和 Debuff（减益）系统，显示在血条下方：

#### 显示规则

- Buff 图标和 Debuff 图标依次排列显示在血条下方
- 如果 Buff/Debuff 有效果值，显示在图标下方
- Buff 显示为蓝色/金色图标，Debuff 显示为红色图标
- 同类型 Buff/Debuff 可叠加，显示叠加层数

#### 显示位置

```
[角色位置UI]
   |
[血条: 135/135]
   |
[Buff/Debuff 图标区]
   |
[怒气值显示]
```

#### Buff 示例

| Buff名称 | 图标  | 效果值显示 |
| ------ | --- | ----- |
| 力量增强   | ⚔️  | +3    |
| 护盾     | 🛡️ | 5     |
| 能量充沛   | ⚡   | +2能量  |

#### Debuff 示例

| Debuff名称 | 图标  | 效果值显示 |
| -------- | --- | ----- |
| 中毒       | ☠️  | 3层    |
| 攻击力下降    | 📉  | -2    |
| 防御下降     | 🛡️ | -1    |

***

## 4. 当前系统分析

### 现状

- `Player.cs` 是单一玩家实体
- 所有卡牌共享同一个卡池
- 无角色选择界面

### 目标改造

- 从"单角色"变为"四角色攻击者"
- 每个角色独立属性和卡牌
- 攻击者拥有4角色总属性
- 队伍共享生命池（总属性）

***

## 5. 角色定义（12生肖）

| 角色              | 生命 | 能量 | 抽牌 | 攻击 | 防御 | 特点   |
| --------------- | -- | -- | -- | -- | -- | ---- |
| **鼠 (Rat)**     | 25 | 2  | 2  | 4  | 2  | 速攻型  |
| **牛 (Ox)**      | 45 | 2  | 1  | 8  | 5  | 高防高攻 |
| **虎 (Tiger)**   | 35 | 3  | 2  | 7  | 3  | 均衡输出 |
| **兔 (Rabbit)**  | 20 | 3  | 3  | 4  | 2  | 速攻抽牌 |
| **龙 (Dragon)**  | 40 | 3  | 2  | 6  | 4  | 范围攻击 |
| **蛇 (Snake)**   | 25 | 2  | 2  | 5  | 2  | 持续伤害 |
| **马 (Horse)**   | 30 | 3  | 2  | 6  | 3  | 冲锋加成 |
| **羊 (Goat)**    | 30 | 2  | 2  | 4  | 4  | 辅助增益 |
| **猴 (Monkey)**  | 25 | 3  | 3  | 5  | 2  | 灵活多变 |
| **鸡 (Rooster)** | 20 | 2  | 2  | 7  | 2  | 高爆发  |
| **狗 (Dog)**     | 30 | 2  | 2  | 5  | 4  | 反击守护 |
| **猪 (Pig)**     | 35 | 2  | 1  | 5  | 5  | 生存回血 |

***

## 6. 卡牌设计（60张）

### 6.1 攻击卡（12张）

| 角色 | 卡牌名  | 费用 | 伤害 |
| -- | ---- | -- | -- |
| 鼠  | 鼠牙咬  | 1  | 4  |
| 牛  | 牛角顶  | 2  | 10 |
| 虎  | 虎爪击  | 2  | 8  |
| 兔  | 兔蹬鹰  | 1  | 5  |
| 龙  | 龙息   | 2  | 7  |
| 蛇  | 蛇咬   | 1  | 5  |
| 马  | 马蹄踏  | 2  | 7  |
| 羊  | 羊角顶  | 1  | 6  |
| 猴  | 猴拳   | 1  | 6  |
| 鸡  | 精准啄击 | 2  | 9  |
| 狗  | 狗咬   | 1  | 7  |
| 猪  | 猪拱   | 1  | 6  |

### 6.2 防御卡（12张）

| 角色 | 卡牌名  | 费用 | 护盾 |
| -- | ---- | -- | -- |
| 鼠  | 鼠洞藏  | 1  | 3  |
| 牛  | 牛皮厚  | 2  | 8  |
| 虎  | 虎啸威  | 1  | 5  |
| 兔  | 兔穴避  | 1  | 3  |
| 龙  | 龙鳞守  | 2  | 6  |
| 蛇  | 蛇蜕皮  | 1  | 4  |
| 马  | 马鬃护  | 1  | 5  |
| 羊  | 羊绒护  | 1  | 5  |
| 猴  | 猴树荡  | 1  | 4  |
| 鸡  | 鸡羽挡  | 1  | 4  |
| 狗  | 狗守夜  | 2  | 6  |
| 猪  | 泥巴护甲 | 1  | 6  |

### 6.3 特殊能力卡（36张，每角色3张）

#### 鼠

- **群体迁逃**: 费用1，全队抽2张
- **盗食**: 费用1，偷取敌人1张牌
- **嗅觉敏锐**: 费用2，下3回合攻击+1

#### 牛

- **蛮力冲撞**: 费用2，伤害=敌人最大生命10%
- **踏地**: 费用2，全体敌人-3攻击2回合
- **坚韧不拔**: 费用3，生命<30%时伤害+50%

#### 虎

- **猛虎下山**: 费用2，本回合攻击+5
- **虎视眈眈**: 费用1，下个敌人行动-3伤害
- **丛林之王**: 费用3，无视护甲伤害

#### 兔

- **跳跃闪避**: 费用1，本回合闪避+50%
- **繁殖力**: 费用2，下2回合每回合抽2张
- **警觉逃脱**: 费用1，敌人下次攻击伤害-5

#### 龙

- **腾云驾雾**: 费用2，攻击2次各10伤害
- **降雨**: 费用2，全体敌人-2攻击
- **龙王吐息**: 费用3，对全体敌人造成15伤害

#### 蛇

- **毒液喷射**: 费用1，敌人中毒3层
- **缠绕**: 费用1，敌人下回合无法行动
- **致命诱惑**: 费用2，偷取敌人所有增益

#### 马

- **疾驰冲锋**: 费用2，本次攻击伤害x1.5
- **嘶鸣**: 费用1，全体敌人-1攻击
- **千里马**: 费用3，下3回合每回合+2能量

#### 羊

- **登山跳跃**: 费用1，抽1张，本回合能量+1
- **团队协作**: 费用2，全体队友+3护盾
- **和平使者**: 费用2，敌人下回合攻击伤害-50%

#### 猴

- **模仿术**: 费用1，复制敌人上次使用的牌
- **偷桃**: 费用1，偷取敌人2张手牌
- **灵巧**: 费用2，本回合攻击2次各8伤害

#### 鸡

- **啼晓**: 费用1，全体敌人-2防御
- **扑翼**: 费用1，敌人下回合无法攻击
- **精准啄击**: 费用2，造成15伤害，无视护甲

#### 狗

- **狂吠**: 费用1，敌人下回合伤害-5
- **追踪**: 费用2，下3回合敌人攻击必中
- **忠诚护主**: 费用3，全体队友+10护盾，+5攻击

#### 猪

- **贪吃回血**: 费用1，回复10生命
- **哼哼歌**: 费用1，全体队友回复5生命
- **泥巴护甲**: 费用2，获得15护盾，回复5生命

***

## 7. 数据结构

### 新增文件

```
Scripts/Battle/CharacterSystem/
├── CharacterDefinition.cs    # 角色定义
├── CharacterTeam.cs         # 队伍管理
├── CharacterPosition.cs     # 战斗位置（含怒气）
└── UltimateSkill.cs        # 大招组件

Scripts/Battle/
└── RageManager.cs          # 怒气管理器
```

### CharacterDefinition.cs

```csharp
public partial class CharacterDefinition : Resource
{
    [Export] public string CharacterId;
    [Export] public string Name;
    [Export] public string NameEn;

    [Export] public int BaseHealth = 30;
    [Export] public int BaseEnergy = 2;
    [Export] public int BaseDrawCount = 2;
    [Export] public int BaseAttack = 5;
    [Export] public int BaseDefense = 3;

    [Export] public string AttackCardId;
    [Export] public string DefenseCardId;
    [Export] public string[] SpecialCardIds = new string[3];

    [Export] public string UltimateSkillId;
}
```

### Attacker.cs

```csharp
public partial class Attacker : Node
{
    [Export] public CharacterDefinition[] Characters = new CharacterDefinition[4];

    public int TotalHealth => Characters.Sum(c => c.BaseHealth);
    public int TotalEnergy => Characters.Sum(c => c.BaseEnergy);
    public int TotalAttack => Characters.Sum(c => c.BaseAttack);
    public int TotalDefense => Characters.Sum(c => c.BaseDefense);
    public int TotalDrawCount => Characters.Sum(c => c.BaseDrawCount);

    public List<Card> GetTeamDeck() { ... }
    public CharacterDefinition GetCharacterByCard(Card card) { ... }
}
```

### CharacterPosition.cs

```csharp
public partial class CharacterPosition : Node2D
{
    [Export] public int PositionIndex;
    [Export] public CharacterDefinition Character;

    public int CurrentRage { get; set; }
    public const int MaxRage = 100;

    public void AddRage(int amount) { }
    public bool CanUseUltimate() => CurrentRage >= MaxRage;
    public void UseUltimate() { }
}
```

***

## 8. 地图/关卡系统

### 8.1 地图结构

每张地图（关卡）包含以下信息：

```
Map:
  - MapId: 地图唯一标识
  - Name: 地图名称
  - Description: 地图描述
  - Difficulty: 难度等级
  - Battles: 战斗列表（1-3场）
  - Rewards: 胜利奖励
```

### 8.2 地图示例

| 地图      | 名称   | 战斗数 | 难度 | 奖励         |
| ------- | ---- | --- | -- | ---------- |
| Map\_01 | 森林深处 | 1   | 简单 | 100金币      |
| Map\_02 | 沼泽地带 | 2   | 普通 | 200金币+卡牌   |
| Map\_03 | 龙之巢穴 | 3   | 困难 | 300金币+稀有卡牌 |

### 8.3 战斗流程

```
选择地图
    ↓
选择4名角色（组成攻击者）
    ↓
第1场战斗
    ↓ (胜利)
是否有下一场? ──是──→ 第2场战斗
    │                   ↓ (胜利)
    否              是否有下一场? ──是──→ 第3场战斗
    │                   │                   ↓ (胜利)
    ↓                   否                  是否还有下一场?
地图胜利            地图胜利                 │
    ↓                                      否
奖励结算                                      ↓
                                          地图胜利
                                              ↓
                                          奖励结算
```

### 8.4 怒气继承

- 怒气在**同一地图内跨战斗继承**
- 每场战斗开始时，继承上场的怒气值
- 释放大招后怒气清零
- **地图切换时怒气清零**

### 8.5 战斗奖励系统

每场战斗胜利后，弹出**造物选择面板**：

```
╔═══════════════════════════════╗
║      选择你的战利品           ║
╠═══════════════════════════════╣
║                               ║
║   [造物1]   [造物2]   [造物3]  ║
║   战士之心   龙的鳞片   怒气之魂  ║
║                               ║
║   +攻击+5    +防御+10   怒气满时 ║
║   -防御-1              伤害+20% ║
║                               ║
║        点击选择一件造物         ║
╚═══════════════════════════════╝
```

- 从造物池中随机抽取3个不同的造物显示
- 玩家选择1个后获得
- 选择后进入下一场战斗或结算

### 8.6 地图胜利奖励

地图胜利后发放**固定金币奖励**：

| 奖励类型 | 说明 |
| ---- | --------- |
| 金币 | 用于技能升级 |

金币数值根据地图难度而定：
- 简单：50金币
- 普通：100金币
- 困难：200金币

### 8.7 玩家金币系统

新增玩家金币属性：

```
PlayerData:
  - Gold: 金币数量
```

金币用于：
- 技能升级（后续扩展）

***

## 9. 造物系统（Artifact）

### 9.1 概述

造物是给攻击者或特定角色带来加成的装备系统。可同时带来正面和负面效果。

### 9.2 造物分类

| 类型 | 作用范围 | 示例 |
| ---- | --------- | ---- |
| 攻击者造物 | 对整个攻击者生效 | 所有角色攻击+3 |
| 角色造物 | 对特定角色生效 | 鼠的攻击+5 |
| 条件造物 | 满足条件时生效 | 当怒气满时伤害+10 |

### 9.3 造物数据结构

```
Artifact:
  - ArtifactId: 造物唯一标识
  - Name: 造物名称
  - Description: 造物描述
  - Rarity: 稀有度（普通/稀有/传说）
  - Effects: 效果列表
  - NegativeEffects: 负面效果列表（可选）
  - TargetType: 作用目标类型
  - TargetCharacterId: 目标角色ID（仅角色造物）
  - TriggerCondition: 触发条件（仅条件造物）
```

### 9.4 造物效果示例

#### 攻击者造物
| 造物名 | 效果 | 负面效果 |
| ---- | ---- | ---- |
| 战士之心 | 所有角色攻击+5 | 所有角色受到伤害+2 |
| 魔力戒指 | 所有角色能量+2 | 所有角色防御-1 |
| 生命护符 | 所有角色生命+20 | 无 |

#### 角色造物
| 造物名 | 效果 | 负面效果 |
| ---- | ---- | ---- |
| 鼠的利爪 | 鼠的攻击+8 | 鼠的防御-2 |
| 牛的坚韧 | 牛的生命+15 | 牛的能量-1 |
| 龙之鳞片 | 龙的防御+10 | 无 |

#### 条件造物
| 造物名 | 触发条件 | 效果 | 负面效果 |
| ---- | ---- | ---- | ---- |
| 怒气之魂 | 怒气满100时 | 伤害+20% | 防御-3 |
| 连击之刃 | 连续攻击3次 | 伤害+15 | 无 |
| 濒死之力 | 生命<30% | 攻击+10 | 受到伤害+5 |

### 9.5 造物获取

- 每场战斗胜利后，从造物池随机抽取3个供玩家选择
- 玩家选择1个后获得
- 获得的造物存储在背包中

### 9.6 造物装备

在角色选择界面，选择完4个角色后，可选择装备造物：

```
已选择: [牛] [虎] [龙] [鼠]

造物装备（最多4件）:
[战士之心+] [龙的鳞片+] [怒气之魂] [无]
                           ▲点击选择/取消

攻击者预览:
  总生命: 135
  总攻击: 25 (+5)
  总防御: 15
```

造物图标有"+"表示已装备。

### 9.7 造物效果显示

造物效果在战斗界面显示在攻击者状态区域：

```
攻击者: 135/135 | ⚔️25(+5) | 🛡️15 | ⚡11

装备造物: [战士之心+] [龙的鳞片+]
```

***

## 10. 数据结构

### 10.1 新增文件

```
Scripts/Battle/CharacterSystem/
├── CharacterDefinition.cs    # 角色定义
├── Attacker.cs              # 攻击者管理
├── CharacterPosition.cs     # 战斗位置（含怒气）
└── UltimateSkill.cs         # 大招组件

Scripts/Battle/
├── RageManager.cs           # 怒气管理器
├── BuffDebuffDisplay.cs    # Buff/Debuff显示
└── ArtifactSystem/
    ├── Artifact.cs         # 造物定义
    └── ArtifactManager.cs  # 造物管理器

Scripts/Battle/MapSystem/
├── MapDefinition.cs        # 地图定义
├── MapManager.cs           # 地图管理器
└── RewardSystem.cs        # 奖励系统

Scenes/UI/
├── CharacterSelection.tscn  # 角色选择界面
├── MapSelection.tscn       # 地图选择界面
└── ArtifactSelection.tscn  # 造物选择界面

Resources/
├── CharacterData/
│   └── characters.json     # 角色+卡牌+大招数据
├── MapData/
│   └── maps.json          # 地图配置数据
└── ArtifactData/
    └── artifacts.json     # 造物配置数据
```

---

## 11. UI设计

### 角色选择界面

```
选择4个角色组成攻击者 (0/4)

[鼠] [牛] [虎] [兔]
[龙] [蛇] [马] [羊]
[猴] [鸡] [狗] [猪]

已选择: [牛] [虎] [龙] [鼠]

造物装备（最多4件）:
[战士之心] [龙的鳞片] [怒气之魂] [无] [无]

攻击者预览:
  总生命: 135 (+20)
  总攻击: 25 (+5)
  总防御: 15 (-1)
  总能量: 11 (+2)

[开始战斗]
```

### 战斗界面

```
攻击者: 135/135 | ⚔️25(+5/-1) | 🛡️15(-1) | ⚡11(+2)

[角色1:牛][角色2:虎][角色3:龙][角色4:鼠]
   待机      待机     待机     待机
 怒气:80    怒气:45  怒气:100⚡怒气:20

装备造物: [战士之心+] [龙的鳞片+]

[手牌区 - 20张卡牌]

[敌人信息区]

[结束回合]
```

### 战斗胜利奖励界面

```
╔═══════════════════════════════╗
║      选择你的战利品           ║
╠═══════════════════════════════╣
║                               ║
║   [战士之心]  [龙的鳞片]  [怒气之魂] ║
║                               ║
║   +攻击+5    +防御+10   怒气满时  ║
║   -防御-1              伤害+20%  ║
║                               ║
║        点击选择一件造物         ║
╚═══════════════════════════════╝
```

### 地图胜利结算界面

```
╔═══════════════════════════════╗
║      地图胜利！              ║
╠═══════════════════════════════╣
║                               ║
║         🏆 森林深处           ║
║                               ║
║      获得奖励: 100 金币       ║
║                               ║
║      当前金币: 250            ║
║                               ║
║        [返回主菜单]           ║
╚═══════════════════════════════╝
```

***

## 12. 实施步骤

### Phase 1: 核心框架

1. 创建 `CharacterDefinition` 类
2. 创建 `Attacker` 类（攻击者/队伍管理）
3. 创建 `CharacterPosition` 位置组件（含怒气）
4. 创建 `RageManager` 怒气管理器
5. 创建 `UltimateSkill` 大招组件

### Phase 2: UI与集成

1. 创建角色选择界面
2. 创建角色战斗位置UI（带怒气条）
3. 实现怒气增加逻辑
4. 实现大招释放逻辑
5. 集成到 `BattleManager`

### Phase 3: 地图系统

1. 创建 `MapDefinition` 地图定义类
2. 创建 `MapManager` 地图管理器
3. 创建 `RewardSystem` 奖励系统
4. 创建 `maps.json` 地图配置数据
5. 创建地图选择界面

### Phase 4: 造物系统

1. 创建 `Artifact` 造物类
2. 创建 `ArtifactManager` 造物管理器
3. 创建 `artifacts.json` 造物配置数据
4. 创建造物选择界面
5. 造物效果应用到属性计算

### Phase 5: 数据层

1. 创建 `characters.json`（12角色+60卡牌+12大招）
2. 更新 `CardConfigLoader`

### Phase 6: 测试验证

1. 编译测试
2. 功能测试

***

## 12. 验收标准

1. 12个角色可正常选择
2. 4角色组成攻击者，总属性正确计算
3. 攻击者拥有20张卡牌
4. 怒气系统在出牌/受击/回合结束时正确增加
5. 怒气满时点击角色可释放大招
6. 出牌时角色动画触发
7. 伤害由攻击者总生命承担
8. 地图选择界面正常显示
9. 每张地图可配置1-3场战斗
10. 战斗胜利后可进入下一场或结算奖励
11. 怒气在同一地图内跨战斗继承
12. 地图切换时怒气清零
13. 造物系统正常生效（攻击者/角色/条件造物）
14. 造物正面和负面效果正确计算
15. 每场战斗胜利后弹出造物选择（随机3选1）
16. 地图胜利后发放金币奖励
17. 玩家金币正确存储和累加
18. 编译通过

***

## 13. 银钥系统

### 13.1 概述

银钥系统是攻击者专属的高级机制，允许玩家在战斗中消耗算力来累积银钥值，达到1000点时可释放强大的"钥令"技能。

### 13.2 银钥值属性

| 属性 | 数值 |
| ---- | ---- |
| 属性名称 | 银钥值 |
| 基础上限 | 1000点 |
| 最大累计层数 | 2层（最大可累计至2000点） |

### 13.3 银钥值获取规则

- **获取途径**：玩家耗费算力时，按耗费算力值×特定比例增加银钥值
- **增长限制**：达到1000点后继续增长，达到2000点时停止增长
- **重置机制**：每次进入**新关卡/地图**时，银钥值自动重置为0
- **跨战斗继承**：每场战斗后银钥值不清零，继承到下一场战斗

### 13.4 钥令系统

钥令是银钥系统的配套技能系统：

| 属性 | 数值 |
| ---- | ---- |
| 系统名称 | 钥令 |
| 消耗资源 | 银钥值 |
| 选择时机 | 进入关卡前，在4个角色选择界面同步选择 |

### 13.5 钥令数据结构

```csharp
public partial class KeyOrder : Resource
{
    [Export] public string KeyOrderId;
    [Export] public string Name;
    [Export] public string Description;
    [Export] public int SilverKeyCost = 1000;       // 消耗银钥值
    [Export] public KeyOrderEffectType EffectType;   // 效果类型
    [Export] public int EffectValue = 50;             // 效果数值
    [Export] public int Duration = 0;                // 持续时间（用于Debuff类型）
    
    // 效果执行委托
    public delegate void KeyOrderExecuteHandler(Player player, Enemy[] enemies);
    public KeyOrderExecuteHandler OnExecute;
    
    // 预设钥令工厂方法
    public static KeyOrder CreateSilverSlash();
    public static KeyOrder CreateKeyShield();
    public static KeyOrder CreateEnergyInfusion();
    public static KeyOrder CreateArmorBreak();
    public static KeyOrder CreateLifeKey();
}

public enum KeyOrderEffectType
{
    Damage,         // 伤害型
    Heal,           // 治疗型
    Buff,           // 增益型（护盾等）
    Debuff,         // 减益型（防御下降等）
    Special         // 特殊型（能量获取等）
}
```

### 13.6 钥令效果示例

| 钥令名称 | 银钥消耗 | 效果类型 | 效果描述 |
| ---- | --- | ---- | ---- |
| 银光斩 | 1000 | 伤害 | 对所有敌人造成50伤害 |
| 钥之护盾 | 1000 | 增益 | 全体队友获得30护盾 |
| 能量灌注 | 1000 | 特殊 | 本回合能量+5 |
| 破甲之钥 | 1000 | 减益 | 敌人防御-10持续3回合 |
| 生命之钥 | 1000 | 治疗 | 恢复攻击者30%最大生命 |

### 13.7 战斗中钥令释放规则

#### 释放条件
- 银钥值达到1000点时可释放携带的钥令
- 每回合可使用2次银钥技能

#### 第二次释放机制
当第一次钥令释放后，银钥值仍≥1000时可释放第二次：

1. 从钥令库中**随机抽取3个银钥技能**
2. 以**弹出卡片形式**展示给玩家
3. 玩家从3个随机钥令中选择1个进行释放

### 13.8 银钥系统UI设计

#### 战斗界面银钥显示

```
攻击者: 135/135 | ⚔️25 | 🛡️15 | ⚡11 | 🔑 850/1000

[角色1:牛][角色2:虎][角色3:龙][角色4:鼠]
   怒气:80      怒气:45   怒气:100⚡  怒气:20
```

银钥值显示在战斗界面**左下角**，格式：`🔑 850/1000`

#### 银钥进度指示器设计规范

##### 位置与尺寸

| 属性 | 数值 |
| ---- | ---- |
| 位置 | 左下角区域 |
| 直径 | 80像素 |
| 距边缘 | 20像素 |

##### 视觉设计

```
┌─────────────────────────────────────────┐
│                                         │
│                                         │
│                                         │
│                                         │
│                    ┌───────┐            │
│                    │       │            │
│                    │ 850   │            │
│                    │ 1000  │            │
│                    │       │            │
│                    └───────┘            │
│                    银钥进度               │
└─────────────────────────────────────────┘
```

**圆形进度指示器结构**：

| 层级 | 说明 | 颜色 |
| ---- | ---- | ---- |
| 背景层 | 灰色底圈 | #333333 |
| 基础进度层 | 0-1000进度 | #C0C0C0（银色） |
| 超额进度层 | 1000-2000进度 | #FFD700（金色） |
| 中心显示 | 当前值/满值 | #FFFFFF |
| 外框 | 装饰边框 | #666666 |

##### 超额进度视觉表现

当钥令值超过1000时：

```
┌─────────────────────────────────────────┐
│                                         │
│                    ┌───────┐            │
│                    │       │            │
│    ╭──────────╮   │ 1450  │            │
│   ╱ 金色超额进度 ╲  │ 1000  │            │
│  │  1000-2000  │  │       │            │
│   ╲ (金色光晕) ╱  └───────┘            │
│    ╰──────────╯                         │
│    银色基础进度                          │
│     0-1000                              │
└─────────────────────────────────────────┘
```

**超额进度显示规范**：

| 状态 | 视觉效果 |
| ---- | -------- |
| 1000-1500 | 银色进度+金色光晕 |
| 1500-2000 | 银色进度+金色进度+强烈光晕 |
| 2000（满） | 全部金色+脉冲动画 |

##### 禁用状态（不足1000）

```
┌─────────────────────────────────────────┐
│                    ┌───────┐            │
│                    │       │            │
│                    │  850  │            │
│                    │ 1000  │            │
│                    │       │            │
│                    └───────┘            │
│              (灰色/暗淡)                 │
└─────────────────────────────────────────┘
```

| 状态 | 视觉效果 |
| ---- | -------- |
| 0-999 | 灰色底圈+暗淡中心文字 |
| 1000+ | 正常银色/金色进度 |
| 钥令使用中 | 进度扣除动画 |

#### 交互与数值逻辑

##### 钥令使用流程

```
消耗1000钥令值
    ↓
进度条平滑过渡动画（0.3秒）
    ↓
┌─────────────────────────────────┐
│ 如果钥令值 >= 1000              │
│   - 显示可用状态                 │
│   - 启用钥令按钮                │
│ 否则                            │
│   - 显示禁用状态（灰色）         │
│   - 禁用钥令按钮                │
└─────────────────────────────────┘
```

##### 动画规范

| 动画类型 | 时长 | 缓动函数 |
| -------- | ---- | -------- |
| 进度增加 | 0.5秒 | EaseOut |
| 进度扣除 | 0.3秒 | EaseInOut |
| 超额光晕 | 持续 | 脉冲动画 |
| 禁用切换 | 0.2秒 | Linear |

#### 战斗界面完整布局

```
┌────────────────────────────────────────────────────────┐
│  [玩家状态栏]                          [敌人意图]     │
├────────────────────────────────────────────────────────┤
│                                                        │
│                                                        │
│                  [战斗区域]                            │
│                  [敌人区域]                            │
│                                                        │
│                                                        │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ┌─────────┐                                          │
│  │ 银钥进度 │                      [手牌区]            │
│  │ ○ 850   │                                          │
│  │   1000  │                                          │
│  └─────────┘                                          │
│                                                        │
│  [结束回合]  [钥令按钮]  [地图按钮]                   │
└────────────────────────────────────────────────────────┘
```

#### 响应式设计

| 分辨率 | 指示器尺寸 | 距边缘 |
| ------ | ---------- | ------ |
| 1920x1080 | 80px | 20px |
| 1280x720 | 60px | 15px |
| 移动端 | 50px | 10px |

#### 技术实现要点

```csharp
// SilverKeyProgressIndicator.cs
public partial class SilverKeyProgressIndicator : Control
{
    [Export] public int Diameter = 80;
    [Export] public Color BaseProgressColor = new Color("#C0C0C0");
    [Export] public Color ExcessProgressColor = new Color("#FFD700");
    [Export] public Color BackgroundColor = new Color("#333333");
    [Export] public Color DisabledColor = new Color("#666666");
    
    private int _currentValue;
    private int _maxValue = 1000;
    private int _maxStackValue = 2000;
    private Tween _progressTween;
    
    public void SetValue(int value, bool animate = true);
    public void UpdateDisplay();
    private void DrawProgressRing();
    private void DrawExcessGlow();
    private void AnimateProgress(int fromValue, int toValue);
}
```

#### 测试验证清单

| 测试项 | 预期结果 |
| ------ | -------- |
| 0钥令值 | 显示灰色禁用状态 |
| 500钥令值 | 显示50%银色进度 |
| 1000钥令值 | 显示满进度+可用状态 |
| 1500钥令值 | 显示银色+金色光晕 |
| 2000钥令值 | 显示全部金色+脉冲动画 |
| 使用钥令 | 平滑扣除动画 |
| 能量消耗 | 进度增加动画 |

#### 钥令释放面板

当银钥值达到1000时，显示钥令释放按钮：

```
╔═══════════════════════════════╗
║      钥令已就绪！           ║
╠═══════════════════════════════╣
║                               ║
║    [银光斩]                   ║
║    消耗: 1000银钥值           ║
║    效果: 对所有敌人造成50伤害   ║
║                               ║
║         [释放钥令]            ║
╚═══════════════════════════════╝
```

#### 随机钥令选择面板（第二次释放）

```
╔═══════════════════════════════╗
║   选择你的钥令 (3/3)          ║
╠═══════════════════════════════╣
║                               ║
║  [银光斩]   [钥之护盾]   [能量灌注] ║
║   伤害型     增益型       特殊型    ║
║                               ║
║  对所有敌人  全体队友      本回合   ║
║  造成50伤害  获得30护盾    能量+5   ║
║                               ║
║       点击选择一项钥令         ║
╚═══════════════════════════════╝
```

### 13.9 银钥系统数据结构

```csharp
// 银钥配置
public partial class SilverKeyConfig : Resource
{
    [Export] public int BaseMaxSilverKey = 1000;
    [Export] public int MaxStack = 2;
    [Export] public float SilverPerEnergy = 50f;  // 每消耗1点能量获得50银钥
    
    public int MaxStackSilverKey => BaseMaxSilverKey * MaxStack;
}

// 钥令管理器
public partial class KeyOrderManager : Node
{
    private List<KeyOrder> _keyOrderLibrary;     // 钥令库
    private KeyOrder _equippedKeyOrder;           // 已装备的钥令
    private int _keyOrderUseCountThisTurn;       // 本回合已使用次数
    private const int MaxKeyOrderUsePerTurn = 2;  // 每回合最大使用次数
    
    // 核心方法
    public void EquipKeyOrder(KeyOrder order);
    public KeyOrder GetEquippedKeyOrder();
    public List<KeyOrder> GetRandomKeyOrders(int count);  // 随机抽取钥令
    public bool CanUseKeyOrder();
    public void ApplyKeyOrderEffect(KeyOrder order, Player player, Enemy[] enemies);
}

// BattleManager 中的银钥状态
public partial class BattleManager : Node2D
{
    private SilverKeyConfig silverKeyConfig;
    private KeyOrderManager keyOrderManager;
    private int currentSilverKey;          // 当前银钥值
    private bool isKeyOrderSelectionOpen;  // 是否正在选择钥令
    
    // 银钥相关方法
    private void AddSilverKey(int energyCost);
    private void UpdateSilverKeyUI();
    private void OnKeyOrderPressed();
    private void ShowRandomKeyOrderSelection();
}
```

### 13.10 银钥系统实施要点

1. **银钥获取时机**：在`BattleManager`的`OnCardPlayed`中，算力消耗后调用`AddSilverKey`
2. **钥令选择界面**：在角色选择界面添加钥令选择步骤
3. **钥令存储**：每个攻击者携带1个钥令
4. **随机钥令池**：维护一个包含所有钥令的池子，用于第二次释放抽取
5. **跨战斗继承**：银钥值存储在`MapManager`中，每场战斗继承

### 13.11 验收标准

1. 银钥值在耗费算力时正确增加
2. 银钥值显示在战斗界面
3. 银钥达到1000时显示钥令释放按钮
4. 点击释放钥令后银钥值正确扣除
5. 每回合可使用2次钥令
6. 第二次释放时弹出3个随机钥令供选择
7. 进入新地图时银钥值重置为0
8. 每场战斗后银钥值正确继承

***

## 14. 刻印系统

### 14.1 概述

刻印系统是卡牌的增强功能，在战斗胜利后为卡牌附加额外效果。每张卡牌只能被刻印一次，刻印后会永久获得指定效果。

### 14.2 系统触发机制

每场战斗结束后，在显示奖励界面之前进行随机判定：

```
战斗胜利
    ↓
随机判定 (50% / 50%)
    ↓
┌─────────────┬─────────────┐
│   50%概率    │   50%概率    │
│  造物选择界面  │  刻印选择界面  │
└─────────────┴─────────────┘
```

**随机算法**：
```csharp
RandomNumberGenerator rng = new RandomNumberGenerator();
rng.Randomize();
int roll = rng.RandiRange(1, 100);
bool showEngraving = roll <= 50;
```

### 14.3 刻印选择界面

#### 界面布局

```
╔════════════════════════════════════════════════╗
║              选择刻印卡牌                     ║
╠════════════════════════════════════════════════╣
║                                                ║
║  从本场战斗使用的卡牌中随机选取3张未刻印的卡牌   ║
║                                                ║
║  ┌──────────┐ ┌──────────┐ ┌──────────┐     ║
║  │  [卡牌1]  │ │  [卡牌2]  │ │  [卡牌3]  │     ║
║  │          │ │          │ │          │     ║
║  │  卡牌名称  │ │  卡牌名称  │ │  卡牌名称  │     ║
║  │  费用: X  │ │  费用: X  │ │  费用: X  │     ║
║  │          │ │          │ │          │     ║
║  │ 刻印效果:  │ │ 刻印效果:  │ │ 刻印效果:  │     ║
║  │ [效果描述] │ │ [效果描述] │ │ [效果描述] │     ║
║  └──────────┘ └──────────┘ └──────────┘     ║
║                                                ║
║            点击卡牌进行刻印                     ║
╚════════════════════════════════════════════════╝
```

#### 候选卡牌筛选规则

1. 从玩家本场战斗使用的卡牌组合（20张）中筛选
2. 排除已被刻印的卡牌（每张卡只能刻印一次）
3. 从符合条件的卡牌中随机选取3张
4. 如果可用卡牌不足3张，则显示所有可用卡牌

### 14.4 刻印效果列表

#### 刻印效果类型

| 刻印编号 | 刻印名称 | 效果描述 | 数值设计 |
| -------- | -------- | -------- | -------- |
| E1 | 护盾刻印 | 使用此卡后为玩家增加护盾 | +5护盾 |
| E2 | 毒伤刻印 | 使用此卡后对敌方施加毒伤害 | 3回合，每回合3点伤害 |
| E3 | 复制刻印 | 复制一张此卡片加入卡组 | 获得1张复制 |
| E4 | 团队怒气 | 打出此卡后为其他队友增加怒气 | +15怒气 |
| E5 | 自身怒气 | 打出此卡后为自己增加怒气 | +30怒气 |
| E6 | 易伤刻印 | 给所有敌人施加易伤debuff | 1回合，+50%受伤 |

#### 刻印效果详细设计

**E1 - 护盾刻印**
```
刻印效果: 护盾增强
效果描述: 使用此卡后获得 +5 护盾
触发时机: 卡牌效果结算后
数值: +5 护盾
优先级: 高（护盾 > 伤害）
```

**E2 - 毒伤刻印**
```
刻印效果: 中毒之印
效果描述: 目标中毒 3 回合，每回合受 3 点伤害
触发时机: 卡牌造成伤害后
数值: 3层中毒，每层3伤害/回合
持续时间: 3回合
```

**E3 - 复制刻印**
```
刻印效果: 复制之印
效果描述: 获得一张此卡牌的复制
触发时机: 打出卡牌后立即触发
数值: +1张复制卡
特殊: 复制卡不计入本回合手牌上限
```

**E4 - 团队怒气刻印**
```
刻印效果: 怒气共鸣
效果描述: 其他队友各获得 +15 怒气值
触发时机: 卡牌效果结算后
生效范围: 出牌角色以外的其他3个角色
数值: +15怒气/角色
```

**E5 - 自身怒气刻印**
```
刻印效果: 怒气爆发
效果描述: 出牌角色获得 +30 怒气值
触发时机: 卡牌效果结算后
生效范围: 出牌角色自身
数值: +30怒气
```

**E6 - 易伤刻印**
```
刻印效果: 易伤之印
效果描述: 所有敌人受到伤害 +50%，持续1回合
触发时机: 卡牌造成伤害后
生效范围: 所有敌人
数值: 受伤+50%
持续时间: 1回合
```

### 14.5 刻印分配规则

- 3张候选卡牌的刻印效果**必须不重复**
- 每次随机分配时从6种效果中抽取3种
- 确保刻印效果的多样性和策略性

### 14.6 数据结构设计

```csharp
// 刻印效果枚举
public enum EngravingType
{
    Shield,       // E1 - 护盾刻印
    Poison,       // E2 - 毒伤刻印
    Copy,         // E3 - 复制刻印
    TeamRage,     // E4 - 团队怒气刻印
    SelfRage,     // E5 - 自身怒气刻印
    Vulnerable    // E6 - 易伤刻印
}

// 刻印效果数据
[System.Serializable]
public class EngravingEffect
{
    public EngravingType Type;
    public string Name;
    public string Description;
    public int PrimaryValue;      // 主数值
    public int SecondaryValue;    // 副数值（如持续回合）
    public int Priority;          // 触发优先级
}

// 刻印数据（附加在卡牌上）
public partial class Card
{
    public bool IsEngraved = false;
    public EngravingType EngravingType = EngravingType.Shield;
    public EngravingEffect EngravingEffect;
}

// 刻印管理器
public partial class EngravingManager : Node
{
    private List<EngravingEffect> _engravingLibrary;
    
    public List<EngravingEffect> GetRandomEngravings(int count);
    public void ApplyEngravingEffect(Card card, Player player, Enemy[] enemies);
    public void OnCardPlayed(Card card, Player player, Enemy[] enemies);
}
```

### 14.7 刻印效果触发流程

```
卡牌打出
    ↓
执行卡牌基础效果
    ↓
检查卡牌是否有刻印
    ↓ (有刻印)
执行刻印效果
    ↓
┌─────────────────────────────────┐
│ 根据刻印类型执行对应逻辑         │
│ - Shield: AddShield()           │
│ - Poison: ApplyPoison()          │
│ - Copy: AddCardToDeck()         │
│ - TeamRage: AddRageToTeam()     │
│ - SelfRage: AddRageToSelf()     │
│ - Vulnerable: ApplyVulnerable()  │
└─────────────────────────────────┘
    ↓
显示刻印效果提示
```

### 14.8 UI设计

#### 刻印选择界面元素

| 元素 | 说明 |
| ---- | ---- |
| 标题 | "选择刻印卡牌" |
| 副标题 | "从本场战斗使用的卡牌中随机选取" |
| 卡牌容器 | 水平排列3张候选卡牌 |
| 卡牌信息 | 显示卡牌名称、费用、效果 |
| 刻印标签 | 显示刻印类型图标 |
| 刻印描述 | 显示刻印效果文字说明 |
| 提示文字 | "点击卡牌进行刻印" |

#### 刻印效果预览

在卡牌上增加刻印徽章：

```
┌────────────────┐
│   [攻击卡]     │
│   卡牌名称     │
│   费用: 2     │
│                │
│ ┌────────────┐ │
│ │ ⚔️ 护盾刻印 │ │
│ │ +5 护盾    │ │
│ └────────────┘ │
└────────────────┘
```

### 14.9 刻印效果叠加规则

- **单卡限制**：每张卡只能被刻印一次
- **效果叠加**：同一张卡的不同效果不叠加
- **多卡刻印**：不同卡牌可以刻印不同效果
- **永久生效**：刻印效果永久存在于卡牌上

### 14.10 平衡性设计

#### 数值平衡表

| 刻印类型 | 基础强度 | 适用场景 | 评价 |
| -------- | -------- | -------- | ---- |
| E1 护盾 | +5 | 防守型 | ★★☆☆☆ |
| E2 毒伤 | 9总伤害 | 持续伤害 | ★★★☆☆ |
| E3 复制 | +1卡 | 资源获取 | ★★★★☆ |
| E4 团队怒气 | +45怒气 | 配合型 | ★★★★☆ |
| E5 自身怒气 | +30怒气 | 大招型 | ★★★☆☆ |
| E6 易伤 | +50%伤害 | 输出型 | ★★★★★ |

#### 策略制约

1. **复制刻印限制**：复制卡不保留刻印效果
2. **怒气刻印限制**：需要怒气系统支持
3. **易伤刻印限制**：效果持续时间短（1回合）

### 14.11 配置文件设计

```json
// Resources/EngravingData/engravings.json
{
    "engravingEffects": [
        {
            "id": "E1",
            "type": "Shield",
            "name": "护盾刻印",
            "description": "使用此卡后获得 +5 护盾",
            "primaryValue": 5,
            "rarity": "common"
        },
        {
            "id": "E2",
            "type": "Poison",
            "name": "毒伤刻印",
            "description": "目标中毒 3 回合，每回合受 3 点伤害",
            "primaryValue": 3,
            "secondaryValue": 3,
            "rarity": "common"
        },
        {
            "id": "E3",
            "type": "Copy",
            "name": "复制刻印",
            "description": "获得一张此卡牌的复制",
            "rarity": "rare"
        },
        {
            "id": "E4",
            "type": "TeamRage",
            "name": "团队怒气刻印",
            "description": "其他队友各获得 +15 怒气值",
            "primaryValue": 15,
            "rarity": "rare"
        },
        {
            "id": "E5",
            "type": "SelfRage",
            "name": "自身怒气刻印",
            "description": "出牌角色获得 +30 怒气值",
            "primaryValue": 30,
            "rarity": "common"
        },
        {
            "id": "E6",
            "type": "Vulnerable",
            "name": "易伤刻印",
            "description": "所有敌人受到伤害 +50%，持续1回合",
            "primaryValue": 50,
            "secondaryValue": 1,
            "rarity": "epic"
        }
    ],
    "selectionCount": 3,
    "probabilityToShow": 50
}
```

### 14.12 与现有系统集成

#### 与战斗奖励系统集成

```csharp
// BattleManager 中的战斗结束处理
private void ShowBattleRewards()
{
    RandomNumberGenerator rng = new RandomNumberGenerator();
    rng.Randomize();
    int roll = rng.RandiRange(1, 100);
    
    if (roll <= 50)
    {
        ShowEngravingSelection();
    }
    else
    {
        ShowArtifactSelection();
    }
}
```

#### 与卡牌系统集成

```csharp
// Card.cs 修改
public partial class Card
{
    public bool IsEngraved { get; set; }
    public EngravingEffect Engraving { get; set; }
}
```

#### 与怒气系统集成

```csharp
// EngravingManager 中的怒气处理
private void ApplyRageEngraving(Card card, CharacterPosition character)
{
    if (card.Engraving.Type == EngravingType.SelfRage)
    {
        character.AddRage(card.Engraving.PrimaryValue);
    }
    else if (card.Engraving.Type == EngravingType.TeamRage)
    {
        foreach (var pos in characterPositions)
        {
            if (pos != character)
            {
                pos.AddRage(card.Engraving.PrimaryValue);
            }
        }
    }
}
```

### 14.13 实施要点

1. **触发时机**：在`BattleManager`的`CheckGameOver`后，奖励界面显示前
2. **卡牌筛选**：从Attacker的20张卡组中筛选未刻印的卡牌
3. **随机分配**：确保3张候选卡的刻印效果不重复
4. **效果应用**：在`OnCardPlayed`中检查并执行刻印效果
5. **状态持久化**：刻印状态保存到卡牌数据中

### 14.14 验收标准

1. 战斗胜利后50%概率显示刻印选择界面
2. 刻印选择界面显示3张未刻印的候选卡牌
3. 每张候选卡牌显示刻印效果预览
4. 3张候选卡的刻印效果互不重复
5. 点击卡牌后刻印永久生效
6. 刻印效果在出牌时正确触发
7. 刻印状态正确持久化
8. 刻印效果与其他系统正确交互

***

## 15. 后续扩展

- 角色羁绊系统（特定组合有加成）
- 怒气溢出奖励（怒气超过100有额外效果）
- 角色外观皮肤
- 技能升级系统（金币消耗）

