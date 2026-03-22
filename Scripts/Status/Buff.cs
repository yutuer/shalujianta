using Godot;

public partial class StrengthBuff : StatusEffect
{
    [Export]
    public int AttackBonus { get; set; } = 3;

    public StrengthBuff()
    {
        EffectName = "Strength";
        Description = "攻击力が+%d 増加";
        Duration = 3;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnDamageDealt;
    }

    public override void OnApplyEnemy(Enemy target)
    {
        target.Attack += AttackBonus;
    }

    public override void OnRemoveEnemy(Enemy target)
    {
        target.Attack -= AttackBonus;
    }

    public override int ModifyDamageEnemy(int baseDamage, Enemy attacker, Enemy defender)
    {
        return baseDamage + AttackBonus;
    }
}

public partial class DefenseBuff : StatusEffect
{
    [Export]
    public int DefenseBonus { get; set; } = 5;

    public DefenseBuff()
    {
        EffectName = "Defense";
        Description = "防御力が+%d 増加";
        Duration = 3;
        EffectType = StatusEffectType.Buff;
    }

    public override void OnApplyEnemy(Enemy target)
    {
        target.Defense += DefenseBonus;
    }

    public override void OnRemoveEnemy(Enemy target)
    {
        target.Defense -= DefenseBonus;
    }
}

public partial class RegenerationBuff : StatusEffect
{
    [Export]
    public int HealPerTurn { get; set; } = 5;

    public RegenerationBuff()
    {
        EffectName = "Regeneration";
        Description = "毎ターン %d 回復";
        Duration = 3;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnTurnEnd;
    }

    public override void OnTurnEndEnemy(Enemy target)
    {
        base.OnTurnEndEnemy(target);
        target.Heal(HealPerTurn);
    }
}

public partial class ThornsBuff : StatusEffect
{
    [Export]
    public int DamageReflect { get; set; } = 3;

    public ThornsBuff()
    {
        EffectName = "Thorns";
        Description = "攻撃者に %d のダメージを反射";
        Duration = 2;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnDamageReceived;
    }

    public override int ModifyDamageEnemy(int baseDamage, Enemy attacker, Enemy defender)
    {
        return baseDamage;
    }
}

public partial class FuryBuff : StatusEffect
{
    [Export]
    public int AttackMultiplier { get; set; } = 2;

    public FuryBuff()
    {
        EffectName = "Fury";
        Description = "HPが低いほど攻撃力増加";
        Duration = 99;
        EffectType = StatusEffectType.Buff;
        Trigger = EffectTrigger.OnDamageDealt;
    }

    public override int ModifyDamageEnemy(int baseDamage, Enemy attacker, Enemy defender)
    {
        float healthPercent = attacker.GetHealthPercent();
        if (healthPercent < 0.3f)
        {
            return baseDamage + AttackMultiplier;
        }
        return baseDamage;
    }
}
