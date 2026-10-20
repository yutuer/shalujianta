using System.Collections.Generic;

namespace FishEatFish.Battle.HexMap
{
	/// <summary>
	/// 视野（FOV）计算规则 —— 可插拔。
	///
	/// 战争迷雾的「哪些格子被照亮」完全由这个接口决定：
	/// 换掉实现就换一套照亮规则，不需要动 HexMapController 和 UI。
	///
	/// 现有实现：
	///   - <see cref="RadiusFovRule"/>   纯半径（默认，无视线阻挡）
	///   - <see cref="BlockingFovRule"/> 半径 + 视线阻挡（<see cref="HexTile.IsVisionBlocker"/>）
	/// </summary>
	public interface IFovRule
	{
		/// <summary>
		/// 算出以 <paramref name="origin"/> 为中心、半径 <paramref name="radius"/> 内的可见格子。
		/// </summary>
		/// <param name="origin">视野中心（通常是玩家所在格子）</param>
		/// <param name="radius">视野半径（0 = 只有中心）</param>
		/// <param name="map">地图（用来判断格子是否存在、是否阻挡视线）</param>
		List<HexCoord> GetVisibleTiles(HexCoord origin, int radius, HexMap map);
	}

	/// <summary>
	/// 默认规则：纯半径，不考虑任何遮挡。
	/// 地图上目前没有墙，所以这是绝大多数情况下的正确行为。
	/// </summary>
	public class RadiusFovRule : IFovRule
	{
		public List<HexCoord> GetVisibleTiles(HexCoord origin, int radius, HexMap map)
		{
			List<HexCoord> visible = new List<HexCoord>();
			if (map == null || radius < 0) return visible;

			foreach (HexTile tile in map.GetAllTiles())
			{
				if (origin.DistanceTo(tile.Coord) <= radius)
				{
					visible.Add(tile.Coord);
				}
			}

			return visible;
		}
	}

	/// <summary>
	/// 带视线阻挡的规则：<see cref="HexTile.IsVisionBlocker"/> 为 true 的格子会挡住它身后的视野。
	///
	/// 视线判定用标准 hex line：在 origin 与目标之间线性插值取样，
	/// 只要线上有阻挡格（且阻挡格不在目标本身），目标就看不见。
	/// </summary>
	public class BlockingFovRule : IFovRule
	{
		public List<HexCoord> GetVisibleTiles(HexCoord origin, int radius, HexMap map)
		{
			List<HexCoord> visible = new List<HexCoord>();
			if (map == null || radius < 0) return visible;

			foreach (HexTile tile in map.GetAllTiles())
			{
				int distance = origin.DistanceTo(tile.Coord);
				if (distance > radius) continue;

				if (distance == 0 || !IsBlocked(origin, tile.Coord, map))
				{
					visible.Add(tile.Coord);
				}
			}

			return visible;
		}

		/// <summary>origin 到 target 的视线上是否有阻挡格</summary>
		private static bool IsBlocked(HexCoord origin, HexCoord target, HexMap map)
		{
			List<HexCoord> line = HexLine(origin, target);
			// 起点和终点本身不算阻挡（站在墙里也能看见自己脚下的格子）
			for (int i = 1; i < line.Count - 1; i++)
			{
				HexTile tile = map.GetTile(line[i]);
				if (tile != null && tile.IsVisionBlocker) return true;
			}
			return false;
		}

		/// <summary>两点之间的 hex 直线（含首尾）</summary>
		public static List<HexCoord> HexLine(HexCoord a, HexCoord b)
		{
			List<HexCoord> line = new List<HexCoord>();
			int distance = a.DistanceTo(b);
			if (distance <= 0)
			{
				line.Add(a);
				return line;
			}

			// cube 坐标：x = q, z = r, y = -q - r
			float ax = a.Q, az = a.R, ay = -ax - az;
			float bx = b.Q, bz = b.R, by = -bx - bz;

			for (int i = 0; i <= distance; i++)
			{
				float t = (float)i / distance;
				line.Add(CubeRound(
					ax + (bx - ax) * t,
					ay + (by - ay) * t,
					az + (bz - az) * t));
			}

			return line;
		}

		/// <summary>cube 坐标四舍五入回 axial 坐标</summary>
		private static HexCoord CubeRound(float x, float y, float z)
		{
			float rx = (float)System.Math.Round(x);
			float ry = (float)System.Math.Round(y);
			float rz = (float)System.Math.Round(z);

			float dx = System.Math.Abs(rx - x);
			float dy = System.Math.Abs(ry - y);
			float dz = System.Math.Abs(rz - z);

			if (dx > dy && dx > dz)
			{
				rx = -ry - rz;
			}
			else if (dy > dz)
			{
				ry = -rx - rz;
			}
			else
			{
				rz = -rx - ry;
			}

			return new HexCoord((int)rx, (int)rz);
		}
	}

	/// <summary>
	/// FOV 计算入口：默认走纯半径规则，需要换规则时传 rule 或改 <see cref="HexMapController.FovRule"/>。
	/// </summary>
	public static class FovCalculator
	{
		/// <summary>默认规则（纯半径）</summary>
		public static IFovRule DefaultRule { get; } = new RadiusFovRule();

		/// <summary>带视线阻挡的规则（备用）</summary>
		public static IFovRule BlockingRule { get; } = new BlockingFovRule();

		/// <summary>按规则算出可见格子</summary>
		public static List<HexCoord> Compute(HexCoord origin, int radius, HexMap map, IFovRule rule = null)
		{
			IFovRule effective = rule ?? DefaultRule;
			return effective.GetVisibleTiles(origin, radius, map);
		}
	}
}
