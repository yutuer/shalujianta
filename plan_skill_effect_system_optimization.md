# 系统技能作用逻辑优化方案

## 1. 背景与目标

### 1.1 当前系统问题分析

通过代码分析，发现当前系统存在以下主要问题：

#### 1.1.1 效果与容器高度耦合

**Card.cs** 中包含了大量效果相关属性：
- `Damage`, `ShieldGain`, `EnergyGain`, `HealAmount`, `DrawCount`
- `ApplyBuffName`, `ApplyBuffDuration`, `ApplyDebuffName`, `ApplyDebuffDuration`
- `Effect` 委托（Lambda表达式）

这些效果属性散落在卡牌对象中，没有统一的抽象。

#### 1.1.2 效果触发机制不统一

不同类型的技能使用不同的触发方式：

| 技能类型 | 触发机制 | 文件位置 |
|---------|---------|---------|
| 卡牌效果 | `Card.Effect` 委托 | Card.cs |
| Buff/Debuff | `EffectTrigger` 枚举 + 虚方法覆盖 | StatusEffect.cs |
| 大招技能 | `OnExecute` 委托 | UltimateSkill.cs |
| 造物效果 | 硬编码属性 + `TriggerCondition` 字符串 | Artifact.cs |
| 银钥技能 | `OnExecute` 委托 | KeyOrder.cs |

#### 1.1.3 缺乏可扩展性

当前系统的扩展方式需要修改源代码：
- 添加新效果类型需要修改 `CardEffectResolver.cs`
- 添加新 Buff 需要创建新类继承 `StatusEffect`
- 效果数值调整需要重新编译代码

#### 1.1.4 配置与逻辑混杂

虽然卡牌配置已经迁移到JSON，但：
- Buff/Debuff 效果逻辑仍在代码中硬编码
- 造物效果完全硬编码
- 银钥技能效果完全硬编码

---

## 2. 优化目标

### 2.1 核心目标

1. **解耦效果容器与具体效果** - 技能容器只负责存储效果列表，不包含效果逻辑
2. **统一效果触发机制** - 所有效果使用相同的触发接口
3. **配置驱动** - 效果数值和类型从JSON读取
4. **可扩展性** - 添加新效果类型无需修改核心代码

### 2.2 架构设计原则

```
┌─────────────────────────────────────────────────────────┐
│                    技能容器层 (Skill Container)         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │   Card   │ │Ultimate  │ │Artifact  │ │KeyOrder  │   │
│  │          │ │  Skill   │ │          │ │          │   │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘   │
└───────┼────────────┼────────────┼────────────┼──────────┘
        │            │            │            │
        └────────────┴─────┬──────┴────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                   效果列表 (Effect List)                │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │  Damage  │ │  Shield  │ │   Heal   │ │  Draw    │   │
│  │  Effect  │ │  Effect  │ │  Effect  │ │  Effect  │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │   Buff   │ │  Debuff  │ │  Energy  │ │  Custom  │   │
│  │  Effect  │ │  Effect  │ │  Effect  │ │  Effect  │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘   │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                   效果解析器 (Effect Resolver)           │
│  ┌──────────────────────────────────────────────────┐  │
│  │  EffectResolver                                  │  │
│  │  - ResolveEffect(Effect, Context)               │  │
│  │  - CheckTrigger(TriggerType, Context)           │  │
│  │  - ApplyEffect(Effect, Target)                  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                   效果触发器 (Trigger System)            │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐        │
│  │CardPlay    │ │TurnStart   │ │TurnEnd     │        │
│  │Trigger     │ │Trigger     │ │Trigger     │        │
│  └────────────┘ └────────────┘ └────────────┘        │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐        │
│  │Damage      │ │Heal        │ │Condition   │        │
│  │Received    │ │Trigger     │ │Trigger     │        │
│  └────────────┘ └────────────┘ └────────────┘        │
└─────────────────────────────────────────────────────────┘
```

---

## 3. 核心组件设计

### 3.1 灵活目标选择系统设计

#### 3.1.1 问题分析

当前设计中，效果类型与作用对象存在硬绑定：
- `Damage` 只能作用于敌方
- `Shield`/`Heal` 只能作用于己方
- `Buff` 只能施加给敌方
- `Debuff` 只能施加给己方

这限制了游戏设计的灵活性，例如：
- 己方伤害效果（如"自残"、"队友伤害"）
- 敌方Buff效果（如"强化敌人"技能）
- 跨阵营的特殊效果

#### 3.1.2 新目标选择体系

**目标选择器 (Target Selector)** - 独立于效果类型的选择机制

```
┌─────────────────────────────────────────────────────────────┐
│                    目标选择器系统                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   TargetSelector (选择器基类)                               │
│       │                                                     │
│       ├── TargetSelector_Unit      (单位选择器)             │
│       │       ├── Self              (自身)                 │
│       │       ├── Single            (单体)                  │
│       │       │       ├── Front      (最前)                │
│       │       │       ├── Rear       (最后)                │
│       │       │       ├── Random     (随机)                │
│       │       │       ├── LowestHP   (最低生命)            │
│       │       │       ├── HighestHP  (最高生命)            │
│       │       │       ├── Weakest    (最弱)                │
│       │       │       └── Strongest  (最强)                │
│       │       └── All              (全体)                  │
│       │                                                     │
│       ├── TargetSelector_Player    (玩家选择器)             │
│       │       └── SelfPlayer       (攻击者自身)             │
│       │                                                     │
│       └── TargetSelector_Conditional (条件选择器)            │
│               └── ConditionBased   (基于条件)               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**阵营系统 (Faction System)**

```csharp
public enum TargetFaction
{
    Any,        // 任意阵营
    Enemy,      // 敌方
    Friendly,   // 己方
    Neutral,    // 中立
    Self        // 自身
}
```

**目标选择上下文 (TargetContext)**

```csharp
public class TargetContext
{
    public Player Player { get; set; }           // 攻击者（玩家）
    public Enemy[] Enemies { get; set; }          // 敌方单位数组
    public Card PlayedCard { get; set; }           // 当前使用的卡牌
    public Effect SourceEffect { get; set; }       // 源效果
    public Unit CurrentUnit { get; set; }          // 当前处理的单位
    public int SourcePosition { get; set; }        // 源位置
}
```

**单位抽象 (Unit Interface)**

```csharp
public interface IUnit
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; }
    int Attack { get; }
    int Defense { get; }
    bool IsDead { get; }
    int Position { get; }
    TargetFaction GetFaction();
    void TakeDamage(int damage);
    void Heal(int amount);
    void AddShield(int amount);
}
```

#### 3.1.3 目标选择器实现

**目标选择器基类**

```csharp
public abstract partial class TargetSelector : Resource
{
    [Export]
    public string SelectorId { get; set; } = "";

    [Export]
    public TargetFaction TargetFaction { get; set; } = TargetFaction.Any;

    [Export]
    public bool Required { get; set; } = true;

    public abstract List<IUnit> SelectTargets(TargetContext context);

    protected List<IUnit> FilterByFaction(List<IUnit> units, TargetFaction faction)
    {
        if (faction == TargetFaction.Any)
            return units;

        return units.Where(u => u.GetFaction() == faction).ToList();
    }
}
```

**单体选择器**

```csharp
public partial class SingleTargetSelector : TargetSelector
{
    [Export]
    public SingleTargetType TargetType { get; set; } = SingleTargetType.Front;

    public override List<IUnit> SelectTargets(TargetContext context)
    {
        List<IUnit> candidates = GetAllCandidateUnits(context);
        candidates = FilterByFaction(candidates, TargetFaction);

        if (candidates.Count == 0)
            return Required ? new List<IUnit>() : null;

        IUnit selected = TargetType switch
        {
            SingleTargetType.Front => candidates.OrderBy(u => u.Position).First(),
            SingleTargetType.Rear => candidates.OrderByDescending(u => u.Position).First(),
            SingleTargetType.Random => candidates[GD.Randi() % candidates.Count],
            SingleTargetType.LowestHP => candidates.OrderBy(u => u.CurrentHealth).First(),
            SingleTargetType.HighestHP => candidates.OrderByDescending(u => u.CurrentHealth).First(),
            SingleTargetType.Self => context.CurrentUnit,
            _ => candidates[0]
        };

        return new List<IUnit> { selected };
    }

    protected virtual List<IUnit> GetAllCandidateUnits(TargetContext context)
    {
        List<IUnit> units = new List<IUnit>();
        units.Add(context.Player);
        units.AddRange(context.Enemies);
        return units;
    }
}
```

**全体选择器**

```csharp
public partial class AllTargetsSelector : TargetSelector
{
    public override List<IUnit> SelectTargets(TargetContext context)
    {
        List<IUnit> candidates = GetAllCandidateUnits(context);
        return FilterByFaction(candidates, TargetFaction);
    }

    protected virtual List<IUnit> GetAllCandidateUnits(TargetContext context)
    {
        List<IUnit> units = new List<IUnit>();
        units.Add(context.Player);
        units.AddRange(context.Enemies);
        return units;
    }
}
```

**敌方单体选择器（继承自单体选择器）**

```csharp
public partial class EnemySingleSelector : SingleTargetSelector
{
    public EnemySingleSelector()
    {
        TargetFaction = TargetFaction.Enemy;
    }

    protected override List<IUnit> GetAllCandidateUnits(TargetContext context)
    {
        return context.Enemies.Cast<IUnit>().ToList();
    }
}
```

**己方全体选择器**

```csharp
public partial class FriendlyAllSelector : AllTargetsSelector
{
    public FriendlyAllSelector()
    {
        TargetFaction = TargetFaction.Friendly;
    }

    protected override List<IUnit> GetAllCandidateUnits(TargetContext context)
    {
        return new List<IUnit> { context.Player };
    }
}
```

**随机选择器**

```csharp
public partial class RandomTargetSelector : TargetSelector
{
    [Export(PropertyHint.Range, "1,10,1")]
    public int SelectCount { get; set; } = 1;

    public override List<IUnit> SelectTargets(TargetContext context)
    {
        List<IUnit> candidates = GetAllCandidateUnits(context);
        candidates = FilterByFaction(candidates, TargetFaction);

        if (candidates.Count == 0)
            return new List<IUnit>();

        int count = Mathf.Min(SelectCount, candidates.Count);
        return candidates.OrderBy(x => GD.Randi()).Take(count).ToList();
    }

    protected virtual List<IUnit> GetAllCandidateUnits(TargetContext context)
    {
        List<IUnit> units = new List<IUnit>();
        units.Add(context.Player);
        units.AddRange(context.Enemies);
        return units;
    }
}
```

#### 3.1.4 Buff/Debuff 与 Effect 的触发时机关系

##### 核心设计原则：单一职责

**关键问题：Buff 的触发时机和 Effect 的触发时机是什么关系？**

**答案：Buff 只负责生命周期管理，触发时机完全由 Effect 决定。**

```
┌─────────────────────────────────────────────────────────────┐
│                    职责分离设计                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   Buff/Debuff (StatusEffect)                               │
│   ├── 职责：生命周期管理                                     │
│   │   ├── Duration (持续时间)                               │
│   │   ├── StackCount (叠加层数)                             │
│   │   ├── RefreshDuration() (刷新持续时间)                   │
│   │   ├── IsExpired() (是否过期)                            │
│   │   └── OnApply/OnRemove (应用/移除时的处理)               │
│   │                                                          │
│   └── 不负责：触发时机判断                                    │
│            (触发时机由内部的 Effect 决定)                     │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   Effect                                                    │
│   ├── 职责：具体效果执行                                     │
│   │   ├── Trigger (何时触发) ← 唯一的触发时机               │
│   │   ├── Target (对谁)                                      │
│   │   ├── Value (数值)                                       │
│   │   └── Apply() (如何执行)                                 │
│   │                                                          │
│   └── 不负责：持续时间管理                                    │
│            (持续时间由容器 Buff 决定)                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

##### 为什么这样设计？

**旧设计的问题（重复且混乱）：**
```csharp
Buff {
    Trigger = OnTurnStart;  // ← Buff 的触发时机
    Effects = [
        Effect {
            Trigger = OnTurnStart;  // ← Effect 的触发时机（重复！）
        }
    ]
}
```

**新设计的优势（清晰且解耦）：**
```csharp
Buff {
    Duration = 3;  // ← Buff 只管持续时间
    Effects = [
        Effect {
            Trigger = OnTurnStart;  // ← Effect 自己决定何时触发
            HealValue = 10;
            Target = FriendlyAll;
        }
    ]
}
```

##### 执行流程对比

**旧设计流程（混乱）：**
```
回合开始
    ↓
检查 Buff.Trigger == OnTurnStart? ← 第一层检查
    ↓ (是)
遍历 Effects
    ↓
检查 Effect.Trigger == OnTurnStart? ← 第二层检查（冗余）
    ↓ (是)
执行 Effect
```

**新设计流程（清晰）：**
```
回合开始
    ↓
检查 Effect.Trigger == OnTurnStart?
    ↓ (是)
执行 Effect (不需要检查 Buff 的触发时机)
```

##### Buff/Debuff 的新定义

```csharp
public abstract partial class StatusEffect : Resource
{
    [Export]
    public string EffectName { get; set; } = "";

    [Export]
    public string Description { get; set; } = "";

    [Export]
    public int Duration { get; set; } = 1;  // 持续时间（回合数）

    [Export]
    public int StackCount { get; set; } = 1;  // 叠加层数

    [Export]
    public StatusEffectType EffectType { get; set; } = StatusEffectType.Buff;

    [Export]
    public List<Effect> Effects { get; set; } = new List<Effect>();  // 包含的效果列表

    protected int _remainingDuration;

    public int RemainingDuration => _remainingDuration;

    public void Initialize()
    {
        _remainingDuration = Duration;
    }

    public bool IsExpired()
    {
        return _remainingDuration <= 0;
    }

    public void RefreshDuration()
    {
        _remainingDuration = Duration;
    }

    public void TickDuration()
    {
        _remainingDuration--;
    }

    public virtual void OnApply(IUnit target) { }
    public virtual void OnRemove(IUnit target) { }
}
```

##### 触发时机检查的统一入口

```csharp
public static class EffectTriggerSystem
{
    public static void ProcessTurnStart(Player player, Enemy[] enemies)
    {
        GD.Print("=== 回合开始，处理触发器 ===");

        ProcessUnitEffects(player, enemies, TriggerType.OnTurnStart);
    }

    public static void ProcessTurnEnd(Player player, Enemy[] enemies)
    {
        GD.Print("=== 回合结束，处理触发器 ===");

        ProcessUnitEffects(player, enemies, TriggerType.OnTurnEnd);

        // 回合结束时减少所有 Buff/Debuff 的持续时间
        ReduceAllStatusDuration(player);
        foreach (var enemy in enemies)
        {
            ReduceAllStatusDuration(enemy);
        }
    }

    private static void ProcessUnitEffects(IUnit unit, Enemy[] enemies, TriggerType triggerType)
    {
        var statusEffects = unit.GetStatusEffects();

        foreach (var status in statusEffects)
        {
            // 遍历 StatusEffect 中的所有 Effect
            foreach (var effect in status.Effects)
            {
                // 检查 Effect 的触发时机
                if (effect.Trigger == triggerType)
                {
                    EffectContext context = new EffectContext
                    {
                        Player = (Player)(unit is Player ? unit : null),
                        Enemies = enemies,
                        SourceEffect = effect,
                        CurrentTrigger = triggerType,
                        CurrentUnit = unit
                    };

                    EffectResolver.ApplyEffect(effect, context);

                    GD.Print($"[触发] {unit.GetName()} 的 「{status.EffectName}」执行效果: {effect.EffectId}");
                }
            }
        }
    }

    private static void ReduceAllStatusDuration(IUnit unit)
    {
        var statusEffects = unit.GetStatusEffects();
        foreach (var status in statusEffects)
        {
            status.TickDuration();
            if (status.IsExpired())
            {
                status.OnRemove(unit);
                unit.RemoveStatusEffect(status);
                GD.Print($"[消失] {unit.GetName()} 的 「{status.EffectName}」消失了");
            }
        }
    }
}
```

##### 简化后的 JSON 配置

```json
{
    "buffs": {
        "regeneration": {
            "name": "回复",
            "description": "回合开始时全体回复10点生命",
            "duration": 3,
            "effects": [
                {
                    "effectId": "heal_10_friendly",
                    "effectType": "heal",
                    "trigger": "on_turn_start",
                    "value": 10,
                    "target": "friendly_all"
                }
            ]
        },
        "strength": {
            "name": "力量增强",
            "description": "攻击力+3",
            "duration": 3,
            "effects": [
                {
                    "effectId": "strength_buff",
                    "effectType": "modify_attack",
                    "trigger": "immediate",
                    "value": 3,
                    "target": "self"
                }
            ]
        }
    },
    "debuffs": {
        "poison": {
            "name": "中毒",
            "description": "每回合受到3点伤害",
            "duration": 3,
            "effects": [
                {
                    "effectId": "poison_damage",
                    "effectType": "damage",
                    "trigger": "on_turn_end",
                    "value": 3,
                    "target": "self",
                    "ignore_defense": true
                }
            ]
        }
    }
}
```

##### 完整执行示例

**场景：** 回合开始时，"回复" Buff 触发，全体回血10点

```
┌─────────────────────────────────────────────────────────────┐
│ 1. TurnManager.StartTurn() 被调用                           │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. EffectTriggerSystem.ProcessTurnStart(player, enemies)   │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. 遍历 player.GetStatusEffects()                           │
│    发现 "回复" Buff                                         │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. 遍历 buff.Effects                                        │
│    发现 HealEffect                                          │
│    HealEffect.Trigger = OnTurnStart ✓                       │
│    HealEffect.Value = 10                                    │
│    HealEffect.Target = FriendlyAllSelector                   │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. 创建 EffectContext 并执行                                 │
│    HealEffect.Apply(context)                                │
│        │                                                    │
│        ├── ResolveTargets()                                 │
│        │   └── FriendlyAllSelector                          │
│        │   └── 返回 [Player]                                │
│        │                                                    │
│        └── Player.Heal(10)                                 │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. 输出日志                                                 │
│    [触发] 攻击者 的「回复」执行效果: heal_10_friendly        │
│    [Heal] 全体回复10点生命                                  │
└─────────────────────────────────────────────────────────────┘
```

**关键点：不需要检查 Buff 的触发时机，只检查 Effect 的触发时机！**

---

#### 3.1.5 复杂触发示例：受伤时反击

##### 场景描述

- 玩家获得了一个 Buff："荆棘" - 受到伤害时，对攻击者造成10点伤害
- 敌人攻击玩家
- 玩家受到伤害，同时触发荆棘效果

##### JSON 配置

```json
{
    "buffs": {
        "thorns": {
            "name": "荆棘",
            "description": "受到伤害时，对攻击者造成10点伤害",
            "duration": 2,
            "effects": [
                {
                    "effectId": "thorns_reflect",
                    "effectType": "damage",
                    "trigger": "on_damage_received",
                    "value": 10,
                    "target": "attacker",
                    "ignore_defense": true
                }
            ]
        }
    }
}
```

##### Effect 定义

```csharp
public partial class DamageEffect : Effect
{
    [Export]
    public bool IgnoreDefense { get; set; } = false;

    [Export]
    public bool IgnoreShield { get; set; } = false;

    public DamageEffect()
    {
        Type = EffectType.Damage;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            if (IgnoreDefense)
            {
                target.TakeDamageDirect(Value);
            }
            else
            {
                target.TakeDamage(Value);
            }
        }
    }
}
```

##### 特殊的目标选择器：AttackerSelector

```csharp
public partial class AttackerSelector : TargetSelector
{
    public AttackerSelector()
    {
        TargetFaction = TargetFaction.Any;
    }

    public override List<IUnit> SelectTargets(TargetContext context)
    {
        // 从上下文中获取攻击者
        if (context.Metadata.ContainsKey("Attacker"))
        {
            IUnit attacker = context.Metadata["Attacker"] as IUnit;
            if (attacker != null && !attacker.IsDead)
            {
                return new List<IUnit> { attacker };
            }
        }

        return new List<IUnit>();
    }
}
```

##### EffectContext 扩展

```csharp
public class EffectContext
{
    public Player Player { get; set; }
    public Enemy[] Enemies { get; set; }
    public Card PlayedCard { get; set; }
    public Effect SourceEffect { get; set; }
    public TriggerType CurrentTrigger { get; set; }
    public IUnit CurrentUnit { get; set; }  // 当前处理的目标
    public IUnit Attacker { get; set; }      // ← 攻击者（用于受伤时反击）
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

##### 受伤处理的统一入口

```csharp
public partial class Player : Node, IUnit
{
    public void TakeDamage(int damage, IUnit attacker = null)
    {
        GD.Print($"[受伤] 玩家受到 {damage} 点伤害");

        // 1. 先扣除护盾
        int remainingDamage = damage;
        if (Shield > 0)
        {
            if (Shield >= remainingDamage)
            {
                Shield -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= Shield;
                Shield = 0;
            }
        }

        // 2. 扣除生命
        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

        // 3. 触发"受到伤害时"的效果 ← 关键步骤
        if (attacker != null)
        {
            EffectTriggerSystem.ProcessDamageReceived(this, attacker, Enemies);
        }
    }
}
```

##### EffectTriggerSystem 扩展

```csharp
public static class EffectTriggerSystem
{
    // ... 其他方法 ...

    public static void ProcessDamageReceived(IUnit target, IUnit attacker, Enemy[] enemies)
    {
        GD.Print($"=== 触发「受到伤害时」效果 ===");

        // 检查目标身上的所有 StatusEffect
        var statusEffects = target.GetStatusEffects();

        foreach (var status in statusEffects)
        {
            foreach (var effect in status.Effects)
            {
                // 检查 Effect 的触发时机
                if (effect.Trigger == TriggerType.OnDamageReceived)
                {
                    // 创建上下文，包含攻击者信息
                    EffectContext context = new EffectContext
                    {
                        Player = (Player)(target is Player ? target : null),
                        Enemies = enemies,
                        SourceEffect = effect,
                        CurrentTrigger = TriggerType.OnDamageReceived,
                        CurrentUnit = target,
                        Attacker = attacker  // ← 传递攻击者
                    };

                    EffectResolver.ApplyEffect(effect, context);

                    GD.Print($"[触发] {target.GetName()} 的「{status.EffectName}」反击！");
                }
            }
        }
    }
}
```

##### 完整执行流程

```
┌─────────────────────────────────────────────────────────────┐
│ 1. 敌人攻击玩家                                             │
│    enemy.AttackPlayer(player, damage: 8)                     │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. 玩家受到伤害                                             │
│    player.TakeDamage(8, attacker: enemy)                     │
│        │                                                   │
│        ├── 扣除护盾 (如果有)                                │
│        │                                                   │
│        └── 扣除生命 100 → 92                                │
│            │                                                │
│            └── 触发 ProcessDamageReceived(target, attacker)   │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. 检查「受到伤害时」触发器                                  │
│    EffectTriggerSystem.ProcessDamageReceived()               │
│        │                                                   │
│        ├── 遍历 player.GetStatusEffects()                   │
│        │   发现 "荆棘" Buff                                  │
│        │                                                   │
│        └── 遍历 buff.Effects                                │
│            发现 ThornsEffect                                 │
│            ThornsEffect.Trigger = OnDamageReceived ✓        │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. 创建 EffectContext（包含攻击者信息）                       │
│    context = {                                              │
│        Attacker = enemy,  ← 关键：记录攻击者                 │
│        CurrentTrigger = OnDamageReceived,                   │
│        CurrentUnit = player                                 │
│    }                                                        │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. 执行反击效果                                              │
│    ThornsEffect.Apply(context)                               │
│        │                                                   │
│        ├── ResolveTargets(context)                           │
│        │   └── 创建 TargetContext                          │
│        │   └── TargetContext.Metadata["Attacker"] = enemy   │
│        │   └── AttackerSelector.SelectTargets()            │
│        │   └── 返回 [enemy]  ← 攻击者                      │
│        │                                                   │
│        └── enemy.TakeDamageDirect(10)                      │
│            └── enemy.CurrentHealth -= 10                    │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. 输出日志                                                 │
│    [受伤] 玩家受到 8 点伤害                                  │
│    [触发] 玩家 的「荆棘」反击！                              │
│    [伤害] 敌人 受到 10 点伤害                                │
└─────────────────────────────────────────────────────────────┘
```

##### 实际运行输出

```
=== 敌人行动 ===
[攻击] 敌人 对 玩家 发起攻击
[受伤] 玩家受到 8 点伤害
[触发] 玩家 的「荆棘」反击！
[伤害] 敌人 受到 10 点伤害
```

##### 扩展：护盾破碎时触发效果

如果护盾破碎时也要触发效果怎么办？

```json
{
    "buffs": {
        "energy_shield": {
            "name": "能量护盾",
            "description": "护盾破碎时，回复10点生命",
            "duration": 3,
            "effects": [
                {
                    "effectId": "shield_break_heal",
                    "effectType": "heal",
                    "trigger": "on_shield_broken",
                    "value": 10,
                    "target": "self"
                }
            ]
        }
    }
}
```

只需要添加新的触发时机 `OnShieldBroken`，并在护盾扣除逻辑中触发：

```csharp
public void TakeDamage(int damage, IUnit attacker = null)
{
    int originalShield = Shield;
    int remainingDamage = damage;

    if (Shield > 0)
    {
        if (Shield >= remainingDamage)
        {
            Shield -= remainingDamage;
            remainingDamage = 0;
        }
        else
        {
            remainingDamage -= Shield;
            Shield = 0;
        }
    }

    // 检查护盾是否被打破
    if (originalShield > 0 && Shield == 0)
    {
        // 触发护盾破碎效果
        EffectTriggerSystem.ProcessShieldBroken(this, Enemies);
    }

    CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

    if (attacker != null)
    {
        EffectTriggerSystem.ProcessDamageReceived(this, attacker, Enemies);
    }
}
```

##### 完整触发时机列表

| 触发时机 | 触发条件 | 上下文需要的额外信息 |
|---------|---------|-------------------|
| `Immediate` | 立即执行 | 无 |
| `OnTurnStart` | 回合开始 | 无 |
| `OnTurnEnd` | 回合结束 | 无 |
| `OnDamageDealt` | 造成伤害时 | Defender（被攻击者） |
| `OnDamageReceived` | 受到伤害时 | Attacker（攻击者） |
| `OnShieldBroken` | 护盾破碎时 | OriginalShieldValue |
| `OnKill` | 击杀敌人时 | Victim（被击杀者） |
| `OnHeal` | 治疗时 | HealAmount |
| `OnCardPlayed` | 使用卡牌时 | PlayedCard |
| `OnTurnStart` | 回合开始 | 无 |

---

#### 3.1.4 效果与选择器解耦

**效果基类 - 移除固定目标绑定**

```csharp
public abstract partial class Effect : Resource
{
    [Export]
    public string EffectId { get; set; } = "";

    [Export]
    public EffectType Type { get; set; } = EffectType.Custom;

    [Export]
    public TriggerType Trigger { get; set; } = TriggerType.Immediate;

    [Export]
    public int Value { get; set; } = 0;

    [Export]
    public int Duration { get; set; } = 0;

    [Export(PropertyHint.ResourceType, "TargetSelector")]
    public TargetSelector Target { get; set; }

    [Export]
    public string Condition { get; set; } = "";

    public abstract void Apply(EffectContext context);

    protected List<IUnit> ResolveTargets(EffectContext context)
    {
        if (Target == null)
        {
            return new List<IUnit>();
        }

        TargetContext targetContext = new TargetContext
        {
            Player = context.Player,
            Enemies = context.Enemies,
            PlayedCard = context.PlayedCard,
            SourceEffect = this,
            CurrentUnit = context.Player
        };

        return Target.SelectTargets(targetContext);
    }
}
```

#### 3.1.5 具体效果实现 - 通用化

**伤害效果 - 可作用于任意阵营**

```csharp
public partial class DamageEffect : Effect
{
    [Export]
    public bool IgnoreDefense { get; set; } = false;

    [Export]
    public bool Piercing { get; set; } = false;

    public DamageEffect()
    {
        Type = EffectType.Damage;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            int damage = Value;

            if (IgnoreDefense)
            {
                target.TakeDamage(damage);
            }
            else if (Piercing)
            {
                target.TakeDamage(damage);
            }
            else
            {
                int actualDamage = CalculateDamageWithDefense(damage, target);
                target.TakeDamage(actualDamage);
            }
        }
    }

    private int CalculateDamageWithDefense(int baseDamage, IUnit target)
    {
        int damageAfterDefense = baseDamage - target.Defense;
        return Mathf.Max(1, damageAfterDefense);
    }
}
```

**护盾效果 - 可作用于任意阵营**

```csharp
public partial class ShieldEffect : Effect
{
    [Export]
    public bool IsRetain { get; set; } = false;

    public ShieldEffect()
    {
        Type = EffectType.Shield;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            target.AddShield(Value);
        }
    }
}
```

**治疗效果 - 可作用于任意阵营**

```csharp
public partial class HealEffect : Effect
{
    [Export]
    public bool IsPercent { get; set; } = false;

    [Export]
    public bool CapAtMaxHealth { get; set; } = true;

    public HealEffect()
    {
        Type = EffectType.Heal;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            int healAmount = IsPercent
                ? (int)(target.MaxHealth * Value / 100.0f)
                : Value;

            target.Heal(healAmount);
        }
    }
}
```

**施加Buff效果 - 可作用于任意阵营**

```csharp
public partial class ApplyBuffEffect : Effect
{
    [Export]
    public string BuffId { get; set; } = "";

    [Export]
    public bool RefreshDuration { get; set; } = true;

    [Export]
    public int StackCount { get; set; } = 1;

    public ApplyBuffEffect()
    {
        Type = EffectType.ApplyBuff;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            StatusEffect buff = EffectRegistry.CreateBuff(BuffId);
            if (buff != null)
            {
                buff.Duration = Duration;
                buff.StackCount = StackCount;

                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(buff);
                }
                else if (target is Player player)
                {
                    player.AddStatusEffect(buff);
                }
            }
        }
    }
}
```

**施加Debuff效果 - 可作用于任意阵营**

```csharp
public partial class ApplyDebuffEffect : Effect
{
    [Export]
    public string DebuffId { get; set; } = "";

    [Export]
    public bool RefreshDuration { get; set; } = true;

    public ApplyDebuffEffect()
    {
        Type = EffectType.ApplyDebuff;
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            StatusEffect debuff = EffectRegistry.CreateDebuff(DebuffId);
            if (debuff != null)
            {
                debuff.Duration = Duration;

                if (target is Enemy enemy)
                {
                    enemy.AddStatusEffect(debuff);
                }
                else if (target is Player player)
                {
                    player.AddStatusEffect(debuff);
                }
            }
        }
    }
}
```

**能量效果**

```csharp
public partial class EnergyEffect : Effect
{
    [Export]
    public bool ThisTurnOnly { get; set; } = false;

    public EnergyEffect()
    {
        Type = EffectType.Energy;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.CurrentEnergy += Value;

        if (ThisTurnOnly)
        {
            // 记录本回合获得的能量，回合结束时清除
        }
    }
}
```

**抽牌效果**

```csharp
public partial class DrawEffect : Effect
{
    [Export]
    public bool FromDiscardOnly { get; set; } = false;

    public DrawEffect()
    {
        Type = EffectType.Draw;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.DrawCards(Value);
    }
}
```

#### 3.1.6 目标选择器配置示例

**JSON配置中的目标选择器**

```json
{
    "targetSelectors": {
        "enemy_front": {
            "type": "single",
            "faction": "enemy",
            "selection": "front"
        },
        "enemy_all": {
            "type": "all",
            "faction": "enemy"
        },
        "enemy_random": {
            "type": "single",
            "faction": "enemy",
            "selection": "random"
        },
        "player_self": {
            "type": "single",
            "faction": "friendly",
            "selection": "self"
        },
        "friendly_all": {
            "type": "all",
            "faction": "friendly"
        },
        "any_random": {
            "type": "random",
            "faction": "any",
            "count": 2
        }
    }
}
```

**效果配置示例 - 展示灵活性**

```json
{
    "effects": {
        "rat_attack": {
            "type": "damage",
            "value": 4,
            "target": "enemy_front"
        },
        "self_damage_skill": {
            "type": "damage",
            "value": 5,
            "target": "player_self",
            "description": "自残5点"
        },
        "enemy_heal": {
            "type": "heal",
            "value": 10,
            "target": "enemy_random",
            "description": "随机治疗一名敌人10点"
        },
        "enemy_strengthen": {
            "type": "apply_buff",
            "buffId": "strength",
            "duration": 2,
            "target": "enemy_random",
            "description": "随机强化一名敌人"
        },
        "friendly_damage": {
            "type": "damage",
            "value": 3,
            "target": "friendly_all",
            "description": "对所有队友造成3点伤害"
        }
    }
}
```

#### 3.1.7 效果解析器更新

```csharp
public static class EffectResolver
{
    public static void ResolveEffects(List<Effect> effects, EffectContext context)
    {
        foreach (var effect in effects)
        {
            if (effect.Trigger == context.CurrentTrigger || effect.Trigger == TriggerType.Immediate)
            {
                ApplyEffect(effect, context);
            }
        }
    }

    public static void ApplyEffect(Effect effect, EffectContext context)
    {
        if (effect is DamageEffect damageEffect)
        {
            damageEffect.Apply(context);
        }
        else if (effect is ShieldEffect shieldEffect)
        {
            shieldEffect.Apply(context);
        }
        else if (effect is HealEffect healEffect)
        {
            healEffect.Apply(context);
        }
        else if (effect is ApplyBuffEffect applyBuff)
        {
            applyBuff.Apply(context);
        }
        else if (effect is ApplyDebuffEffect applyDebuff)
        {
            applyDebuff.Apply(context);
        }
        else if (effect is EnergyEffect energyEffect)
        {
            energyEffect.Apply(context);
        }
        else if (effect is DrawEffect drawEffect)
        {
            drawEffect.Apply(context);
        }
        else if (effect is CompositeEffect composite)
        {
            composite.Apply(context);
        }
        else if (effect is CustomEffect custom)
        {
            custom.Apply(context);
        }
    }
}
```

#### 3.1.8 单位接口实现

**Enemy 实现 IUnit**

```csharp
public partial class Enemy : Node, IUnit
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Position { get; set; }

    public TargetFaction GetFaction() => TargetFaction.Enemy;

    public bool IsDead => CurrentHealth <= 0;
}
```

**Player 实现 IUnit**

```csharp
public partial class Player : Node, IUnit
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Attack { get; set; }
    public int Shield { get; set; }

    public TargetFaction GetFaction() => TargetFaction.Friendly;

    public bool IsDead => CurrentHealth <= 0;

    public void TakeDamage(int damage)
    {
        int remainingDamage = damage;

        if (Shield > 0)
        {
            if (Shield >= remainingDamage)
            {
                Shield -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= Shield;
                Shield = 0;
            }
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

    public void AddShield(int amount)
    {
        Shield += amount;
    }
}
```

#### 3.1.9 选择器注册表

```csharp
public static class TargetSelectorRegistry
{
    private static Dictionary<string, TargetSelector> _selectors = new();

    public static void Register(string id, TargetSelector selector)
    {
        selector.SelectorId = id;
        _selectors[id] = selector;
    }

    public static TargetSelector Get(string id)
    {
        if (_selectors.ContainsKey(id))
        {
            return _selectors[id].Duplicate() as TargetSelector;
        }

        GD.PrintErr($"Unknown target selector: {id}");
        return null;
    }

    public static void Initialize()
    {
        Register("enemy_front", new EnemySingleSelector());
        Register("enemy_all", new EnemyAllSelector());
        Register("enemy_random", new EnemyRandomSelector());
        Register("player_self", new PlayerSelfSelector());
        Register("friendly_all", new FriendlyAllSelector());
        Register("any_random", new RandomTargetSelector());
        Register("enemy_lowest_hp", new EnemyLowestHPSelector());
        Register("enemy_highest_hp", new EnemyHighestHPSelector());
    }
}
```

---

### 3.2 效果基类 (Effect)

```csharp
// 效果类型枚举
public enum EffectType
{
    Damage,
    Shield,
    Heal,
    Draw,
    Energy,
    ApplyBuff,
    ApplyDebuff,
    ModifyDamage,
    ModifyShield,
    Custom
}

// 触发时机枚举
public enum TriggerType
{
    Immediate,           // 立即执行（使用卡牌时）
    OnTurnStart,         // 回合开始
    OnTurnEnd,           // 回合结束
    OnDamageDealt,       // 造成伤害时
    OnDamageReceived,    // 受到伤害时
    OnHeal,              // 治疗时
    OnKill,              // 击杀敌人时
    OnCardPlayed,        // 使用卡牌时
    OnConditionMet       // 满足条件时
}
```

#### 3.1.10 触发时机执行流程详解

##### 问题：触发时机枚举的作用是什么？

触发时机枚举（`TriggerType`）用于定义**何时**执行效果，而不是**对谁**或**执行什么**效果。

核心机制：
1. **效果携带触发时机** - 每个效果对象知道自己应该在什么时机触发
2. **游戏事件触发检查** - 当游戏事件发生时，系统检查所有相关效果的触发时机
3. **匹配执行** - 触发时机匹配的效果被执行

##### 示例：回合开始时全体回血效果

**场景描述：**
- 玩家获得了一个 Buff："回合开始时，全体回复10点生命"
- 这个 Buff 会持续 3 回合

**代码执行流程：**

**Step 1: 定义效果**

```csharp
public partial class RegenerationBuff : StatusEffect
{
    [Export]
    public int HealPerTurn { get; set; } = 10;

    public RegenerationBuff()
    {
        EffectName = "Regeneration";
        Description = "回合开始时全体回复10点生命";
        Duration = 3;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnTurnStart;  // ← 关键：指定触发时机
    }

    public override void OnTurnStartPlayer(Player target)
    {
        // 这是旧版实现，我们会在新系统中使用统一的效果机制
    }
}
```

**Step 2: 在新系统中使用效果**

```csharp
public partial class RegenerationEffect : Effect
{
    [Export]
    public int HealValue { get; set; } = 10;

    [Export]
    public bool AffectsAllFriendly { get; set; } = true;

    public RegenerationEffect()
    {
        EffectId = "regeneration";
        Type = EffectType.Heal;
        Trigger = TriggerType.OnTurnStart;  // ← 回合开始时触发
        Target = new FriendlyAllSelector();  // ← 作用于全体友方
    }

    public override void Apply(EffectContext context)
    {
        List<IUnit> targets = ResolveTargets(context);

        foreach (var target in targets)
        {
            target.Heal(HealValue);

            // 触发日志
            if (context.Metadata.ContainsKey("LogCallback"))
            {
                var logCallback = context.Metadata["LogCallback"] as System.Action<string, LogType>;
                logCallback($"全体回复{HealValue}点生命", LogType.Heal);
            }
        }
    }
}
```

**Step 3: Buff 配置到 JSON**

```json
{
    "buffs": {
        "regeneration": {
            "name": "回复",
            "description": "回合开始时全体回复10点生命",
            "duration": 3,
            "icon": "regeneration.png",
            "effects": [
                {
                    "effectType": "heal",
                    "trigger": "on_turn_start",
                    "value": 10,
                    "target": "friendly_all"
                }
            ]
        }
    }
}
```

**Step 4: 回合管理器触发流程**

```csharp
public partial class TurnManager : Node
{
    public Player Player { get; set; }
    public Enemy[] Enemies { get; set; }

    public void StartTurn()
    {
        GD.Print("=== 回合开始 ===");

        // 1. 处理玩家Buff的回合开始效果
        ProcessTurnStartEffects(Player, Enemies);

        // 2. 处理敌人Buff的回合开始效果
        foreach (var enemy in Enemies)
        {
            ProcessTurnStartEffects(enemy);
        }

        // 3. 玩家抽牌
        Player.DrawCards(Player.DrawCount);
    }

    private void ProcessTurnStartEffects(Unit unit)
    {
        // 获取单位身上的所有Buff
        var buffs = unit.GetStatusEffects().Where(b => b.EffectType == StatusEffectType.Buff);

        foreach (var buff in buffs)
        {
            // 检查Buff是否在回合开始触发
            if (buff.Trigger == EffectTrigger.OnTurnStart)
            {
                // 获取Buff中包含的所有效果
                foreach (var effect in buff.Effects)
                {
                    // 检查效果的触发时机
                    if (effect.Trigger == TriggerType.OnTurnStart)
                    {
                        // 创建效果上下文
                        EffectContext context = new EffectContext
                        {
                            Player = Player,
                            Enemies = Enemies,
                            SourceEffect = effect,
                            CurrentTrigger = TriggerType.OnTurnStart,
                            Metadata = new Dictionary<string, object>()
                            {
                                { "SourceUnit", unit },
                                { "LogCallback", new System.Action<string, LogType>(LogMessage) }
                            }
                        };

                        // 执行效果
                        EffectResolver.ApplyEffect(effect, context);

                        GD.Print($"[触发] {unit.Name} 的 Buff 「{buff.EffectName}」触发效果: {effect.EffectId}");
                    }
                }
            }
        }
    }

    private void LogMessage(string message, LogType type)
    {
        GD.Print($"[{type}] {message}");
    }
}
```

**Step 5: 完整执行流程图**

```
┌─────────────────────────────────────────────────────────────┐
│                   回合开始触发流程                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  TurnManager.StartTurn()                                    │
│       │                                                     │
│       ├── ProcessTurnStartEffects(Player)                   │
│       │       │                                             │
│       │       ├── 获取 Player 的所有 Buff                   │
│       │       │                                             │
│       │       ├── 遍历每个 Buff                            │
│       │       │                                             │
│       │       └── 对于 "回复" Buff:                        │
│       │               │                                     │
│       │               ├── 检查 Trigger == OnTurnStart ✓     │
│       │               │                                     │
│       │               ├── 遍历 Buff 中的 Effects           │
│       │               │                                     │
│       │               ├── 对于 RegenerationEffect:           │
│       │               │                                     │
│       │               ├── 检查 Trigger == OnTurnStart ✓     │
│       │               │                                     │
│       │               ├── 创建 EffectContext               │
│       │               │                                     │
│       │               ├── Effect.Apply(context)             │
│       │               │       │                             │
│       │               │       ├── ResolveTargets()          │
│       │               │       │   └── FriendlyAllSelector  │
│       │               │       │   └── 返回 [Player]         │
│       │               │       │                             │
│       │               │       └── 对每个目标执行 Heal(10)   │
│       │               │           └── Player.Heal(10)      │
│       │               │                                       │
│       │               └── 输出日志: "全体回复10点生命"     │
│       │                                                             │
│       └── [继续] 玩家抽牌...                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Step 6: 实际输出示例**

```
=== 回合开始 ===
[触发] 攻击者 的 Buff 「回复」触发效果: regeneration
[Heal] 全体回复10点生命
[触发] 攻击者 的 Buff 「回复」持续时间 3→2
[抽牌] 抽了5张牌
```

##### 多种触发时机的应用场景

| 触发时机 | 应用场景 | 示例 |
|---------|---------|------|
| `Immediate` | 立即执行 | 卡牌使用时造成伤害、获得护盾 |
| `OnTurnStart` | 回合开始 | 回合开始时回血、清除debuff |
| `OnTurnEnd` | 回合结束 | 回合结束时中毒扣血、护盾消失 |
| `OnDamageDealt` | 造成伤害时 | 造成伤害时附加中毒、回复生命 |
| `OnDamageReceived` | 受到伤害时 | 受到伤害时反击、获得护盾 |
| `OnHeal` | 治疗时 | 治疗时额外回复、清除debuff |
| `OnKill` | 击杀敌人时 | 击杀时抽牌、获得能量 |
| `OnCardPlayed` | 使用卡牌时 | 使用攻击卡时获得怒气 |
| `OnConditionMet` | 满足条件时 | 血量低于30%时获得力量 |

##### 复杂效果示例：中毒Buff（回合结束触发）

```json
{
    "debuffs": {
        "poison": {
            "name": "中毒",
            "description": "每回合受到3点伤害，持续3回合",
            "effects": [
                {
                    "effectType": "damage",
                    "value": 3,
                    "target": "self",
                    "trigger": "on_turn_end",
                    "ignore_defense": true
                }
            ]
        }
    }
}
```

```csharp
public partial class PoisonDebuff : StatusEffect
{
    [Export]
    public int DamagePerTurn { get; set; } = 3;

    public PoisonDebuff()
    {
        EffectName = "Poison";
        Description = "每回合受到3点伤害";
        Duration = 3;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnTurnEnd;  // ← 回合结束时触发
    }
}
```

```csharp
// 在 TurnManager.EndTurn() 中调用
public void EndTurn()
{
    GD.Print("=== 回合结束 ===");

    // 处理玩家DeBuff的回合结束效果
    foreach (var debuff in Player.GetStatusEffects().Where(d => d.EffectType == StatusEffectType.Debuff))
    {
        if (debuff.Trigger == EffectTrigger.OnTurnEnd)
        {
            foreach (var effect in debuff.Effects)
            {
                if (effect.Trigger == TriggerType.OnTurnEnd)
                {
                    EffectContext context = new EffectContext
                    {
                        Player = Player,
                        Enemies = Enemies,
                        CurrentTrigger = TriggerType.OnTurnEnd
                    };

                    EffectResolver.ApplyEffect(effect, context);
                }
            }
        }
    }

    // 中毒扣血输出示例：
    // [Damage] 攻击者 受到 中毒 效果: 3点伤害
    // [Damage] 攻击者 受到 3 点伤害
}
```

##### 条件触发示例：低血量时获得力量

```json
{
    "buffs": {
        "desperate_strength": {
            "name": "绝境逢生",
            "description": "生命低于30%时攻击+5",
            "trigger_condition": {
                "type": "health_below",
                "threshold": 0.3
            },
            "effects": [
                {
                    "effectType": "modify_damage",
                    "modifier": 5,
                    "condition": "health_below_30"
                }
            ]
        }
    }
}
```

---

### 3.2 效果基类 (Effect)
public abstract partial class Effect : Resource
{
    [Export]
    public string EffectId { get; set; } = "";

    [Export]
    public EffectType Type { get; set; } = EffectType.Custom;

    [Export]
    public TriggerType Trigger { get; set; } = TriggerType.Immediate;

    [Export]
    public int Value { get; set; } = 0;

    [Export]
    public int Duration { get; set; } = 0;

    [Export]
    public string TargetType { get; set; } = "enemy_front";

    [Export]
    public string Condition { get; set; } = "";

    public abstract void Apply(EffectContext context);
}
```

### 3.2 效果上下文 (EffectContext)

```csharp
public class EffectContext
{
    public Player Player { get; set; }
    public Enemy[] Enemies { get; set; }
    public Card PlayedCard { get; set; }
    public Effect SourceEffect { get; set; }
    public TriggerType CurrentTrigger { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### 3.3 具体效果实现

```csharp
// 伤害效果
public partial class DamageEffect : Effect
{
    public DamageEffect()
    {
        Type = EffectType.Damage;
    }

    public override void Apply(EffectContext context)
    {
        Enemy target = EffectResolver.GetTarget(TargetType, context.Enemies);
        if (target != null)
        {
            int damage = Value;
            target.TakeDamage(damage);
        }
    }
}

// 护盾效果
public partial class ShieldEffect : Effect
{
    public ShieldEffect()
    {
        Type = EffectType.Shield;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.AddShield(Value);
    }
}

// 治疗效果
public partial class HealEffect : Effect
{
    public HealEffect()
    {
        Type = EffectType.Heal;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.Heal(Value);
    }
}

// 抽牌效果
public partial class DrawEffect : Effect
{
    public DrawEffect()
    {
        Type = EffectType.Draw;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.DrawCards(Value);
    }
}

// 添加Buff效果
public partial class ApplyBuffEffect : Effect
{
    [Export]
    public string BuffId { get; set; } = "";

    public ApplyBuffEffect()
    {
        Type = EffectType.ApplyBuff;
    }

    public override void Apply(EffectContext context)
    {
        Enemy target = EffectResolver.GetTarget(TargetType, context.Enemies);
        if (target != null)
        {
            StatusEffect buff = EffectRegistry.CreateBuff(BuffId);
            buff.Duration = Duration;
            target.AddStatusEffect(buff);
        }
    }
}

// 添加Debuff效果
public partial class ApplyDebuffEffect : Effect
{
    [Export]
    public string DebuffId { get; set; } = "";

    public ApplyDebuffEffect()
    {
        Type = EffectType.ApplyDebuff;
    }

    public override void Apply(EffectContext context)
    {
        Enemy target = EffectResolver.GetTarget(TargetType, context.Enemies);
        if (target != null)
        {
            StatusEffect debuff = EffectRegistry.CreateDebuff(DebuffId);
            debuff.Duration = Duration;
            target.AddStatusEffect(debuff);
        }
    }
}

// 能量效果
public partial class EnergyEffect : Effect
{
    public EnergyEffect()
    {
        Type = EffectType.Energy;
    }

    public override void Apply(EffectContext context)
    {
        context.Player.CurrentEnergy += Value;
    }
}
```

### 3.4 效果解析器 (EffectResolver)

```csharp
public static class EffectResolver
{
    public static void ResolveEffects(List<Effect> effects, EffectContext context)
    {
        foreach (var effect in effects)
        {
            if (effect.Trigger == context.CurrentTrigger || effect.Trigger == TriggerType.Immediate)
            {
                ApplyEffect(effect, context);
            }
        }
    }

    public static void ApplyEffect(Effect effect, EffectContext context)
    {
        switch (effect.Type)
        {
            case EffectType.Damage:
                ApplyDamage(effect, context);
                break;
            case EffectType.Shield:
                ApplyShield(effect, context);
                break;
            case EffectType.Heal:
                ApplyHeal(effect, context);
                break;
            case EffectType.Draw:
                ApplyDraw(effect, context);
                break;
            case EffectType.Energy:
                ApplyEnergy(effect, context);
                break;
            case EffectType.ApplyBuff:
                ApplyBuff(effect, context);
                break;
            case EffectType.ApplyDebuff:
                ApplyDebuff(effect, context);
                break;
            case EffectType.Custom:
                ApplyCustomEffect(effect, context);
                break;
        }
    }

    public static Enemy GetTarget(string targetType, Enemy[] enemies)
    {
        enemies = enemies.Where(e => !e.IsDead()).ToArray();
        if (enemies.Length == 0) return null;

        return targetType switch
        {
            "enemy_front" => GetFrontmost(enemies),
            "enemy_rear" => GetRearmost(enemies),
            "enemy_random" => enemies[GD.Randi() % enemies.Length],
            "enemy_all" => enemies[0], // 群体效果会遍历所有
            "player" => null,
            _ => GetFrontmost(enemies)
        };
    }

    private static Enemy GetFrontmost(Enemy[] enemies)
    {
        return enemies.OrderBy(e => e.Position).FirstOrDefault();
    }

    private static Enemy GetRearmost(Enemy[] enemies)
    {
        return enemies.OrderByDescending(e => e.Position).FirstOrDefault();
    }
}
```

### 3.5 效果注册表 (EffectRegistry)

```csharp
public static class EffectRegistry
{
    private static Dictionary<string, Type> _effectTypes = new();
    private static Dictionary<string, Effect> _effectTemplates = new();

    public static void RegisterEffect(string id, Type effectType)
    {
        _effectTypes[id] = effectType;
    }

    public static void RegisterEffectTemplate(string id, Effect template)
    {
        _effectTemplates[id] = template;
    }

    public static Effect CreateEffect(string effectId)
    {
        if (_effectTemplates.ContainsKey(effectId))
        {
            return _effectTemplates[effectId].Duplicate();
        }

        if (_effectTypes.ContainsKey(effectId))
        {
            return Activator.CreateInstance(_effectTypes[effectId]) as Effect;
        }

        GD.PrintErr($"Unknown effect type: {effectId}");
        return null;
    }

    public static StatusEffect CreateBuff(string buffId)
    {
        return buffId.ToLower() switch
        {
            "strength" => new StrengthBuff(),
            "defense" => new DefenseBuff(),
            "regeneration" => new RegenerationBuff(),
            "thorns" => new ThornsBuff(),
            "fury" => new FuryBuff(),
            _ => null
        };
    }

    public static StatusEffect CreateDebuff(string debuffId)
    {
        return debuffId.ToLower() switch
        {
            "weak" => new WeakDebuff(),
            "vulnerable" => new VulnerableDebuff(),
            "poison" => new PoisonDebuff(),
            "slow" => new SlowDebuff(),
            "silence" => new SilenceDebuff(),
            _ => null
        };
    }

    public static void Initialize()
    {
        RegisterEffect("damage", typeof(DamageEffect));
        RegisterEffect("shield", typeof(ShieldEffect));
        RegisterEffect("heal", typeof(HealEffect));
        RegisterEffect("draw", typeof(DrawEffect));
        RegisterEffect("energy", typeof(EnergyEffect));
        RegisterEffect("apply_buff", typeof(ApplyBuffEffect));
        RegisterEffect("apply_debuff", typeof(ApplyDebuffEffect));
    }
}
```

---

## 4. JSON配置文件设计

### 4.1 效果定义文件 (effects.json)

```json
{
    "effects": {
        "rat_attack": {
            "type": "damage",
            "value": 4,
            "target": "enemy_front",
            "trigger": "immediate",
            "rage_gain": 15
        },
        "ox_attack": {
            "type": "damage",
            "value": 8,
            "target": "enemy_front",
            "trigger": "immediate",
            "rage_gain": 15
        },
        "rat_group_escape": {
            "type": "composite",
            "effects": [
                {
                    "type": "draw",
                    "value": 2
                },
                {
                    "type": "damage",
                    "value": 3,
                    "target": "enemy_all"
                }
            ],
            "cost": 1
        },
        "ox_charge": {
            "type": "damage",
            "value": "target_max_health_percent_10",
            "target": "enemy_front",
            "cost": 2
        }
    },
    "buffs": {
        "strength": {
            "name": "力量增强",
            "description": "攻击力+%d",
            "duration": 3,
            "modify_damage": {
                "type": "add",
                "value": 3
            },
            "on_apply": {
                "target": "self",
                "stat": "attack",
                "modify": 3
            }
        },
        "poison": {
            "name": "中毒",
            "description": "每回合受%d伤害",
            "duration": 3,
            "on_turn_end": {
                "type": "damage",
                "target": "self",
                "value": 3
            }
        }
    }
}
```

### 4.2 技能配置扩展 (characters.json)

```json
{
    "characters": [
        {
            "characterId": "rat",
            "name": "鼠",
            "attackCardId": "rat_attack",
            "defenseCardId": "rat_defense",
            "specialCards": [
                {
                    "cardId": "rat_special1",
                    "name": "群体迁逃",
                    "effects": [
                        {
                            "effectType": "draw",
                            "value": 2
                        }
                    ],
                    "cost": 1,
                    "description": "全队抽2张"
                },
                {
                    "cardId": "rat_special2",
                    "name": "盗食",
                    "effects": [
                        {
                            "effectType": "steal_card",
                            "value": 1
                        }
                    ],
                    "cost": 1,
                    "description": "偷取敌人1张牌"
                },
                {
                    "cardId": "rat_special3",
                    "name": "嗅觉敏锐",
                    "effects": [
                        {
                            "effectType": "apply_buff",
                            "buffId": "strength",
                            "duration": 3,
                            "value": 1
                        }
                    ],
                    "cost": 2,
                    "description": "下3回合攻击+1"
                }
            ]
        }
    ]
}
```

### 4.3 造物效果配置 (artifacts.json)

```json
{
    "artifacts": [
        {
            "artifactId": "warrior_heart",
            "name": "战士之心",
            "description": "所有角色攻击+5",
            "type": "attacker",
            "rarity": "common",
            "effects": [
                {
                    "effectType": "stat_modify",
                    "target": "all_characters",
                    "stat": "attack",
                    "value": 5
                }
            ],
            "conditions": []
        },
        {
            "artifactId": "rage_soul",
            "name": "怒气之魂",
            "description": "怒气满100时伤害+20%",
            "type": "condition",
            "rarity": "common",
            "effects": [
                {
                    "effectType": "damage_modify",
                    "condition": "rage_full",
                    "multiplier": 1.2
                }
            ],
            "conditions": [
                {
                    "type": "rage_full",
                    "threshold": 100
                }
            ]
        }
    ]
}
```

### 4.4 银钥技能配置 (key_orders.json)

```json
{
    "keyOrders": [
        {
            "keyOrderId": "silver_slash",
            "name": "银光斩",
            "description": "对所有敌人造成50伤害",
            "silverKeyCost": 1000,
            "effects": [
                {
                    "effectType": "damage",
                    "value": 50,
                    "target": "enemy_all"
                }
            ]
        },
        {
            "keyOrderId": "key_shield",
            "name": "钥之护盾",
            "description": "全体队友获得30护盾",
            "silverKeyCost": 1000,
            "effects": [
                {
                    "effectType": "shield",
                    "value": 30,
                    "target": "player"
                }
            ]
        }
    ]
}
```

---

## 5. 实施计划

### 5.1 第一阶段：基础设施重构

**目标：** 建立效果系统核心框架

**任务：**
1. 创建 `Effect` 基类
2. 创建 `EffectContext` 上下文类
3. 创建具体效果类（DamageEffect, ShieldEffect, HealEffect等）
4. 创建 `EffectResolver` 解析器
5. 创建 `EffectRegistry` 注册表

**交付物：**
- `Scripts/Effect/Effect.cs`
- `Scripts/Effect/EffectContext.cs`
- `Scripts/Effect/DamageEffect.cs`
- `Scripts/Effect/ShieldEffect.cs`
- `Scripts/Effect/HealEffect.cs`
- `Scripts/Effect/DrawEffect.cs`
- `Scripts/Effect/EnergyEffect.cs`
- `Scripts/Effect/ApplyBuffEffect.cs`
- `Scripts/Effect/ApplyDebuffEffect.cs`
- `Scripts/Effect/EffectResolver.cs`
- `Scripts/Effect/EffectRegistry.cs`

### 5.2 第二阶段：效果配置JSON化

**目标：** 将所有效果配置迁移到JSON

**任务：**
1. 创建 `effects.json` 效果定义文件
2. 创建 `buffs.json` Buff定义文件
3. 创建 `debuffs.json` Debuff定义文件
4. 创建 `EffectConfigLoader.cs` 加载器
5. 修改 `EffectRegistry` 支持JSON加载

**交付物：**
- `Data/effects.json`
- `Data/buffs.json`
- `Data/debuffs.json`
- `Scripts/Effect/EffectConfigLoader.cs`

### 5.3 第三阶段：技能容器改造

**目标：** 改造现有技能容器使用新效果系统

**任务：**
1. 改造 `Card` 类支持效果列表
2. 改造 `UltimateSkill` 类支持效果列表
3. 改造 `Artifact` 类支持效果列表
4. 改造 `KeyOrder` 类支持效果列表
5. 更新 `CardEffectResolver` 使用新系统

**交付物：**
- 修改后的 `Card.cs`
- 修改后的 `UltimateSkill.cs`
- 修改后的 `Artifact.cs`
- 修改后的 `KeyOrder.cs`

### 5.4 第四阶段：Buff/Debuff系统重构

**目标：** 统一Buff/Debuff与效果系统

**任务：**
1. 修改 `StatusEffect` 基类继承 `Effect`
2. 改造现有Buff类使用新系统
3. 改造现有Debuff类使用新系统
4. 统一Buff/Debuff触发机制

**交付物：**
- 修改后的 `StatusEffect.cs`
- 修改后的 `Buff.cs`
- 修改后的所有Buff/Debuff子类

### 5.5 第五阶段：数据迁移

**目标：** 迁移现有数据到新JSON格式

**任务：**
1. 导出现有卡牌效果到 `effects.json`
2. 导出现有Buff/Debuff配置到JSON
3. 导出造物效果到 `artifacts.json`
4. 导出银钥技能到 `key_orders.json`
5. 创建迁移脚本（可选）

**交付物：**
- 完整的 `effects.json`
- 完整的 `buffs.json`
- 完整的 `artifacts.json`
- 完整的 `key_orders.json`

### 5.6 第六阶段：测试与优化

**目标：** 确保系统稳定运行

**任务：**
1. 单元测试效果解析器
2. 集成测试完整流程
3. 性能优化
4. 文档完善

---

## 6. 关键设计决策

### 6.1 为什么使用类继承而非接口？

考虑到 Godot 的 Resource 系统，使用类继承更方便：
- Resource 可以被序列化和保存
- 子类可以通过 `[Export]` 自动暴露属性到 Inspector
- 使用 `Duplicate()` 方法可以轻松克隆效果实例

### 6.2 为什么使用 EffectContext 而非参数列表？

效果执行时需要访问多种上下文信息：
- 玩家状态
- 敌人列表
- 当前触发的卡牌
- 触发类型
- 元数据（如伤害值、治疗值等）

使用上下文对象可以：
- 避免方法签名膨胀
- 便于添加新的上下文信息
- 提高代码可读性

### 6.3 为什么保留硬编码的 Buff/Debuff 类？

原因：
1. Buff/Debuff 有复杂的生命周期管理（持续时间、叠加、移除）
2. 需要访问宿主对象（Enemy/Player）进行属性修改
3. 部分Buff有特殊逻辑（如Fury的HP百分比判断）

解决方案：
- Buff/Debuff 继承自 `Effect`
- 通过 `EffectRegistry` 创建实例
- 效果参数从JSON配置，逻辑仍由类处理

### 6.4 如何处理复合效果？

对于复杂效果（如"造成伤害并附加Buff"）：
1. 在JSON中使用 `composite` 类型
2. `composite` 类型包含多个子效果列表
3. `EffectResolver` 依次执行每个子效果

---

## 7. 兼容性考虑

### 7.1 向后兼容性

- 保留现有的 `Card.Effect` 委托作为后备
- 现有的 `StatusEffect` 子类继续工作
- 现有的 `UltimateSkill.OnExecute` 委托保留

### 7.2 迁移策略

1. **渐进式迁移** - 可以逐步将卡牌迁移到新系统
2. **混合模式** - 新旧系统可以共存
3. **回退机制** - JSON加载失败时回退到硬编码值

---

## 8. 预期收益

### 8.1 可维护性提升

- 效果逻辑集中管理
- 新增效果类型只需添加新类
- 效果数值调整无需修改代码

### 8.2 可扩展性增强

- 易于添加新的效果类型
- 支持复合效果
- 支持条件触发效果

### 8.3 配置灵活性

- 所有数值可从JSON调整
- 易于平衡游戏数值
- 支持热更新（如果需要）

### 8.4 代码复用

- 统一的效果解析逻辑
- 减少重复代码
- 更容易测试

---

## 9. 风险与缓解

### 9.1 风险：系统复杂度增加

**缓解：**
- 分阶段实施，每个阶段有明确目标
- 提供完整的文档和示例
- 保持代码简洁，避免过度设计

### 9.2 风险：性能影响

**缓解：**
- 效果列表通常很短（1-5个效果）
- 使用对象池优化频繁创建的效果
- 必要时进行性能测试

### 9.3 风险：迁移成本

**缓解：**
- 提供向后兼容性
- 分阶段迁移，逐步验证
- 提供迁移工具辅助

---

## 10. 下一步行动

### 10.1 立即行动

1. **评审方案** - 团队评审此设计方案
2. **确定范围** - 确认第一阶段实施范围
3. **任务分配** - 分配具体开发任务

### 10.2 后续步骤

1. 根据评审意见修订方案
2. 开始第一阶段开发
3. 每周检查进度
4. 根据实际情况调整计划

---

## 附录

### A. 术语表

| 术语 | 定义 |
|-----|------|
| Effect | 效果基类，代表一个具体的游戏效果 |
| Skill Container | 技能容器，如Card、UltimateSkill等 |
| Trigger | 触发时机，如立即执行、回合开始等 |
| Effect Resolver | 效果解析器，负责执行效果 |
| Effect Registry | 效果注册表，管理所有可用效果类型 |
| Effect Context | 效果上下文，包含执行效果所需的所有信息 |
| EffectInstance | 效果实例，包含Owner、Target等运行时信息 |
| EffectApplier | 效果应用器，统一处理不同来源的Effect配置 |
| TargetSelector | 目标选择器，根据上下文解析出真正的目标单位 |

---

## 附录B：Effect系统架构总结

### B.1 Effect与Owner/Target的关系

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Effect的核心属性                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  EffectInstance                                                         │
│  ├── Template    → 效果模板（定义做什么）                               │
│  ├── Owner       → 效果归属于谁（附加在谁身上）                         │
│  ├── Target      → 效果作用于谁（通过TargetSelector解析）               │
│  ├── SourceId    → 效果来源追踪（某些效果需要知道是谁施放的）           │
│  └── SourceType  → 来源类型（Buff/Artifact/Skill/Passive）             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.2 不同来源的Effect配置规则

| 情况 | 附加在 | Effect.Owner | Effect.Target | 说明 |
|------|--------|--------------|---------------|------|
| Buff自己用 | 自己 | 自己 | Self | 回血、护盾等增益 |
| Debuff给敌人 | 敌人 | 敌人 | Self | 中毒、减速等 |
| 造物效果给敌人 | 玩家 | 玩家 | Enemy | 造物反击、追击等 |
| 敌人Debuff给玩家 | 玩家 | 玩家 | Self | 易伤、虚弱等 |
| 被动技能 | 角色 | 角色自己 | Self/Enemy | 角色固有效果 |

### B.3 EffectApplier - 效果应用器

```csharp
public enum EffectSourceType
{
    Skill,           // 主动释放的技能
    Buff,            // Buff/Debuff
    Passive,         // 被动技能
    Artifact,        // 造物
    SilverKey        // 银钥技能
}

public class EffectSourceContext
{
    public IUnit Caster { get; set; }      // 施放者
    public IUnit Target { get; set; }      // 目标
    public EffectSourceType SourceType { get; set; }
    public string SourceId { get; set; }  // 来源标识
}

public class EffectApplier
{
    public EffectInstance Apply(EffectDefinition definition, EffectSourceContext context)
    {
        var effectInstance = new EffectInstance
        {
            Template = definition,
            SourceId = context.Caster?.Id,
            SourceType = context.SourceType
        };
        
        switch (context.SourceType)
        {
            case EffectSourceType.Buff:
                ConfigureForBuff(definition, context, effectInstance);
                break;
            case EffectSourceType.Artifact:
                ConfigureForArtifact(definition, context, effectInstance);
                break;
            case EffectSourceType.Skill:
                ConfigureForSkill(definition, context, effectInstance);
                break;
            case EffectSourceType.Passive:
                ConfigureForPassive(definition, context, effectInstance);
                break;
        }
        
        EventTriggerSystem.Instance.RegisterEffect(effectInstance);
        return effectInstance;
    }
    
    private void ConfigureForBuff(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Target;           // Buff附加在目标身上
        inst.Target = TargetFaction.Self;  // 效果作用于自己
        inst.TrackSource = ctx.Caster;     // 追踪来源（用于某些特殊效果）
    }
    
    private void ConfigureForArtifact(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Caster;           // 造物属于玩家
        inst.Target = TargetFaction.Enemy; // 作用于敌人
        inst.TrackSource = null;           // 造物不需要追踪来源
    }
    
    private void ConfigureForSkill(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        if (def.IsDebuff)
        {
            inst.Owner = ctx.Target;           // Debuff附加在目标身上
            inst.Target = TargetFaction.Self;  // 效果作用于自己
        }
        else
        {
            inst.Owner = ctx.Caster;
            inst.Target = TargetFaction.Enemy;
        }
    }
    
    private void ConfigureForPassive(EffectDefinition def, EffectSourceContext ctx, EffectInstance inst)
    {
        inst.Owner = ctx.Caster;           // 被动技能属于角色自己
        inst.Target = def.DefaultTarget;   // 根据Effect定义的目标类型
    }
}
```

### B.4 EffectInstance - 效果实例

```csharp
public class EffectInstance
{
    public EffectDefinition Template { get; set; }
    
    // 效果拥有者（附加在谁身上）
    public IUnit Owner { get; set; }
    
    // 效果作用于谁（通过TargetSelector解析）
    public TargetFaction TargetFaction { get; set; }
    
    // 效果来源追踪（用于某些需要知道"是谁施放的"效果）
    public int? SourceId { get; set; }
    public EffectSourceType SourceType { get; set; }
    
    // 效果实例数据
    public int CurrentValue { get; set; }
    public int RemainingDuration { get; set; }
    public bool IsActive { get; set; } = true;
    
    // 检查是否可以触发
    public bool CanTrigger(EffectContext context)
    {
        // 基础检查：Owner匹配
        if (Owner != context.Target) return false;
        
        // 某些效果需要检查来源是否还活着
        if (SourceId.HasValue && RequiresSourceValidation())
        {
            if (!EntityManager.Instance.Exists(SourceId.Value))
                return false;
        }
        
        return true;
    }
    
    private bool RequiresSourceValidation()
    {
        // 只有某些特定效果才需要验证来源
        // 比如"反击"：如果攻击者死了，反击目标就没了
        return Template.EffectType == EffectType.Counter;
    }
}
```

### B.5 TargetSelector - 目标选择器

```csharp
public enum TargetFaction
{
    Self,      // 自己
    Enemy,     // 敌人
    Friendly,  // 友方
    Any       // 任意
}

public class TargetSelector
{
    public TargetFaction TargetFaction { get; set; } = TargetFaction.Self;
    
    public List<IUnit> Resolve(EffectContext context)
    {
        var results = new List<IUnit>();
        var owner = context.Owner;
        var source = context.Source;
        var target = context.Target;
        
        switch (TargetFaction)
        {
            case TargetFaction.Self:
                results.Add(owner);
                break;
            case TargetFaction.Enemy:
                if (source != null && FactionHelper.IsEnemy(owner, source))
                    results.Add(source);
                else if (target != null && FactionHelper.IsEnemy(owner, target))
                    results.Add(target);
                break;
            case TargetFaction.Friendly:
                if (source != null && FactionHelper.IsFriendly(owner, source))
                    results.Add(source);
                else if (target != null && FactionHelper.IsFriendly(owner, target))
                    results.Add(target);
                break;
        }
        
        return results;
    }
}
```

### B.5 完整事件触发流程

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    事件触发完整流程                                       │
└─────────────────────────────────────────────────────────────────────────┘

  事件触发: 敌人A受伤

                     ▼

  EventTriggerSystem.Trigger(EventType.OnDamageAfter, context)

  context = {
      Owner = 敌人A,        ← Effect拥有者
      Source = 玩家,        ← 攻击来源
      Target = 敌人A,        ← 受伤目标
      Value = 50
  }

                     ▼

  遍历注册的Effect:

  ┌─────────────────────────────────────────────────────────────────┐
  │  Effect1: 敌人身上的debuff "易伤"                                │
  │  ─────────────────────────────────────────────────────────────   │
  │  TargetFaction = Self                                           │
  │  Resolve(context):                                              │
  │  → 返回 [敌人A] (Self = context.Owner)                          │
  │  → 对敌人A生效                                                   │
  └─────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────┐
  │  Effect2: 玩家身上的造物 "荆棘之冠"                              │
  │  ─────────────────────────────────────────────────────────────   │
  │  TargetFaction = Enemy                                          │
  │  Resolve(context):                                              │
  │  → 返回 [敌人A] (Enemy = 玩家的敌人)                            │
  │  → 对敌人A生效                                                   │
  └─────────────────────────────────────────────────────────────────┘
```

### B.6 可复用框架设计

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SkillSystemFramework (技能系统框架)                    │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                        核心层 (Core)                               │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐   │  │
│  │  │   Effect    │  │ TargetSelector │  │    EventTriggerSystem │   │  │
│  │  │   效果基类   │  │   目标选择器   │  │      事件触发系统      │   │  │
│  │  └─────────────┘  └──────────────┘  └───────────────────────┘   │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐   │  │
│  │  │EffectContext│  │ EffectApplier │  │    Buff/Debuff        │   │  │
│  │  │   上下文     │  │   效果应用器   │  │       状态效果        │   │  │
│  │  └─────────────┘  └──────────────┘  └───────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                        接口层 (Interfaces)                         │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌───────────────────────┐   │  │
│  │  │   IUnit     │  │  IFactionSys │  │    IEntityManager    │   │  │
│  │  │   单位接口   │  │   阵营系统    │  │      实体管理        │   │  │
│  │  └─────────────┘  └──────────────┘  └───────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

                           ▲ 继承/实现 ▲
                           │           │
          ┌────────────────┴───────────┴────────────────┐
          │              游戏项目接入层                   │
          │  ┌─────────────┐        ┌─────────────────┐  │
          │  │ FishEatFish │        │   其他游戏项目   │  │
          │  │  实现IUnit   │        │    实现IUnit    │  │
          │  └─────────────┘        └─────────────────┘  │
          └─────────────────────────────────────────────┘
```

### B.7 框架优势

| 优势 | 说明 |
|------|------|
| **代码复用** | Effect、TargetSelector、EventTriggerSystem 所有系统共用 |
| **数据驱动** | 技能配置全部在JSON/资源文件中，无需硬编码 |
| **易于扩展** | 新增Effect类型只需实现一个类 |
| **统一调试** | 事件触发流程统一，日志清晰 |
| **组合灵活** | 任意多个Effect组合，实现复杂技能 |

### B.8 复杂技能示例

```
【技能：暗影之触】
├── Effect1: 对目标造成30点伤害 (Immediate)
├── Effect2: 施加"虚弱"debuff (添加buff)
│   ├── Duration: 2
│   └── Effect: 受伤时额外扣10血
└── Effect3: 若击杀目标，回复20能量 (OnKill)

【造物：噬魂之刃】
├── Effect1: 攻击时额外造成15点伤害 (OnAttack)
└── Effect2: 击杀时获得攻击力+5 buff (OnKill)

【被动：龙族血脉】
├── Effect1: 回合开始恢复10%最大生命 (OnTurnStart)
├── Effect2: 受到火属性伤害减半 (OnDamageReceived)
└── Effect3: 生命值低于30%时，伤害+50% (条件触发)
```

### B. 参考文件

- 当前 `Card.cs` 实现
- 当前 `StatusEffect.cs` 实现
- 当前 `CardEffectResolver.cs` 实现
- 当前 `UltimateSkill.cs` 实现
- 当前 `Artifact.cs` 实现
- 当前 `KeyOrder.cs` 实现
