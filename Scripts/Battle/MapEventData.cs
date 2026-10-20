using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle
{
	/// <summary>
	/// 事件弹窗里的一个选项
	/// </summary>
	public class MapEventOption
	{
		/// <summary>选项按钮上的完整文案（参考图里形如「【进入】穿过迷雾，抵达另一端。」）</summary>
		public string Text = "";

		/// <summary>选项的动作标识（confirm / cancel / 自定义）</summary>
		public string Action = "";
	}

	/// <summary>
	/// 一个地图事件的表现数据（文案与图），来自 Data/events.json
	/// </summary>
	public class MapEventData
	{
		private const string ConfigPath = "res://Data/events.json";

		public string Title = "";
		public string Image = "";
		public string Body = "";
		public bool Hidden;
		public List<MapEventOption> Options = new List<MapEventOption>();

		private static Dictionary<string, MapEventData> _cache;

		/// <summary>
		/// 按事件 id 取数据（带缓存）。找不到时返回一个占位数据，不返回 null。
		/// </summary>
		public static MapEventData Load(string eventId)
		{
			Dictionary<string, MapEventData> all = LoadAll();
			if (all.TryGetValue(eventId, out MapEventData data))
			{
				return data;
			}

			GD.PrintErr($"[MapEventData] 找不到事件 '{eventId}' 的数据，返回占位数据");
			return new MapEventData
			{
				Title = "未知事件",
				Body = "（缺少事件数据）",
				Options = new List<MapEventOption>
				{
					new MapEventOption { Text = "【确定】", Action = "confirm" }
				}
			};
		}

		/// <summary>
		/// 载入全部事件数据
		/// </summary>
		public static Dictionary<string, MapEventData> LoadAll()
		{
			if (_cache != null) return _cache;

			_cache = new Dictionary<string, MapEventData>();

			if (!FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[MapEventData] 找不到事件配置文件: {ConfigPath}");
				return _cache;
			}

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
			Json json = new Json();
			json.Parse(file.GetAsText());
			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

			if (!root.ContainsKey("events"))
			{
				GD.PrintErr("[MapEventData] 事件配置缺少 events 字段");
				return _cache;
			}

			Godot.Collections.Dictionary events = root["events"].AsGodotDictionary();
			foreach (var pair in events)
			{
				string key = pair.Key.ToString();
				Godot.Collections.Dictionary d = pair.Value.AsGodotDictionary();

				MapEventData data = new MapEventData
				{
					Title = d.ContainsKey("title") ? d["title"].ToString() : key,
					Image = d.ContainsKey("image") ? d["image"].ToString() : "",
					Body = d.ContainsKey("body") ? d["body"].ToString() : "",
					Hidden = d.ContainsKey("hidden") && d["hidden"].AsBool()
				};

				if (d.ContainsKey("options"))
				{
					foreach (var opt in d["options"].AsGodotArray())
					{
						Godot.Collections.Dictionary o = opt.AsGodotDictionary();
						data.Options.Add(new MapEventOption
						{
							Text = o.ContainsKey("text") ? o["text"].ToString() : "【确定】",
							Action = o.ContainsKey("action") ? o["action"].ToString() : "confirm"
						});
					}
				}

				_cache[key] = data;
			}

			GD.Print($"[MapEventData] 载入 {_cache.Count} 个事件: {string.Join(", ", _cache.Keys)}");
			return _cache;
		}
	}
}
