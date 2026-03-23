using Godot;
using System.Collections.Generic;

public enum RewardType
{
    Artifact,
    Engraving,
    Gold,
    Card
}

public partial class RewardSystem : Node
{
    private int _playerGold = 0;

    public override void _Ready()
    {
    }

    public void AddGold(int amount)
    {
        _playerGold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (_playerGold >= amount)
        {
            _playerGold -= amount;
            return true;
        }
        return false;
    }

    public int GetGold()
    {
        return _playerGold;
    }

    public void SetGold(int amount)
    {
        _playerGold = Mathf.Max(0, amount);
    }

    public void ResetGold()
    {
        _playerGold = 0;
    }

    public List<Artifact> GenerateRandomArtifacts(int count)
    {
        List<Artifact> result = new List<Artifact>();

        for (int i = 0; i < count; i++)
        {
            result.Add(Artifact.GenerateRandom());
        }

        return result;
    }

    public void ApplyBattleVictoryReward(Attacker attacker, out List<Artifact> artifacts, out bool showEngraving)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(1, 100);

        if (roll <= 50)
        {
            showEngraving = true;
            artifacts = new List<Artifact>();
        }
        else
        {
            showEngraving = false;
            artifacts = GenerateRandomArtifacts(3);
        }
    }

    public void ApplyMapVictoryReward(MapDefinition map)
    {
        AddGold(map.GoldReward);
    }

    public bool ShouldShowEngraving()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(1, 100);
        return roll <= 50;
    }
}
