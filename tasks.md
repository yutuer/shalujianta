# 角色系统改进 - 任务清单

## Phase 1: 核心框架

### 任务 1.1: 创建 CharacterDefinition 类
- [ ] 创建 `Scripts/Battle/CharacterSystem/CharacterDefinition.cs`
- [ ] 定义属性：`CharacterId`, `Name`, `NameEn`
- [ ] 定义基础属性：`BaseHealth`, `BaseEnergy`, `BaseDrawCount`, `BaseAttack`, `BaseDefense`
- [ ] 定义卡牌引用：`AttackCardId`, `DefenseCardId`, `SpecialCardIds`
- [ ] 定义大招引用：`UltimateSkillId`

### 任务 1.2: 创建 Attacker 类
- [ ] 创建 `Scripts/Battle/CharacterSystem/Attacker.cs`
- [ ] 实现 `Characters` 数组（4个角色）
- [ ] 实现 `TotalHealth` 总生命计算
- [ ] 实现 `TotalEnergy` 总能量计算
- [ ] 实现 `TotalAttack` 总攻击计算
- [ ] 实现 `TotalDefense` 总防御计算
- [ ] 实现 `TotalDrawCount` 总抽牌计算
- [ ] 实现 `GetTeamDeck()` 收集20张卡牌
- [ ] 实现 `GetCharacterByCard()` 根据卡牌找角色

### 任务 1.3: 创建 CharacterPosition 类（含怒气）
- [ ] 创建 `Scripts/Battle/CharacterSystem/CharacterPosition.cs`
- [ ] 实现 `PositionIndex` 和 `Character` 属性
- [ ] 实现 `CurrentRage` 怒气属性（上限100）
- [ ] 实现 `AddRage()` 添加怒气
- [ ] 实现 `CanUseUltimate()` 判断是否可释放大招
- [ ] 实现 `UseUltimate()` 释放大招

### 任务 1.4: 创建 RageManager 类
- [ ] 创建 `Scripts/Battle/RageManager.cs`
- [ ] 实现 `OnAttackCardPlayed()` 出攻击卡+15怒气
- [ ] 实现 `OnDefenseCardPlayed()` 出防御卡+10怒气
- [ ] 实现 `OnCharacterDamaged()` 受攻击+5怒气
- [ ] 实现 `OnTurnEnd()` 回合结束+5怒气
- [ ] 实现怒气跨战斗继承（不清零）
- [ ] 实现怒气达到100时角色特效触发

### 任务 1.5: 创建 UltimateSkill 类
- [ ] 创建 `Scripts/Battle/CharacterSystem/UltimateSkill.cs`
- [ ] 定义 `SkillId`, `Name`, `Description`
- [ ] 定义 `RageCost` 怒气消耗（100）
- [ ] 定义 `Execute` 技能执行委托

---

## Phase 2: UI与集成

### 任务 2.1: 创建角色选择界面
- [ ] 创建 `Scenes/UI/CharacterSelection.tscn`
- [ ] 显示12个角色选项
- [ ] 实现4角色选择逻辑
- [ ] 显示攻击者总属性预览
- [ ] 实现开始战斗按钮

### 任务 2.2: 创建角色战斗位置UI
- [ ] 创建怒气显示（格式：怒气: X/100，显示在角色位置正上方）
- [ ] 实现待机动画
- [ ] 实现攻击动画
- [ ] 实现受伤动画
- [ ] 实现技能动画
- [ ] 实现大招释放动画
- [ ] 实现怒气满100时角色发光特效

### 任务 2.3: 修改 CardUI 支持怒气显示
- [ ] 在攻击卡和防御卡上显示怒气获取值（如：造成X伤害，获得Y怒气）
- [ ] 点击怒气满的角色触发大招释放

### 任务 2.4: 创建地图系统
- [ ] 创建 `Scripts/Battle/MapSystem/MapDefinition.cs`
- [ ] 创建 `Scripts/Battle/MapSystem/MapManager.cs`
- [ ] 创建 `Scripts/Battle/MapSystem/RewardSystem.cs`
- [ ] 创建 `Resources/MapData/maps.json` 地图配置
- [ ] 实现地图选择界面

### 任务 2.5: 修改 BattleManager
- [ ] 支持传入攻击者数据
- [ ] 创建4个角色位置UI
- [ ] 实现出牌时角色动画触发
- [ ] 实现怒气增加逻辑
- [ ] 实现大招释放逻辑
- [ ] 更新攻击者属性显示
- [ ] 支持多场战斗流程
- [ ] 支持战斗胜利后进入下一场或结算

### 任务 2.6: 创建 Buff/Debuff 显示系统
- [ ] 创建 `Scripts/Battle/BuffDebuffDisplay.cs`
- [ ] 实现 Buff 图标显示（蓝色/金色）
- [ ] 实现 Debuff 图标显示（红色）
- [ ] 实现效果值显示在图标下方
- [ ] 实现 Buff/Debuff 叠加层数显示
- [ ] 集成到角色位置UI的血条下方

---

## Phase 3: 地图系统

### 任务 3.1: 创建地图系统
- [ ] 创建 `Scripts/Battle/MapSystem/MapDefinition.cs`
- [ ] 创建 `Scripts/Battle/MapSystem/MapManager.cs`
- [ ] 创建 `Scripts/Battle/MapSystem/RewardSystem.cs`
- [ ] 创建 `Resources/MapData/maps.json` 地图配置

### 任务 3.2: 实现地图选择界面
- [ ] 创建 `Scenes/UI/MapSelection.tscn`
- [ ] 显示可选地图列表
- [ ] 显示地图难度和奖励预览
- [ ] 实现地图选择逻辑

### 任务 3.3: 实现多场战斗流程
- [ ] 实现战斗胜利后进入下一场
- [ ] 实现地图胜利结算
- [ ] 实现怒气跨战斗继承
- [ ] 实现战斗胜利后造物选择面板

### 任务 3.4: 实现奖励系统
- [ ] 实现战斗胜利后造物随机3选1
- [ ] 实现地图胜利后金币奖励
- [ ] 实现玩家金币累加
- [ ] 实现玩家金币存储

---

## Phase 4: 造物系统

### 任务 4.1: 创建造物类
- [ ] 创建 `Scripts/Battle/ArtifactSystem/Artifact.cs`
- [ ] 定义造物属性（ID、名称、稀有度、效果）
- [ ] 定义作用目标（攻击者/角色/条件）
- [ ] 定义正面和负面效果

### 任务 4.2: 创建造物管理器
- [ ] 创建 `Scripts/Battle/ArtifactSystem/ArtifactManager.cs`
- [ ] 实现造物装备逻辑
- [ ] 实现造物效果应用
- [ ] 实现条件造物触发检测

### 任务 4.3: 创建造物配置数据
- [ ] 创建 `Resources/ArtifactData/artifacts.json`
- [ ] 定义攻击者造物（至少3个）
- [ ] 定义角色造物（每个角色至少1个）
- [ ] 定义条件造物（至少3个）

### 任务 4.4: 实现造物选择界面
- [ ] 创建 `Scenes/UI/ArtifactSelection.tscn`
- [ ] 显示已获得造物列表
- [ ] 实现造物装备/卸下逻辑
- [ ] 显示造物效果预览

### 任务 4.5: 造物效果集成
- [ ] 造物效果应用到属性计算
- [ ] 造物效果显示在战斗界面
- [ ] 条件造物触发逻辑

---

## Phase 5: 数据层

### 任务 5.1: 创建 characters.json
- [ ] 创建 `Resources/CharacterData/characters.json`
- [ ] 定义12角色基础属性
- [ ] 定义60张卡牌数据（12攻击+12防御+36特殊）
- [ ] 定义12个角色大招数据

### 任务 5.2: 实现卡牌数据加载
- [ ] 更新 `CardConfigLoader` 支持角色数据
- [ ] 更新 `CardConfigLoader` 支持大招数据
- [ ] 实现 `GetCharacterCards()` 获取角色卡牌
- [ ] 实现 `CreateAttackerDeck()` 创建20张攻击者卡组
- [ ] 实现 `GetUltimateSkill()` 获取角色大招

---

## Phase 6: 测试验证

### 任务 6.1: 编译测试
- [ ] `dotnet build` 成功
- [ ] 无新增警告

### 任务 6.2: 功能测试
- [ ] 测试12角色加载
- [ ] 测试4角色组成攻击者
- [ ] 测试攻击者总属性计算
- [ ] 测试20张卡牌加载
- [ ] 测试怒气增加（出牌/受击/回合结束）
- [ ] 测试怒气满时点击释放大招
- [ ] 测试出牌动画
- [ ] 测试伤害机制
- [ ] 测试战斗流程
- [ ] 测试地图系统（多场战斗）
- [ ] 测试造物系统（正面/负面效果）

---

## Phase 7: 银钥系统

### 任务 7.1: 创建银钥配置类
- [ ] 创建 `Scripts/Battle/SilverKeySystem/SilverKeyConfig.cs`
- [ ] 定义 `BaseMaxSilverKey` = 1000
- [ ] 定义 `MaxStack` = 2
- [ ] 定义 `SilverPerEnergy` = 50f（每消耗1点能量获得50银钥）

### 任务 7.2: 创建钥令数据类
- [ ] 创建 `Scripts/Battle/SilverKeySystem/KeyOrder.cs`
- [ ] 定义 `KeyOrderId`, `Name`, `Description`
- [ ] 定义 `SilverKeyCost`（默认1000）
- [ ] 定义 `EffectType` 枚举（Damage/Heal/Buff/Debuff/Special）
- [ ] 定义 `ExecuteEffect` 委托

### 任务 7.3: 创建钥令管理器
- [ ] 创建 `Scripts/Battle/SilverKeySystem/KeyOrderManager.cs`
- [ ] 维护钥令库和随机抽取逻辑
- [ ] 实现 `GetRandomKeyOrders(int count)` 随机获取钥令
- [ ] 实现钥令效果应用

### 任务 7.4: 修改Attacker集成银钥系统
- [ ] 添加 `CurrentSilverKey` 属性
- [ ] 添加 `EquippedKeyOrder` 槽位
- [ ] 实现 `AddSilverKey(int energyCost)` 银钥增加逻辑
- [ ] 实现 `CanUseKeyOrder()` 判断是否可释放
- [ ] 实现 `UseKeyOrder()` 释放钥令

### 任务 7.5: 修改BattleManager集成银钥UI
- [ ] 添加银钥值显示（格式：🔑 X/1000）
- [ ] 实现银钥增加逻辑（每消耗能量时）
- [ ] 实现钥令释放按钮
- [ ] 实现第二次释放随机钥令面板
- [ ] 实现每回合2次限制逻辑

### 任务 7.6: 创建银钥进度指示器
- [ ] 创建 `Scripts/UI/SilverKeyProgressIndicator.cs`
- [ ] 实现圆形进度指示器（直径80px）
- [ ] 实现基础进度显示（0-1000，银色）
- [ ] 实现超额进度显示（1000-2000，金色光晕）
- [ ] 实现禁用状态（灰色，暗淡效果）
- [ ] 实现进度动画（平滑过渡）
- [ ] 实现脉冲光晕动画（2000满值时）
- [ ] 实现响应式设计（不同分辨率适配）

### 任务 7.7: 银钥系统持久化
- [ ] 银钥值跨战斗继承（存储在MapManager）
- [ ] 进入新地图时银钥值重置为0

---

## Phase 8: 刻印系统

### 任务 8.1: 创建刻印效果数据类
- [ ] 创建 `Scripts/Battle/EngravingSystem/EngravingType.cs`
- [ ] 定义 `EngravingType` 枚举（Shield/Poison/Copy/TeamRage/SelfRage/Vulnerable）
- [ ] 创建6种刻印效果数据

### 任务 8.2: 创建刻印管理器
- [ ] 创建 `Scripts/Battle/EngravingSystem/EngravingManager.cs`
- [ ] 实现 `GetRandomEngravings(int count)` 获取随机刻印
- [ ] 实现 `GetUnengravedCards()` 获取未刻印卡牌
- [ ] 实现 `ApplyEngravingEffect()` 应用刻印效果

### 任务 8.3: 修改Card类添加刻印状态
- [ ] 修改 `Scripts/Card.cs`
- [ ] 添加 `IsEngraved` 属性
- [ ] 添加 `EngravingEffect` 属性
- [ ] 添加刻印效果触发逻辑

### 任务 8.4: 创建刻印效果实现
- [ ] 实现护盾刻印（E1）- +5护盾
- [ ] 实现毒伤刻印（E2）- 3回合，每回合3伤害
- [ ] 实现复制刻印（E3）- 获得1张复制
- [ ] 实现团队怒气刻印（E4）- +15怒气/角色
- [ ] 实现自身怒气刻印（E5）- +30怒气
- [ ] 实现易伤刻印（E6）- +50%受伤，1回合

### 任务 8.5: 修改战斗奖励逻辑
- [ ] 修改 `BattleManager` 的战斗结束处理
- [ ] 实现50%随机判定逻辑
- [ ] 实现刻印选择界面显示

### 任务 8.6: 创建刻印选择界面
- [ ] 创建 `Scenes/UI/EngravingSelection.tscn`
- [ ] 显示3张候选卡牌
- [ ] 显示刻印效果预览
- [ ] 实现刻印确认逻辑

### 任务 8.7: 创建刻印配置文件
- [ ] 创建 `Resources/EngravingData/engravings.json`
- [ ] 定义6种刻印效果参数
- [ ] 配置选择概率（50%）

---

## 任务统计

| Phase | 任务数 | 完成数 | 状态 |
|-------|--------|--------|------|
| Phase 1 | 5 | 0 | 待开始 |
| Phase 2 | 6 | 0 | 待开始 |
| Phase 3 | 4 | 0 | 待开始 |
| Phase 4 | 5 | 0 | 待开始 |
| Phase 5 | 2 | 0 | 待开始 |
| Phase 6 | 2 | 0 | 待开始 |
| Phase 7 | 7 | 0 | 待开始 |
| Phase 8 | 7 | 0 | 待开始 |
| **总计** | **38** | **0** | - |

---

## 文件清单

### 新增文件
```
Scripts/Battle/CharacterSystem/
├── CharacterDefinition.cs
├── Attacker.cs
├── CharacterPosition.cs
└── UltimateSkill.cs

Scripts/Battle/
├── RageManager.cs
├── BuffDebuffDisplay.cs
├── SilverKeySystem/
│   ├── SilverKeyConfig.cs
│   ├── KeyOrder.cs
│   └── KeyOrderManager.cs
├── EngravingSystem/
│   ├── EngravingType.cs
│   └── EngravingManager.cs
├── ArtifactSystem/
│   ├── Artifact.cs
│   └── ArtifactManager.cs
└── ..


Scripts/UI/
├── SilverKeyProgressIndicator.cs

Scripts/Battle/MapSystem/
├── MapDefinition.cs
├── MapManager.cs
└── RewardSystem.cs

Scenes/UI/
├── CharacterSelection.tscn
├── MapSelection.tscn
├── ArtifactSelection.tscn
└── EngravingSelection.tscn

Resources/
├── CharacterData/
│   └── characters.json
├── MapData/
│   └── maps.json
└── ArtifactData/
    └── artifacts.json
└── EngravingData/
    └── engravings.json
```

### 修改文件
```
Scripts/Player.cs
Scripts/Card.cs
Scripts/CardConfigLoader.cs
Scripts/BattleManager.cs
```

---

## 卡牌统计

| 类型 | 数量 | 每角色 |
|------|------|--------|
| 攻击卡 | 12张 | 1张 |
| 防御卡 | 12张 | 1张 |
| 特殊卡 | 36张 | 3张 |
| **总计** | **60张** | **5张/角色** |

## 大招统计

| 类型 | 数量 | 每角色 |
|------|------|--------|
| 终极技能 | 12个 | 1个 |
