namespace FishEatFish.Battle.CharacterSystem;

public enum CharacterRace
{
    Chaos,
    Abyss,
    Flesh,
    Hyperdimension
}

public static class CharacterRaceExtensions
{
    /// <summary>
    /// 界域显示名 —— **wiki 口径**（主人 2026-11-04 拍板）。
    ///
    /// wiki 角色页字段是 `|界域=超维`（短名），正文里称「超维界域」「血肉界域」；
    /// 这里用正文的「X界域」写法。
    ///
    /// ⚠️ 之前这里是「混沌之域 / 深海之遗 / 血肉之沼 / 超维之旅」——
    /// 那 4 个名字 **wiki 里根本不存在**（来源不明），已按 wiki 口径改掉。
    /// </summary>
    public static string GetDisplayName(this CharacterRace race)
    {
        return race switch
        {
            CharacterRace.Chaos => "混沌界域",
            CharacterRace.Abyss => "深海界域",
            CharacterRace.Flesh => "血肉界域",
            CharacterRace.Hyperdimension => "超维界域",
            _ => "未知"
        };
    }

    /// <summary>
    /// 界域短名 —— wiki 角色页 `|界域=` 字段的原值（超维 / 混沌 / 深海 / 血肉）。
    /// </summary>
    public static string GetShortName(this CharacterRace race)
    {
        return race switch
        {
            CharacterRace.Chaos => "混沌",
            CharacterRace.Abyss => "深海",
            CharacterRace.Flesh => "血肉",
            CharacterRace.Hyperdimension => "超维",
            _ => "?"
        };
    }

    public static CharacterRace FromString(string value)
    {
        return value.ToLower() switch
        {
            "chaos" => CharacterRace.Chaos,
            "abyss" => CharacterRace.Abyss,
            "flesh" => CharacterRace.Flesh,
            "hyperdimension" => CharacterRace.Hyperdimension,
            _ => CharacterRace.Chaos
        };
    }
}
