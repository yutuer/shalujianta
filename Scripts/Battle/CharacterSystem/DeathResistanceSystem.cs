using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.CharacterSystem;

public class DeathResistanceSystem
{
    /// <summary>
    /// 地图层级的死亡抵抗存量。
    /// ⚠️ 用 float —— 死亡抵抗本身带小数（wiki 珊 61.6 / 参考图珈伦 36.4%），
    /// 存成 int 会在这里再丢一次精度。
    /// </summary>
    private Dictionary<int, float> _mapLevelDeathResistance = new Dictionary<int, float>();

    public bool TryRevive(CharacterAttributes attributes)
    {
        if (attributes.DeathResistance <= 0)
        {
            return false;
        }

        // ⚠️ float 不取整 —— 61.6 就按 61.6% 的概率判定（roll 是 1~100 的整数，
        // 所以实际命中 61/100）。取整成 62 反而比 wiki 原值更"虚高"。
        float resistChance = Mathf.Min(attributes.DeathResistance, 100f);
        int roll = (int)(GD.Randi() % 100) + 1;

        return roll <= resistChance;
    }

    public void OnRevive(CharacterAttributes attributes)
    {
        // 复活后减半 —— **保留小数**（61.6 → 30.8）。
        // ⚠️ 以前这里是 floor 到整数（62 → 31）；改成 float 后不再取整，
        // 因为参考游戏里死亡抵抗本身就是带小数的值（地图 HUD 显示 17%、属性面板 36.4%）。
        attributes.DeathResistance = attributes.DeathResistance / 2f;
    }

    public void SaveDeathResistanceToMap(int mapLevel, float deathResistance)
    {
        _mapLevelDeathResistance[mapLevel] = deathResistance;
    }

    public float GetDeathResistanceFromMap(int mapLevel)
    {
        if (_mapLevelDeathResistance.ContainsKey(mapLevel))
        {
            return _mapLevelDeathResistance[mapLevel];
        }
        return 0f;
    }

    public void ResetMapDeathResistance()
    {
        _mapLevelDeathResistance.Clear();
    }
}

public class TeamReviveSystem
{
    private DeathResistanceSystem _deathResistanceSystem = new DeathResistanceSystem();

    public void OnTeamMemberDeath(Player deadMember, List<Player> teamMembers, int currentMapLevel)
    {
        foreach (var member in teamMembers)
        {
            if (member == deadMember || member.IsDead)
            {
                continue;
            }

            if (member.Attributes != null && member.Attributes.DeathResistance > 0)
            {
                float savedResistance = _deathResistanceSystem.GetDeathResistanceFromMap(currentMapLevel);
                if (savedResistance <= 0f)
                {
                    _deathResistanceSystem.SaveDeathResistanceToMap(currentMapLevel, member.Attributes.DeathResistance);
                }
                else
                {
                    member.Attributes.DeathResistance = savedResistance;
                }

                if (_deathResistanceSystem.TryRevive(member.Attributes))
                {
                    member.ForceRevive(1);
                    _deathResistanceSystem.OnRevive(member.Attributes);
                    GD.Print($"[TeamRevive] {member.CharacterName} 死亡抵抗触发！复活后死亡抵抗降至 {member.Attributes.DeathResistance:F1}%");
                }
            }
        }
    }

    public void ResetForNewMap(int mapLevel)
    {
        _deathResistanceSystem.ResetMapDeathResistance();
    }
}
