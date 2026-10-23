using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.Battle.EngravingSystem;

/// <summary>
/// 刻印系统（tasks.md 1.2）：卡牌刻印的**唯一真相源**。
///
/// 设计要点（对应踩坑记录 §3.2 的教训）：
///   刻印状态必须跨场景存活 —— 玩家在地图上给卡刻了印，回到战斗时那张卡仍然带刻印。
///   所以本类是 **autoload**（历史上 MapManager / BattleTransitionManager / BlackMarkShopManager
///   都因为「是场景节点 → 切场景就销毁」而丢过状态，这里不再重蹈覆辙）。
///
/// 数据流：
///   获得刻印 → ApplyEngraving(cardId, effect) 写入映射
///   组战斗卡组 → ApplyToDeck(deck) 把刻印回填到新建的 Card 上
///              （卡组每次战斗都重建，不回填就等于刻印全丢）
///   出牌     → OnCardPlayed(...) 触发刻印效果，返回可读的触发描述
/// </summary>
public partial class EngravingManager : Node
{
    private static EngravingManager _instance;
    public static EngravingManager Instance => _instance;

    private List<EngravingEffect> _engravingLibrary = new List<EngravingEffect>();

    /// <summary>cardId → 刻印效果。跨场景存活（autoload）。</summary>
    private readonly Dictionary<string, EngravingEffect> _engravedCards = new Dictionary<string, EngravingEffect>();

    public override void _Ready()
    {
        if (_instance != null && _instance != this)
        {
            GD.PrintErr("[EngravingManager] 实例重复，释放后来者");
            QueueFree();
            return;
        }
        _instance = this;

        InitializeEngravingLibrary();
        GD.Print($"[EngravingManager] _Ready called: 刻印库 {_engravingLibrary.Count} 项，已刻印 {_engravedCards.Count} 张卡");
    }

    private void InitializeEngravingLibrary()
    {
        _engravingLibrary.Clear();
        _engravingLibrary.AddRange(EngravingEffect.GetAllEngravingEffects());
    }

    // ===== 查询 =====

    /// <summary>该卡是否已刻刻印</summary>
    public bool IsCardEngraved(string cardId)
    {
        return !string.IsNullOrEmpty(cardId) && _engravedCards.ContainsKey(cardId);
    }

    /// <summary>取某张卡的刻印效果，没有则返回 null</summary>
    public EngravingEffect GetEngravingForCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        return _engravedCards.TryGetValue(cardId, out EngravingEffect effect) ? effect : null;
    }

    /// <summary>全部已刻印的卡牌 id（供 UI 展示 / 测试断言）</summary>
    public List<string> GetEngravedCardIds()
    {
        return _engravedCards.Keys.ToList();
    }

    public List<EngravingEffect> GetAllEngravingEffects()
    {
        return new List<EngravingEffect>(_engravingLibrary);
    }

    /// <summary>随机抽 n 个不重复的刻印（奖励三选一用）</summary>
    public List<EngravingEffect> GetRandomEngravings(int count)
    {
        List<EngravingEffect> result = new List<EngravingEffect>();
        List<EngravingEffect> available = new List<EngravingEffect>(_engravingLibrary);

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        int actualCount = Mathf.Min(count, available.Count);

        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = rng.RandiRange(0, available.Count - 1);
            result.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        return result;
    }

    // ===== 写入 =====

    /// <summary>
    /// 给某张卡刻上刻印（按 cardId 记录，跨场景存活）
    /// </summary>
    /// <param name="cardId">卡牌 id，例如 "rat_attack"</param>
    /// <param name="engraving">刻印效果</param>
    /// <returns>是否写入成功</returns>
    public bool ApplyEngraving(string cardId, EngravingEffect engraving)
    {
        if (string.IsNullOrEmpty(cardId) || engraving == null) return false;

        _engravedCards[cardId] = engraving;
        GD.Print($"[EngravingManager] ApplyEngraving called: {cardId} ← {engraving.Name}");
        GD.Print($"[EngravingManager] ApplyEngraving completed: 现有刻印 {_engravedCards.Count} 张卡");
        return true;
    }

    /// <summary>
    /// 给卡牌对象刻上刻印（同时更新对象字段，供战斗内直接读取）
    /// </summary>
    public bool ApplyEngraving(FishEatFish.Battle.Card.Card card, EngravingEffect engraving)
    {
        if (card == null || engraving == null) return false;

        card.IsEngraved = true;
        card.EngravingType = engraving.Type;
        card.EngravingEffect = engraving;

        return ApplyEngraving(card.CardId, engraving);
    }

    /// <summary>
    /// 把已记录的刻印回填到战斗卡组上。
    ///
    /// 为什么必须有这一步 —— 战斗卡组每次都由 <see cref="FishEatFish.Battle.CharacterSystem.Attacker"/>
    /// 重新 new 出来，不回填的话「上回合刻的印」在这一回合根本不存在。
    /// </summary>
    public int ApplyToDeck(List<FishEatFish.Battle.Card.Card> deck)
    {
        if (deck == null || deck.Count == 0) return 0;

        int applied = 0;
        foreach (FishEatFish.Battle.Card.Card card in deck)
        {
            if (card == null) continue;

            EngravingEffect effect = GetEngravingForCard(card.CardId);
            if (effect == null) continue;

            card.IsEngraved = true;
            card.EngravingType = effect.Type;
            card.EngravingEffect = effect;
            applied++;
        }

        if (applied > 0)
        {
            GD.Print($"[EngravingManager] ApplyToDeck called: 回填 {applied} 张卡的刻印");
        }
        GD.Print("[EngravingManager] ApplyToDeck completed");
        return applied;
    }

    /// <summary>
    /// 卡牌出牌时触发刻印效果。
    /// </summary>
    /// <param name="card">打出的卡</param>
    /// <param name="player">玩家</param>
    /// <param name="enemies">敌人列表</param>
    /// <param name="character">出牌角色（自身怒气刻印用）</param>
    /// <param name="allCharacters">全队角色（团队怒气刻印用）</param>
    /// <returns>触发描述（供战斗日志展示）；没有刻印或刻印未触发时返回空串</returns>
    public string OnCardPlayed(
        FishEatFish.Battle.Card.Card card,
        Player player,
        Enemy[] enemies,
        FishEatFish.Battle.CharacterSystem.CharacterPosition character,
        List<FishEatFish.Battle.CharacterSystem.CharacterPosition> allCharacters)
    {
        if (card == null || !card.IsEngraved || card.EngravingEffect == null) return "";

        EngravingEffect effect = card.EngravingEffect;
        GD.Print($"[EngravingManager] OnCardPlayed called: {card.Name} 触发 {effect.Name}");

        string description;
        switch (effect.Type)
        {
            case EngravingType.Shield:
                description = ApplyShieldEffect(player, effect);
                break;
            case EngravingType.Poison:
                description = ApplyPoisonEffect(enemies, effect);
                break;
            case EngravingType.Copy:
                description = ApplyCopyEffect(card, player);
                break;
            case EngravingType.TeamRage:
                description = ApplyTeamRageEffect(character, allCharacters, effect);
                break;
            case EngravingType.SelfRage:
                description = ApplySelfRageEffect(character, effect);
                break;
            case EngravingType.Vulnerable:
                description = ApplyVulnerableEffect(enemies, effect);
                break;
            default:
                description = "";
                break;
        }

        GD.Print($"[EngravingManager] OnCardPlayed completed: {description}");
        return description;
    }

    // ===== 各类刻印的具体效果 =====

    private string ApplyShieldEffect(Player player, EngravingEffect effect)
    {
        if (player == null) return "";

        int amount = effect.PrimaryValue > 0 ? effect.PrimaryValue : 5;
        player.AddShield(amount);
        return $"护盾刻印触发：获得 {amount} 点护盾";
    }

    private string ApplyPoisonEffect(Enemy[] enemies, EngravingEffect effect)
    {
        if (enemies == null) return "";

        int damagePerTurn = effect.PrimaryValue > 0 ? effect.PrimaryValue : 3;
        int duration = effect.SecondaryValue > 0 ? effect.SecondaryValue : 3;
        int count = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            PoisonDebuff poison = new PoisonDebuff
            {
                DamagePerTurn = damagePerTurn,
                Duration = duration
            };
            poison.Initialize();
            enemy.AddStatusEffect(poison);
            count++;
        }

        return count > 0 ? $"毒伤刻印触发：{count} 个敌人中毒 {duration} 回合" : "";
    }

    private string ApplyCopyEffect(FishEatFish.Battle.Card.Card originalCard, Player player)
    {
        if (originalCard == null || player == null) return "";

        // ⚠️ 用 Clone() 而不是手抄字段：
        // 手抄会漏掉百分比字段（DamagePercent / ShieldPercent）与角色属性（LinkedAttributes），
        // 复制出来的卡就会算不出伤害 / 护盾（= 0）。
        FishEatFish.Battle.Card.Card copy = originalCard.Clone();
        copy.CardId = originalCard.CardId + "_copy";
        copy.Name = originalCard.Name + "(复制)";
        copy.IsEngraved = false;

        player.Deck.Add(copy);
        return "复制刻印触发：获得一张此卡牌的复制";
    }

    private string ApplyTeamRageEffect(
        FishEatFish.Battle.CharacterSystem.CharacterPosition character,
        List<FishEatFish.Battle.CharacterSystem.CharacterPosition> allCharacters,
        EngravingEffect effect)
    {
        if (allCharacters == null) return "";

        int amount = effect.PrimaryValue > 0 ? effect.PrimaryValue : 15;
        int count = 0;

        foreach (var pos in allCharacters)
        {
            if (pos != null && pos != character)
            {
                pos.AddRage(amount);
                count++;
            }
        }

        return count > 0 ? $"团队怒气刻印触发：{count} 名队友各获得 {amount} 点怒气" : "";
    }

    private string ApplySelfRageEffect(FishEatFish.Battle.CharacterSystem.CharacterPosition character, EngravingEffect effect)
    {
        if (character == null) return "";

        int amount = effect.PrimaryValue > 0 ? effect.PrimaryValue : 30;
        character.AddRage(amount);
        return $"自身怒气刻印触发：获得 {amount} 点怒气";
    }

    private string ApplyVulnerableEffect(Enemy[] enemies, EngravingEffect effect)
    {
        if (enemies == null) return "";

        int increase = effect.PrimaryValue > 0 ? effect.PrimaryValue : 50;
        int duration = effect.SecondaryValue > 0 ? effect.SecondaryValue : 1;
        int count = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            VulnerableDebuff vulnerable = new VulnerableDebuff
            {
                DamageIncrease = increase,
                Duration = duration
            };
            vulnerable.Initialize();
            enemy.AddStatusEffect(vulnerable);
            count++;
        }

        return count > 0 ? $"易伤刻印触发：{count} 个敌人受到伤害 +{increase}%，持续 {duration} 回合" : "";
    }

    /// <summary>
    /// 刻印与造物二选一（奖励三选一用）。
    /// 与 <see cref="FishEatFish.Battle.MapSystem.RewardSystem.ShouldShowEngraving"/> 同一口径（50%）。
    /// </summary>
    public bool ShouldShowEngravingInsteadOfArtifact()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(1, 100);
        return roll <= 50;
    }

    /// <summary>清空全部刻印（新一轮探险用）</summary>
    public void ClearAll()
    {
        _engravedCards.Clear();
        GD.Print("[EngravingManager] ClearAll completed");
    }
}
