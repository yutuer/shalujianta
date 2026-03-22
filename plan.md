# 战斗逻辑优化计划

## 当前问题分析

### 1. BattleManager.cs 过于臃肿 (~550行)
- 承担了太多职责：UI管理、卡牌管理、敌人管理、战斗流程控制
- 难以扩展和维护

### 2. 敌人AI极度简单
- 只是盲目攻击，没有任何策略
- 没有血量判断、威胁评估、行动选择

### 3. 卡牌效果系统简陋
- 仅支持固定伤害/护盾/治疗/能量
- 没有状态效果(buff/debuff)
- 没有条件触发效果
- 没有连击/组合机制

### 4. 玩家和敌人数据类过于简单
- 缺乏状态效果管理
- 缺乏战斗状态追踪

---

## 优化方案

### Phase 1: 架构重构
1. **分离BattleManager职责**
   - 创建 `TurnManager.cs` - 管理回合流程
   - 创建 `CombatCalculator.cs` - 伤害计算公式
   - 创建 `CardEffectResolver.cs` - 卡牌效果解析

2. **创建状态效果系统**
   - 创建 `StatusEffect.cs` - 状态效果基类
   - 创建 `Buff.cs` / `Debuff.cs` - 增益/减益效果
   - 在 Player/Enemy 中添加状态效果列表

### Phase 2: 敌人AI增强
1. **创建AI策略接口**
   - `IEnemyAI.cs` - 敌人AI接口
   - `AggressiveAI.cs` - 激进型AI(优先攻击)
   - `DefensiveAI.cs` - 防守型AI(优先叠甲)
   - `BalancedAI.cs` - 平衡型AI

2. **AI决策优化**
   - 根据血量选择攻击目标
   - 根据玩家护盾决定是否攻击
   - 添加预判机制

### Phase 3: 卡牌效果系统增强
1. **扩展Card类**
   - 添加条件效果字段
   - 添加持续效果字段
   - 添加触发效果

2. **添加新效果类型**
   - 护盾转移
   - 伤害反弹
   - 抽牌效果
   - 能量消耗降低

### Phase 4: 代码清理
1. 重构 BattleManager 为更小的类
2. 添加适当的接口和抽象
3. 整理代码结构和命名

---

## 文件结构

```
Scripts/
├── Battle/
│   ├── BattleManager.cs      (重构为协调者)
│   ├── TurnManager.cs       (新增)
│   ├── CombatCalculator.cs   (新增)
│   └── CardEffectResolver.cs (新增)
├── Entity/
│   ├── Player.cs            (添加状态效果支持)
│   └── Enemy.cs             (添加状态效果和AI)
├── Status/
│   ├── StatusEffect.cs      (新增)
│   ├── Buff.cs              (新增)
│   └── Debuff.cs            (新增)
├── AI/
│   ├── IEnemyAI.cs          (新增)
│   ├── AggressiveAI.cs      (新增)
│   ├── DefensiveAI.cs       (新增)
│   └── BalancedAI.cs         (新增)
├── Card/
│   ├── Card.cs              (扩展)
│   └── CardData.cs          (扩展)
```

---

## 优先级

1. **高优先级**: 敌人AI增强、伤害计算分离
2. **中优先级**: 状态效果系统、卡牌效果扩展
3. **低优先级**: 完整重构BattleManager
