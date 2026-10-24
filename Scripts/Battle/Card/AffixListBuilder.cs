using Godot;

namespace FishEatFish.Battle.Card
{
	/// <summary>
	/// **词缀说明列表的构建**（一份实现，多处共用）——
	/// 长按卡牌弹的「词缀说明」面板、衍生卡说明面板都用它。
	///
	/// 条目来源：
	/// - 卡牌特有的 <c>AffixNotes</c>（灵知觉醒 / 罪印…）
	/// - 通用词缀（保留 / 消耗…）→ 说明去 <c>Data/affixes.json</c> 查
	/// </summary>
	public static class AffixListBuilder
	{
		/// <summary>
		/// 把一张卡的词缀说明填进列表容器（**会先清空容器**）。
		/// </summary>
		/// <param name="list">目标容器（VBoxContainer）</param>
		/// <param name="card">卡牌</param>
		/// <returns>条目数量（0 = 这张卡没有词缀，调用方据此决定要不要显示面板）</returns>
		public static int Fill(VBoxContainer list, Card card)
		{
			if (list == null) return 0;

			// ⚠️ **必须先 RemoveChild 再 QueueFree**：
			// QueueFree 是**帧末**才删，同帧又 AddChild 新条目 → 旧条目还挂着，
			// 面板就会越叠越长（2026-10-24 踩过）。
			foreach (Node child in list.GetChildren())
			{
				list.RemoveChild(child);
				child.QueueFree();
			}

			int count = 0;
			if (card == null) return count;

			// ① 卡牌特有的词缀（灵知觉醒 / 罪印…）
			//    记下已列过的 id —— ② 里同一个词缀不再重复列一遍。
			var listed = new System.Collections.Generic.HashSet<string>();
			if (card.AffixNotes != null)
			{
				foreach (CardAffixNote note in card.AffixNotes)
				{
					if (note == null) continue;
					AffixInfo info = AffixLibrary.Get(note.Id);
					AddEntry(list, note.Name, note.Description, info?.Symbol ?? "⚠", info?.Color ?? "#e8a34a");
					if (!string.IsNullOrEmpty(note.Id)) listed.Add(note.Id);
					count++;
				}
			}

			// ② 通用词缀（保留 / 消耗…）→ 说明去 affixes.json 查
			if (card.Affixes != null)
			{
				foreach (string affixId in card.Affixes)
				{
					if (listed.Contains(affixId)) continue;   // ① 已经列过了
					AffixInfo info = AffixLibrary.Get(affixId);
					if (info == null) continue;
					AddEntry(list, info.Name, info.Description, info.Symbol, info.Color);
					count++;
				}
			}

			return count;
		}

		/// <summary>加一条词缀说明（符号 + 名称 + 一段说明文字）</summary>
		private static void AddEntry(VBoxContainer list, string name, string description, string symbol, string colorHex)
		{
			Color color = ParseColor(colorHex);

			var entry = new VBoxContainer();
			entry.AddThemeConstantOverride("separation", 4);

			var titleRow = new HBoxContainer();
			titleRow.AddThemeConstantOverride("separation", 6);

			Label symbolLabel = new Label();
			symbolLabel.Text = string.IsNullOrEmpty(symbol) ? "⚠" : symbol;
			symbolLabel.AddThemeColorOverride("font_color", color);
			symbolLabel.AddThemeFontSizeOverride("font_size", 22);

			Label nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.AddThemeColorOverride("font_color", color);
			nameLabel.AddThemeFontSizeOverride("font_size", 22);

			titleRow.AddChild(symbolLabel);
			titleRow.AddChild(nameLabel);

			Label descLabel = new Label();
			descLabel.Text = description;
			descLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.93f));
			descLabel.AddThemeFontSizeOverride("font_size", 17);
			descLabel.AutowrapMode = TextServer.AutowrapMode.Word;
			descLabel.CustomMinimumSize = new Vector2(480, 0);

			entry.AddChild(titleRow);
			entry.AddChild(descLabel);

			list.AddChild(entry);
		}

		/// <summary>解析 #RRGGBB 颜色</summary>
		public static Color ParseColor(string hex)
		{
			if (string.IsNullOrEmpty(hex)) return new Color(0.91f, 0.64f, 0.29f);
			if (!hex.StartsWith("#")) hex = "#" + hex;
			return new Color(hex);
		}
	}
}
