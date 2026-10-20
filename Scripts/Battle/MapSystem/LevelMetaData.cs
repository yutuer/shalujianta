using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.MapSystem
{
	/// <summary>
	/// 关卡展示页（D10）的元数据加载器 —— 读 Data/levels.json。
	///
	/// 只提供「章节 / 关卡节点 / 全局进度（UID、共鸣占位）」的读取，
	/// 星级评分与关卡进度不在这里 —— 它们由实际通关状态实时算（见 LevelMap）。
	/// </summary>
	public static class LevelMetaData
	{
		private const string ConfigPath = "res://Data/levels.json";

		private static List<LevelChapterMeta> _chapters;
		private static string _uid = "";
		private static int _resonanceCurrent;
		private static int _resonanceTotal;

		/// <summary>全部章节（带缓存）。文件缺失时返回空列表，不返回 null。</summary>
		public static List<LevelChapterMeta> LoadChapters()
		{
			if (_chapters != null) return _chapters;

			_chapters = new List<LevelChapterMeta>();

			if (!FileAccess.FileExists(ConfigPath))
			{
				GD.PrintErr($"[LevelMetaData] 找不到关卡元数据文件: {ConfigPath}");
				return _chapters;
			}

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
			Json json = new Json();
			json.Parse(file.GetAsText());
			Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

			if (root.ContainsKey("chapters"))
			{
				foreach (var rawChapter in root["chapters"].AsGodotArray())
				{
					Godot.Collections.Dictionary c = rawChapter.AsGodotDictionary();
					LevelChapterMeta chapter = new LevelChapterMeta
					{
						Id = c.ContainsKey("id") ? c["id"].ToString() : "",
						Name = c.ContainsKey("name") ? c["name"].ToString() : ""
					};

					if (c.ContainsKey("levels"))
					{
						foreach (var rawLevel in c["levels"].AsGodotArray())
						{
							Godot.Collections.Dictionary l = rawLevel.AsGodotDictionary();
							LevelNodeMeta node = new LevelNodeMeta
							{
								MapId = l.ContainsKey("mapId") ? l["mapId"].ToString() : "",
								Number = l.ContainsKey("number") ? l["number"].ToString() : "",
								Title = l.ContainsKey("title") ? l["title"].ToString() : "",
								Difficulty = l.ContainsKey("difficulty") ? l["difficulty"].ToString() : "normal",
								RecommendedLevel = l.ContainsKey("recommendedLevel") ? l["recommendedLevel"].AsInt32() : 1,
								Unlocked = !l.ContainsKey("unlocked") || l["unlocked"].AsBool(),
								Chapter = chapter
							};
							chapter.Levels.Add(node);
						}
					}

					_chapters.Add(chapter);
				}
			}

			if (root.ContainsKey("progress"))
			{
				Godot.Collections.Dictionary p = root["progress"].AsGodotDictionary();
				_uid = p.ContainsKey("uid") ? p["uid"].ToString() : "";
				if (p.ContainsKey("resonance"))
				{
					Godot.Collections.Dictionary res = p["resonance"].AsGodotDictionary();
					_resonanceCurrent = res.ContainsKey("current") ? res["current"].AsInt32() : 0;
					_resonanceTotal = res.ContainsKey("total") ? res["total"].AsInt32() : 0;
				}
			}

			GD.Print($"[LevelMetaData] 载入 {_chapters.Count} 个章节，" +
				$"关卡 {CountAllLevels()} 个，UID={_uid}，共鸣 {_resonanceCurrent}/{_resonanceTotal}（占位）");
			return _chapters;
		}

		/// <summary>全部关卡节点（跨章节）</summary>
		public static List<LevelNodeMeta> LoadAllLevels()
		{
			List<LevelNodeMeta> all = new List<LevelNodeMeta>();
			foreach (LevelChapterMeta chapter in LoadChapters())
			{
				all.AddRange(chapter.Levels);
			}
			return all;
		}

		/// <summary>按 mapId 找关卡节点</summary>
		public static LevelNodeMeta FindByMapId(string mapId)
		{
			foreach (LevelNodeMeta node in LoadAllLevels())
			{
				if (node.MapId == mapId) return node;
			}
			return null;
		}

		/// <summary>关卡总数</summary>
		public static int CountAllLevels()
		{
			int count = 0;
			foreach (LevelChapterMeta chapter in LoadChapters())
			{
				count += chapter.Levels.Count;
			}
			return count;
		}

		/// <summary>UID（项目暂无账号系统，这里是配置里的占位值）</summary>
		public static string Uid
		{
			get { LoadChapters(); return _uid; }
		}

		/// <summary>共鸣进度（占位：项目尚无共鸣系统）</summary>
		public static int ResonanceCurrent
		{
			get { LoadChapters(); return _resonanceCurrent; }
		}

		/// <summary>共鸣进度上限（占位）</summary>
		public static int ResonanceTotal
		{
			get { LoadChapters(); return _resonanceTotal; }
		}
	}
}
