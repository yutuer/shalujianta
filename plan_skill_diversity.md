# 角色定位与攻击者系统

## 一、系统概述

攻击者系统是FishEatFish战斗系统的核心抽象层。它将4名单独的角色整合为一个统一的战斗实体，所有战斗计算、状态管理和交互都以"攻击者"为单位进行。

### 1.1 核心设计原则

#### 整体性原则
- 4名角色在战斗开始时**合并为一个整体**
- 不存在单个角色的独立血量或死亡状态
- 攻击者的生存状态代表整个队伍的生存状态

#### 属性聚合原则
攻击者的核心属性由4名角色的对应属性**求和计算**：
```
Attacker.MaxHealth = Σ(角色i.Constitution × 10)
Attacker.Attack = Σ(角色i.Attack)
Attacker.Defense = Σ(角色i.Defense)
Attacker.DeathResistance = Σ(角色i.DeathResistance)
```

## 二、属性系统

### 2.1 聚合属性（攻击者整体）
| 属性 | 计算 | 说明 |
|------|------|------|
| **MaxHealth** | Σ(角色i.Constitution × 10) | 4人总血量 |
| **CurrentHealth** | 攻击者当前血量 | 整体存活状态 |
| **Attack** | Σ(角色i.Attack) | 4人总攻击力（受Buff/Debuff影响） |
| **Defense** | Σ(角色i.Defense) | 4人总防御力 |
| **DeathResistance** | Σ(角色i.DeathResistance) | 整体死亡抵抗 |

### 2.2 独立属性（出牌角色自有）
| 属性 | 来源 | 说明 |
|------|------|------|
| **CritRate** | 当前出牌角色 | 使用该角色的暴击率 |
| **CritDamage** | 当前出牌角色 | 使用该角色的暴击伤害 |
| **SilverKeyCharge** | 当前出牌角色 | 使用该角色的银钥充能 |
| **MaxRage** | 统一100 | 所有角色相同 |
| **RageReturn** | 当前出牌角色 | 使用该角色的怒气回冲 |

### 2.3 Buff/Debuff系统
```
┌─────────────────────────────────────────────────────────┐
│                    攻击者 (Buff/Debuff持有者)              │
│  ┌─────────────────────────────────────────────────┐  │
│  │ Buff列表: 力量+, 护盾+, 回复+                      │  │
│  │ Debuff列表: 虚弱-, 易伤-, 中毒-                    │  │
│  └─────────────────────────────────────────────────┘  │
│                           │                              │
│                           │ 影响所有角色行动               │
│         ┌─────────────────┼─────────────────┐             │
│         ▼                 ▼                 ▼             │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐       │
│   │ 角色1出卡 │    │ 角色2出卡 │    │ 角色3出卡 │       │
│   │ 受到:    │    │ 受到:    │    │ 受到:    │       │
│   │ +力量   │    │ +力量   │    │ +力量   │       │
│   │ -虚弱   │    │ -虚弱   │    │ -虚弱   │       │
│   └──────────┘    └──────────┘    └──────────┘       │
└─────────────────────────────────────────────────────────┘
```

## 三、战斗流程

### 3.1 回合开始
```
玩家回合:
  ↓
攻击者开始回合
  ↓
显示: 攻击者血量(整体) + 4张手牌(各角色)
  ↓
玩家选择出牌
```

### 3.2 出牌计算
```
    ┌─────────────────────────────┐
    │ 假设出 [角色2的卡牌]         │
    │                             │
    │ 1. 角色属性(自有):          │
    │    - CritRate = 角色2的15%  │
    │    - CritDamage = 角色2的175%│
    │                             │
    │ 2. 攻击者Buff/Debuff(全局): │
    │    - Buff: 力量(+3攻击)     │
    │    - Debuff: 虚弱(-3攻击)   │
    │                             │
    │ 3. 最终伤害计算:            │
    │    Base = 角色2.Attack + 卡牌.Damage │
    │    Buff加成 = +3            │
    │    Debuff减免 = -3          │
    │    Final = Base ± Buff/Debuff│
    │                             │
    │ 4. 暴击判定(用角色2属性):    │
    │    Random < 15% ? 暴击 : 正常│
    │    暴击伤害 = Final × 175% │
    └─────────────────────────────┘
```

### 3.3 伤害计算公式
```
最终伤害 = (出牌角色.Attack + 卡牌.Damage + Buff加成 - Debuff减免) × 暴击倍数

其中:
- 暴击倍数 = 1 (正常) 或 出牌角色.CritDamage/100 (暴击)
- 暴击判定 = Random(1-100) < 出牌角色.CritRate
```

## 四、角色定位系统

### 4.1 定位枚举 (CharacterArchetype)
| 定位 | 关键词 | 技能风格 |
|------|--------|---------|
| striker | 连击、追击、爆发 | 连续攻击、追击伤害 |
| counter | 反击、格挡、反弹 | 受伤反击、护盾 |
| poison | 中毒、腐蚀、持续 | DOT伤害、叠加 |
| healer | 回复、净化、护盾 | 治疗、增益 |
| debuffer | 减益、控制、弱化 | 施加debuff |
| tank | 守护、嘲讽、吸收 | 高护盾、反击 |
| mage | 范围、元素、爆发 | AOE、元素伤害 |

### 4.2 种族枚举 (CharacterRace)
| 种族 | 说明 |
|------|------|
| Chaos | 混沌之域 |
| Abyss | 深海之遗 |
| Flesh | 血肉之沼 |
| Hyperdimension | 超维之旅 |

### 4.3 角色定位分配

| 角色 | 定位 | 种族 |
|------|------|------|
| 鼠 | 追击者 | 混沌 |
| 牛 | 坦克 | 血肉 |
| 虎 | 追击者 | 混沌 |
| 兔 | 追击者 | 超维 |
| 龙 | 法师 | 深海 |
| 蛇 | 毒师 | 深海 |
| 马 | 追击者 | 超维 |
| 羊 | 治疗者 | 血肉 |
| 猴 | 削弱者 | 混沌 |
| 鸡 | 追击者 | 超维 |
| 狗 | 坦克 | 血肉 |
| 猪 | 反击者 | 血肉 |

## 五、属性归属总结

| 属性类别 | 聚合属性 | 独立属性 |
|---------|---------|---------|
| **生命值** | ✅ Attacker.CurrentHealth | ❌ |
| **攻击力** | ❌ | ✅ 出牌角色.BaseAttack + Buff/Debuff |
| **防御力** | ✅ Attacker.Defense (敌人伤害计算用) | ❌ |
| **暴击率** | ❌ | ✅ 出牌角色.CritRate |
| **暴击伤害** | ❌ | ✅ 出牌角色.CritDamage |
| **死亡抵抗** | ✅ Attacker.DeathResistance | ❌ |
| **Buff/Debuff** | ✅ Attacker持有，影响所有角色 | ❌ |
| **银钥充能** | ❌ | ✅ 出牌角色.SilverKeyCharge |
| **怒气回冲** | ❌ | ✅ 出牌角色.RageReturn |
| **怒气上限** | 统一100 | ❌ |

## 六、死亡抵抗机制

```
触发条件: Attacker.CurrentHealth <= 0
判定公式: Random(1-100) <= Σ(DeathResistance)
复活效果: Attacker.CurrentHealth = 1
衰减效果: DeathResistance = Σ(DeathResistance) / 2
持续性: 直到当前地图结束
```

## 七、设计优势

| 优势 | 说明 |
|-----|------|
| 策略性 | 选卡=选暴击属性+Buff效果 |
| 团队感 | Buff是共享的，需要配合 |
| 简化逻辑 | Buff在攻击者层，无需同步到4个角色 |
| 视觉清晰 | UI只显示攻击者的Buff/Debuff |
| 角色特色 | 每张卡对应角色自己的暴击属性 |

## 八、实现文件

### 核心类
- `Scripts/Battle/CharacterSystem/CharacterRace.cs` - 种族枚举
- `Scripts/Battle/CharacterSystem/CharacterArchetype.cs` - 角色定位枚举
- `Scripts/Battle/CharacterSystem/CharacterAttributes.cs` - 属性定义
- `Scripts/Battle/CharacterSystem/CharacterDefinition.cs` - 角色定义
- `Scripts/Battle/CharacterSystem/DeathResistanceSystem.cs` - 死亡抵抗系统
- `Scripts/Battle/Core/Player.cs` - 玩家战斗单位
- `Scripts/Battle/Card/Card.cs` - 卡牌定义（含Style字段）

### 待实现
- 种族特性系统（混沌/深海/血肉/超维的特殊效果）
- 队伍验证系统（4人组队规则）
