using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.CharacterSystem;

public class DeathResistanceSystem
{
    private Dictionary<int, int> _mapLevelDeathResistance = new Dictionary<int, int>();

    public bool TryRevive(CharacterAttributes attributes)
    {
        if (attributes.DeathResistance <= 0)
        {
            return false;
        }

        int resistChance = Mathf.Min(attributes.DeathResistance, 100);
        int roll = (int)(GD.Randi() % 100) + 1;

        return roll <= resistChance;
    }

    public void OnRevive(CharacterAttributes attributes)
    {
        int halfResistance = Mathf.FloorToInt(attributes.DeathResistance / 2);
        attributes.DeathResistance = halfResistance;
    }

    public void SaveDeathResistanceToMap(int mapLevel, int deathResistance)
    {
        _mapLevelDeathResistance[mapLevel] = deathResistance;
    }

    public int GetDeathResistanceFromMap(int mapLevel)
    {
        if (_mapLevelDeathResistance.ContainsKey(mapLevel))
        {
            return _mapLevelDeathResistance[mapLevel];
        }
        return 0;
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
                int savedResistance = _deathResistanceSystem.GetDeathResistanceFromMap(currentMapLevel);
                if (savedResistance == 0)
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
                    GD.Print($"[TeamRevive] {member.CharacterName} 死亡抵抗触发！复活后死亡抵抗降至 {member.Attributes.DeathResistance}%");
                }
            }
        }
    }

    public void ResetForNewMap(int mapLevel)
    {
        _deathResistanceSystem.ResetMapDeathResistance();
    }
}
