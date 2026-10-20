using Godot;
using FishEatFish.Battle.MapSystem;

namespace FishEatFish.UI
{
	/// <summary>
	/// 关卡展示页（D10）里的关卡卡片：星级 + 编号 + 标题 + 推荐等级 + 难度。
	///
	/// 只负责渲染，**不持有游戏状态** —— 数据由 LevelMap 算好后传进来。
	/// </summary>
	public partial class LevelMapCard : PanelContainer
	{
		private Label _numberLabel;
		private Label _titleLabel;
		private Label _metaLabel;
		private Label[] _starLabels = new Label[3];
		private Button _button;

		public override void _Ready()
		{
			GD.Print("[LevelMapCard] _Ready called");

			_numberLabel = GetNodeOrNull<Label>("MainVBox/NumberLabel");
			_titleLabel = GetNodeOrNull<Label>("MainVBox/TitleLabel");
			_metaLabel = GetNodeOrNull<Label>("MainVBox/MetaLabel");

			for (int i = 0; i < 3; i++)
			{
				_starLabels[i] = GetNodeOrNull<Label>($"StarsRow/Star{i}");
			}

			_button = GetNodeOrNull<Button>("MainVBox/OpenButton");
			if (_button != null)
			{
				_button.Pressed += OnButtonPressed;
			}

			GD.Print("[LevelMapCard] _Ready completed");
		}

		/// <summary>玩家点了这张卡片（未解锁时不会触发）</summary>
		public System.Action<LevelNodeMeta> OnOpen;

		/// <summary>当前卡片对应的关卡节点</summary>
		public LevelNodeMeta Node { get; private set; }

		/// <summary>
		/// 设置卡片内容。
		/// </summary>
		/// <param name="node">关卡元数据</param>
		/// <param name="stars">已获得的星数（0~3）</param>
		/// <param name="unlocked">是否已解锁</param>
		public void ApplyNode(LevelNodeMeta node, int stars, bool unlocked)
		{
			Node = node;

			if (_numberLabel == null) _numberLabel = GetNodeOrNull<Label>("MainVBox/NumberLabel");
			if (_titleLabel == null) _titleLabel = GetNodeOrNull<Label>("MainVBox/TitleLabel");
			if (_metaLabel == null) _metaLabel = GetNodeOrNull<Label>("MainVBox/MetaLabel");
			for (int i = 0; i < 3; i++)
			{
				if (_starLabels[i] == null) _starLabels[i] = GetNodeOrNull<Label>($"StarsRow/Star{i}");
			}

			if (_numberLabel != null) _numberLabel.Text = node?.Number ?? "";
			if (_titleLabel != null) _titleLabel.Text = node?.Title ?? "";

			if (_metaLabel != null)
			{
				_metaLabel.Text = unlocked
					? $"推荐等级 {node?.RecommendedLevel ?? 0}　·　{GetDifficultyText(node?.Difficulty)}"
					: "尚未解锁";
			}

			for (int i = 0; i < _starLabels.Length; i++)
			{
				if (_starLabels[i] == null) continue;
				_starLabels[i].Text = i < stars ? "★" : "☆";
			}

			if (_button == null) _button = GetNodeOrNull<Button>("MainVBox/OpenButton");
			if (_button != null)
			{
				_button.Disabled = !unlocked;
				_button.Text = unlocked ? "查看详情" : "未解锁";
			}

			// 未解锁的卡片压暗
			Modulate = unlocked ? Colors.White : new Color(0.55f, 0.55f, 0.6f, 1f);

			GD.Print($"[LevelMapCard] {node?.Number} {node?.Title}（星 {stars}，{(unlocked ? "已解锁" : "未解锁")}）");
		}

		/// <summary>难度档的中文名</summary>
		public static string GetDifficultyText(string difficulty)
		{
			return difficulty switch
			{
				"hard" => "困难",
				"dream" => "溯梦",
				_ => "普通"
			};
		}

		private void OnButtonPressed()
		{
			if (Node == null) return;
			GD.Print($"[LevelMapCard] 点击关卡: {Node.Number} {Node.Title}");
			OnOpen?.Invoke(Node);
		}
	}
}
