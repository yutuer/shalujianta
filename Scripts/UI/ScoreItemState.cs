namespace FishEatFish.UI
{
	/// <summary>
	/// 评分项的三种状态（参考图 R-04）：
	///   Active   —— 已达成   ✓ 绿勾 + 绿色文字
	///   Expired  —— 已失效   ✗ 红叉 + 暗灰文字（如「每日首次」今天已经用掉了）
	///   Pending  —— 未触发   • 白点 + 白色文字
	/// </summary>
	public enum ScoreItemState
	{
		Active,
		Expired,
		Pending
	}
}
