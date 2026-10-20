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
    }
}
