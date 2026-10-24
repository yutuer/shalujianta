using Godot;
using System.Collections.Generic;

namespace FishEatFish.UI
{
	/// <summary>
	/// **圆形徽章返回按钮** —— 放在页面右上角。
	///
	/// 样式照参考游戏《终末前夜 Morimens》卡牌总览页右上角那枚圆形徽章：
	/// **深色圆盘 + 细环 + 中间的米白箭镞徽记**
	/// （一支从左尖射出的长针 + 米白箭镞 + 迷你四角星 + 右侧两道金色月牙）。
	///
	/// 用代码画（`_Draw`）而不是贴图：
	/// - 不引入新的美术资源，任意尺寸都清晰（参考图那枚徽章直径约 110px）；
	/// - 颜色 / 半径 / 比例都能在检视器里直接调。
	///
	/// ⚠️ 主题自带的矩形底板会被清掉（`StyleBoxEmpty`），只留我们画的圆形。
	/// </summary>
	public partial class CircleBackButton : Button
	{
		/// <summary>外圈细环颜色</summary>
		[Export] public Color RingColor { get; set; } = new Color(0.86f, 0.87f, 0.9f, 0.5f);

		/// <summary>圆盘底色（深色，压住底图）</summary>
		[Export] public Color DiscColor { get; set; } = new Color(0.05f, 0.05f, 0.07f, 0.92f);

		/// <summary>箭镞 / 迷你星的颜色（米白）</summary>
		[Export] public Color IvoryColor { get; set; } = new Color(0.93f, 0.9f, 0.81f);

		/// <summary>右侧两道月牙的颜色（金）</summary>
		[Export] public Color GoldColor { get; set; } = new Color(0.76f, 0.71f, 0.47f);

		/// <summary>内侧那道细弧的颜色</summary>
		[Export] public Color ArcColor { get; set; } = new Color(0.72f, 0.7f, 0.55f, 0.26f);

		/// <summary>外圈细环的粗细</summary>
		[Export] public float RingWidth { get; set; } = 1.6f;

		private bool _hovered;

		public override void _Ready()
		{
			GD.Print("[CircleBackButton] _Ready called");

			// 去掉主题自带的矩形底板（正常 / 悬停 / 按下 / 焦点 / 禁用 全清），
			// 只留我们自己画的圆形徽章。
			AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
			AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
			AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
			AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());

			MouseEntered += OnMouseEntered;
			MouseExited += OnMouseExited;

			GD.Print("[CircleBackButton] _Ready completed");
		}

		private void OnMouseEntered()
		{
			_hovered = true;
			QueueRedraw();
		}

		private void OnMouseExited()
		{
			_hovered = false;
			QueueRedraw();
		}

		public override void _Draw()
		{
			Vector2 center = Size / 2f;
			float radius = Mathf.Min(Size.X, Size.Y) / 2f - 1f;
			if (radius <= 2f) return;

			// 悬停时整体提亮（不改变大小，避免抖动）
			float glow = _hovered ? 1f : 0f;

			// ① 深色圆盘
			DrawCircle(center, radius - RingWidth, DiscColor);

			// ② 外圈细环
			Color ring = RingColor;
			ring.A = Mathf.Min(1f, ring.A + glow * 0.35f);
			DrawArc(center, radius - RingWidth * 0.5f, 0f, Mathf.Tau, 72, ring, RingWidth, true);

			// ③ 内侧那道细弧（参考图里徽记后方、左下方的那道弧）
			DrawArc(center, radius * 0.72f, Mathf.DegToRad(30f), Mathf.DegToRad(160f), 48, ArcColor, 1.1f, true);

			// ④ 徽记（整组必须落在圆盘内 → 坐标绝对值 ≤ 0.85r）
			Color ivory = IvoryColor.Lerp(new Color(1f, 0.98f, 0.9f), glow * 0.8f);
			Color gold = GoldColor.Lerp(new Color(1f, 0.93f, 0.62f), glow * 0.8f);

			// ④-1 横贯徽记的那根细线（从左尖一直拉到右边）
			Color hairline = ivory;
			hairline.A = 0.85f;
			DrawLine(center + new Vector2(-0.84f * radius, 0f),
					 center + new Vector2(0.84f * radius, 0f),
					 hairline, 1.1f, true);

			// ④-2 米白箭镞：左尖长而细 → 细颈 → 右端外扩
			DrawColoredPolygon(BuildDartPoints(center, radius), ivory);

			// ④-3 紧挨着的迷你四角星
			DrawColoredPolygon(BuildMiniStarPoints(center + new Vector2(0.18f * radius, 0f), radius), ivory);

			// ④-4 右侧上下两道金色月牙（带尖头，用多边形画）
			DrawColoredPolygon(BuildCrescentPoints(center, radius, -1f), gold);
			DrawColoredPolygon(BuildCrescentPoints(center, radius, 1f), gold);
		}

		/// <summary>
		/// 米白箭镞的顶点（单位：圆心 + 半径 × 系数）。
		///
		/// 参考图里那支箭镞是「左尖极细长 → 一段细颈 → 到右边鼓出来」的形状，
		/// 所以左侧两个点压在轴线上（形成细颈），右侧两个点往外扩。
		/// </summary>
		private static Vector2[] BuildDartPoints(Vector2 center, float radius)
		{
			// 顺序：左尖 → 上颈口 → 上外扩 → 右外上 → 右外下 → 下外扩 → 下颈口
			float[,] shape =
			{
				{ -0.80f,  0.00f },
				{ -0.50f, -0.045f },
				{ -0.02f, -0.30f },
				{  0.08f, -0.17f },
				{  0.08f,  0.17f },
				{ -0.02f,  0.30f },
				{ -0.50f,  0.045f },
			};

			var points = new Vector2[shape.GetLength(0)];
			for (int i = 0; i < points.Length; i++)
			{
				points[i] = center + new Vector2(shape[i, 0] * radius, shape[i, 1] * radius);
			}
			return points;
		}

		/// <summary>迷你四角星（尖在上下左右，腰很靠里 → 凹边的闪光形）</summary>
		private static Vector2[] BuildMiniStarPoints(Vector2 center, float radius)
		{
			(float angle, float length)[] pts =
			{
				(180f, 0.090f),
				(225f, 0.026f),
				(270f, 0.090f),
				(315f, 0.026f),
				(0f,   0.090f),
				(45f,  0.026f),
				(90f,  0.090f),
				(135f, 0.026f),
			};

			var points = new Vector2[pts.Length];
			for (int i = 0; i < pts.Length; i++)
			{
				float rad = Mathf.DegToRad(pts[i].angle);
				points[i] = center + new Vector2(Mathf.Cos(rad) * radius * pts[i].length,
												 Mathf.Sin(rad) * radius * pts[i].length);
			}
			return points;
		}

		/// <summary>
		/// 金色月牙（带尖头）。
		///
		/// 用两段二次贝塞尔围成：外弧鼓得多、内弧鼓得少，两头自然收成尖。
		/// <paramref name="sign"/> -1 = 上半，+1 = 下半。
		/// </summary>
		private static Vector2[] BuildCrescentPoints(Vector2 center, float radius, float sign)
		{
			Vector2 p0 = center + new Vector2(0.22f * radius, sign * 0.05f * radius);
			Vector2 p2 = center + new Vector2(0.60f * radius, sign * 0.34f * radius);
			Vector2 cOuter = center + new Vector2(0.26f * radius, sign * 0.36f * radius);
			Vector2 cInner = center + new Vector2(0.40f * radius, sign * 0.13f * radius);

			const int steps = 12;
			var points = new List<Vector2>();

			// 外弧：p0 → p2
			for (int i = 0; i < steps; i++)
			{
				points.Add(Bezier(p0, cOuter, p2, i / (float)(steps - 1)));
			}

			// 内弧：p2 → p0
			for (int i = 0; i < steps; i++)
			{
				points.Add(Bezier(p0, cInner, p2, 1f - i / (float)(steps - 1)));
			}

			return points.ToArray();
		}

		/// <summary>二次贝塞尔求值</summary>
		private static Vector2 Bezier(Vector2 p0, Vector2 c, Vector2 p1, float t)
		{
			float u = 1f - t;
			return u * u * p0 + 2f * u * t * c + t * t * p1;
		}
	}
}
