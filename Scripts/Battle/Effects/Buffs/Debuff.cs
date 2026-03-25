using Godot;
using FishEatFish.Battle.Effects;

namespace FishEatFish.Battle.Effects.Buffs;

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
    }

    public override void OnApplyEnemy(Core.Enemy target)
    {
        base.OnApplyEnemy(target);
        target.Attack = Mathf.Max(0, target.Attack - AttackReduction);
    }

    public override void OnRemoveEnemy(Core.Enemy target)
    {
        target.Attack += AttackReduction;
    }

    public override int ModifyDamageEnemy(int baseDamage, Core.Enemy attacker, Core.Enemy defender)
    {
        if (attacker == defender)
        {
            return baseDamage - AttackReduction;
        }
        return baseDamage;
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
    }

    public override int ModifyDamageEnemy(int baseDamage, Core.Enemy attacker, Core.Enemy defender)
    {
        if (defender == GetOwner())
        {
            return baseDamage + DamageIncrease;
        }
        return baseDamage;
    }

    private Core.IUnit _owner;

    public void SetOwner(Core.IUnit owner)
    {
        _owner = owner;
    }

    private Core.IUnit GetOwner()
    {
        return _owner;
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
    }

    public override void OnTurnEndEnemy(Core.Enemy target)
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
    }

    public override void OnApplyEnemy(Core.Enemy target)
    {
    }
}

public partial class DefenseDownDebuff : StatusEffect
{
    [Export]
    public int DefenseReduction { get; set; } = 10;

    public DefenseDownDebuff()
    {
        EffectName = "DefenseDown";
        Description = "防御力が %d 減少";
        Duration = 3;
        EffectType = StatusEffectType.Debuff;
    }

    public override void OnApplyEnemy(Core.Enemy target)
    {
        base.OnApplyEnemy(target);
        target.Defense = Mathf.Max(0, target.Defense - DefenseReduction);
    }

    public override void OnRemoveEnemy(Core.Enemy target)
    {
        target.Defense += DefenseReduction;
    }
}
