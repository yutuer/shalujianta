using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle
{
    /// <summary>
    /// 角色在本次调查中的同调率提升
    /// </summary>
    public class CharacterSyncEntry
    {
        public string CharacterId = "";
        public string CharacterName = "";
        public int SyncGain;
    }

    /// <summary>
    /// 调查奖励条目
    /// </summary>
    public class BattleRewardEntry
    {
        public string RewardName = "";
        public string IconPath = "";
        public int Amount;
    }

    /// <summary>
    /// 战斗结算数据：战斗胜利后由 BattleManager 组装，交给结算界面显示
    /// </summary>
    public class BattleResultData
    {
        public string MapId = "";
        public string MapName = "";
        public int TurnCount;
        public int StarCount;
        public string StarDescription = "";
        public List<CharacterSyncEntry> SyncEntries = new List<CharacterSyncEntry>();
        public List<BattleRewardEntry> Rewards = new List<BattleRewardEntry>();
    }

    /// <summary>
    /// 结算规则：星级阈值与同调率公式
    /// （阈值与公式均为占位值，后续可按策划需求调整；改这里即可，界面不用动）
    /// </summary>
    public static class BattleResultRules
    {
        /// <summary>达到该回合数及以内 = 三星</summary>
        public const int ThreeStarTurns = 5;

        /// <summary>达到该回合数及以内 = 二星</summary>
        public const int TwoStarTurns = 8;

        /// <summary>达到该回合数及以内 = 一星</summary>
        public const int OneStarTurns = 12;

        /// <summary>同调率总量：所有角色的提升值加起来恒等于这个数</summary>
        public const int SyncTotal = 100;

        /// <summary>
        /// 按回合数算星级（0~3）
        /// </summary>
        public static int GetStarCount(int turnCount)
        {
            if (turnCount <= ThreeStarTurns) return 3;
            if (turnCount <= TwoStarTurns) return 2;
            if (turnCount <= OneStarTurns) return 1;
            return 0;
        }

        /// <summary>
        /// 星级达成描述（对照参考图 R-09 的「最终战 N 回合内取胜」）
        /// </summary>
        public static string GetStarDescription(int turnCount)
        {
            return $"最终战 {turnCount} 回合内取胜";
        }

        /// <summary>
        /// 星级文本，例如 ★★★ / ★★☆ / ☆☆☆
        /// </summary>
        public static string GetStarText(int starCount)
        {
            int filled = Mathf.Clamp(starCount, 0, 3);
            return new string('★', filled) + new string('☆', 3 - filled);
        }

        /// <summary>
        /// 计算各角色的同调率提升。
        /// 占位规则：按角色综合属性（攻+防+体）的占比分配 SyncTotal，
        /// 最后一个角色拿走取整余数，保证**总和恒等于 SyncTotal**且每个都大于 0。
        /// </summary>
        public static List<CharacterSyncEntry> BuildSyncEntries(CharacterDefinition[] characters)
        {
            var result = new List<CharacterSyncEntry>();
            if (characters == null) return result;

            var valid = characters.Where(c => c != null).ToList();
            if (valid.Count == 0) return result;

            int[] weights = valid
                .Select(c => Mathf.Max(1, c.BaseAttack + c.BaseDefense + c.BaseConstitution))
                .ToArray();
            int totalWeight = weights.Sum();

            int remaining = SyncTotal;
            for (int i = 0; i < valid.Count; i++)
            {
                int gain;
                if (i == valid.Count - 1)
                {
                    gain = remaining;
                }
                else
                {
                    gain = Mathf.FloorToInt((float)weights[i] / totalWeight * SyncTotal);
                    remaining -= gain;
                }

                result.Add(new CharacterSyncEntry
                {
                    CharacterId = valid[i].CharacterId,
                    CharacterName = valid[i].Name,
                    SyncGain = gain
                });
            }

            return result;
        }
    }
}
