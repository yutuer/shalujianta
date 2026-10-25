using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 狂气爆发效果执行器
/// 负责按 ultimates.json 的字段结算：伤害 / 治疗 / 护盾 / 抽牌 / 能量 / 附加减益
/// 并输出中文执行摘要，便于日志与测试断言
/// </summary>
public static class UltimateExecutor
{
    /// <summary>
    /// 执行狂气爆发的所有字段效果
    /// </summary>
    /// <param name="skill">狂气爆发数据</param>
    /// <param name="player">玩家（可为空，空时跳过治疗/护盾/抽牌/能量）</param>
    /// <param name="enemies">敌人数组（可为空，空时跳过伤害与减益）</param>
    /// <param name="owner">释放者（置入手中的衍生卡靠它归属，可为空）</param>
    /// <returns>中文执行摘要，例如「全体敌人受20伤害，抽3张」</returns>
    public static string Execute(UltimateSkill skill, Player player, Enemy[] enemies, CharacterPosition owner = null)
    {
        if (skill == null)
        {
            GD.PrintErr("[UltimateExecutor] Execute called: 狂气爆发数据为空，无法结算");
            return "狂气爆发数据缺失";
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
        ApplyRemoveSlowFromHand(skill, player, summary);
        ApplyAddCardToHand(skill, player, owner, summary);

        string result = summary.Count > 0 ? string.Join("，", summary) : "无实际效果";
        GD.Print($"[UltimateExecutor] Execute completed: {skill.Name} -> {result}");
        return result;
    }

    /// <summary>
    /// **移除所有手牌的「迟缓」状态**（珈伦「客官请慢用！」）。
    ///
    /// 照参考游戏《终末前夜 Morimens》：「移除所有手牌的 ⏱迟缓状态。」
    /// 「迟缓」是**卡牌身上的状态**（使这张卡的算力消耗提高），
    /// 不是敌人身上的减益 —— 所以这里改的是手牌，不是 <c>player.StatusEffects</c>。
    /// </summary>
    private static void ApplyRemoveSlowFromHand(UltimateSkill skill, Player player, List<string> summary)
    {
        if (!skill.RemoveSlowFromHand || player == null) return;

        int removed = 0;
        foreach (Card.Card card in player.Hand)
        {
            if (card == null || card.SlowStacks <= 0) continue;
            card.SlowStacks = 0;
            card.RecalcCost();
            removed++;
        }

        GD.Print($"[UltimateExecutor] 【{skill.Name}】移除手牌迟缓状态 {removed} 张");
        summary.Add(removed > 0 ? $"移除{removed}张手牌的迟缓状态" : "手牌没有迟缓状态");
    }

    /// <summary>
    /// **把衍生卡置入手中**（珈伦「客官请慢用！」→ 1 张「奇妙料理」）。
    ///
    /// 衍生卡不在卡组映射里（卡组只有打击 / 防御 / 专属卡），
    /// 所以这里从 <c>character_cards.json</c> 取模板 → 克隆一份独立实例 →
    /// 挂上释放者的属性与归属 → 放进手牌。
    /// </summary>
    private static void ApplyAddCardToHand(UltimateSkill skill, Player player, CharacterPosition owner, List<string> summary)
    {
        if (string.IsNullOrEmpty(skill.AddCardToHandId) || skill.AddCardToHandCount <= 0 || player == null) return;

        if (!CharacterCardLoader.TryGetCharacterCard(skill.AddCardToHandId, out Card.Card template))
        {
            GD.PrintErr($"[UltimateExecutor] 【{skill.Name}】衍生卡 {skill.AddCardToHandId} 不在 character_cards.json 里，无法置入手中");
            return;
        }

        string ownerCharacterId = !string.IsNullOrEmpty(skill.AddCardOwnerCharacterId)
            ? skill.AddCardOwnerCharacterId
            : (owner?.Character?.CharacterId ?? "");

        int added = 0;
        for (int i = 0; i < skill.AddCardToHandCount; i++)
        {
            Card.Card card = template.Clone();
            card.OwnerCharacterId = ownerCharacterId;

            // 挂上释放者的属性 —— 卡面与战斗结算都按这个角色的属性算
            if (owner?.Character != null)
            {
                card.LinkedAttributes = owner.Character.GetAttributes();
            }

            player.Hand.Add(card);
            added++;
        }

        GD.Print($"[UltimateExecutor] 【{skill.Name}】将 {added} 张「{template.Name}」置入手中（归属 {ownerCharacterId}）");
        summary.Add($"将{added}张「{template.Name}」置入手中");
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
