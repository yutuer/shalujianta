namespace MapValidation
{
	/// <summary>
	/// 一条地图问题 —— 必须能定位到**具体格子**并说明原因，不能只说「有问题」。
	/// </summary>
	public class MapIssue
	{
		/// <summary>问题类型</summary>
		public MapIssueType Type;

		/// <summary>出问题的节点 id（-1 表示与具体节点无关的全局问题）</summary>
		public int NodeId = -1;

		/// <summary>节点的可读标签（如 "(3, 5)"）</summary>
		public string NodeLabel = "";

		/// <summary>原因说明</summary>
		public string Reason = "";

		public MapIssue()
		{
		}

		public MapIssue(MapIssueType type, int nodeId, string nodeLabel, string reason)
		{
			Type = type;
			NodeId = nodeId;
			NodeLabel = nodeLabel ?? "";
			Reason = reason ?? "";
		}

		/// <summary>报告里的一行文本</summary>
		public override string ToString()
		{
			string where = NodeId >= 0 ? $"[{NodeLabel}]" : "[全局]";
			return $"{Type} {where} {Reason}";
		}
	}
}
