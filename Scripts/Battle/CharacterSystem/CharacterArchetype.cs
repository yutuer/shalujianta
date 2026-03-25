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
}
