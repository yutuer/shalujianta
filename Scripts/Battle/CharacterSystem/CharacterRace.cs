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
    public static string GetDisplayName(this CharacterRace race)
    {
        return race switch
        {
            CharacterRace.Chaos => "混沌之域",
            CharacterRace.Abyss => "深海之遗",
            CharacterRace.Flesh => "血肉之沼",
            CharacterRace.Hyperdimension => "超维之旅",
            _ => "未知"
        };
    }

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
