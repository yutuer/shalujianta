using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.HexMap
{
	/// <summary>
	/// 关卡规格 —— 「这张关卡长什么样」的**声明式描述**，来自 Data/level_specs.json。
	///
	/// 关卡是**固定内容**（重复进入要一样），但不该逐格手画：
	/// 规格只说「多少格、多少个战斗格、几个传送门」，具体布局由种子决定。
	///
	/// 关键性质：
	///   - **确定性**：同一份规格 + 同一个种子 → 逐格完全相同的地图
	///   - **可满足性**：配额放不下时**明确报错**，而不是静默产出残缺地图
	///   - **可复用**：地图按 mapId 缓存，重进同一关卡拿同一张图
	/// </summary>
	public class MapSpec
	{
		private const string ConfigPath = "res://Data/level_specs.json";

		/// <summary>关卡 id（对应 MapDefinition.MapId）</summary>
		public string MapId = "";

		/// <summary>随机种子 —— 决定这张地图的具体布局</summary>
		public int Seed;

		/// <summary>初始簇数（簇之间靠传送门连通；1 = 单块，D12 多簇后生效）</summary>
		public int Clusters = 1;

		/// <summary>地图总格数</summary>
		public int TotalTiles = 121;

		/// <summary>事件配额</summary>
		public MapQuota Quota = new MapQuota();

		private static Dictionary<string, MapSpec> _cache;

		/// <summary>载入全部规格（带缓存）</summary>
		public static List<MapSpec> LoadAll()
		{
			Dictionary<string, MapSpec> all = LoadAllMap();
			List<MapSpec> list = new List<MapSpec>();
			foreach (KeyValuePair<string, MapSpec> kv in all)
			{
				list.Add(kv.Value);
			}
			return list;
		}

		/// <summary>载入全部规格（按 mapId 索引）</summary>
		public static Dictionary<string, MapSpec> LoadAllMap()
		{
			if (_cache != null) return _cache;

			_cache = new Dictionary<string, MapSpec>();

			if (!FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[MapSpec] 找不到关卡规格文件: {ConfigPath}");
				return _cache;
			}

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
			Json json = new Json();
			json.Parse(file.GetAsText());
			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

			if (!root.ContainsKey("specs"))
			{
				GD.PrintErr("[MapSpec] 关卡规格缺少 specs 字段");
				return _cache;
			}

			foreach (var raw in root["specs"].AsGodotArray())
			{
				Godot.Collections.Dictionary d = raw.AsGodotDictionary();
				MapSpec spec = new MapSpec
				{
					MapId = d.ContainsKey("mapId") ? d["mapId"].ToString() : "",
					Seed = d.ContainsKey("seed") ? d["seed"].AsInt32() : 0,
					Clusters = d.ContainsKey("clusters") ? d["clusters"].AsInt32() : 1,
					TotalTiles = d.ContainsKey("totalTiles") ? d["totalTiles"].AsInt32() : 121
				};

				if (d.ContainsKey("quota"))
				{
					Godot.Collections.Dictionary q = d["quota"].AsGodotDictionary();
					spec.Quota = new MapQuota
					{
						Battle = ReadInt(q, "battle"),
						Elite = ReadInt(q, "elite"),
						Shop = ReadInt(q, "shop"),
						Heal = ReadInt(q, "heal"),
						BlackMark = ReadInt(q, "blackMark"),
						Swamp = ReadInt(q, "swamp"),
						Hole = ReadInt(q, "hole"),
						Void = ReadInt(q, "void"),
						Searchlight = ReadInt(q, "searchlight"),
						TeleportOneWay = ReadInt(q, "teleportOneWay"),
						TeleportTwoWay = ReadInt(q, "teleportTwoWay")
					};
				}

				if (!string.IsNullOrEmpty(spec.MapId))
				{
					_cache[spec.MapId] = spec;
				}
			}

			GD.Print($"[MapSpec] 载入 {_cache.Count} 份关卡规格");
			return _cache;
		}

		private static int ReadInt(Godot.Collections.Dictionary d, string key)
		{
			return d.ContainsKey(key) ? d[key].AsInt32() : 0;
		}

		/// <summary>按 mapId 取规格；没有则返回 null</summary>
		public static MapSpec Load(string mapId)
		{
			Dictionary<string, MapSpec> all = LoadAllMap();
			MapSpec spec;
			return all.TryGetValue(mapId, out spec) ? spec : null;
		}

		/// <summary>
		/// **可满足性检查**：配额要求的东西放不下时，明确报错。
		/// </summary>
		/// <param name="availableTiles">可用来放事件的格子数</param>
		/// <exception cref="System.InvalidOperationException">规格不可满足</exception>
		public void Validate(int availableTiles)
		{
			if (string.IsNullOrEmpty(MapId))
			{
				throw new System.InvalidOperationException("关卡规格缺少 mapId");
			}

			if (TotalTiles < 10)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：totalTiles={TotalTiles} 太小（至少 10）");
			}

			// 网格是 (2r+1) × (2r+1)，总格数必须能还原成这种形状
			int side = (int)System.Math.Round(System.Math.Sqrt(TotalTiles));
			if (side % 2 == 0) side += 1;
			if (side * side != TotalTiles)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：totalTiles={TotalTiles} 不是 (2r+1)² 形式的网格大小");
			}

			if (Quota.TotalEventCount + Quota.TeleportTileCount > availableTiles)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：配额合计需要 "
					+ $"{Quota.TotalEventCount + Quota.TeleportTileCount} 个格子，"
					+ $"但只有 {availableTiles} 个可用格子（{Quota}）");
			}

			if (Quota.Hole < 0 || Quota.Hole > 3)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：洞数量必须是 1~3（当前 {Quota.Hole}）");
			}

			if (Quota.Void < 0 || Quota.Void > 4)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：空洞数量必须是 0~4（当前 {Quota.Void}）");
			}

			if (Clusters < 1)
			{
				throw new System.InvalidOperationException(
					$"关卡 [{MapId}] 规格不可满足：clusters 至少为 1（当前 {Clusters}）");
			}
		}

		/// <summary>
		/// 统计一张实际地图里各类事件的数量（用于验证「配额精确匹配」）。
		/// </summary>
		public static Dictionary<HexEventType, int> CountEvents(HexMap map)
		{
			Dictionary<HexEventType, int> counts = new Dictionary<HexEventType, int>();
			if (map == null) return counts;

			foreach (HexTile tile in map.GetAllTiles())
			{
				int n;
				counts.TryGetValue(tile.EventType, out n);
				counts[tile.EventType] = n + 1;
			}
			return counts;
		}

		/// <summary>地图布局指纹（用于判断两张地图是否逐格相同）</summary>
		public static string Fingerprint(HexMap map)
		{
			if (map == null) return "";

			List<string> parts = new List<string>();
			foreach (HexTile tile in map.GetAllTiles())
			{
				parts.Add($"{tile.Coord.Q},{tile.Coord.R}:{tile.EventType}");
			}
			parts.Sort();
			return string.Join("|", parts);
		}

		public override string ToString()
		{
			return $"[{MapId}] seed={Seed} clusters={Clusters} tiles={TotalTiles} quota=({Quota})";
		}
	}
}
