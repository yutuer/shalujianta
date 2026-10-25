using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 必杀技效果执行器
/// 负责按 ultimates.json 的字段结算：伤害 / 治疗 / 护盾 / 抽牌 / 能量 / 附加减益
/// 并输出中文执行摘要，便于日志与测试断言
/// </summary>
public static class UltimateExecutor
{
    /// <summary>
    /// 执行必杀技的所有字段效果
    /// </summary>
    /// <param name="skill">必杀技数据</param>
    /// <param name="player">玩家（可为空，空时跳过治疗/护盾/抽牌/能量）</param>
    /// <param name="enemies">敌人数组（可为空，空时跳过伤害与减益）</param>
    /// <returns>中文执行摘要，例如「全体敌人受20伤害，抽3张」</returns>
    public static string Execute(UltimateSkill skill, Player player, Enemy[] enemies)
    {
        if (skill == null)
        {
            GD.PrintErr("[UltimateExecutor] Execute called: 必杀技数据为空，无法结算");
            return "必杀技数据缺失";
        }

        GD.Print($"[UltimateExecutor] Execute called: {skill.SkillId}（{skill.Name}）");

        var summary = new List<string>();

        List<Enemy> aliveEnemies = GetAliveEnemies(enemies);
        List<Enemy> targets = ResolveTargets(skill, aliveEnemies);

        ApplyDamage(skill, targets, summary);
        ApplyDebuff(skill, targets, summary);
        ApplyHeal(skill, player, summary);
        ApplyShield(skill, player, summary);
        ApplyDrawCards(skill, player, summary);
        ApplyEnergy(skill, player, summary);

        string result = summary.Count > 0 ? string.Join("，", summary) : "无实际效果";
        GD.Print($"[UltimateExecutor] Execute completed: {skill.Name} -> {result}");
        return result;
    }

    /// <summary>
    /// 取出所有存活敌人
    /// </summary>
    private static List<Enemy> GetAliveEnemies(Enemy[] enemies)
    {
        var alive = new List<Enemy>();
        if (enemies == null)
        {
            return alive;
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead)
            {
                continue;
            }

            alive.Add(enemy);
        }

        return alive;
    }

    /// <summary>
    /// 按 IsAreaAttack / Target 解析受击目标
    /// front=Position 最小者，rear=Position 最大者，random=随机存活敌人，其它=第一个存活敌人
    /// </summary>
    private static List<Enemy> ResolveTargets(UltimateSkill skill, List<Enemy> aliveEnemies)
    {
        var targets = new List<Enemy>();
        if (aliveEnemies.Count == 0)
        {
            return targets;
        }

        if (skill.IsAreaAttack)
        {
            targets.AddRange(aliveEnemies);
            return targets;
        }

        Enemy selected = null;
        switch (skill.Target?.ToLower())
        {
            case "front":
                selected = aliveEnemies[0];
                foreach (Enemy enemy in aliveEnemies)
                {
                    if (enemy.Position < selected.Position)
                    {
                        selected = enemy;
                    }
                }
                break;

            case "rear":
                selected = aliveEnemies[0];
                foreach (Enemy enemy in aliveEnemies)
                {
                    if (enemy.Position > selected.Position)
                    {
                        selected = enemy;
                    }
                }
                break;

            case "random":
                var random = new RandomNumberGenerator();
                random.Randomize();
                selected = aliveEnemies[random.RandiRange(0, aliveEnemies.Count - 1)];
                break;

            default:
                selected = aliveEnemies[0];
                break;
        }

        if (selected != null)
        {
            targets.Add(selected);
        }

        return targets;
    }

    /// <summary>
    /// 伤害结算
    /// </summary>
    private static void ApplyDamage(UltimateSkill skill, List<Enemy> targets, List<string> summary)
    {
        int damage = skill.CalculatedDamage;
        if (damage <= 0 || targets.Count == 0)
        {
            return;
        }

        foreach (Enemy enemy in targets)
        {
            int healthBefore = enemy.CurrentHealth;
            enemy.TakeDamage(damage);
            GD.Print($"[UltimateExecutor] 【{skill.Name}】对 {enemy.EnemyName} 造成 {damage} 点伤害（生命 {healthBefore} → {enemy.CurrentHealth}）");
        }

        if (skill.IsAreaAttack)
        {
            summary.Add($"全体敌人受{damage}伤害");
        }
        else
        {
            summary.Add($"{targets[0].EnemyName}受{damage}伤害");
        }
    }

    /// <summary>
    /// 附加减益（poison / weak / slow / silence）
    /// </summary>
    private static void ApplyDebuff(UltimateSkill skill, List<Enemy> targets, List<string> summary)
    {
        string debuffName = skill.ApplyDebuffName?.ToLower();
        if (string.IsNullOrEmpty(debuffName) || targets.Count == 0)
        {
            return;
        }

        int buffValue = skill.CalculatedDebuffValue;
        int buffDuration = skill.BuffDuration;

        foreach (Enemy enemy in targets)
        {
            StatusEffect debuff = CreateDebuff(debuffName, buffValue, buffDuration);
            if (debuff == null)
            {
                GD.PrintErr($"[UltimateExecutor] 未知的减益类型：{debuffName}，已跳过附加");
                continue;
            }

            enemy.AddStatusEffect(debuff);
            GD.Print($"[UltimateExecutor] 【{skill.Name}】为 {enemy.EnemyName} 附加减益 {debuff.EffectName}（数值 {buffValue}，持续 {buffDuration} 回合）");
        }

        summary.Add($"附加{debuffName}（数值{buffValue}，持续{buffDuration}回合）");
    }

    /// <summary>
    /// 按减益名创建减益实例（Duration 与数值都显式赋值，不使用类内默认值）
    /// </summary>
    private static StatusEffect CreateDebuff(string debuffName, int buffValue, int buffDuration)
    {
        switch (debuffName)
        {
            case "poison":
                return new PoisonDebuff
                {
                    DamagePerTurn = buffValue,
                    Duration = buffDuration
                };

            case "weak":
                return new WeakDebuff
                {
                    AttackReduction = buffValue,
                    Duration = buffDuration
                };

            case "slow":
                return new SlowDebuff
                {
                    EnergyReduction = buffValue,
                    Duration = buffDuration
                };

            case "silence":
                return new SilenceDebuff
                {
                    Duration = buffDuration
                };

            default:
                return null;
        }
    }

    /// <summary>
    /// 治疗结算
    /// </summary>
    private static void ApplyHeal(UltimateSkill skill, Player player, List<string> summary)
    {
        int heal = skill.CalculatedHeal;
        if (heal <= 0)
        {
            return;
        }

        if (player == null)
        {
            GD.PrintErr($"[UltimateExecutor] 【{skill.Name}】需要玩家才能治疗，已跳过");
            return;
        }

        int healthBefore = player.CurrentHealth;
        player.Heal(heal);
        GD.Print($"[UltimateExecutor] 【{skill.Name}】回复 {heal} 点生命（生命 {healthBefore} → {player.CurrentHealth}）");
        summary.Add($"回复{heal}生命");
    }

    /// <summary>
    /// 护盾结算
    /// </summary>
    private static void ApplyShield(UltimateSkill skill, Player player, List<string> summary)
    {
        int shield = skill.CalculatedShield;
        if (shield <= 0)
        {
            return;
        }

        if (player == null)
        {
            GD.PrintErr($"[UltimateExecutor] 【{skill.Name}】需要玩家才能获得护盾，已跳过");
            return;
        }

        int shieldBefore = player.Shield;
        player.AddShield(shield);
        GD.Print($"[UltimateExecutor] 【{skill.Name}】获得 {shield} 点护盾（护盾 {shieldBefore} → {player.Shield}）");
        summary.Add($"获得{shield}护盾");
    }

    /// <summary>
    /// 抽牌结算
    /// </summary>
    private static void ApplyDrawCards(UltimateSkill skill, Player player, List<string> summary)
    {
        int drawCount = skill.CalculatedDrawCount;
        if (drawCount <= 0)
        {
            return;
        }

        if (player == null)
        {
            GD.PrintErr($"[UltimateExecutor] 【{skill.Name}】需要玩家才能抽牌，已跳过");
            return;
        }

        int handBefore = player.Hand.Count;
        player.DrawCards(drawCount);
        GD.Print($"[UltimateExecutor] 【{skill.Name}】抽 {drawCount} 张牌（手牌 {handBefore} → {player.Hand.Count}）");
        summary.Add($"抽{drawCount}张");
    }

    /// <summary>
    /// 能量结算
    /// </summary>
    private static void ApplyEnergy(UltimateSkill skill, Player player, List<string> summary)
    {
        if (skill.EnergyGain <= 0)
        {
            return;
        }

        if (player == null)
        {
            GD.PrintErr($"[UltimateExecutor] 【{skill.Name}】需要玩家才能获得能量，已跳过");
            return;
        }

        int energyBefore = player.CurrentEnergy;
        player.CurrentEnergy += skill.EnergyGain;
        GD.Print($"[UltimateExecutor] 【{skill.Name}】获得 {skill.EnergyGain} 点能量（能量 {energyBefore} → {player.CurrentEnergy}）");
        summary.Add($"获得{skill.EnergyGain}能量");
    }
}
