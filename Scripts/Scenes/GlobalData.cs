using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Scenes
{
    /// <summary>
    /// 全局流程状态：当前关卡、已选角色、已装备钥令、是否进入六边形地图模式。
    ///
    /// 这些量跨场景存活（场景切换时不会重建），所以放在静态类里。
    /// </summary>
    public static class GlobalData
    {
        public static MapDefinition CurrentMap { get; set; }
        public static CharacterDefinition[] SelectedCharacters { get; set; }
        public static KeyOrder EquippedKeyOrder { get; set; }
        public static bool EnterHexMapMode { get; set; } = false;

        /// <summary>
        /// **调查级死亡抵抗状态**（D66）—— 「持续到调查结束」的跨战斗存量。
        ///
        /// 为什么挂在这儿：死亡抵抗要**跨战斗**存活（触发后减半的效果持续到调查结束），
        /// 而 <c>GlobalData</c> 本来就是「跨场景存活」的状态容器（CurrentMap / SelectedCharacters）。
        ///
        /// ⚠️ 换新地图 = 换新调查 → 调用 <see cref="DeathResistanceSystem.ResetForNewInvestigation"/> 重置。
        /// </summary>
        public static DeathResistanceSystem DeathResistance { get; } = new DeathResistanceSystem();
    }
}
