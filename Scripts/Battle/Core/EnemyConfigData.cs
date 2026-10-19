using System.Collections.Generic;

namespace FishEatFish.Battle.Core
{
	/// <summary>
	/// 敌人波次数据（对应 Data/enemies.json 中的一条 wave）
	/// </summary>
	public class EnemyWaveConfig
	{
		public string WaveKey { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public List<EnemyDefinition> Enemies { get; set; } = new List<EnemyDefinition>();
	}

	/// <summary>
	/// 单个敌人的配置数据
	/// </summary>
	public class EnemyDefinition
	{
		public string Name { get; set; } = "Enemy";
		public int MaxHealth { get; set; } = 50;
		public int Attack { get; set; } = 10;
		public int Defense { get; set; } = 0;
		public int Position { get; set; } = 0;
		public string Behavior { get; set; } = "balanced";
		public bool IsElite { get; set; } = false;
		public bool IsBoss { get; set; } = false;
	}
}
