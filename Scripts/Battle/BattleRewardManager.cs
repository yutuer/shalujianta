using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Shop;

namespace FishEatFish.Battle;

/// <summary>
/// 战斗奖励管理器（tasks.md 1.3.3 / 1.3.4）。
///
/// 职责：
///   1. 决定这场战斗发「造物」还是「刻印」—— 与 <see cref="RewardSystem"/> 同一口径（50%）
///   2. 生成 3 个奖励候选（三选一，对照参考图 R-10 / R-11）
///   3. 执行发奖：造物进背包，刻印镶到卡上
///
/// 设计取舍：刻印奖励需要先选「哪张卡」再镶，所以刻印的最终落地由
/// <see cref="ApplyEngravingChoice"/> 完成 —— 由调用方（UI）传入目标卡。
/// </summary>
public partial class BattleRewardManager : Node
{
    /// <summary>三选一的候选数量</summary>
    public const int ChoiceCount = 3;

    private static BattleRewardManager _instance;
    public static BattleRewardManager Instance => _instance;

    public override void _Ready()
    {
        _instance = this;
        GD.Print("[BattleRewardManager] _Ready called");
        GD.Print("[BattleRewardManager] _Ready completed");
    }

    /// <summary>
    /// 生成一场战斗的奖励候选列表（长度 = <see cref="ChoiceCount"/>）。
    /// </summary>
    /// <param name="forceKind">指定奖励类型；传 null 表示按 50% 概率随机决定</param>
    /// <returns>候选奖励列表</returns>
    public List<BattleRewardChoice> GenerateChoices(BattleRewardKind? forceKind = null)
    {
        BattleRewardKind kind = forceKind ?? RollRewardKind();
        GD.Print($"[BattleRewardManager] GenerateChoices called: 类型={kind}");

        List<BattleRewardChoice> choices = kind == BattleRewardKind.Artifact
            ? GenerateArtifactChoices()
            : GenerateEngravingChoices();

        GD.Print($"[BattleRewardManager] GenerateChoices completed: {choices.Count} 个候选");
        return choices;
    }

    /// <summary>
    /// 掷骰决定这次发造物还是刻印（50% / 50%，与 RewardSystem 同口径）。
    /// </summary>
    public BattleRewardKind RollRewardKind()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(1, 100);
        return roll <= 50 ? BattleRewardKind.Artifact : BattleRewardKind.Engraving;
    }

    /// <summary>生成 3 个造物候选（排除已拥有的）</summary>
    private List<BattleRewardChoice> GenerateArtifactChoices()
    {
        List<BattleRewardChoice> choices = new List<BattleRewardChoice>();

        BlackMarkShopManager shop = BlackMarkShopManager.Instance;
        if (shop == null)
        {
            GD.PrintErr("[BattleRewardManager] BlackMarkShopManager 不可用，造物奖励为空");
            return choices;
        }

        List<ArtifactData> all = shop.GetAllArtifacts();
        List<ArtifactData> owned = shop.GetOwnedArtifacts();
        HashSet<string> ownedIds = new HashSet<string>(owned.Select(a => a.artifactId));

        List<ArtifactData> available = all.Where(a => !ownedIds.Contains(a.artifactId)).ToList();

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        int count = Mathf.Min(ChoiceCount, available.Count);
        for (int i = 0; i < count; i++)
        {
            int index = rng.RandiRange(0, available.Count - 1);
            ArtifactData artifact = available[index];
            available.RemoveAt(index);

            choices.Add(new BattleRewardChoice
            {
                Kind = BattleRewardKind.Artifact,
                Id = artifact.artifactId,
                Name = artifact.name,
                Description = artifact.description,
                IconPath = artifact.icon,
                Artifact = artifact
            });
        }

        return choices;
    }

    /// <summary>生成 3 个刻印候选</summary>
    private List<BattleRewardChoice> GenerateEngravingChoices()
    {
        List<BattleRewardChoice> choices = new List<BattleRewardChoice>();

        FishEatFish.Battle.EngravingSystem.EngravingManager engravingManager =
            FishEatFish.Battle.EngravingSystem.EngravingManager.Instance;
        if (engravingManager == null)
        {
            GD.PrintErr("[BattleRewardManager] EngravingManager 不可用，刻印奖励为空");
            return choices;
        }

        // 注意：Shop 命名空间下也有一个 EngravingEffect（属性型），这里必须写全限定名
        foreach (FishEatFish.Battle.EngravingSystem.EngravingEffect engraving in engravingManager.GetRandomEngravings(ChoiceCount))
        {
            choices.Add(new BattleRewardChoice
            {
                Kind = BattleRewardKind.Engraving,
                Id = engraving.Id,
                Name = engraving.Name,
                Description = engraving.Description,
                IconPath = engraving.IconPath,
                Engraving = engraving
            });
        }

        return choices;
    }

    /// <summary>
    /// 执行发奖：造物进背包。
    /// </summary>
    public bool ApplyArtifactChoice(BattleRewardChoice choice)
    {
        GD.Print("[BattleRewardManager] ApplyArtifactChoice called");

        if (choice == null || choice.Kind != BattleRewardKind.Artifact || choice.Artifact == null)
        {
            GD.PrintErr("[BattleRewardManager] ApplyArtifactChoice 收到非法奖励");
            return false;
        }

        BlackMarkShopManager shop = BlackMarkShopManager.Instance;
        if (shop == null)
        {
            GD.PrintErr("[BattleRewardManager] BlackMarkShopManager 不可用，发放失败");
            return false;
        }

        bool granted = shop.GrantArtifact(choice.Artifact);
        GD.Print($"[BattleRewardManager] ApplyArtifactChoice completed: {choice.Name} 发放={granted}");
        return granted;
    }

    /// <summary>
    /// 执行发奖：刻印镶到指定卡上。
    /// </summary>
    /// <param name="choice">刻印奖励</param>
    /// <param name="targetCard">目标卡（为空则失败）</param>
    public bool ApplyEngravingChoice(BattleRewardChoice choice, FishEatFish.Battle.Card.Card targetCard)
    {
        GD.Print("[BattleRewardManager] ApplyEngravingChoice called");

        if (choice == null || choice.Kind != BattleRewardKind.Engraving || choice.Engraving == null)
        {
            GD.PrintErr("[BattleRewardManager] ApplyEngravingChoice 收到非法奖励");
            return false;
        }

        if (targetCard == null)
        {
            GD.PrintErr("[BattleRewardManager] ApplyEngravingChoice 没有目标卡");
            return false;
        }

        FishEatFish.Battle.EngravingSystem.EngravingManager engravingManager =
            FishEatFish.Battle.EngravingSystem.EngravingManager.Instance;
        if (engravingManager == null)
        {
            GD.PrintErr("[BattleRewardManager] EngravingManager 不可用，发放失败");
            return false;
        }

        bool engraved = engravingManager.ApplyEngraving(targetCard, choice.Engraving);
        GD.Print($"[BattleRewardManager] ApplyEngravingChoice completed: {targetCard.Name} ← {choice.Name} 成功={engraved}");
        return engraved;
    }
}
