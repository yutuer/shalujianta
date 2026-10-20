using System;
using System.Collections.Generic;
using FishEatFish.Shop;

namespace FishEatFish.Battle;

/// <summary>战斗奖励的类型</summary>
public enum BattleRewardKind
{
    /// <summary>造物（进背包）</summary>
    Artifact,

    /// <summary>刻印（镶到卡上）</summary>
    Engraving
}

/// <summary>
/// 一条战斗奖励候选（造物或刻印）。
///
/// 界面上两种奖励共用同一个选项卡，所以这里用一个统一的数据结构承载。
/// </summary>
public class BattleRewardChoice
{
    public BattleRewardKind Kind { get; set; }

    /// <summary>奖励 id（造物 id 或刻印 id）</summary>
    public string Id { get; set; } = "";

    /// <summary>显示名</summary>
    public string Name { get; set; } = "";

    /// <summary>效果描述</summary>
    public string Description { get; set; } = "";

    /// <summary>图标资源路径（可能为空或不存在，UI 会做存在性检查）</summary>
    public string IconPath { get; set; } = "";

    /// <summary>造物数据（Kind == Artifact 时有效）</summary>
    public ArtifactData Artifact { get; set; }

    /// <summary>刻印数据（Kind == Engraving 时有效）</summary>
    public FishEatFish.Battle.EngravingSystem.EngravingEffect Engraving { get; set; }
}
