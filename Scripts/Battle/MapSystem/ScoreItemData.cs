using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.MapSystem
{
	/// <summary>
	/// 一条评分项的定义 —— 文案与判定条件都来自 Data/score_items.json。
	///
	/// 改评分项只要改 JSON，不用动代码。
	/// </summary>
	public class ScoreItemData
	{
		private const string ConfigPath = "res://Data/score_items.json";

		/// <summary>评分项 id（唯一）</summary>
		public string Id = "";

		/// <summary>显示文案，可含占位符（如 {remainingBattles}）</summary>
		public string Text = "";

		/// <summary>
		/// 判定条件类型：
		///   always            —— 恒成立
		///   remaining_battles —— 剩余战斗数（param 为最小值）
		///   silver_key        —— 持有银钥数
		///   team_size         —— 出战角色数（param 为达标数）
		///   daily_first       —— 每日首次游玩
		/// </summary>
		public string Condition = "always";

		/// <summary>条件的附加参数（含义随 Condition 而定）</summary>
		public int Param;

		private static List<ScoreItemData> _cache;

		/// <summary>载入全部评分项定义（带缓存）。文件缺失时返回空列表，不返回 null。</summary>
		public static List<ScoreItemData> LoadAll()
		{
			if (_cache != null) return _cache;

			_cache = new List<ScoreItemData>();

			if (!FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[ScoreItemData] 找不到评分项配置文件: {ConfigPath}");
				return _cache;
			}

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
			Json json = new Json();
			json.Parse(file.GetAsText());
			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

			if (!root.ContainsKey("items"))
			{
				GD.PrintErr("[ScoreItemData] 评分项配置缺少 items 字段");
				return _cache;
			}

			foreach (var raw in root["items"].AsGodotArray())
			{
				Godot.Collections.Dictionary d = raw.AsGodotDictionary();
				_cache.Add(new ScoreItemData
				{
					Id = d.ContainsKey("id") ? d["id"].ToString() : "",
					Text = d.ContainsKey("text") ? d["text"].ToString() : "",
					Condition = d.ContainsKey("condition") ? d["condition"].ToString() : "always",
					Param = d.ContainsKey("param") ? d["param"].AsInt32() : 0
				});
			}

			GD.Print($"[ScoreItemData] 载入 {_cache.Count} 条评分项");
			return _cache;
		}
	}
}
