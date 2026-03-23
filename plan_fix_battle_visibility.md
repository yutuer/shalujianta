# 战斗场景手牌和敌人不可见问题修复计划

## 问题分析

经过代码审查，发现以下**根本原因**导致进入战斗场景后看不见手牌和敌人：

### 问题1: EnemyUI.cs引用不存在的节点
- **位置**: `Scripts/UI/EnemyUI.cs` 第28行
- **问题**: `GetNode<TextureRect>("VBoxContainer/IntentIcon")` - 节点不存在于Enemy.tscn中
- **影响**: 导致EnemyUI._Ready()抛出异常，EnemyUI无法正常工作

### 问题2: BattleManager.cs引用不存在的UI节点
- **位置**: `Scripts/BattleManager.cs` 第147-148行
- **问题1**: `GetNode<Label>("UI/TopBar/SilverKeyLabel")` - 节点不存在于BattleScene.tscn中
- **问题2**: `GetNode<Button>("UI/BottomBar/KeyOrderButton")` - 节点不存在于BattleScene.tscn中
- **影响**: 导致BattleManager._Ready()抛出异常，战斗系统无法初始化

### 问题3: 可能存在更多未检查的依赖
- CardUI.cs可能存在类似问题
- ResourceManager加载失败时的错误处理不足

## 修复计划

### Phase 1: 修复EnemyUI.cs
- [ ] 移除或注释掉对不存在的`IntentIcon`节点的引用
- [ ] 添加null检查以防止类似问题

### Phase 2: 修复BattleScene.tscn
- [ ] 在`UI/TopBar`下添加`SilverKeyLabel`节点
- [ ] 在`UI/BottomBar`下添加`KeyOrderButton`节点
- [ ] 添加IntentIcon到Enemy.tscn的VBoxContainer中（可选）

### Phase 3: 修复BattleManager.cs
- [ ] 添加null检查，防止GetNode失败导致程序崩溃
- [ ] 添加调试日志，追踪初始化过程
- [ ] 确保ResourceManager加载失败时有fallback处理

### Phase 4: 测试验证
- [ ] 编译项目，确保无编译错误
- [ ] 运行游戏，验证手牌显示
- [ ] 验证敌人显示
- [ ] 验证钥令按钮和银钥标签功能

## 预期结果

修复后应实现：
1. ✅ 敌人正确显示在EnemyZone中
2. ✅ 手牌正确显示在HandArea中
3. ✅ 银钥标签可见且更新
4. ✅ 钥令按钮可见且可用
5. ✅ 无运行时错误或警告
