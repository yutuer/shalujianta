using Godot;

namespace FishEatFish.Battle.Effects.Buffs;

/// <summary>
/// **死亡抵抗触发后的回合奖励** —— D66。
///
/// 照参考图 `docs/screenshots/战斗4_死亡抵抗.jpg` 的官方 tooltip：
/// 「在战斗中受到致命伤害时有概率保留 1 点生命。
///   触发后**下回合获得 2 点算力和抽 2 张牌**，队伍的死亡抵抗减半，
///   后续获得的死亡抵抗减半，持续到调查结束。」
///
/// ## 为什么做成 StatusEffect 而不是直接在触发处加
/// 官方说的是「**下回合**」—— 触发发生在**敌人回合**（玩家被打到致命伤害），
/// 而算力与抽牌要在**玩家的下一个回合**才生效。
/// 项目的回合开始钩子 <c>Player.StartTurn()</c> 内部顺序是：
/// <code>
/// CurrentEnergy = MaxEnergy;   ← ① 先把算力重置满
/// ProcessTurnStartEffects();   ← ② 再跑回合开始效果（这里 +2 算力才不会被覆盖）
/// DrawCards(DrawCount);        ← ③ 最后抽牌
/// </code>
/// 所以做成 <see cref="StatusEffect"/>（Duration = 1）挂在玩家身上，
/// 下一个玩家回合的 ② 里结算 → 效果正好落在「下回合」。
/// </summary>
public partial class DeathResistanceReviveEffect : StatusEffect
{
    /// <summary>下回合获得的额外算力</summary>
    public const int BonusEnergy = 2;

    /// <summary>下回合额外抽的牌数</summary>
    public const int BonusDrawCount = 2;

    public DeathResistanceReviveEffect()
    {
        EffectName = "DeathResistanceRevive";
        Description = "下回合获得 2 点算力和抽 2 张牌";
        Duration = 1;
        EffectType = StatusEffectType.Buff;
    }

    public override void OnTurnStartPlayer(Core.Player target)
    {
        if (target == null) return;

        int energyBefore = target.CurrentEnergy;
        target.CurrentEnergy += BonusEnergy;
        target.DrawCards(BonusDrawCount);

        GD.Print($"[DeathResistanceRevive] 下回合奖励：算力 +{BonusEnergy}（{energyBefore} → {target.CurrentEnergy}），抽 {BonusDrawCount} 张牌");
    }
}
