using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.AI;
using FishEatFish.UI;

public partial class EnemyUI : Panel
{
    private Label nameLabel;
    private TextureRect enemySprite;
    private ProgressBar healthBar;
    private Label healthLabel;
    private Label intentLabel;
    private TextureRect intentIcon;

    private Enemy enemyData;
    private Enemy pendingEnemy;
    private Texture2D pendingTexture;

    /// <summary>意图图标与颜色的单一数据源（避免与 EnemyIntentDisplay 重复实现）</summary>
    private readonly EnemyIntentDisplay _intentDisplay = new EnemyIntentDisplay();

    /// <summary>敌人身上的状态图标容器（血条下方那排 buff / debuff）</summary>
    private HBoxContainer _statusIconContainer;

    /// <summary>状态图标最多显示数量</summary>
    private const int MaxStatusIcons = 8;

    public override void _Ready()
    {
        GD.Print("[EnemyUI] _Ready called");

        nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        enemySprite = GetNode<TextureRect>("VBoxContainer/EnemySprite");
        healthBar = GetNode<ProgressBar>("VBoxContainer/HealthBar");
        healthLabel = GetNode<Label>("VBoxContainer/HealthLabel");
        intentLabel = GetNode<Label>("VBoxContainer/IntentLabel");

        if (HasNode("VBoxContainer/IntentIcon"))
        {
            intentIcon = GetNode<TextureRect>("VBoxContainer/IntentIcon");
        }

        _statusIconContainer = GetNodeOrNull<HBoxContainer>("VBoxContainer/StatusIconContainer");
        if (_statusIconContainer == null)
        {
            GD.PrintErr("[EnemyUI] 未找到 StatusIconContainer 节点");
        }


        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        healthLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));

        ApplyPendingSetup();
        GD.Print("[EnemyUI] _Ready completed");
    }

    
    private void ApplyPendingSetup()
    {
        if (pendingEnemy != null)
        {
            ApplySetup(pendingEnemy);
            if (pendingTexture != null && enemySprite != null)
            {
                enemySprite.Texture = pendingTexture;
            }
        }
    }

    public void Setup(Enemy enemy)
    {
        enemyData = enemy;
        pendingEnemy = enemy;

        if (IsInsideTree())
        {
            ApplySetup(enemy);
        }
    }

    private void ApplySetup(Enemy enemy)
    {
        Name = $"Enemy_{enemy.EnemyName}";
        nameLabel.Text = enemy.EnemyName;
        healthBar.MaxValue = enemy.MaxHealth;
        healthBar.Value = enemy.CurrentHealth;
        healthLabel.Text = $"{enemy.CurrentHealth}/{enemy.MaxHealth}";
        // 初始不写死「攻击」——真实意图由 BattleManager.UpdateEnemyUI 用 AI 预测填入
        if (intentLabel != null) intentLabel.Text = "?";
        if (intentIcon != null) intentIcon.Texture = null;

        // 换上新敌人时清掉旧的状态图标
        UpdateStatusIcons(null);
    }

    public void SetSprite(Texture2D texture)
    {
        pendingTexture = texture;
        if (enemySprite != null && texture != null)
        {
            enemySprite.Texture = texture;
        }
    }

    public void UpdateHealth()
    {
        if (enemyData == null) return;

        healthBar.MaxValue = enemyData.MaxHealth;
        healthBar.Value = enemyData.CurrentHealth;
        healthLabel.Text = $"{enemyData.CurrentHealth}/{enemyData.MaxHealth}";

        // 血量变化时状态也可能变了，顺手刷新一次状态图标
        // （UpdateStatusIcons 不会回调 UpdateHealth，因此不会递归）
        UpdateStatusIcons(enemyData.StatusEffects);

        if (enemyData.CurrentHealth <= 0)
        {
            healthLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    public void UpdateIntent(AIActionType actionType, int value)
    {
        UpdateIntentDisplay(actionType, value);
    }

    private void UpdateIntentDisplay(AIActionType actionType, int value)
    {
        if (intentLabel == null) return;

        Texture2D icon = GetIconForAction(actionType);
        if (intentIcon != null && icon != null)
        {
            intentIcon.Texture = icon;
        }

        Color intentColor = GetColorForAction(actionType);
        intentLabel.AddThemeColorOverride("font_color", intentColor);

        string intentText = actionType switch
        {
            AIActionType.Attack => $"攻击 {value}",
            AIActionType.Defend => $"防御 +{value}",
            AIActionType.Buff => $"强化 +{value}",
            AIActionType.Heal => $"治疗 +{value}",
            _ => $"攻击 {value}"
        };

        intentLabel.Text = intentText;
    }

    private Texture2D GetIconForAction(AIActionType actionType)
    {
        return _intentDisplay.GetIconForAction(actionType);
    }

    private Color GetColorForAction(AIActionType actionType)
    {
        return _intentDisplay.GetColorForAction(actionType);
    }

    /// <summary>
    /// 刷新该敌人身上的 buff / debuff 图标
    /// </summary>
    /// <param name="effects">状态列表；null 或空表示清空图标区</param>
    public void UpdateStatusIcons(List<StatusEffect> effects)
    {
        int buffCount = CountStatus(effects, StatusEffectType.Buff);
        int debuffCount = CountStatus(effects, StatusEffectType.Debuff);
        GD.Print($"[EnemyUI] UpdateStatusIcons called: buff={buffCount}, debuff={debuffCount}");

        if (_statusIconContainer == null)
        {
            GD.PrintErr("[EnemyUI] 未找到 StatusIconContainer 节点，无法刷新状态图标");
            GD.Print("[EnemyUI] UpdateStatusIcons completed");
            return;
        }

        // 先清空旧图标（先摘出再释放，避免同一帧内新旧图标重叠）
        foreach (Node child in _statusIconContainer.GetChildren())
        {
            _statusIconContainer.RemoveChild(child);
            child.QueueFree();
        }

        // buff 在前、debuff 在后（与玩家侧一致），总共最多显示 8 个
        int shown = 0;
        shown = AddStatusIcons(effects, StatusEffectType.Buff, false, shown);
        shown = AddStatusIcons(effects, StatusEffectType.Debuff, true, shown);

        GD.Print("[EnemyUI] UpdateStatusIcons completed");
    }

    /// <summary>批量添加某一类状态图标，返回当前已显示的数量</summary>
    private int AddStatusIcons(List<StatusEffect> effects, StatusEffectType effectType, bool isDebuff, int shown)
    {
        if (effects == null) return shown;

        foreach (StatusEffect effect in effects)
        {
            if (shown >= MaxStatusIcons) break;
            if (effect == null) continue;
            if (effect.EffectType != effectType) continue;

            Control iconNode = StatusIconFactory.CreateStatusIcon(effect, isDebuff);
            if (iconNode == null)
            {
                GD.PrintErr($"[EnemyUI] 状态图标创建失败：{effect.EffectName}");
                continue;
            }

            iconNode.Name = $"StatusIcon_{effect.GetType().Name}_{effect.EffectName}";
            _statusIconContainer.AddChild(iconNode);
            shown++;
        }
        return shown;
    }

    /// <summary>统计指定类型的状态数量</summary>
    private static int CountStatus(List<StatusEffect> effects, StatusEffectType effectType)
    {
        if (effects == null) return 0;

        int count = 0;
        foreach (StatusEffect effect in effects)
        {
            if (effect == null) continue;
            if (effect.EffectType == effectType) count++;
        }
        return count;
    }

    public Enemy GetEnemyData()
    {
        return enemyData;
    }
}
