using Godot;

public partial class SilverKeyConfig : Resource
{
    [Export]
    public int BaseMaxSilverKey = 1000;

    [Export]
    public int MaxStack = 2;

    [Export]
    public float SilverPerEnergy = 50f;

    public int MaxStackSilverKey => BaseMaxSilverKey * MaxStack;

    public static SilverKeyConfig CreateDefault()
    {
        var config = new SilverKeyConfig();
        return config;
    }
}

public enum KeyOrderEffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Special
}
