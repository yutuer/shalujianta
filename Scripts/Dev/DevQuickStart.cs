using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Scenes;

namespace FishEatFish.Dev
{
	/// <summary>
	/// 调试入口：一键复刻「主菜单 → 选人 → 地图」的全局状态，直接进入六边形地图。
	///
	/// 与「直接跑 HexMapScene 场景」的区别 —— 后者会跳过选人流程，导致：
	///   GlobalData.SelectedCharacters = null → 战斗回退到默认阵容（鼠牛虎兔），不是你选的队伍
	///   GlobalData.EquippedKeyOrder  = null → 钥令回退默认「银斩」
	///   GlobalData.CurrentMap       = null → 关卡进度不记录
	///   HexMapUI 的刻印可选卡牌列表也拿不到所选角色的卡组
	/// 本类把这些全局量按真实流程补齐，所以「又快」和「状态正确」可以兼得。
	///
	/// 启动方式：
	///   1) 命令行直接跑本场景：
	///      Godot.exe --path &lt;项目&gt; res://Scenes/Dev/DevQuickStart.tscn
	///      可加用户参数覆盖：--team=rat,ox,tiger,rabbit  --key=key_shield  --map=map02
	///   2) 主菜单点「调试直达地图」按钮
	/// </summary>
	public partial class DevQuickStart : Node
	{
		/// <summary>默认队伍：与 BattleManager 的回退默认一致（鼠牛虎兔）</summary>
		public static CharacterDefinition[] DefaultTeam()
		{
			return new CharacterDefinition[]
			{
				CharacterDefinition.CreateRat(),
				CharacterDefinition.CreateOx(),
				CharacterDefinition.CreateTiger(),
				CharacterDefinition.CreateRabbit()
			};
		}

		/// <summary>按真实流程写入全局状态</summary>
		public static void ApplyGlobalState(CharacterDefinition[] team, KeyOrder keyOrder, MapDefinition map)
		{
			GlobalData.SelectedCharacters = team;
			GlobalData.EquippedKeyOrder = keyOrder;
			GlobalData.CurrentMap = map;
			GlobalData.EnterHexMapMode = true;

			MapManager.Instance?.StartMap(map);

			string teamNames = team != null
				? string.Join(",", System.Array.ConvertAll(team, c => c?.Name ?? "?"))
				: "(无)";

			GD.Print($"[DevQuickStart] 队伍={teamNames} 钥令={keyOrder?.Name ?? "(无)"} 关卡={map?.Name ?? "(无)"}");
		}

		/// <summary>用默认阵容 + 默认钥令 + 第一张关卡直接进地图</summary>
		public static void SetupAndGo(SceneTree tree)
		{
			SetupAndGo(tree, DefaultTeam(), KeyOrder.CreateSilverSlash(), MapDefinition.GetAllMaps()[0]);
		}

		public static void SetupAndGo(SceneTree tree, CharacterDefinition[] team, KeyOrder keyOrder, MapDefinition map)
		{
			ApplyGlobalState(team, keyOrder, map);
			tree.ChangeSceneToFile("res://Scenes/HexMapScene.tscn");
		}

		public override void _Ready()
		{
			CharacterDefinition[] team = DefaultTeam();
			KeyOrder keyOrder = KeyOrder.CreateSilverSlash();
			MapDefinition map = MapDefinition.GetAllMaps()[0];
			string target = "hexmap";

			// 命令行覆盖：--team=rat,ox,tiger,rabbit  --key=key_shield  --map=map02  --target=battle
			foreach (string arg in OS.GetCmdlineUserArgs())
			{
				if (arg.StartsWith("--team="))
				{
					team = ParseTeam(arg.Substring("--team=".Length));
				}
				else if (arg.StartsWith("--key="))
				{
					keyOrder = ParseKeyOrder(arg.Substring("--key=".Length));
				}
				else if (arg.StartsWith("--map="))
				{
					map = ParseMap(arg.Substring("--map=".Length));
				}
				else if (arg.StartsWith("--target="))
				{
					target = arg.Substring("--target=".Length).ToLower();
				}
			}

			ApplyGlobalState(team, keyOrder, map);

			string scenePath = target == "battle"
				? "res://Scenes/BattleScene.tscn"
				: "res://Scenes/HexMapScene.tscn";

			GD.Print($"[DevQuickStart] 目标场景 = {scenePath}");

			// _Ready 期间场景树正在装配，直接切场景会报 "Parent node is busy"，延后一帧
			GetTree().CallDeferred("change_scene_to_file", scenePath);
		}

		// ===== 解析命令行参数 =====

		private static CharacterDefinition[] ParseTeam(string csv)
		{
			List<CharacterDefinition> team = new List<CharacterDefinition>();
			foreach (string part in csv.Split(','))
			{
				CharacterDefinition c = CreateCharacter(part.Trim());
				if (c != null)
				{
					team.Add(c);
				}
				else
				{
					GD.PrintErr($"[DevQuickStart] 未知角色 id: {part}");
				}
			}

			if (team.Count != 4)
			{
				GD.PrintErr($"[DevQuickStart] 队伍需要 4 人，当前 {team.Count} 人，回退默认队伍");
				return DefaultTeam();
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
				default: return null;
			}
		}

		private static KeyOrder ParseKeyOrder(string id)
		{
			switch (id)
			{
				case "silver_slash": return KeyOrder.CreateSilverSlash();
				case "key_shield": return KeyOrder.CreateKeyShield();
				case "energy_infusion": return KeyOrder.CreateEnergyInfusion();
				case "armor_break": return KeyOrder.CreateArmorBreak();
				case "life_key": return KeyOrder.CreateLifeKey();
				default:
					GD.PrintErr($"[DevQuickStart] 未知钥令 id: {id}");
					return KeyOrder.CreateSilverSlash();
			}
		}

		private static MapDefinition ParseMap(string id)
		{
			MapDefinition[] maps = MapDefinition.GetAllMaps();
			foreach (MapDefinition m in maps)
			{
				if (m.MapId == id) return m;
			}

			// 允许用序号：map1 / map2 / map3
			if (id.StartsWith("map") && int.TryParse(id.Substring(3), out int index))
			{
				int i = index - 1;
				if (i >= 0 && i < maps.Length) return maps[i];
			}

			GD.PrintErr($"[DevQuickStart] 未知关卡 id: {id}，使用第一张");
			return maps[0];
		}
	}
}
