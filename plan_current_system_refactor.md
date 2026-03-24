# 现有系统重构计划

## 1. 当前系统分析

### 1.1 系统架构概览

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         当前系统架构                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  技能容器层                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │   Card   │ │Ultimate  │ │Artifact  │ │ KeyOrder │ │  敌人AI   │    │
│  │  .cs     │ │ Skill    │ │  .cs     │ │  .cs     │ │  行为     │    │
│  └───┬──────┘ └───┬──────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘    │
│      │            │            │           │           │            │
│      │            │            │           │           │            │
│      ▼            ▼            ▼           ▼           ▼            │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │              CardEffectResolver.cs                           │     │
│  │              (卡牌效果解析 - 硬编码)                          │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                              │                                        │
│                              ▼                                        │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │              StatusEffect.cs (Buff/Debuff基类)               │     │
│  │              (虚方法: OnApply, OnTurnStart, OnTurnEnd...)     │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                              │                                        │
│                              ▼                                        │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │              Player.cs / Enemy.cs                             │     │
│  │              (单位 - 管理StatusEffect列表)                    │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 1.2 核心文件清单

| 文件路径 | 行数 | 职责 | 问题 |
|----------|------|------|------|
| `Scripts/Card.cs` | 145 | 卡牌数据定义 | 效果属性耦合 |
| `Scripts/Status/StatusEffect.cs` | 93 | Buff/Debuff基类 | 虚方法冗余 |
| `Scripts/Status/Buff.cs` | 121 | 具体Buff实现 | 硬编码数值 |
| `Scripts/Status/Debuff.cs` | 128 | 具体Debuff实现 | 硬编码数值 |
| `Scripts/Battle/CardEffectResolver.cs` | 174 | 卡牌效果解析 | 逻辑分散 |
| `Scripts/Battle/ArtifactSystem/Artifact.cs` | 255 | 造物系统 | 无动态效果 |
| `Scripts/Battle/SilverKeySystem/KeyOrder.cs` | 105 | 银钥技能 | 硬编码 |
| `Scripts/Battle/CharacterSystem/UltimateSkill.cs` | 223 | 终极技能 | 部分硬编码 |
| `Scripts/Player.cs` | 218 | 玩家单位 | 无IUnit抽象 |
| `Scripts/Enemy.cs` | 193 | 敌人单位 | 无IUnit抽象 |
| `Scripts/Battle/TurnManager.cs` | 145 | 回合管理 | 分散处理 |

---

## 2. 发现的问题

### 问题汇总表

| # | 问题类别 | 严重程度 | 位置 | 描述 |
|---|----------|----------|------|------|
| P1 | 效果属性耦合 | 高 | Card.cs | Damage, Shield等直接作为Card属性 |
| P2 | 硬编码数值 | 高 | Buff.cs/Debuff.cs | 数值在构造函数中写死 |
| P3 | 虚方法冗余 | 中 | StatusEffect.cs | Player/Enemy各一套虚方法 |
| P4 | 目标选择分散 | 中 | CardEffectResolver | 目标选择与其他逻辑混合 |
| P5 | 无统一事件系统 | 中 | StatusEffect | 各Effect自己判断触发时机 |
| P6 | 造物无动态效果 | 低 | Artifact.cs | 只有静态属性加成 |
| P7 | 无IUnit抽象 | 中 | Player/Enemy | 直接操作具体类型 |
| P8 | 触发时机重复 | 中 | StatusEffect | EffectTrigger与虚方法重复 |

---

## 3. 详细问题分析

### P1: Card.cs 效果属性耦合

**当前代码**:
```csharp
// Scripts/Card.cs (行 43-70)
[Export]
public int Damage { get; set; } = 0;
[Export]
public int ShieldGain { get; set; } = 0;
[Export]
public int EnergyGain { get; set; } = 0;
[Export]
public int HealAmount { get; set; } = 0;
[Export]
public int DrawCount { get; set; } = 0;
[Export]
public string ApplyBuffName { get; set; } = "";
[Export]
public string ApplyDebuffName { get; set; } = "";
// ... 效果委托
public System.Action<Player> Effect { get; set; }
```

**问题**:
- 每种效果类型都需要一个属性
- 新增效果类型需要修改Card类
- 效果逻辑与Card类耦合

**建议方案**:
```csharp
// 新设计
[Export]
public List<Effect> Effects { get; set; } = new List<Effect>();
```

---

### P2: Buff/Debuff 硬编码数值

**当前代码**:
```csharp
// Scripts/Status/Buff.cs (行 3-16)
public partial class StrengthBuff : StatusEffect
{
    [Export]
    public int AttackBonus { get; set; } = 3;

    public StrengthBuff()
    {
        EffectName = "Strength";
        Description = "攻撃力が+%d 増加";
        Duration = 3;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnDamageDealt;
    }

    public override void OnApplyEnemy(Enemy target)
    {
        target.Attack += AttackBonus;
    }
}
```

**问题**:
- 数值写死在代码中
- 无法热更新
- 设计与配置混杂

**建议方案**:
- 数值从JSON加载
- 使用 `EffectApplier` 统一处理

---

### P3: StatusEffect 虚方法冗余

**当前代码**:
```csharp
// Scripts/Status/StatusEffect.cs (行 43-77)
public abstract partial class StatusEffect : Resource
{
    // Player版本
    public virtual void OnApplyPlayer(Player target) { }
    public virtual void OnRemovePlayer(Player target) { }
    public virtual void OnTurnStartPlayer(Player target) { }
    public virtual void OnTurnEndPlayer(Player target) { }
    public virtual int ModifyDamagePlayer(...) { return baseDamage; }

    // Enemy版本
    public virtual void OnApplyEnemy(Enemy target) { }
    public virtual void OnRemoveEnemy(Enemy target) { }
    public virtual void OnTurnStartEnemy(Enemy target) { }
    public virtual void OnTurnEndEnemy(Enemy target) { }
    public virtual int ModifyDamageEnemy(...) { return baseDamage; }
}
```

**问题**:
- Player和Enemy各一套方法
- 代码重复
- 维护困难

**建议方案**:
- 引入 `IUnit` 接口统一
- 使用 `Effect` 代替虚方法

---

### P4: 目标选择分散

**当前代码**:
```csharp
// Scripts/Battle/CardEffectResolver.cs (行 93-137)
private static Enemy GetFrontmostEnemy(List<Enemy> enemies)
{
    Enemy frontmost = null;
    int minPosition = int.MaxValue;
    foreach (Enemy enemy in enemies)
    {
        if (enemy.Position < minPosition)
        {
            minPosition = enemy.Position;
            frontmost = enemy;
        }
    }
    return frontmost ?? enemies[0];
}

private static Enemy GetRearmostEnemy(List<Enemy> enemies) { ... }
private static Enemy GetEnemyAtPosition(int position, List<Enemy> enemies) { ... }
```

**问题**:
- 目标选择逻辑与其他逻辑混合
- 无法复用
- 难以扩展

**建议方案**:
- 独立的 `TargetSelector` 组件
- 支持多种选择策略

---

### P5: 无统一事件系统

**当前代码**:
```csharp
// Scripts/Status/Buff.cs (行 68-75)
public partial class RegenerationBuff : StatusEffect
{
    public RegenerationBuff()
    {
        Trigger = EffectTrigger.OnTurnEnd;
    }

    public override void OnTurnEndEnemy(Enemy target)
    {
        base.OnTurnEndEnemy(target);
        target.Heal(HealPerTurn);
    }
}
```

**问题**:
- 每个Buff自己判断何时触发
- 触发逻辑分散
- 无法统一管理

**建议方案**:
- `EventTriggerSystem` 集中管理
- 效果注册到事件系统

---

### P6: 造物无动态效果

**当前代码**:
```csharp
// Scripts/Battle/ArtifactSystem/Artifact.cs (行 40-64)
[Export]
public int HealthBonus = 0;
[Export]
public int AttackBonus = 0;
[Export]
public int DefenseBonus = 0;
[Export]
public string TriggerCondition;
```

**问题**:
- 只有静态属性加成
- 无法实现"受伤时反击"等效果
- 效果类型受限

---

### P7: 无IUnit抽象

**当前代码**:
```csharp
// Scripts/Player.cs - 直接操作Player
public void TakeDamage(int damage) { ... }
public void Heal(int amount) { ... }
public void AddShield(int amount) { ... }

// Scripts/Enemy.cs - 直接操作Enemy
public int TakeDamage(int damage) { ... }
public void Heal(int amount) { ... }
```

**问题**:
- StatusEffect需要为Player和Enemy各写一套方法
- 无法统一处理

**建议方案**:
```csharp
public interface IUnit
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; }
    int Attack { get; set; }
    int Defense { get; set; }
    FactionType Faction { get; }
    bool IsDead { get; }
    void TakeDamage(int damage);
    void Heal(int amount);
    void AddShield(int amount);
}
```

---

## 4. 架构调整方案

### 4.1 目标架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         调整后架构                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  技能容器层                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐                   │
│  │   Card   │ │Ultimate  │ │Artifact  │ │ KeyOrder │                   │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘                   │
│       │            │            │           │                          │
│       └────────────┴─────┬──────┴───────────┘                          │
│                          │                                             │
│                          ▼                                             │
│              ┌─────────────────────────┐                               │
│              │   List<Effect>           │  ← 统一效果列表               │
│              └────────────┬────────────┘                               │
│                           │                                             │
│                           ▼                                             │
│              ┌─────────────────────────┐                               │
│              │   EffectApplier         │  ← 统一处理Owner/Target        │
│              └────────────┬────────────┘                               │
│                           │                                             │
│                           ▼                                             │
│              ┌─────────────────────────┐                               │
│              │   EventTriggerSystem   │  ← 事件驱动                    │
│              └────────────┬────────────┘                               │
│                           │                                             │
│                           ▼                                             │
│              ┌─────────────────────────┐                               │
│              │   TargetSelector        │  ← 统一目标选择                │
│              └─────────────────────────┘                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.2 需要新增的组件

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| IUnit | `Scripts/Battle/Interfaces/IUnit.cs` | 统一单位接口 |
| Effect | `Scripts/Battle/Effects/Effect.cs` | 效果基类 |
| DamageEffect | `Scripts/Battle/Effects/DamageEffect.cs` | 伤害效果 |
| HealEffect | `Scripts/Battle/Effects/HealEffect.cs` | 治疗效果 |
| ShieldEffect | `Scripts/Battle/Effects/ShieldEffect.cs` | 护盾效果 |
| TargetSelector | `Scripts/Battle/Effects/TargetSelector.cs` | 目标选择器 |
| EventTriggerSystem | `Scripts/Battle/Effects/EventTriggerSystem.cs` | 事件触发系统 |
| EffectApplier | `Scripts/Battle/Effects/EffectApplier.cs` | 效果应用器 |

### 4.3 需要修改的组件

| 组件 | 修改内容 | 优先级 |
|------|----------|--------|
| Card.cs | 移除硬编码属性，改为`List<Effect>` | 高 |
| StatusEffect.cs | 改为使用`List<Effect>` | 高 |
| Buff.cs | 数值从JSON加载 | 高 |
| Debuff.cs | 数值从JSON加载 | 高 |
| Player.cs | 实现IUnit接口 | 中 |
| Enemy.cs | 实现IUnit接口 | 中 |
| Artifact.cs | 支持动态效果 | 中 |
| KeyOrder.cs | 支持效果列表 | 中 |
| UltimateSkill.cs | 添加效果列表支持 | 中 |
| TurnManager.cs | 接入事件系统 | 中 |

---

## 5. 实施计划

### 第一阶段：基础设施 (预计影响: Card.cs, StatusEffect.cs)

1. 创建 `IUnit` 接口
2. 修改 Player/Enemy 实现 IUnit
3. 创建 Effect 基类
4. 创建 EventTriggerSystem

### 第二阶段：效果系统 (预计影响: Buff.cs, Debuff.cs)

1. 实现 DamageEffect, HealEffect, ShieldEffect
2. 实现 TargetSelector
3. 实现 EffectApplier
4. 改造 Buff/Debuff 使用新系统

### 第三阶段：容器改造 (预计影响: Card.cs, KeyOrder.cs, UltimateSkill.cs)

1. 改造 Card 使用效果列表
2. 改造 KeyOrder 支持效果
3. 改造 UltimateSkill 支持效果

### 第四阶段：造物增强 (预计影响: Artifact.cs)

1. 改造造物支持动态效果
2. 实现条件触发效果

---

## 6. 讨论要点

### 待讨论问题

1. **向后兼容性**: 是否需要保留旧的 `Effect` 委托？

2. **JSON配置格式**: 使用独立JSON还是继续用Resource？

3. **Effect继承 vs 组合**: Effect是继承实现还是组合实现？

4. **事件系统粒度**: 是否需要细分事件类型？

5. **Buff/Debuff分离**: 是否合并为一个StatusEffect？

---

## 7. 相关文档

- [plan_skill_effect_system_optimization.md](./plan_skill_effect_system_optimization.md) - 技能效果系统详细设计
