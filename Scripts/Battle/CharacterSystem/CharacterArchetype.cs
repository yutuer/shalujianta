namespace FishEatFish.Battle.CharacterSystem;

public enum CharacterArchetype
{
    Striker,
    Counter,
    Poison,
    Healer,
    Debuffer,
    Tank,
    Mage
}

public static class CharacterArchetypeExtensions
{
    public static string GetDisplayName(this CharacterArchetype archetype)
    {
        return archetype switch
        {
            CharacterArchetype.Striker => "追击者",
            CharacterArchetype.Counter => "反击者",
            CharacterArchetype.Poison => "毒师",
            CharacterArchetype.Healer => "治疗者",
            CharacterArchetype.Debuffer => "削弱者",
            CharacterArchetype.Tank => "坦克",
            CharacterArchetype.Mage => "法师",
            _ => "未知"
        };
    }

    public static string GetShortName(this CharacterArchetype archetype)
    {
        return archetype switch
        {
            CharacterArchetype.Striker => "追击",
            CharacterArchetype.Counter => "反击",
            CharacterArchetype.Poison => "毒师",
            CharacterArchetype.Healer => "治疗",
            CharacterArchetype.Debuffer => "削弱",
            CharacterArchetype.Tank => "坦克",
            CharacterArchetype.Mage => "法师",
            _ => "?"
        };
    }

    public static CharacterArchetype FromString(string value)
    {
        return value.ToLower() switch
        {
            "striker" => CharacterArchetype.Striker,
            "counter" => CharacterArchetype.Counter,
            "poison" => CharacterArchetype.Poison,
            "healer" => CharacterArchetype.Healer,
            "debuffer" => CharacterArchetype.Debuffer,
            "tank" => CharacterArchetype.Tank,
            "mage" => CharacterArchetype.Mage,
            _ => CharacterArchetype.Striker
        };
    }

    /// <summary>
    /// **三分类定位**（伤害 / 辅助 / 防御）—— 对照参考图「人物列表」右侧的类型标签
    /// （波吕克斯那张图上写的是「伤害型」）。
    ///
    /// ⚠️ **暂按 Archetype 映射**：主人 2026-10-24 说「定位以后给具体名称，
    /// 参考图截图上有该角色的类型」→ 截图到手后改这一个函数即可，界面不用动。
    /// 校验：波吕克斯的 Archetype 是 Striker → 映射出「伤害」，与参考图一致 ✓
    /// </summary>
    public static string GetRoleType(this CharacterArchetype archetype)
    {
        return archetype switch
        {
            CharacterArchetype.Striker => "伤害",
            CharacterArchetype.Mage => "伤害",
            CharacterArchetype.Poison => "伤害",
            CharacterArchetype.Healer => "辅助",
            CharacterArchetype.Debuffer => "辅助",
            CharacterArchetype.Tank => "防御",
            CharacterArchetype.Counter => "防御",
            _ => "伤害"
        };
    }
}
