using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.Scenes;
using FishEatFish.Shop;

/// <summary>
/// 战斗场景左下角的**玩家区**：角色立绘排 + 一条队伍血条 + 状态（buff/debuff）条 + 钥令徽标。
///
/// 对照参考图 R-08：角色立绘横排在左下，血条压在立绘下方，
/// 状态图标排（带层数）紧跟血条，钥令做成圆形徽标。
///
/// 设计约束：
///   - 纯展示组件，数据来自 <see cref="Player"/> 与 <c>GlobalData</c>
///   - 布局全部写在 <c>PlayerZone.tscn</c>，这里只填数据
/// </summary>
public partial class PlayerZoneUI : Control
{
    /// <summary>立绘数量（= 队伍人数上限）</summary>
    private const int PortraitCount = 4;

    /// <summary>立绘显示高度（资源本身是 256×320）</summary>
    private const int PortraitDisplayHeight = 168;

    private HBoxContainer _portraitRow;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private PanelContainer _keyOrderBadge;
    private Label _keyOrderInitial;
    private Label _keyOrderLabel;
    private BuffDebuffDisplay _buffDisplay;

    private readonly List<TextureRect> _portraits = new List<TextureRect>();
    private Player _playerData;

    public override void _Ready()
    {
        GD.Print("[PlayerZoneUI] _Ready called");

        _portraitRow = GetNodeOrNull<HBoxContainer>("PortraitRow");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthRow/HealthBar");
        _healthLabel = GetNodeOrNull<Label>("HealthRow/HealthLabel");
        _keyOrderBadge = GetNodeOrNull<PanelContainer>("HealthRow/KeyOrderBadge");
        _keyOrderInitial = GetNodeOrNull<Label>("HealthRow/KeyOrderBadge/KeyOrderInitial");
        _keyOrderLabel = GetNodeOrNull<Label>("HealthRow/KeyOrderLabel");
        _buffDisplay = GetNodeOrNull<BuffDebuffDisplay>("BuffDebuffDisplay");

        if (_healthLabel != null)
        {
            _healthLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        }

        BuildPortraits();
        RefreshKeyOrder();

        GD.Print("[PlayerZoneUI] _Ready completed");
    }

    /// <summary>生成 4 个立绘占位（角色顺序 = 上阵顺序）</summary>
    private void BuildPortraits()
    {
        if (_portraitRow == null) return;

        _portraits.Clear();
        for (int i = 0; i < PortraitCount; i++)
        {
            TextureRect portrait = new TextureRect
            {
                Name = $"Portrait{i}",
                CustomMinimumSize = new Vector2(PortraitDisplayHeight * 256 / 320, PortraitDisplayHeight),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _portraitRow.AddChild(portrait);
            _portraits.Add(portrait);
        }
    }

    /// <summary>
    /// 设置玩家数据并刷新全部内容。
    /// </summary>
    public void Setup(Player player)
    {
        _playerData = player;
        UpdateStats();
        SetPortraits(GlobalData.SelectedCharacters);
    }

    /// <summary>刷新血条</summary>
    public void UpdateStats()
    {
        if (_playerData == null) return;

        if (_healthBar != null)
        {
            _healthBar.MaxValue = _playerData.MaxHealth;
            _healthBar.Value = _playerData.CurrentHealth;
        }
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"{_playerData.CurrentHealth}/{_playerData.MaxHealth}";
        }
    }

    /// <summary>血量变化时刷新</summary>
    public void UpdateHealth()
    {
        if (_playerData == null) return;

        if (_healthBar != null)
        {
            _healthBar.MaxValue = _playerData.MaxHealth;
            _healthBar.Value = _playerData.CurrentHealth;
        }
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"{_playerData.CurrentHealth}/{_playerData.MaxHealth}";
        }
    }

    /// <summary>设置角色立绘（按上阵顺序；没上阵的位置留空）</summary>
    public void SetPortraits(CharacterDefinition[] team)
    {
        for (int i = 0; i < _portraits.Count; i++)
        {
            TextureRect portrait = _portraits[i];
            if (portrait == null) continue;

            if (team == null || i >= team.Length || team[i] == null)
            {
                portrait.Texture = null;
                continue;
            }

            CharacterDefinition character = team[i];
            string path = character.GetPortraitPath();
            if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
            {
                portrait.Texture = null;
                GD.Print($"[PlayerZoneUI] 角色 {character.CharacterId} 没有可用立绘（{path}）");
                continue;
            }

            portrait.Texture = ResourceLoader.Load<Texture2D>(path);
        }
    }

    /// <summary>刷新状态（buff / debuff）条</summary>
    public void SetStatus(List<StatusEffect> allStatus)
    {
        if (_buffDisplay == null) return;
        _buffDisplay.RefreshStatus(allStatus);
    }

    /// <summary>
    /// 钥令徽标：圆形牌 + 名字（放左下角，对照参考图 R-08 的左下角布局）。
    /// </summary>
    private void RefreshKeyOrder()
    {
        KeyOrder key = GlobalData.EquippedKeyOrder;

        if (_keyOrderLabel != null)
        {
            _keyOrderLabel.Text = key != null ? key.Name : "无钥令";
        }

        if (_keyOrderInitial != null)
        {
            _keyOrderInitial.Text = key != null && !string.IsNullOrEmpty(key.Name)
                ? key.Name.Substring(0, 1)
                : "—";
        }

        // 钥令没有图标资源，先用圆形色块表示
        if (_keyOrderBadge != null)
        {
            StyleBoxFlat disc = new StyleBoxFlat();
            disc.BgColor = new Color(0.16f, 0.18f, 0.24f);
            disc.BorderColor = new Color(0.85f, 0.75f, 0.45f);
            disc.SetBorderWidthAll(2);
            disc.SetCornerRadiusAll(22);
            _keyOrderBadge.AddThemeStyleboxOverride("panel", disc);
        }
    }
}
