# 战斗场景UI布局及交互功能优化计划

## 问题分析

经过代码审查，发现以下需要优化的内容：

### 当前UI布局问题
1. **银钥进度指示器** - 位置在(20, 60)，需要调整到手牌区域左侧
2. **攻击体编队** - 位置在(100, 50)，需要迁移至原玩家信息显示位置
3. **角色怒气框** - 使用ProgressBar样式，需要改为圆形进度条
4. **角色显示体** - 显示了过多信息（属性值、怒气值、等级）
5. **玩家能量** - 显示格式为"能量: X"，包含前缀文字

## 修改计划

### Phase 1: 银钥值显示位置调整
- **文件**: `Scripts/BattleManager.cs`
- **位置**: `InitializeNewUIComponents()` 方法
- **修改内容**:
  - 银钥进度指示器位置从 `new Vector2(20, 60)` 调整为 `new Vector2(50, 400)`
  - 确保与手牌区域左对齐

### Phase 2: 攻击体编队位置调整
- **文件**: `Scripts/BattleManager.cs`
- **位置**: `InitializeNewUIComponents()` 方法
- **修改内容**:
  - 攻击体编队位置从 `new Vector2(100, 50)` 调整为 `new Vector2(200, 10)`
  - 适配TopBar区域

### Phase 3: 角色怒气框显示优化
- **文件**: `Scripts/UI/AttackerDisplayUI.cs`
- **位置**: `CharacterSlotUI` 组件
- **修改内容**:
  - 将ProgressBar怒气条替换为圆形进度指示器样式
  - 在角色头像上方添加小型圆形怒气显示
  - 保持与银钥相同的视觉风格

### Phase 4: 角色显示体信息精简
- **文件**: `Scripts/UI/AttackerDisplayUI.cs`
- **位置**: `CharacterSlotUI.SetupUI()` 方法
- **修改内容**:
  - **移除**: 等级Label (_levelLabel)
  - **移除**: 属性图标 (❤️⚔️🛡️⚡)
  - **移除**: 怒气进度条 (_rageBar)
  - **移除**: 怒气Label (_rageLabel)
  - **移除**: 状态效果Label (_statusLabel)
  - **移除**: 大招就绪Label (_ultReadyLabel)
  - **保留**: 头像、名称
- **同步修改**: `SetCharacter()`, `UpdateRageStatus()` 等方法

### Phase 5: 玩家能量显示优化
- **文件**: `Scripts/UI/PlayerStatsUI.cs`
- **位置**: `ApplyStats()` 方法
- **修改内容**:
  - 能量显示从 `"能量: {playerData.CurrentEnergy}"` 改为仅显示数字 `"{playerData.CurrentEnergy}"`
  - 移除"能量:"前缀

### Phase 6: 钥令点击交互功能实现
- **文件**: `Scripts/BattleManager.cs`
- **位置**: `OnKeyOrderPressed()` 方法
- **修改内容**:

#### 逻辑分支：
```
点击钥令按钮:
  ├─ 钥令值 < 1000
  │   └─ 显示钥令说明界面（使用按钮禁用）
  │
  └─ 钥令值 ≥ 1000
      ├─ 本回合首次使用
      │   └─ 显示钥令说明界面（使用按钮启用）
      │
      └─ 本回合非首次使用
          └─ 显示提示 + 随机3个钥令选择
```

#### 新增UI组件：
- **KeyOrderDescriptionUI** - 钥令说明界面
  - 显示当前装备钥令的名称、类型、效果、消耗
  - 包含"使用"按钮和"取消"按钮
  - 按钮启用/禁用状态由外部控制

#### 代码修改：
1. 新增 `KeyOrderDescriptionUI` 类
2. 重写 `OnKeyOrderPressed()` 方法
3. 添加 `ShowKeyOrderDescription()` 方法
4. 添加 `UseEquippedKeyOrder()` 方法
5. 添加 `ShowRandomKeyOrderSelectionWithPrompt()` 方法

### Phase 7: 手牌技能显示优化
- **文件**: `Scripts/UI/CardUI.cs`
- **位置**: `ApplySetup()` 方法
- **问题分析**:
  - 特殊技能过多原因：`costLabel.Text = $"费用: {card.Cost}"` 包含"费用:"前缀
  - 卡牌可能因费用显示占位过大导致布局问题
- **修改内容**:
  - 费用显示位置保持左上角
  - 费用文本从 `"费用: {card.Cost}"` 改为仅显示 `{card.Cost}`
  - 调整costLabel样式使其更紧凑

## 实现步骤

### Step 1: 银钥进度指示器位置调整
```csharp
silverKeyProgressIndicator.Position = new Vector2(50, 400);
```

### Step 2: 攻击体编队位置调整
```csharp
attackerDisplayUI.Position = new Vector2(200, 10);
```

### Step 3: 创建简化版CharacterSlotUI
- 移除所有属性、怒气相关显示
- 仅保留头像和名称
- 调整组件大小

### Step 4: 创建KeyOrderDescriptionUI
```csharp
public partial class KeyOrderDescriptionUI : Control
{
    // 显示钥令详情
    // 包含"使用"和"取消"按钮
    // 按钮启用/禁用由参数控制
}
```

### Step 5: 重写OnKeyOrderPressed方法
```csharp
private void OnKeyOrderPressed()
{
    if (isKeyOrderSelectionOpen || !isPlayerTurn) return;

    if (currentSilverKey < 1000)
    {
        ShowKeyOrderDescription(false); // 禁用使用按钮
    }
    else if (keyOrderManager.CanUseKeyOrder())
    {
        ShowKeyOrderDescription(true); // 启用使用按钮
    }
    else
    {
        ShowPromptAndRandomSelection(); // 显示提示+随机选择
    }
}
```

### Step 6: 优化CardUI费用显示
```csharp
costLabel.Text = card.Cost.ToString();
```

### Step 7: 编译测试
```bash
dotnet build
```

## 预期效果

1. ✅ 银钥显示与手牌区域关联
2. ✅ 攻击体编队位于界面顶部
3. ✅ 角色信息简洁清晰
4. ✅ 玩家能量显示精简
5. ✅ 钥令交互逻辑完善
6. ✅ 手牌费用显示优化

## 文件清单

需要修改的文件：
- `Scripts/BattleManager.cs` - UI位置调整、钥令逻辑重写
- `Scripts/UI/AttackerDisplayUI.cs` - 简化角色显示
- `Scripts/UI/PlayerStatsUI.cs` - 能量显示优化
- `Scripts/UI/CardUI.cs` - 费用显示优化

需要创建的文件：
- `Scripts/UI/KeyOrderDescriptionUI.cs` - 钥令说明界面
