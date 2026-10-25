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
    /// <summary>
    /// 易伤强度：敌人**受到伤害增加的百分比**。
    /// </summary>
    [Export]
    public int DamageIncrease { get; set; } = 50;

    public VulnerableDebuff()
    {
        EffectName = "Vulnerable";
        Description = "受到伤害+%d%%";
        Duration = 2;
        EffectType = StatusEffectType.Debuff;
    }

    /// <summary>
    /// 易伤 = 受到伤害 +N%。
    ///
    /// ⚠️ 原来这里靠 <c>_owner</c> 判断「被打的是不是被施加的那个敌人」，
    /// 但 <c>Enemy.AddStatusEffect</c> **从不调用 SetOwner** → _owner 恒为 null →
    /// 判定恒不成立 → **易伤从来没生效过**（狂气爆发「龙王咆哮」的毒+易伤都打不出来）。
    /// 现在改成不依赖 owner：易伤总是施加给承受伤害的那个敌人自己。
    /// </summary>
    public override int ModifyDamageEnemy(int baseDamage, Core.Enemy attacker, Core.Enemy defender)
    {
        return baseDamage + baseDamage * DamageIncrease / 100;
    }
}

public partial class PoisonDebuff : StatusEffect
{
    /// <summary>
    /// **中毒层数** = 回合结束时受到的纯粹伤害。
    ///
    /// 照参考游戏《终末前夜 Morimens》「☠中毒」词条：「**回合结束时受到等量层数的伤害。**」
    /// 「固定伤害」词条：「不被力量、虚弱、易伤等状态影响，直接造成伤害。」
    /// → 所以这里的伤害**不走防御、不吃易伤/破甲**（见 <see cref="Core.Enemy.TakeFixedDamage"/>）。
    ///
    /// ⚠️ 语义变了：以前这个字段是「每回合固定掉多少血」（12 生肖那几个狂气爆发按这个口径配），
    /// 现在它就是**层数**——施加 41 层 → 回合结束掉 41 点；再叠加 30 层 → 掉 71 点。
    /// </summary>
    [Export]
    public int DamagePerTurn { get; set; } = 3;

    public PoisonDebuff()
    {
        EffectName = "Poison";
        Description = "毎ターン %d の毒ダメージ";
        Duration = 3;
        EffectType = StatusEffectType.Debuff;
    }

    /// <summary>
    /// 再次施加中毒时**层数累加**（不是覆盖）——
    /// 「等量层数」的伤害要求层数能累起来：41 + 30 = 71 层 → 回合结束掉 71 点。
    /// </summary>
    public override void MergeStacks(StatusEffect incoming)
    {
        base.MergeStacks(incoming);
        if (incoming is PoisonDebuff p)
        {
            DamagePerTurn += p.DamagePerTurn;
        }
    }

    public override void OnTurnEndEnemy(Core.Enemy target)
    {
        base.OnTurnEndEnemy(target);
        // ⚠️ 固定伤害：不走防御、不吃易伤/破甲（参考游戏的「固定中毒」）
        target.TakeFixedDamage(DamagePerTurn);
        GD.Print($"[PoisonDebuff] {target.EnemyName} 回合结束受到 {DamagePerTurn} 点中毒伤害（{DamagePerTurn} 层）");
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
    /// <summary>
    /// 破甲强度：敌人**受到伤害增加的百分比**。
    ///
    /// 字段名从 <c>DefenseReduction</c> 改为 <c>DamageIncreasePercent</c>，
    /// 因为实现方式变了（见下方说明）—— 现在它表示「受到伤害 +N%」，
    /// 而不是「防御值 -N」。旧名保留为别名，避免外部引用编译不过。
    /// </summary>
    [Export]
    public int DamageIncreasePercent { get; set; } = 10;

    /// <summary>旧字段名（= <see cref="DamageIncreasePercent"/>），保留兼容</summary>
    [Export]
    public int DefenseReduction
    {
        get => DamageIncreasePercent;
        set => DamageIncreasePercent = value;
    }

    public DefenseDownDebuff()
    {
        EffectName = "DefenseDown";
        Description = "受到伤害+%d%%";
        Duration = 3;
        EffectType = StatusEffectType.Debuff;
    }

    /// <summary>
    /// 破甲不再直接改写 <c>Enemy.Defense</c>，而是**参与伤害计算**。
    ///
    /// 为什么改：直接减属性的做法在「防御本来就是 0」的敌人身上完全失效
    /// （<c>Mathf.Max(0, 0 - 10)</c> 仍是 0）—— 减益看起来挂了却毫无作用。
    /// 改成参与伤害计算后，破甲 = 「受到伤害 +N%」，对任何敌人都成立，
    /// 语义也和参考图里的「破甲」一致。
    ///
    /// ⚠️ 这里**不依赖 _owner**：<c>Enemy.AddStatusEffect</c> 从不调用 <c>SetOwner</c>，
    /// 所以凡是用 _owner 判断归属的减益（易伤）实际上都不生效。
    /// 本减益由「施加者就是承受者」这一事实保证正确性 —— 破甲总是施加给敌人自己。
    /// </summary>
    /// <param name="baseDamage">减伤前的伤害</param>
    public override int ModifyDamageEnemy(int baseDamage, Core.Enemy attacker, Core.Enemy defender)
    {
        return baseDamage + baseDamage * DamageIncreasePercent / 100;
    }
}
