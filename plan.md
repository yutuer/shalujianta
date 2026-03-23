# BattleManager.cs 模块化优化计划

## 当前状态
已完成所有计划重构：
- [CardLayoutManager.cs](file:///e:/trae_Projects/FishEatFish/Scripts/Battle/CardLayoutManager.cs) - 已创建
- [EnemyManager.cs](file:///e:/trae_Projects/FishEatFish/Scripts/Battle/EnemyManager.cs) - 已创建
- [ResourceManager.cs](file:///e:/trae_Projects/FishEatFish/Scripts/Battle/ResourceManager.cs) - 已创建
- BattleManager.cs 从 ~740 行精简至 **519 行**

---

## 已完成的模块

### 1. EnemyManager ✅
**职责**：敌人状态查询和管理

**包含方法**：
- `GetLivingEnemies()` - 获取存活敌人列表
- `HasLivingEnemies()` - 是否有存活敌人
- `GetFrontmostEnemy()` - 获取最前方敌人
- `GetRearmostEnemy()` - 获取最后方敌人
- `GetEnemyAtPosition(int position)` - 获取指定位置敌人
- `ResolveTarget(TargetType, int)` - 解析卡牌目标

**代码行数**：~98 行

### 2. ResourceManager ✅
**职责**：游戏资源加载和管理

**包含内容**：
- `LoadScenes()` - 场景加载
- `LoadResources()` - 资源加载
- `LoadAll()` - 一键加载所有资源
- `CardTextures` - 卡牌纹理字典
- `EnemyTexture` - 敌人纹理
- 各场景引用属性

**代码行数**：~44 行

---

## 当前模块结构

```
Scripts/
├── Battle/
│   ├── BattleManager.cs      (519行) - 战斗流程协调 ✅
│   ├── CardLayoutManager.cs  (~160行) - 卡牌布局 ✅
│   ├── EnemyManager.cs       (98行)  - 敌人状态查询 ✅
│   ├── ResourceManager.cs    (44行)  - 资源加载 ✅
│   ├── EnemyIntentDisplay.cs - 敌人意图显示
│   ├── TurnManager.cs        - 回合管理
│   └── CardEffectResolver.cs - 卡牌效果解析
```

---

## 实施状态

### Step 1: 创建 EnemyManager.cs ✅
- 移动敌人查询方法
- BattleManager 持有 EnemyManager 实例

### Step 2: 创建 ResourceManager.cs ✅
- 移动资源加载方法
- BattleManager 持有 ResourceManager 实例

### Step 3: 验证编译通过 ✅
- 编译成功，无错误

---

## 可选后续优化

### CardHandManager (可选)
**职责**：手牌UI创建和管理

**移动方法**：
- `UpdateHandUI()` - 更新手牌UI
- `CreateCardUI()` - 创建单张卡牌UI
- `ClearHandUI()` - 清除手牌UI
- `PlayCardDrawAnimation()` - 抽牌动画

**注意**：此模块依赖 BattleManager 的多个状态，抽取需要传入依赖

**预估精简**：~65 行
