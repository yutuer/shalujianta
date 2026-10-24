using System.Collections.Generic;
using Godot;

namespace FishEatFish.Battle.Card
{
	/// <summary>一条词缀说明（id + 显示名 + 符号 + 颜色 + 说明文字）</summary>
	public class AffixInfo
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
		public string Symbol { get; set; } = "";
		public string Color { get; set; } = "";
		public string Description { get; set; } = "";
	}

	/// <summary>
	/// **词缀说明表**（<c>Data/affixes.json</c>）。
	///
	/// 为什么要有：参考游戏里长按卡牌会弹一个「词缀说明」面板，
	/// 里面每条词缀都有一段**说明文字**（不只是「保留」两个字）。
	/// 通用词缀（保留 / 消耗 / 回合唯一 / 灵知觉醒 / 罪印…）的说明统一放这张表，
	/// 卡牌特有���说明走卡牌自己的 <c>affixNotes</c>。
	/// </summary>
	public static class AffixLibrary
	{
		private const string ConfigPath = "res://Data/affixes.json";

		private static Dictionary<string, AffixInfo> _affixes;
		private static bool _loaded;

		/// <summary>按 id 取词缀说明（取不到返回一个占位项，不返回 null）</summary>
		public static AffixInfo Get(string affixId)
		{
			if (string.IsNullOrEmpty(affixId)) return null;

			Load();

			if (_affixes.TryGetValue(affixId, out AffixInfo info))
			{
				return info;
			}

			GD.PrintErr($"[AffixLibrary] 未知词缀 id：{affixId}（affixes.json 里没有）");
			return new AffixInfo
			{
				Id = affixId,
				Name = affixId,
				Symbol = "⚠",
				Color = "#e8a34a",
				Description = "（这条词缀还没有说明文字）"
			};
		}

		/// <summary>载入词缀表</summary>
		public static void Load()
		{
			if (_loaded) return;
			_loaded = true;

			_affixes = new Dictionary<string, AffixInfo>();

			if (!Godot.FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[AffixLibrary] 找不到词缀表：{ConfigPath}");
				return;
			}

			using var file = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PrintErr($"[AffixLibrary] 打不开词缀表：{ConfigPath}");
				return;
			}

			Json json = new Json();
			json.Parse(file.GetAsText());
			if (json.Data.VariantType != Variant.Type.Dictionary)
			{
				GD.PrintErr("[AffixLibrary] 词缀表格式不对（顶层不是字典）");
				return;
			}

			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
			if (!root.ContainsKey("affixes")) return;

			foreach (var item in root["affixes"].AsGodotArray())
			{
				if (item.Obj == null) continue;
				Godot.Collections.Dictionary dict = item.AsGodotDictionary();
				if (!dict.ContainsKey("id")) continue;

				AffixInfo info = new AffixInfo
				{
					Id = dict["id"].ToString(),
					Name = dict.ContainsKey("name") ? dict["name"].ToString() : dict["id"].ToString(),
					Symbol = dict.ContainsKey("symbol") ? dict["symbol"].ToString() : "",
					Color = dict.ContainsKey("color") ? dict["color"].ToString() : "#e8a34a",
					Description = dict.ContainsKey("description") ? dict["description"].ToString() : ""
				};

				_affixes[info.Id] = info;
			}

			GD.Print($"[AffixLibrary] 载入 {_affixes.Count} 条词缀说明");
		}
	}
}
