using Godot;

public partial class WeakDebuff : StatusEffect
{
    [Export]
    public int AttackReduction { get; set; } = 3;

    public WeakDebuff()
    {
        EffectName = "Weak";
        Description = "攻撃力が %d 減少";
        Duration = 2;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnDamageDealt;
    }

    public override void OnApplyEnemy(Enemy target)
    {
        target.Attack = Mathf.Max(0, target.Attack - AttackReduction);
    }

    public override void OnRemoveEnemy(Enemy target)
    {
        target.Attack += AttackReduction;
    }

    public override int ModifyDamageEnemy(int baseDamage, Enemy attacker, Enemy defender)
    {
        return baseDamage - AttackReduction;
    }
}

public partial class VulnerableDebuff : StatusEffect
{
    [Export]
    public int DamageIncrease { get; set; } = 3;

    public VulnerableDebuff()
    {
        EffectName = "Vulnerable";
        Description = "受けるダメージが %d 増加";
        Duration = 2;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnDamageReceived;
    }

    public override int ModifyDamageEnemy(int baseDamage, Enemy attacker, Enemy defender)
    {
        return baseDamage + DamageIncrease;
    }
}

public partial class PoisonDebuff : StatusEffect
{
    [Export]
    public int DamagePerTurn { get; set; } = 3;

    public PoisonDebuff()
    {
        EffectName = "Poison";
        Description = "毎ターン %d の毒ダメージ";
        Duration = 3;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnTurnEnd;
    }

    public override void OnTurnEndEnemy(Enemy target)
    {
        base.OnTurnEndEnemy(target);
        target.TakeDamage(DamagePerTurn);
    }
}

public partial class SlowDebuff : StatusEffect
{
    [Export]
    public int EnergyReduction { get; set; } = 1;

    public SlowDebuff()
    {
        EffectName = "Slow";
        Description = "毎ターン %d のエネルギーを失う";
        Duration = 2;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnTurnStart;
    }
}

public partial class SilenceDebuff : StatusEffect
{
    public SilenceDebuff()
    {
        EffectName = "Silence";
        Description = "カードを使えない";
        Duration = 1;
        EffectType = StatusEffectType.Debuff;
        Trigger = EffectTrigger.OnApply;
    }

    public override void OnApplyEnemy(Enemy target)
    {
    }
}
