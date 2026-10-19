using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.Core
{
	/// <summary>
	/// 敌人波次配置加载器
	/// 读取 Data/enemies.json，为地图上的普通 / 精英 / BOSS 战斗格子提供对应的敌人组合。
	/// </summary>
	public class EnemyConfigLoader
	{
		private static Dictionary<string, EnemyWaveConfig> waveDatabase = new Dictionary<string, EnemyWaveConfig>();
		private static bool isLoaded = false;

		public static void LoadWaves(string configPath = "res://Data/enemies.json")
		{
			if (isLoaded) return;

			if (!FileAccess.FileExists(configPath))
			{
				GD.PrintErr($"Enemy config file not found: {configPath}");
				return;
			}

			using var file = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
			ParseJson(file.GetAsText());
			isLoaded = true;

			GD.Print($"[EnemyConfigLoader] 加载完成，共 {waveDatabase.Count} 个波次: {string.Join(", ", waveDatabase.Keys)}");
		}

		private static void ParseJson(string json)
		{
			Json jsonNode = new Json();
			jsonNode.Parse(json);
			Godot.Collections.Dictionary resultDict = jsonNode.Data.AsGodotDictionary();

			if (!resultDict.ContainsKey("waves"))
			{
				GD.PrintErr("[EnemyConfigLoader] 缺少 waves 字段");
				return;
			}

			Godot.Collections.Dictionary waves = resultDict["waves"].AsGodotDictionary();

			foreach (var wavePair in waves)
			{
				string waveKey = wavePair.Key.ToString();
				Godot.Collections.Dictionary waveDict = wavePair.Value.AsGodotDictionary();

				EnemyWaveConfig wave = new EnemyWaveConfig
				{
					WaveKey = waveKey,
					DisplayName = GetString(waveDict, "displayName", waveKey),
					Enemies = new List<EnemyDefinition>()
				};

				if (waveDict.ContainsKey("enemies"))
				{
					foreach (Godot.Collections.Dictionary enemyDict in waveDict["enemies"].AsGodotArray())
					{
						wave.Enemies.Add(new EnemyDefinition
						{
							Name = GetString(enemyDict, "name", "Enemy"),
							MaxHealth = GetInt(enemyDict, "maxHealth", 50),
							Attack = GetInt(enemyDict, "attack", 10),
							Defense = GetInt(enemyDict, "defense", 0),
							Position = GetInt(enemyDict, "position", 0),
							Behavior = GetString(enemyDict, "behavior", "balanced"),
							IsElite = GetBool(enemyDict, "isElite", false),
							IsBoss = GetBool(enemyDict, "isBoss", false)
						});
					}
				}

				waveDatabase[waveKey] = wave;
			}
		}

		public static EnemyWaveConfig GetWave(string waveKey)
		{
			if (!isLoaded) LoadWaves();

			if (!string.IsNullOrEmpty(waveKey) && waveDatabase.TryGetValue(waveKey, out EnemyWaveConfig wave))
			{
				return wave;
			}

			GD.PrintErr($"[EnemyConfigLoader] 未找到敌人波次: {waveKey}");
			return null;
		}

		/// <summary>
		/// 按波次键创建敌人实例列表
		/// </summary>
		public static List<Enemy> CreateEnemies(string waveKey)
		{
			List<Enemy> enemies = new List<Enemy>();

			EnemyWaveConfig wave = GetWave(waveKey);
			if (wave == null)
			{
				return enemies;
			}

			foreach (EnemyDefinition def in wave.Enemies)
			{
				enemies.Add(new Enemy
				{
					EnemyName = def.Name,
					MaxHealth = def.MaxHealth,
					CurrentHealth = def.MaxHealth,
					Attack = def.Attack,
					Defense = def.Defense,
					Position = def.Position,
					Behavior = def.Behavior,
					IsElite = def.IsElite,
					IsBoss = def.IsBoss
				});
			}

			return enemies;
		}

		public static List<string> GetAllWaveKeys()
		{
			if (!isLoaded) LoadWaves();
			return new List<string>(waveDatabase.Keys);
		}

		public static void Reload()
		{
			isLoaded = false;
			waveDatabase.Clear();
			LoadWaves();
		}

		// ===== 类型安全取值 =====

		private static string GetString(Godot.Collections.Dictionary dict, string key, string fallback)
		{
			if (!dict.ContainsKey(key)) return fallback;

			Variant value = dict[key];
			return value.VariantType == Variant.Type.Nil ? fallback : value.AsString();
		}

		private static int GetInt(Godot.Collections.Dictionary dict, string key, int fallback)
		{
			if (!dict.ContainsKey(key)) return fallback;

			Variant value = dict[key];
			switch (value.VariantType)
			{
				case Variant.Type.Int:
					return (int)value.AsInt64();
				case Variant.Type.Float:
					return (int)value.AsDouble();
				default:
					return fallback;
			}
		}

		private static bool GetBool(Godot.Collections.Dictionary dict, string key, bool fallback)
		{
			if (!dict.ContainsKey(key)) return fallback;

			Variant value = dict[key];
			return value.VariantType == Variant.Type.Bool ? value.AsBool() : fallback;
		}
	}
}
