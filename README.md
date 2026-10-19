# FishEatFish（千夜）

> 回合制卡牌 roguelike（《杀戮尖塔》玩法参考）。**Godot 4.6.1 + C#**。
> 四名角色组队 → 抽卡出牌 → 探索六边形地图 → 遭遇敌人 / 商店 / 事件 → 带回奖励再出发。

![主菜单](开始示意图.png)
![游玩效果](游玩效果图.png)

| | |
|---|---|
| 引擎 | Godot 4.6.1-stable **mono**（.NET 8.0 / `net8.0`） |
| 仓库 | <https://github.com/yutuer/shalujianta.git>（分支 `main`） |
| 规模 | 114 个 C# 脚本 / 18,104 行 · 30 个 `.tscn` · 10 张 JSON 数据表 |
| 原始需求 | [`docs/原始策划案.md`](docs/原始策划案.md) |

---

## 📖 给后来者：按这个顺序读

| # | 文件 | 什么时候看 |
| :-: |---|---|
| 1 | **本文** | 先扫一遍：现状、坑、下一步 |
| 2 | [`.trae/rules/project_rules.md`](.trae/rules/project_rules.md) | **动手前必读**——写代码的铁律 |
| 3 | [`tasks.md`](tasks.md) | 87 行任务清单，带依赖关系与状态 |
| 4 | [`GameDesign.md`](GameDesign.md) | 完整策划案（18 章 + ✅/🔄/❌ 标记） |
| 5 | [`CODE_WIKI.md`](CODE_WIKI.md) | 技术档案（自动生成，**已滞后，以源码为准**） |
| 6 | [`checklist.md`](checklist.md) | 12 大类验收清单 |

> ⚠️ **文档会失同步。** 本项目历史上至少 4 处文档与代码脱节（见 §6.1）。**源码是唯一真相**，任何文档与代码冲突时以源码为准，并顺手把文档修掉。

---

## 1. 快速上手

### 1.1 环境

| 依赖 | 版本 | 备注 |
|---|---|---|
| Godot | 4.6.1-stable **mono** | 必须带 .NET 版 |
| .NET SDK | 8.0+ | 本机实测 10.0.201 可用（`net8.0` 目标框架） |
| 显卡 | D3D12 | `project.godot` 已锁定 `rendering_device/driver.windows="d3d12"` |

> 🖥️ **本机 Godot 编辑器不在 PATH**，常见目录也搜不到。所以：
> - **编译** → 用 `dotnet build`（不依赖 Godot 可执行文件）
> - **跑起来看** → 用 `godot-runtime` MCP 的 `run_project` / `take_screenshot`
> - **改场景/布局** → 需要编辑器，目前只能手工编辑 `.tscn` 文本

### 1.2 构建

```powershell
cd E:\trae_Projects\FishEatFish
dotnet build          # 输出到 .godot/mono/temp/bin/Debug/
```

### 1.3 运行（MCP 方式）

```powershell
# 通过 godot-runtime MCP 启动（会自动构建并注入 MCP 桥）
run_project(projectPath="E:/trae_Projects/FishEatFish")
take_screenshot()
```

导出配置见 `export_presets.cfg`（Windows / x86_64）。

---

## 2. 目录地图

```
FishEatFish/
├─ Data/            10 张 JSON 数据表（卡牌/角色/刻印/造物/buff/效果/大招/敌人波次…）
├─ Scenes/          30 个 .tscn
│  ├─ MainMenu / LevelMap / CharacterSelection / BattleScene / HexMapScene
│  └─ UI/           24 个 UI 组件场景
├─ Scripts/         114 个 .cs，按职责分层
│  ├─ Scenes/       场景驱动（BattleManager 1007 行是核心）
│  ├─ Battle/       战斗核心
│  │  ├─ Core/          Player / Enemy / IUnit / 敌人波次加载
│  │  ├─ Card/          卡牌数据、等级、描述生成
│  │  ├─ CharacterSystem/ 12 生肖定义、Attacker、终极技能
│  │  ├─ Effects/       效果注册表、效果实现、Buff/Debuff
│  │  ├─ HexMap/        坐标、格子、地图、生成器、事件
│  │  ├─ MapSystem/     MapManager / 地图定义 / 奖励
│  │  ├─ ArtifactSystem/ EngravingSystem/ SilverKeySystem/
│  │  └─ CardLayoutManager / RageManager / BattleTransitionManager …
│  ├─ AI/           敌人 AI（激进/均衡/防守）
│  ├─ Shop/         黑印商店
│  └─ UI/           20+ UI 组件脚本
├─ docs/            专项技术文档（4 篇）+ 原始策划案
├─ .trae/           Trae 的项目规则与历史计划
└─ project.godot    工程配置（含 [autoload]）
```

---

## 3. 架构要点

### 3.1 场景流转

```
MainMenu
  └→ LevelMap（选关卡）
       └→ CharacterSelection（选 4 角色 + 1 钥令）
            ├→ HexMapScene（六边形地图探索）⇄ BattleScene（战斗）
            └→ BattleScene（直接进战斗，测试用）
```

跨场景数据靠两套机制：

- **`GlobalData`**（`Scripts/Scenes/LevelMap.cs` 末尾的静态类）— 已选角色、钥令、当前地图
- **autoload**（见下）— 需要跨场景存活的运行时状态

### 3.2 autoload（跨场景存活，`project.godot`）

| 名称 | 脚本 | 职责 |
|---|---|---|
| `MapManager` | `Scripts/Battle/MapSystem/MapManager.cs` | 当前关卡、进度、造物数量上限 |
| `BattleTransitionManager` | `Scripts/Battle/BattleTransitionManager.cs` | 地图↔战斗切换、**地图存档**（地图对象/位置/生命/黑印）、敌人波次键 |
| `BlackMarkShopManager` | `Scripts/Shop/BlackMarkShopManager.cs` | 黑印余额、商店物品、已购造物、已刻印卡牌 |

> 这三个**必须是 autoload**。历史上它们分别是场景节点 / 子节点，切场景就销毁，导致黑印清零、地图存档丢失。

### 3.3 六边形地图坐标（唯一权威常量）

写在 `.trae/rules/project_rules.md`，实现在 `HexMapUI.HexToWorld/WorldToHex` 与 `HexTileView.UpdateHexShape`：

```
尺寸      180 × 156（Control），多边形本体 x∈[15,165] y∈[0,156]
顶点      V0(90,0) V1(165,39) V2(165,117) V3(90,156) V4(15,117) V5(15,39)
转换      x = 150*Q + 75*R     y = 117*R
反解      R = round(y/117)     Q = round((x - 75*R)/150)
边↔方向   a↔North  b↔NorthWest  c↔SouthWest  d↔South  e↔SouthEast  f↔NorthEast
```

> `docs/六边形地图算法.md` 曾长期停留在旧的 120×104 尺寸，**已于 2026-10-20 对齐**。改尺寸要同步改：规则文件 + `HexTileView.UpdateHexShape()` + `HexMapUI` 两个转换函数 + 该文档，**四处**。

### 3.4 敌人波次配置

`Data/enemies.json` → `Scripts/Battle/Core/EnemyConfigLoader.cs` → `BattleManager.InitializeEnemies()`

| 波次键 | 来源 | 敌人 |
|---|---|---|
| `normal_wave` | 普通战斗格子 | 守卫 / 游兵 / 斥候 |
| `elite_wave` | 精英战斗格子 | 精锐·虎 / 精锐·蛇 |
| `boss_wave` | 终点 BOSS 格子 | 千夜之主 |

格子上的 `EnemyConfig` 字段 → `BattleTransitionManager.CurrentEnemyConfig` → 战斗场景按此加载。**新增敌人只改 JSON，不改代码。**

---

## 4. 开发铁律（摘要，完整版见规则文件）

1. **逻辑与页面分离** —— 布局/尺寸/颜色进 `.tscn`，业务逻辑/动态数据/事件绑定进 `.cs`
2. **UI 优先建独立 `.tscn`**，禁止在代码里硬编码 `PanelContainer` 之类的布局
3. **事件回调首尾必须有日志**：
   ```csharp
   GD.Print($"[类名] 方法名 called");
   // ... 逻辑 ...
   GD.Print($"[类名] 方法名 completed");
   ```
4. **改完代码必须 `dotnet build` 通过**再让用户测
5. **六边形坐标常量见 §3.3**

---

## 5. 当前进度基线

> 统计于 **2026-10-20**，口径：按 `tasks.md` 一~五节任务行逐行实算（共 **87** 行）。

| 分类 | 总数 | ✅ | 🔄 | ⬜ |
|---|:-:|:-:|:-:|:-:|
| 高优先级 | 26 | 4 | 5 | 17 |
| 中优先级 | 15 | 0 | 1 | 14 |
| 低优先级 | 13 | 0 | 0 | 13 |
| 商店系统（已完成） | 28 | 26 | 1 | 1 |
| Bug 修复 | 5 | 3 | 2 | 0 |
| **合计** | **87** | **33** | **9** | **45** |

**已跑通的主循环**：主菜单 → 选图 → 选角色 → 六边形地图 → 踩战斗格子 → 按波次配置生成敌人 → 战斗结束 → **回到地图且地图/位置/生命/黑印完整恢复**（24 项集成断言通过）。

**尚未打通的**：
- 刻印效果在出牌时**实际触发**
- 大招 / 钥令的**实际战斗效果**
- 战斗胜利后的**奖励选择界面**
- Buff / Debuff 的 UI 呈现
- **BOSS 战斗**（配置已备好，但触发路径被 `ProcessTile` 提前 return 挡掉）

---

## 6. 踩过的坑 / 经验

### 6.1 文档与代码会失同步（本项目最大的坑）

真实发生过的失真，**改任何状态类表格都要同步**：

| 文档 | 失真表现 |
|---|---|
| `tasks.md` 统计表 | 自称 61 任务 / 23 完成，实际 87 行 / 33 完成（写完商店那轮 Sprint 后没回头更新） |
| `CODE_WIKI.md` | 说 `GlobalData` 是独立文件，实际在 `LevelMap.cs` 末尾；`HexEventType` 首项写 `None` 实际是 `Empty` |
| `docs/六边形地图算法.md` | 尺寸停留在 120×104 旧几何，与代码差了一整代 |
| `GameDesign.md` | 第 14/15 章子节编号撞号（都用 14.x），`14.14 验收标准` 出现两次 |

**教训**：凡是"统计/状态/进度"类表格，只要任务行变了就得跟着改；文档与代码冲突时以源码为准，并顺手把文档修掉。

### 6.2 `.tscn` 是布局的唯一真相

`SilverKeyProgressIndicator.cs` 找 `ValueLabel` / `MaxLabel`，但 `.tscn` 里它们嵌在 `VBoxContainer` 下 → 每次进地图报 2 次 `Node not found`，而且**银钥圈里的数字从来没显示过**。
`HexMapUI.cs` 连 `SceneTree.screen_resized`——**Godot 4 已移除该信号**，应为 `Viewport.SizeChanged`。

**教训**：`GetNode` 路径必须和 `.tscn` 实际层级一致；用 `GetNodeOrNull` 让缺失降级为警告而不是错误堆栈。跨 Godot 版本迁移时重点查信号名。

### 6.3 C# ↔ GDScript 的桥接限制（调试时会撞墙）

- C# 属性不加 `[Export]` → **GDScript 读不到**（`get("CurrentMap")` 返回 null）
- C# 的 public 方法 → **GDScript 也调不到**（`call("GetSavedMap")` 报 `Nonexistent function`）
- `.tscn` 里 Control 的位置存的是 `offset_left/offset_top`，不是 `position`

**教训**：跨语言调试要么写 C# 侧代码，要么改用 Godot 原生 API（Label.text、Control.mouse_filter、Node.is_processing_input 等）去观察。

### 6.4 无头 / 后台窗口里模拟点击不可靠

godot-runtime MCP 的 `run_project(background: true)` 把窗口放在**屏幕外 `(-9999,-9999)`**，`Control.GetLocalMousePosition()` 读的是**真实 OS 光标**位置，合成事件改不了它 → 任何"点击命中某控件"的代码路径都测不到。
`HexTileView` 正是用 `GetLocalMousePosition()` 判断点击是否落在格子内。

**可行的替代**：写一个**临时 C# 集成测试**（临时注册成 autoload 以跨场景存活），由 C# 直接调游戏自己的 API 驱动流程，断言结果写进 `user://` 文件；测完把 autoload 注册和测试文件一并删掉。2026-10-20 用这招跑通了 24 项断言。

### 6.5 `git push` 会真的上传大文件

push 传输的是「**远端缺失的对象**」，不是增量 diff。若某个待推送提交引入了大文件而远端还没有，push 会真把那些 blob 传上去（实测 2.4GB 传了 4 分钟还没完），**哪怕下一个提交就删了它们**。

**正确做法**：先改写**从未推送过的**本地历史再推：

```powershell
git filter-repo --force --path <路径> --invert-paths --refs <远端SHA>..HEAD
```

只改写远端没有的提交，**不影响已推送历史，无需 force push**。
配套：改写后要清掉 IDE 留的记账引用（Trae 的 `refs/trae_merge/*`，用 `git update-ref -d <ref>`），再 `git reflog expire --expire=now --all` + `git gc --prune=now` 才会真正回收磁盘空间。

### 6.6 工具链在 Windows 上的坑

- `winget install GitHub.cli` → `0x8007029c`（安装器断言失败）
- 官方 MSI 静默安装 → `msiexec` 返回 **1603**
- ✅ **便携版 ZIP 直接解压即可**，放到 `C:\Users\yutue\Tools\gh\bin` 并加进用户 PATH
- `git-filter-repo` 用 `pip install`（脚本装到 `%APPDATA%\Python\Python314\Scripts`，同样要加 PATH）
- **PowerShell 里 `\"` 不能转义双引号**（要用反引号），`git commit -m "含 \" 引号\""` 会被拆成多个参数 → **多行提交信息一律用 `git commit -F 文件`**

### 6.7 Godot 细节备忘

- JSON 里的整数经 `Godot.Collections.Dictionary` 取出是 **long**，要 `(int)(long)` 或按 `Variant.VariantType` 判断
- `Control` 子节点的 `mouse_filter` 默认是 `Stop`，会吃掉点击；`Label` 默认 `Ignore`
- 地图格子颜色直接编码事件类型（`HexTileView.EventColors`），可用来在无头环境下识别格子

---

## 7. 下一步做什么

### P0 · 需要先决策

1. **BOSS 触发规则** —— 终点格子目前直接 `CompleteMap()`，永远打不到 BOSS。要「到终点先打 BOSS」还是「到终点即通关」？
   涉及：`Scripts/Battle/HexMap/HexMapController.cs` 的 `ProcessTile()`
2. **地图血量与战斗血量打通** —— 现在是两套独立系统，战斗掉的血不会带回地图
   涉及：`HexMapController.PlayerCurrentHealth` ↔ `BattleManager.player.CurrentHealth`

### P0 · 战斗闭环补全（`tasks.md` 高优先级）

| 任务 | 现状 | 入口 |
|---|---|---|
| 战斗奖励选择界面（1.3） | 0/6 | 需新建 `BattleRewardUI.tscn` + `BattleRewardManager` |
| 刻印效果实际触发（1.2） | 1/5 | `Scripts/Battle/Effects/EffectResolver.cs` |
| 大招实际效果（1.4） | 1/5 | `Scripts/Battle/CharacterSystem/UltimateSkill.cs` |
| 钥令实际效果（1.5） | 1/4 | `Scripts/Battle/SilverKeySystem/KeyOrderManager.cs` |
| Buff/Debuff UI（1.1） | 1/6 | `Scripts/Battle/BuffDebuffDisplay.cs` |

### P1 · 体验与工程

- **点击远距离格子移动**：现在悬停会高亮整条路径，但点击只接受相邻格子（`HexMapUI.OnTileClicked`）
- **存档持久化到磁盘**：所有跨场景状态目前只在内存，重启即丢
- **造物装备界面 / 金币系统**（`tasks.md` 中优先级，2.1 / 2.2）
- **`.trae/plans/hexmapui-refactor-plan.md` 阶段 6**：`HexMapUI.cs` 拆分 `TopRightButtonsPanel` / `PlayerStatusPanel` / `RageCirclesPanel` / `HexMapView`（目标 802 行 → <400 行）

---

## 8. 变更记录

| 日期 | 内容 |
|---|---|
| 2026-10-20 | 文档与代码对齐：`tasks.md` 统计表实算校准、`GameDesign.md` 章节编号去撞号、`docs/六边形地图算法.md` 常量同步、补「边↔方向」对应表 |
| 2026-10-20 | 打通地图→战斗→返回地图闭环；敌人波次配置化；三个管理器改 autoload；修银钥指示器节点路径与 Godot 4 窗口尺寸信号 |
| 2026-10-20 | 仓库瘦身：打包/导出产物移出版本库（`.git` 2.4GB → 1MB，GitHub 仓库 1.1MB） |
| — | 初始需求见 [`docs/原始策划案.md`](docs/原始策划案.md) |

---

## 9. 文档索引

| 路径 | 内容 |
|---|---|
| `.trae/rules/project_rules.md` | **开发铁律**（第一准则、UI 规范、日志规范、六边形常量） |
| `tasks.md` | 87 行任务清单 + 依赖 + 状态 |
| `GameDesign.md` | 完整策划案 18 章 + 未完成功能清单 |
| `checklist.md` | 12 大类验收清单 |
| `CODE_WIKI.md` | 自动生成技术档案（**已滞后**） |
| `plan.md` | 商店 Bug 修复专项计划（历史） |
| `docs/六边形地图算法.md` | 六边形坐标权威文档 |
| `docs/手牌预览功能使用说明.md` | 手牌扇形调试面板用法 |
| `docs/修复六边形地图Bug.md` | 早期 AI 思考过程存档（归档性质） |
| `docs/战斗场景布局.md` | 残稿，未写完 |
| `docs/原始策划案.md` | 项目立项需求原文 |
| `.trae/plan_hexmap_battle_trigger.md` | 地图战斗触发系统实现计划（部分已完成，勾选框未同步） |
| `.trae/plans/hexmapui-refactor-plan.md` | HexMapUI 组件拆分计划（阶段 1-5 完成，阶段 6 待做） |
