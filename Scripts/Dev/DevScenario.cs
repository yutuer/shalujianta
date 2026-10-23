using Godot;
using System.Collections.Generic;
using FishEatFish.Battle;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.HexMap;
using FishEatFish.Scenes;

namespace FishEatFish.Dev
{
	/// <summary>
	/// 一个调试场景预设（对应 Data/dev_scenarios.json 里的一条）
	/// </summary>
	public class DevScenarioPreset
	{
		public string Name { get; set; } = "default";
		public string Desc { get; set; } = "";
		public string[] Team { get; set; } = new string[0];
		public string KeyOrder { get; set; } = "silver_slash";
		public string Map { get; set; } = "map01";
		public string Target { get; set; } = "hexmap";
		/// <summary>进地图后的血量比例（0~1），缺省不干预</summary>
		public float PlayerHealthRatio { get; set; } = 1f;
		/// <summary>进地图后落点对应的格子事件类型（shop/elite/boss/...），缺省不干预</summary>
		public string StartAt { get; set; } = "";
		/// <summary>直接站到指定类型格子上（会立即触发该格子的事件），缺省不干预</summary>
		public string StartOn { get; set; } = "";
		/// <summary>直接进战斗时强制使用的敌人波次</summary>
		public string EnemyWave { get; set; } = "";
	}

	/// <summary>
	/// 调试场景准备器：一键构造「完成选人之后」的状态并进入目标场景。
	///
	/// 为什么要它 —— 直接跑 HexMapScene 会跳过选人流程，导致
	///   GlobalData.SelectedCharacters / EquippedKeyOrder / CurrentMap 全为 null，
	///   战斗回退到默认阵容「鼠牛虎兔」而不是所选队伍，刻印可选卡牌列表也拿不到卡组。
	/// 本类把这些全局量按真实流程补齐，所以「快」和「状态正确」可以兼得。
	///
	/// 启动方式：
	///   1) 命令行：Godot.exe --path &lt;项目&gt; res://Scenes/Dev/DevScenario.tscn
	///      可加 --scenario=xxx 指定预设，或用 --team/--key/--map/--target 临时覆盖
	///   2) 主菜单「调试直达地图」按钮
	///
	/// 扩展方式（以后要加"一半血量""在商店旁边""带特定装备"这类需求）：
	///   ① 往 Data/dev_scenarios.json 里加一条 preset
	///   ② 若该预设需要新的状态维度，在本类加一个应用钩子（见 ApplyInSceneState）
	/// </summary>
	public partial class DevScenario : Node
	{
		private const string ConfigPath = "res://Data/dev_scenarios.json";

		private static Dictionary<string, DevScenarioPreset> _presets;

		// ===== 预设加载 =====

		public static Dictionary<string, DevScenarioPreset> LoadPresets()
		{
			if (_presets != null) return _presets;

			_presets = new Dictionary<string, DevScenarioPreset>();

			if (!FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[DevScenario] 找不到预设文件: {ConfigPath}");
				return _presets;
			}

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
			Json json = new Json();
			json.Parse(file.GetAsText());
			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

			if (!root.ContainsKey("presets"))
			{
				GD.PrintErr("[DevScenario] 预设文件缺少 presets 字段");
				return _presets;
			}

			Godot.Collections.Dictionary presets = root["presets"].AsGodotDictionary();
			foreach (var pair in presets)
			{
				Godot.Collections.Dictionary d = pair.Value.AsGodotDictionary();
				string key = pair.Key.ToString();

				List<string> team = new List<string>();
				if (d.ContainsKey("team"))
				{
					foreach (var t in d["team"].AsGodotArray())
					{
						team.Add(t.ToString());
					}
				}

				DevScenarioPreset preset = new DevScenarioPreset
				{
					Name = key,
					Desc = d.ContainsKey("desc") ? d["desc"].ToString() : "",
					Team = team.ToArray(),
					KeyOrder = d.ContainsKey("keyOrder") ? d["keyOrder"].ToString() : "silver_slash",
					Map = d.ContainsKey("map") ? d["map"].ToString() : "map01",
					Target = d.ContainsKey("target") ? d["target"].ToString() : "hexmap",
					PlayerHealthRatio = d.ContainsKey("playerHealthRatio") ? (float)d["playerHealthRatio"].AsDouble() : 1f,
					StartAt = d.ContainsKey("startAt") ? d["startAt"].ToString() : "",
					StartOn = d.ContainsKey("startOn") ? d["startOn"].ToString() : "",
					EnemyWave = d.ContainsKey("enemyWave") ? d["enemyWave"].ToString() : ""
				};

				_presets[key] = preset;
			}

			GD.Print($"[DevScenario] 载入 { _presets.Count } 个预设: {string.Join(", ", _presets.Keys)}");
			return _presets;
		}

		/// <summary>解析最终使用的预设：--scenario 指定预设，细粒度参数可再覆盖</summary>
		public static DevScenarioPreset ResolvePreset()
		{
			Dictionary<string, DevScenarioPreset> presets = LoadPresets();

			string presetKey = "default";
			string teamOverride = null;
			string keyOverride = null;
			string mapOverride = null;
			string targetOverride = null;

			foreach (string arg in OS.GetCmdlineUserArgs())
			{
				if (arg.StartsWith("--scenario=")) presetKey = arg.Substring("--scenario=".Length);
				else if (arg.StartsWith("--team=")) teamOverride = arg.Substring("--team=".Length);
				else if (arg.StartsWith("--key=")) keyOverride = arg.Substring("--key=".Length);
				else if (arg.StartsWith("--map=")) mapOverride = arg.Substring("--map=".Length);
				else if (arg.StartsWith("--target=")) targetOverride = arg.Substring("--target=".Length);
			}

			DevScenarioPreset preset;
			if (!presets.TryGetValue(presetKey, out preset))
			{
				GD.PrintErr($"[DevScenario] 未知预设 '{presetKey}'，改用 default。可用: {string.Join(", ", presets.Keys)}");
				preset = presets["default"];
			}

			if (teamOverride != null) preset.Team = teamOverride.Split(',');
			if (keyOverride != null) preset.KeyOrder = keyOverride;
			if (mapOverride != null) preset.Map = mapOverride;
			if (targetOverride != null) preset.Target = targetOverride;

			return preset;
		}

		// ===== 入口 =====

		public override void _Ready()
		{
			// 挂到 root 下，切换场景后本节点仍存活，才能继续应用「场景内状态」
			CallDeferred(nameof(RehomeToRoot));
		}

		private void RehomeToRoot()
		{
			// 先取引用：RemoveChild 之后本节点已脱离场景树，GetTree() 会变成 null
			SceneTree tree = GetTree();
			Node parent = GetParent();
			if (parent != null) parent.RemoveChild(this);
			tree.Root.AddChild(this);

			DevScenarioPreset preset = ResolvePreset();
			ApplyGlobalState(preset);

			string scenePath = preset.Target == "battle"
				? "res://Scenes/BattleScene.tscn"
				: "res://Scenes/HexMapScene.tscn";

			GD.Print($"[DevScenario] 预设={preset.Name} 目标={scenePath}");
			GD.Print($"[DevScenario] {preset.Desc}");

			tree.ChangeSceneToFile(scenePath);

			// 场景切换是帧末完成的，这里等两帧再去改场景内的东西
			CallDeferred(nameof(ApplyInSceneState));
		}

		// ===== 全局状态（切换场景前写入）=====

		public static void ApplyGlobalState(DevScenarioPreset preset)
		{
			CharacterDefinition[] team = BuildTeam(preset.Team);
			KeyOrder keyOrder = BuildKeyOrder(preset.KeyOrder);
			MapDefinition map = BuildMap(preset.Map);

			GlobalData.SelectedCharacters = team;
			GlobalData.EquippedKeyOrder = keyOrder;
			GlobalData.CurrentMap = map;
			GlobalData.EnterHexMapMode = true;

			MapManager.Instance?.StartMap(map);

			if (!string.IsNullOrEmpty(preset.EnemyWave))
			{
				BattleTransitionManager.Instance?.SetEnemyConfig(preset.EnemyWave);
			}

			string teamNames = team != null
				? string.Join(",", System.Array.ConvertAll(team, c => c?.Name ?? "?"))
				: "(无)";

			GD.Print($"[DevScenario] 队伍={teamNames} 钥令={keyOrder?.Name ?? "(无)"} 关卡={map?.Name ?? "(无)"}");
		}

		// ===== 场景内状态（切换场景后写入）=====

		private async void ApplyInSceneState()
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			DevScenarioPreset preset = ResolvePreset();
			HexMapController controller = FindController();

			if (controller == null)
			{
				GD.Print("[DevScenario] 目标场景没有 HexMapController，跳过场景内状态");
				QueueFree();
				return;
			}

			bool changed = false;

			// 血量比例
			if (preset.PlayerHealthRatio > 0f && preset.PlayerHealthRatio < 1f)
			{
				float max = controller.PlayerMaxHealth;
				float current = max * preset.PlayerHealthRatio;
				controller.SetPlayerHealth(max, current);
				GD.Print($"[DevScenario] 血量设为 {current:F0}/{max:F0} ({preset.PlayerHealthRatio:P0})");
				changed = true;
			}

			// 落点：放在目标事件格子的旁边，避免一进去就触发该事件
			if (!string.IsNullOrEmpty(preset.StartAt))
			{
				HexCoord? standAt = FindAdjacentTileOf(controller, preset.StartAt);
				if (standAt.HasValue)
				{
					controller.QuickMoveTo(standAt.Value);
					GD.Print($"[DevScenario] 落在 {preset.StartAt} 旁边: {standAt.Value}");
					changed = true;
				}
				else
				{
					GD.PrintErr($"[DevScenario] 地图上找不到 {preset.StartAt} 附近的可站立格子");
				}
			}

			// 直接站到指定类型的格子上（会立刻触发该格子的事件，比如 BOSS 战）
			if (!string.IsNullOrEmpty(preset.StartOn))
			{
				HexCoord? onTile = FindTileOf(controller, preset.StartOn);
				if (onTile.HasValue)
				{
					controller.QuickMoveTo(onTile.Value);
					GD.Print($"[DevScenario] 站上 {preset.StartOn} 格子: {onTile.Value}");
					changed = true;
				}
				else
				{
					GD.PrintErr($"[DevScenario] 地图上找不到 {preset.StartOn} 格子");
				}
			}

			if (!changed) GD.Print("[DevScenario] 无需调整场景内状态");
			QueueFree();
		}

		private HexMapController FindController()
		{
			Node scene = GetTree().CurrentScene;
			if (scene == null) return null;
			foreach (Node child in scene.GetChildren())
			{
				if (child is HexMapController ctrl) return ctrl;
			}
			return null;
		}

		/// <summary>找指定事件类型格子的一个可站立邻格</summary>
		private static HexCoord? FindAdjacentTileOf(HexMapController controller, string eventName)
		{
			HexCoord? tile = FindTileOf(controller, eventName);
			if (!tile.HasValue) return null;

			HexMap map = controller.CurrentMap;
			foreach (HexCoord neighbor in tile.Value.GetNeighbors())
			{
				HexTile n = map.GetTile(neighbor);
				if (n != null && n.CanEnter && n.EventType == HexEventType.Empty)
				{
					return neighbor;
				}
			}
			return null;
		}

		/// <summary>找指定事件类型的一个格子坐标</summary>
		private static HexCoord? FindTileOf(HexMapController controller, string eventName)
		{
			HexEventType? target = ParseEventType(eventName);
			if (!target.HasValue) return null;

			HexMap map = controller.CurrentMap;
			if (map == null) return null;

			foreach (HexTile tile in map.GetAllTiles())
			{
				if (tile.EventType == target.Value && tile.CanEnter)
				{
					return tile.Coord;
				}
			}
			return null;
		}

		private static HexEventType? ParseEventType(string name)
		{
			switch (name.ToLower())
			{
				case "shop": return HexEventType.Shop;
				case "battle": return HexEventType.BattleNormal;
				case "elite": return HexEventType.BattleElite;
				case "boss": return HexEventType.BattleBoss;
				case "heal": return HexEventType.Heal;
				case "blackmark": return HexEventType.GainBlackMark;
				case "swamp": return HexEventType.Swamp;
				case "hole": return HexEventType.Hole;
				case "teleport": return HexEventType.TwoWayTeleport;
				default:
					GD.PrintErr($"[DevScenario] 未知格子事件类型: {name}");
					return null;
			}
		}

		// ===== 构件工厂 =====

		public static CharacterDefinition[] BuildTeam(string[] ids)
		{
			List<CharacterDefinition> team = new List<CharacterDefinition>();
			if (ids != null)
			{
				foreach (string raw in ids)
				{
					string id = raw.Trim();
					if (id.Length == 0) continue;

					CharacterDefinition c = CreateCharacter(id);
					if (c != null) team.Add(c);
					else GD.PrintErr($"[DevScenario] 未知角色 id: {id}");
				}
			}

			if (team.Count != 4)
			{
				GD.PrintErr($"[DevScenario] 队伍需要 4 人，当前 {team.Count} 人，回退默认队伍");
				return new CharacterDefinition[]
				{
					CharacterDefinition.CreateRat(),
					CharacterDefinition.CreateOx(),
					CharacterDefinition.CreateTiger(),
					CharacterDefinition.CreateRabbit()
				};
			}

			return team.ToArray();
		}

		private static CharacterDefinition CreateCharacter(string id)
		{
			switch (id)
			{
				case "rat": return CharacterDefinition.CreateRat();
				case "ox": return CharacterDefinition.CreateOx();
				case "tiger": return CharacterDefinition.CreateTiger();
				case "rabbit": return CharacterDefinition.CreateRabbit();
				case "dragon": return CharacterDefinition.CreateDragon();
				case "snake": return CharacterDefinition.CreateSnake();
				case "horse": return CharacterDefinition.CreateHorse();
				case "goat": return CharacterDefinition.CreateGoat();
				case "monkey": return CharacterDefinition.CreateMonkey();
				case "rooster": return CharacterDefinition.CreateRooster();
				case "dog": return CharacterDefinition.CreateDog();
				case "pig": return CharacterDefinition.CreatePig();
				case "pollux": return CharacterDefinition.CreatePollux();
				default: return null;
			}
		}

		public static KeyOrder BuildKeyOrder(string id)
		{
			switch (id)
			{
				case "silver_slash": return KeyOrder.CreateSilverSlash();
				case "key_shield": return KeyOrder.CreateKeyShield();
				case "energy_infusion": return KeyOrder.CreateEnergyInfusion();
				case "armor_break": return KeyOrder.CreateArmorBreak();
				case "life_key": return KeyOrder.CreateLifeKey();
				default:
					GD.PrintErr($"[DevScenario] 未知钥令 id: {id}，使用银光斩");
					return KeyOrder.CreateSilverSlash();
			}
		}

		public static MapDefinition BuildMap(string id)
		{
			MapDefinition[] maps = MapDefinition.GetAllMaps();
			foreach (MapDefinition m in maps)
			{
				if (m.MapId == id) return m;
			}
			if (id.StartsWith("map") && int.TryParse(id.Substring(3), out int index))
			{
				int i = index - 1;
				if (i >= 0 && i < maps.Length) return maps[i];
			}
			GD.PrintErr($"[DevScenario] 未知关卡 id: {id}，使用第一张");
			return maps[0];
		}
	}
}
