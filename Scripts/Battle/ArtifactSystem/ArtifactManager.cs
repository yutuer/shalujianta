using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ArtifactManager : Node
{
    private List<Artifact> _equippedArtifacts = new List<Artifact>();
    private List<Artifact> _inventoryArtifacts = new List<Artifact>();

    private const int MaxEquipped = 4;

    public override void _Ready()
    {
    }

    public bool EquipArtifact(Artifact artifact)
    {
        if (artifact == null) return false;
        if (_equippedArtifacts.Count >= MaxEquipped) return false;
        if (_equippedArtifacts.Contains(artifact)) return false;

        _equippedArtifacts.Add(artifact);
        _inventoryArtifacts.Remove(artifact);
        return true;
    }

    public bool UnequipArtifact(Artifact artifact)
    {
        if (artifact == null) return false;
        if (!_equippedArtifacts.Contains(artifact)) return false;

        _equippedArtifacts.Remove(artifact);
        _inventoryArtifacts.Add(artifact);
        return true;
    }

    public List<Artifact> GetEquippedArtifacts()
    {
        return new List<Artifact>(_equippedArtifacts);
    }

    public List<Artifact> GetInventoryArtifacts()
    {
        return new List<Artifact>(_inventoryArtifacts);
    }

    public int GetEquippedCount()
    {
        return _equippedArtifacts.Count;
    }

    public bool CanEquipMore()
    {
        return _equippedArtifacts.Count < MaxEquipped;
    }

    public int CalculateTotalAttackBonus()
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Attacker)
            {
                bonus += artifact.AttackBonus;
            }
            else if (artifact.Type == ArtifactType.Condition)
            {
                bonus += artifact.AttackBonus;
            }
        }
        return bonus;
    }

    public int CalculateTotalDefenseBonus()
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Attacker)
            {
                bonus += artifact.DefenseBonus;
            }
        }
        return bonus;
    }

    public int CalculateTotalHealthBonus()
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Attacker)
            {
                bonus += artifact.HealthBonus;
            }
        }
        return bonus;
    }

    public int CalculateTotalEnergyBonus()
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Attacker)
            {
                bonus += artifact.EnergyBonus;
            }
        }
        return bonus;
    }

    public int CalculateTotalAttackPenalty()
    {
        int penalty = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            penalty += artifact.AttackPenalty;
        }
        return penalty;
    }

    public int CalculateTotalDefensePenalty()
    {
        int penalty = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            penalty += artifact.DefensePenalty;
        }
        return penalty;
    }

    public int CalculateCharacterAttackBonus(string characterId)
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Character && artifact.TargetCharacterId == characterId)
            {
                bonus += artifact.AttackBonus;
            }
        }
        return bonus;
    }

    public int CalculateCharacterDefenseBonus(string characterId)
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Character && artifact.TargetCharacterId == characterId)
            {
                bonus += artifact.DefenseBonus;
            }
        }
        return bonus;
    }

    public int CalculateCharacterHealthBonus(string characterId)
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Character && artifact.TargetCharacterId == characterId)
            {
                bonus += artifact.HealthBonus;
            }
        }
        return bonus;
    }

    public int CalculateCharacterEnergyBonus(string characterId)
    {
        int bonus = 0;
        foreach (var artifact in _equippedArtifacts)
        {
            if (artifact.Type == ArtifactType.Character && artifact.TargetCharacterId == characterId)
            {
                bonus += artifact.EnergyBonus;
            }
        }
        return bonus;
    }

    public void AddToInventory(Artifact artifact)
    {
        if (artifact != null)
        {
            _inventoryArtifacts.Add(artifact);
        }
    }

    public void ClearAll()
    {
        _equippedArtifacts.Clear();
        _inventoryArtifacts.Clear();
    }
}
