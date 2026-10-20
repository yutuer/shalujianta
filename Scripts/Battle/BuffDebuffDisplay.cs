using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Battle.Effects.Buffs;

/// <summary>
/// 战斗中的 Buff / Debuff 图标层（对照参考图 R-08 角色区下方那排图标）
///
/// 图标来源：StatusEffect.IconPath（由 EffectRegistry 按 buffId 配置）
/// 层数：同名效果叠加时显示 ×N
/// 过期：SetStatusEffects 传入的状态里消失的图标会淡出
/// </summary>
public partial class BuffDebuffDisplay : Control
{
    private HBoxContainer _iconContainer;

    private const int MaxDisplayIcons = 8;

    public override void _Ready()
    {
        GD.Print("[BuffDebuffDisplay] _Ready called");

        _iconContainer = GetNodeOrNull<HBoxContainer>("IconContainer");
        if (_iconContainer == null)
        {
            GD.PrintErr("[BuffDebuffDisplay] 未找到 IconContainer 节点");
            return;
        }

        _iconContainer.Alignment = HBoxContainer.AlignmentMode.Center;
        GD.Print("[BuffDebuffDisplay] _Ready completed");
    }

    /// <summary>
    /// 显示一组状态效果（buff 与 debuff 混排，自动按类型上色）
    /// </summary>
    public void DisplayAllStatus(List<StatusEffect> allStatus)
    {
        if (_iconContainer == null) return;

        ClearDisplay();
        if (allStatus == null) return;

        List<StatusEffect> buffList = allStatus.Where(s => s.EffectType == StatusEffectType.Buff).ToList();
        List<StatusEffect> debuffList = allStatus.Where(s => s.EffectType == StatusEffectType.Debuff).ToList();

        foreach (StatusEffect buff in buffList.Take(MaxDisplayIcons))
        {
            AddStatusIcon(buff, false);
        }

        foreach (StatusEffect debuff in debuffList.Take(MaxDisplayIcons))
        {
            AddStatusIcon(debuff, true);
        }

        GD.Print($"[BuffDebuffDisplay] DisplayAllStatus called: buff={buffList.Count} debuff={debuffList.Count}");
    }

    /// <summary>
    /// 清空并淡出（状态全部过期时用）
    /// </summary>
    public void PlayExpireAnimation()
    {
        if (_iconContainer == null) return;

        foreach (Node child in _iconContainer.GetChildren())
        {
            if (child is CanvasItem item)
            {
                Tween tween = item.CreateTween();
                tween.TweenProperty(item, "modulate:a", 0f, 0.25f);
            }
        }

        GD.Print("[BuffDebuffDisplay] PlayExpireAnimation called");
    }

    private void AddStatusIcon(StatusEffect effect, bool isDebuff)
    {
        VBoxContainer container = new VBoxContainer();
        container.Alignment = VBoxContainer.AlignmentMode.Center;

        TextureRect icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(26, 26),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };

        string iconPath = effect.IconPath;
        if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
        {
            icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
        }
        else
        {
            // 没有图标资源时用颜色区分（蓝=增益 / 红=减益）
            icon.Modulate = isDebuff ? new Color(1f, 0.35f, 0.35f) : new Color(0.4f, 0.7f, 1f);
        }

        container.AddChild(icon);

        Label valueLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        valueLabel.AddThemeColorOverride("font_color", isDebuff ? new Color(1f, 0.5f, 0.5f) : new Color(0.4f, 0.85f, 1f));
        valueLabel.AddThemeFontSizeOverride("font_size", 11);
        valueLabel.Text = BuildValueText(effect, isDebuff);
        container.AddChild(valueLabel);

        _iconContainer.AddChild(container);
    }

    /// <summary>
    /// 数值文本；层数 &gt; 1 时追加 ×N
    /// </summary>
    private string BuildValueText(StatusEffect effect, bool isDebuff)
    {
        string value = isDebuff ? GetDebuffValueText(effect) : GetBuffValueText(effect);
        if (effect.StackCount > 1)
        {
            value += $" ×{effect.StackCount}";
        }
        return value;
    }

    private string GetBuffValueText(StatusEffect effect)
    {
        if (effect is StrengthBuff sb) return $"+{sb.AttackBonus}";
        if (effect is DefenseBuff db) return $"+{db.DefenseBonus}";
        if (effect is RegenerationBuff rb) return $"+{rb.HealPerTurn}";
        if (effect is ThornsBuff tb) return $"+{tb.DamageReflect}";
        return $"{effect.RemainingDuration}";
    }

    private string GetDebuffValueText(StatusEffect effect)
    {
        if (effect is WeakDebuff wd) return $"-{wd.AttackReduction}";
        if (effect is VulnerableDebuff vd) return $"+{vd.DamageIncrease}";
        if (effect is PoisonDebuff pd) return $"{pd.DamagePerTurn}";
        if (effect is SlowDebuff sd) return $"-{sd.EnergyReduction}";
        return $"{effect.RemainingDuration}";
    }

    public void ClearDisplay()
    {
        if (_iconContainer == null) return;

        foreach (Node child in _iconContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
