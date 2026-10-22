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
    private Label _energyLabel;
    private Label _shieldValue;
    private PanelContainer _shieldBadge;
    private ColorRect _incomingDamage;
    private Label _incomingDamageLabel;
    private PanelContainer _keyOrderBadge;
    private TextureRect _keyOrderIcon;
    private Label _keyOrderLabel;
    private Label _silverKeyLabel;
    private BuffDebuffDisplay _buffDisplay;

    private readonly List<TextureRect> _portraits = new List<TextureRect>();
    private Player _playerData;

    /// <summary>敌人这一回合预告要打掉的血量（画在血条上的深红段）</summary>
    private int _incomingDamageAmount = 0;

    public override void _Ready()
    {
        GD.Print("[PlayerZoneUI] _Ready called");

        _portraitRow = GetNodeOrNull<HBoxContainer>("PortraitRow");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthRow/HealthStack/HealthBar");
        _healthLabel = GetNodeOrNull<Label>("HealthRow/HealthStack/HealthLabel");
        _energyLabel = GetNodeOrNull<Label>("BottomInfoRow/EnergyBlock/EnergyValue");
        _shieldValue = GetNodeOrNull<Label>("HealthRow/ShieldBadge/ShieldInner/ShieldValue");
        _shieldBadge = GetNodeOrNull<PanelContainer>("HealthRow/ShieldBadge");
        _incomingDamage = GetNodeOrNull<ColorRect>("HealthRow/HealthStack/IncomingDamage");
        _incomingDamageLabel = GetNodeOrNull<Label>("HealthRow/HealthStack/IncomingDamageLabel");
        _keyOrderBadge = GetNodeOrNull<PanelContainer>("BottomInfoRow/KeyOrderBlock/KeyOrderBadge");
        _keyOrderIcon = GetNodeOrNull<TextureRect>("BottomInfoRow/KeyOrderBlock/KeyOrderBadge/KeyOrderIcon");
        _keyOrderLabel = GetNodeOrNull<Label>("BottomInfoRow/KeyOrderBlock/KeyOrderInfo/KeyOrderLabel");
        _silverKeyLabel = GetNodeOrNull<Label>("BottomInfoRow/KeyOrderBlock/KeyOrderInfo/SilverKeyLabel");
        _buffDisplay = GetNodeOrNull<BuffDebuffDisplay>("BuffDebuffDisplay");

        if (_healthLabel != null)
        {
            _healthLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        }

        BuildPortraits();
        RefreshKeyOrderInfo();

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
                // 均分整行宽度 → 4 个立绘**铺满**玩家区（而不是挤在中间）
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
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

    /// <summary>
    /// 刷新血条 + 能量（当前算力）。
    /// </summary>
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
        if (_energyLabel != null)
        {
            _energyLabel.Text = $"{_playerData.CurrentEnergy} / {_playerData.MaxEnergy}";
        }
        // 护盾（盾量）：从顶部 PlayerStats 挪到了**血条末端**的盾牌徽标上
        if (_shieldValue != null)
        {
            _shieldValue.Text = _playerData.Shield.ToString();
        }

        // 盾量为 0 时**不显示盾图标**（空盾牌徽标只是噪音）
        if (_shieldBadge != null)
        {
            _shieldBadge.Visible = _playerData.Shield > 0;
        }
    }

    /// <summary>血量变化时刷新</summary>
    public void UpdateHealth()
    {
        UpdateStats();
    }

    /// <summary>能量变化时刷新</summary>
    public void UpdateEnergy()
    {
        UpdateStats();
    }

    /// <summary>
    /// 设置「敌人这一回合预告要打掉的血量」。
    /// 它会画成玩家血条上紧跟着绿条的**深红段**，并在下面标出「-XXX」
    /// （对照参考图 R-08：血条上绿=当前血、红=即将损失）。
    /// </summary>
    /// <param name="damage">预计将要承受的伤害</param>
    public void SetIncomingDamage(int damage)
    {
        _incomingDamageAmount = Mathf.Max(0, damage);
        UpdateIncomingDamageVisual();
    }

    public override void _Process(double delta)
    {
        // 血条宽度会随布局变化（窗口大小 / 布局重排），
        // 所以深红段的位置每帧跟着重算一次。
        if (_incomingDamageAmount > 0)
        {
            UpdateIncomingDamageVisual();
        }
    }

    /// <summary>把「预扣血」画成血条上的深红段 + 数字</summary>
    private void UpdateIncomingDamageVisual()
    {
        if (_incomingDamage == null) return;

        if (_incomingDamageAmount <= 0 || _playerData == null || _playerData.MaxHealth <= 0)
        {
            _incomingDamage.Visible = false;
            if (_incomingDamageLabel != null) _incomingDamageLabel.Visible = false;
            return;
        }

        float barWidth = _healthBar != null ? _healthBar.Size.X : 0f;
        if (barWidth <= 0f) return;

        float max = _playerData.MaxHealth;
        float current = Mathf.Clamp(_playerData.CurrentHealth, 0f, max);
        float damage = Mathf.Min(_incomingDamageAmount, max);
        if (damage <= 0f)
        {
            _incomingDamage.Visible = false;
            if (_incomingDamageLabel != null) _incomingDamageLabel.Visible = false;
            return;
        }

        // 血条刻度 = max(血量上限, 当前血 + 即将损失)
        // → 满血时深红段也画得出来（条会为"即将损失"变长），对照参考图 R-08
        float scale = Mathf.Max(max, current + damage);
        if (_healthBar != null)
        {
            _healthBar.MaxValue = scale;
            _healthBar.Value = current;
        }

        float x = barWidth * (current / scale);
        float w = barWidth * (damage / scale);

        _incomingDamage.Visible = true;
        _incomingDamage.OffsetLeft = x;
        _incomingDamage.OffsetRight = x + w;
        _incomingDamage.OffsetTop = 0f;
        _incomingDamage.OffsetBottom = 22f;

        if (_incomingDamageLabel != null)
        {
            _incomingDamageLabel.Visible = true;
            _incomingDamageLabel.Text = $"-{Mathf.RoundToInt(damage)}";
            _incomingDamageLabel.OffsetLeft = x - 30f;
            _incomingDamageLabel.OffsetRight = x + w + 30f;
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
    /// 钥令徽标：图标 + 名称 + 银钥持有数（对照参考图 R-08 左下角）。
    /// </summary>
    public void RefreshKeyOrderInfo()
    {
        KeyOrder key = GlobalData.EquippedKeyOrder;

        if (_keyOrderLabel != null)
        {
            _keyOrderLabel.Text = key != null ? key.Name : "无钥令";
        }

        // 钥令图标（没有专属资源时用通用钥令图标）
        string iconPath = "res://Assets/Icons/keyorder.svg";
        if (_keyOrderIcon != null)
        {
            if (ResourceLoader.Exists(iconPath))
            {
                _keyOrderIcon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
                _keyOrderIcon.Visible = true;
            }
            else
            {
                _keyOrderIcon.Visible = false;
            }
        }

        // 银钥持有 / 消耗上限
        if (_silverKeyLabel != null)
        {
            int owned = MapManager.Instance?.GetSilverKey() ?? 0;
            int cost = key != null ? key.SilverKeyCost : 1000;
            _silverKeyLabel.Text = $"银钥 {owned}/{cost}";
        }

        // 钥令徽标：深色圆底 + 金边
        if (_keyOrderBadge != null)
        {
            StyleBoxFlat disc = new StyleBoxFlat();
            disc.BgColor = new Color(0.16f, 0.18f, 0.24f);
            disc.BorderColor = new Color(0.85f, 0.75f, 0.45f);
            disc.SetBorderWidthAll(2);
            disc.SetCornerRadiusAll(38);   // 徽标 76x76 → 圆形底板半径跟着图标一起放大
            _keyOrderBadge.AddThemeStyleboxOverride("panel", disc);
        }
    }
}
