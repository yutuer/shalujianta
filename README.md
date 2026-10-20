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

导出配置见 `export_presets.cfg`（预设名 `Windows Desktop`，输出到 `dabao/qianye.exe`）。

### 1.4 常用命令速查

```powershell
cd E:\trae_Projects\FishEatFish
```

| 目的 | 命令 |
|---|---|
| 编译 | `dotnet build` |
| 清理后重编 | `dotnet clean; dotnet build` |
| **校验任务统计表** | `powershell -ExecutionPolicy Bypass -File tools\check-task-stats.ps1` |
| 导出 Windows 版 | `godot --headless --export-release "Windows Desktop" dabao/qianye.exe` |
| 提交（多行信息） | `git commit -F <消息文件>` ← 多行信息别用 `-m`，PowerShell 会拆参数 |
| 推送 | `git push origin main` |
| GitHub 认证状态 | `gh auth status` |
| 看 CI / PR / Issue | `gh run list` · `gh pr list` · `gh issue list` |
| 查仓库 API | `gh api repos/yutuer/shalujianta` |

> 导出产物（`dabao/`、`qianye.*`、`*.tpz`、`*.rar`）已在 `.gitignore` 中，不会误入版本库。

**跑起来看**（本机无 Godot 编辑器，用 MCP）：
`run_project(projectPath=...)` 启动 · `take_screenshot()` 截图 · `simulate_input(...)` 模拟输入 · `run_script(...)` 注入 GDScript

### 1.5 调试场景准备器（带着选人完成的状态进场景）

改完 bug 要验证时，**不必走「主菜单 → 选关卡 → 选角色 → 进地图」那一大段**。

> ⚠️ **别直接跑 `HexMapScene.tscn`** —— 那样会跳过选人流程，`GlobalData.SelectedCharacters` /
> `EquippedKeyOrder` / `CurrentMap` 全是 null，战斗会**回退到默认阵容「鼠牛虎兔」**而不是你选的队伍，
> 刻印可选卡牌列表也拿不到卡组。

**调试场景准备器**（`Scenes/Dev/DevScenario.tscn`）会先把这些全局量按真实流程补齐，再进目标场景。

```powershell
cd E:\trae_Projects\FishEatFish
dotnet build                                          # 改了 C# 必须先编译
$godot = 'K:\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe'

# 默认预设
& $godot --path 'E:\trae_Projects\FishEatFish' 'res://Scenes/Dev/DevScenario.tscn'

# 指定预设
& $godot --path 'E:\trae_Projects\FishEatFish' 'res://Scenes/Dev/DevScenario.tscn' -- --scenario=near_shop

# 预设 + 临时覆盖
& $godot --path 'E:\trae_Projects\FishEatFish' 'res://Scenes/Dev/DevScenario.tscn' -- `
    --scenario=default --team=dragon,horse,monkey,dog --key=key_shield --map=map02 --target=battle
```

**内置预设**（`Data/dev_scenarios.json`）：

| 预设 | 效果 |
|---|---|
| `default` | 选人完成（鼠牛虎兔 + 银光斩 + 第一张关卡）→ 六边形地图 |
| `battle` | 选人完成 → 直接进战斗（敌人 `normal_wave`） |
| `near_shop` | 选人完成 + 落在商店格子**旁边**（不触发商店事件） |
| `half_hp` | 选人完成 + 进地图时血量只有一半（125 → 62） |
| `elite_battle` | 选人完成 → 直接进战斗，敌人 `elite_wave` |
| `near_boss` | 选人完成 + 站在 BOSS 格子旁边（走上去即触发 BOSS 战） |
| `step_on_boss` | 选人完成 + **直接站上 BOSS 格子**（立刻触发 BOSS 战，用于验证触发链路） |
| `boss_battle` | 选人完成 → 直接打 BOSS 战（敌人 `boss_wave`） |

**命令行参数**：

| 参数 | 取值 |
|---|---|
| `--scenario=` | 上表预设名 |
| `--team=` | 12 生肖 id 逗号分隔，必须 4 个：`rat` `ox` `tiger` `rabbit` `dragon` `snake` `horse` `goat` `monkey` `rooster` `dog` `pig` |
| `--key=` | `silver_slash`→银光斩 `key_shield`→钥之护盾 `energy_infusion`→能量灌注 `armor_break`→破甲 `life_key`→生命钥 |
| `--map=` | `map01` `map02` `map03` |
| `--target=` | `hexmap`（默认）/ `battle` |

**以后要加新预设（比如"带特定装备""站在 BOSS 房门口"）**：
① 往 `Data/dev_scenarios.json` 加一条 preset；
② 若需要新的状态维度，在 `Scripts/Dev/DevScenario.cs` 的 `ApplyInSceneState()` 里加一个钩子（现有钩子：血量比例、落点、敌人波次）。

主菜单也有「调试直达地图 (默认阵容)」按钮，点它等同于跑 `default` 预设。

**其他场景**（只适合不依赖队伍的调试，如纯地图逻辑）：

| 场景 | 路径 |
|---|---|
| 六边形地图 | `res://Scenes/HexMapScene.tscn` ⚠️ 无队伍/钥令/关卡 |
| 战斗 | `res://Scenes/BattleScene.tscn` ⚠️ 同上，敌人按 `normal_wave` |
| 角色选择 | `res://Scenes/CharacterSelection.tscn` |
| 主菜单 | `res://Scenes/MainMenu.tscn` |

> 直接跑单个场景时 **autoload 照常加载**（`MapManager` / `BattleTransitionManager` / `BlackMarkShopManager`），地图存档机制可用。

**让我开** —— 我用 MCP 的 `run_project(scene="Scenes/Dev/DevScenario.tscn", background=false)`，
窗口会弹在你屏幕，**你自己验证、你自己关**，我不去 stop 它；你关掉后我还能读到运行日志。

---

## 2. 目录地图

```
FishEatFish/
├─ Data/            10 张 JSON 数据表（卡牌/角色/刻印/造物/buff/效果/大招/敌人波次…）
│                   + dev_scenarios.json —— 调试场景预设表
├─ Scenes/          30+ 个 .tscn
│  ├─ MainMenu / LevelMap / CharacterSelection / BattleScene / HexMapScene
│  ├─ Dev/           DevScenario.tscn —— 调试场景准备器入口
│  └─ UI/           24 个 UI 组件场景
├─ Scripts/         114+ 个 .cs，按职责分层
│  ├─ Scenes/       场景驱动（BattleManager 1007 行是核心）
│  ├─ Dev/          DevScenario.cs —— 调试场景准备器逻辑
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
├─ tools/           check-task-stats.ps1 —— 校验任务统计表是否漂移
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

> **本表是快照，不是活数据。** 权威来源是 [`tasks.md`](tasks.md) 的任务行；下表由脚本实算后同步。
> 统计于 **2026-10-20**，口径：按 `tasks.md` 一~五节任务行逐行实算（共 **87** 行）。

| 分类 | 总数 | ✅ | 🔄 | ⬜ |
|---|:-:|:-:|:-:|:-:|
| 高优先级 | 26 | 4 | 5 | 17 |
| 中优先级 | 15 | 0 | 1 | 14 |
| 低优先级 | 13 | 0 | 0 | 13 |
| 商店系统（已完成） | 28 | 26 | 1 | 1 |
| Bug 修复 | 5 | 3 | 2 | 0 |
| **合计** | **87** | **33** | **9** | **45** |

📌 **改完任务状态别只改表**——跑校验脚本，它一次核对三处（实算 / `tasks.md` 统计表 / 本表）：

```powershell
powershell -ExecutionPolicy Bypass -File tools\check-task-stats.ps1
```

退出码 `0` = 一致；非 `0` = 有漂移，脚本会逐条列出该改哪几个数字。
（本项目历史上 `tasks.md` 的统计表曾长期失真——自称 61/23，实际 87/33——就是因为只靠人记得同步。）

**已跑通的主循环**：主菜单 → 选图 → 选角色 → 六边形地图 → 踩战斗格子 → 按波次配置生成敌人 → 战斗结束 → **回到地图且地图/位置/生命/黑印完整恢复**（24 项集成断言通过）。

**血量是同一份**：玩家血量 = 队伍总血量（鼠牛虎兔 = 125）。地图上显示它，进战斗带着当前血量进去（`携带地图血量进入战斗: 125/125`），战斗胜利把剩余血量写回地图；战败不写回（战斗会重开，血量保持战前值）。

**已打通的**：
- 地图 → 战斗 → 返回地图（状态完整恢复）
- 敌人按波次配置加载（普通 / 精英 / BOSS 三档）
- **BOSS 战**：终点格子是 👹 BOSS，走上去即触发 BOSS 战（`boss_wave`），打赢后回到地图并结算「地图探索完成」
- 地图血量与战斗血量打通

**尚未打通的**：
- 刻印效果在出牌时**实际触发**
- 大招 / 钥令的**实际战斗效果**
- 战斗胜利后的**奖励选择界面**
- Buff / Debuff 的 UI 呈现

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

### 6.8 `.tscn` 根节点漏绑 script → 组件静默退化成空节点

2026-10-20 在战斗场景排查报错时发现：`AttackerDisplayUI.tscn`、`RageDisplayUI.tscn`、
`KeyOrderSelectionUI.tscn` **三个场景的根节点都没有 `script = ExtResource(...)`**（ext_resource 声明了脚本，但没挂到根节点上）。

后果很隐蔽：`BattleManager.LoadUIComponent<T>()` 里的 `packedScene.Instantiate<T>()` 因为类型不匹配返回 null，
于是走**代码兜底** `new T()` —— 得到一个**没有任何子节点的空 Control**。
于是运行日志里出现：

```
[BattleManager] 攻击体角色显示UI初始化成功      ← 看着像成功
ERROR: Node not found: "VBoxContainer/TeamStatsLabel"   ← 其实是空节点
```

**攻击体编队、怒气槽、钥令选择这三个 UI 实际上一直是空的**，只是没有崩所以没人发现。
修法就是在根节点补一行 `script = ExtResource("1")`。

**教训**：
- 新建 `.tscn` 时，**根节点一定要绑脚本**——只声明 ext_resource 不等于挂上了
- 「从场景加载: xxx.tscn」这条日志是组件是否真的走了场景路径的判据；缺它就说明退回了代码兜底
- 一键排查：`Scenes/**/*.tscn` 里凡是有 Script ext_resource 的，根节点都要有 `script =`

### 6.9 引用的资源必须真的存在

`CardUI.cs` / `KeyOrderSelectionUI.cs` 引用了 `sword.png` `shield.png` `star.png`，
但 `Assets/Icons/` 里根本没有这三个 png（只有 svg）。因为代码有 null 兜底，不崩，只是图标不显示 + 每次刷 "Resource file not found"。
已改指向现有资源：`intent_attack.svg`（剑）、`intent_defend.svg`（盾）、`intent_buff.svg`（增益）、`relic_charm.svg`（恢复）。

### 6.10 子节点的脚本也要挂，而且一个 .cs 只能挂一个类

接 6.8：修好根节点的 script 之后，`AttackerDisplayUI` / `RageDisplayUI` 虽然能加载了，
但它们的 **4 个槽位节点（`CharacterSlot0..3` / `RageSlot0..3`）还是空 Panel**，日志报
「未找到角色槽位节点」。原因有两层：

1. 槽位节点也没挂脚本 → `GetNode<Panel>(path) as CharacterSlotUI` 得到 null
2. **`CharacterSlotUI` / `RageSlotUI` 原本和宿主类写在同一个 .cs 文件里**
   （Godot 一个脚本文件只能对应一个类，`.tscn` 挂脚本时按文件绑定），
   必须先拆成 `Scripts/UI/CharacterSlotUI.cs` / `Scripts/UI/RageSlotUI.cs` 再挂

另外 `RageDisplayUI` 的槽位路径本身也写错了：实际是 `VBoxContainer/RageSlotsContainer/RageSlotN`，
代码写的是 `RageSlotsContainer/RageSlotN`（少一层）。修正前 UI 显示的是 .tscn 里的静态占位文字
「怒气值 0/100 0/100…」，修正后才显示「鼠 0/100 0%　牛…　虎…　兔…」+「总怒气 0」。

**教训**：新建带子组件的 UI 场景时，**每个需要脚本的节点都要单独挂脚本，且对应独立的 .cs 文件**；
写完用 `HasNode(path)` 验证过的路径去取，取不到时 Godot 只报「未找到」不崩，很容易被忽略。

### 6.11 血条初始值需要手动推一次

`HexMapUI` 只在 `OnHealthChanged` 事件里更新血条，但 `HexMapController` 比 `HexMapUI` **先**就绪，
初始那次事件早于订阅，**不会被补发** —— 所以血条一直显示 .tscn 里的默认值 100/100，
跟队伍总血量（125）毫无关系。修法是在订阅后手动推一次：

```csharp
_healthBar?.SetHealth(_controller.PlayerCurrentHealth, _controller.PlayerMaxHealth);
```

**教训**：跨节点的"初始状态同步"不会自动发生，凡是 UI 靠事件驱动的值，都要在订阅后手动同步一次。

---

## 7. 下一步做什么

### P0 · 战斗闭环补全（`tasks.md` 高优先级）

| 任务 | 现状 | 入口 |
|---|---|---|
| 战斗奖励选择界面（1.3） | 0/6 | 需新建 `BattleRewardUI.tscn` + `BattleRewardManager` |
| 刻印效果实际触发（1.2） | 1/5 | `Scripts/Battle/Effects/EffectResolver.cs` |
| 大招实际效果（1.4） | 1/5 | `Scripts/Battle/CharacterSystem/UltimateSkill.cs` |
| 钥令实际效果（1.5） | 1/4 | `Scripts/Battle/SilverKeySystem/KeyOrderManager.cs` |
| Buff/Debuff UI（1.1） | 1/6 | `Scripts/Battle/BuffDebuffDisplay.cs` |

### P1 · 体验与工程

- **战斗奖励发放**：现在打赢 BOSS 只结算「地图完成」，没有发奖（金币/造物/刻印）——与 1.3 一起做
- **点击远距离格子移动**：现在悬停会高亮整条路径，但点击只接受相邻格子（`HexMapUI.OnTileClicked`）
- **存档持久化到磁盘**：所有跨场景状态目前只在内存，重启即丢
- **造物装备界面 / 金币系统**（`tasks.md` 中优先级，2.1 / 2.2）
- **`.trae/plans/hexmapui-refactor-plan.md` 阶段 6**：`HexMapUI.cs` 拆分 `TopRightButtonsPanel` / `PlayerStatusPanel` / `RageCirclesPanel` / `HexMapView`（目标 802 行 → <400 行）

### ⚠️ 用 Agent 跑测试时的提交陷阱

`godot-runtime` MCP 启动项目时会**往 `project.godot` 的 `[autoload]` 注入一行**：

```ini
McpBridge="*res://.mcp/godot-runtime/bridge/mcp_bridge.gd"
```

测试会话运行期间 `git status` 会显示 `M project.godot`（**这是正常的**），停止/关闭项目后会自动还原。

**但如果你在会话运行期间执行 `git add .` 或 `git commit -a`**，这行会被提交进仓库 —— 而 `.mcp/` 本身是 gitignore 的，别人克隆后这条 autoload 指向不存在的脚本，**项目直接启动失败**。

**规矩**：跑测试期间只 `git add` 具体文件；确认 `project.godot` 没有意外改动再提交。

---

## 8. 变更记录

| 日期 | 内容 |
|---|---|
| 2026-10-20 | 打通 BOSS 触发（终点格子走上去即打 BOSS，打赢结算地图完成）与血量打通（队伍总血量=地图血量=战斗血量，战斗胜利写回） |
| 2026-10-20 | 修掉战斗场景 3 个 UI 组件静默失效的根因：.tscn 根节点/槽位节点漏绑 script；CharacterSlotUI / RageSlotUI 拆分为独立脚本；RageDisplayUI 槽位路径少一层 VBoxContainer/；HexMapUI 血条初始值手动推送 |
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
| `docs/开发计划.md` | **本轮迭代执行计划**：需求→开发→验证三步流程 + 11 个条目 + 参考图索引 |
| `docs/screenshots/` | 界面参考图（目标效果基准，JPG 版入库；原始 PNG 只留本地） |
| `docs/六边形地图算法.md` | 六边形坐标权威文档 |
| `docs/手牌预览功能使用说明.md` | 手牌扇形调试面板用法 |
| `docs/修复六边形地图Bug.md` | 早期 AI 思考过程存档（归档性质） |
| `docs/战斗场景布局.md` | 残稿，未写完 |
| `docs/原始策划案.md` | 项目立项需求原文 |
| `.trae/plan_hexmap_battle_trigger.md` | 地图战斗触发系统实现计划（部分已完成，勾选框未同步） |
| `.trae/plans/hexmapui-refactor-plan.md` | HexMapUI 组件拆分计划（阶段 1-5 完成，阶段 6 待做） |
