using Godot;
using System.Collections.Generic;

public partial class ResourceManager
{
    private Dictionary<string, Texture2D> _cardTextures = new Dictionary<string, Texture2D>();
    private Texture2D _enemyTexture;

    private PackedScene _cardScene;
    private PackedScene _enemyScene;
    private PackedScene _playerStatsScene;
    private PackedScene _battleLogScene;

    public Dictionary<string, Texture2D> CardTextures => _cardTextures;
    public Texture2D EnemyTexture => _enemyTexture;
    public PackedScene CardScene => _cardScene;
    public PackedScene EnemyScene => _enemyScene;
    public PackedScene PlayerStatsScene => _playerStatsScene;
    public PackedScene BattleLogScene => _battleLogScene;

    public void LoadScenes()
    {
        _cardScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Card.tscn");
        _enemyScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Enemy.tscn");
        _playerStatsScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/PlayerStats.tscn");
        _battleLogScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/BattleLogWindow.tscn");
    }

    public void LoadResources()
    {
        _cardTextures["打击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/strike.png");
        _cardTextures["铁壁"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/defend.png");
        _cardTextures["猛击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/bash.png");
        _cardTextures["背上疯狂"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/whirlwind.png");
        _cardTextures["双重打击"] = ResourceLoader.Load<Texture2D>("res://Assets/Cards/twin_strike.png");
        _enemyTexture = ResourceLoader.Load<Texture2D>("res://Assets/Icons/enemy_elite.svg");
    }

    public void LoadAll()
    {
        LoadScenes();
        LoadResources();
    }
}
