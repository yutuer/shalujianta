using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BuffDebuffDisplay : Control
{
    private HBoxContainer _iconContainer;
    private List<TextureRect> _buffIcons = new List<TextureRect>();
    private List<Label> _buffValueLabels = new List<Label>();

    private const int MaxDisplayIcons = 5;

    public override void _Ready()
    {
        _iconContainer = new HBoxContainer();
        _iconContainer.Alignment = HBoxContainer.AlignmentMode.Center;
        AddChild(_iconContainer);
    }

    public void DisplayBuffs(List<StatusEffect> buffs)
    {
        ClearDisplay();

        var displayBuffs = buffs.Take(MaxDisplayIcons).ToList();

        foreach (var buff in displayBuffs)
        {
            AddBuffIcon(buff);
        }
    }

    public void DisplayDebuffs(List<StatusEffect> debuffs)
    {
        var displayDebuffs = debuffs.Take(MaxDisplayIcons).ToList();

        foreach (var debuff in displayDebuffs)
        {
            AddDebuffIcon(debuff);
        }
    }

    public void DisplayAllStatus(List<StatusEffect> allStatus)
    {
        ClearDisplay();

        var buffs = allStatus.Where(s => s.EffectType == StatusEffectType.Buff).Take(MaxDisplayIcons);
        var debuffs = allStatus.Where(s => s.EffectType == StatusEffectType.Debuff).Take(MaxDisplayIcons);

        foreach (var buff in buffs)
        {
            AddBuffIcon(buff);
        }

        foreach (var debuff in debuffs)
        {
            AddDebuffIcon(debuff);
        }
    }

    private void AddBuffIcon(StatusEffect effect)
    {
        var container = new VBoxContainer();
        container.Alignment = VBoxContainer.AlignmentMode.Center;

        var icon = new TextureRect();
        icon.CustomMinimumSize = new Vector2(24, 24);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

        string iconPath = GetBuffIconPath(effect.EffectName);
        if (!string.IsNullOrEmpty(iconPath))
        {
            icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
        }
        else
        {
            icon.Modulate = new Color(0.3f, 0.5f, 1f);
        }

        container.AddChild(icon);

        Label valueLabel = new Label();
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        valueLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.8f, 1f));
        valueLabel.AddThemeFontSizeOverride("font_size", 10);
        valueLabel.Text = GetBuffValueText(effect);
        container.AddChild(valueLabel);

        _iconContainer.AddChild(container);
        _buffIcons.Add(icon);
        _buffValueLabels.Add(valueLabel);
    }

    private void AddDebuffIcon(StatusEffect effect)
    {
        var container = new VBoxContainer();
        container.Alignment = VBoxContainer.AlignmentMode.Center;

        var icon = new TextureRect();
        icon.CustomMinimumSize = new Vector2(24, 24);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

        string iconPath = GetDebuffIconPath(effect.EffectName);
        if (!string.IsNullOrEmpty(iconPath))
        {
            icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
        }
        else
        {
            icon.Modulate = new Color(1f, 0.3f, 0.3f);
        }

        container.AddChild(icon);

        Label valueLabel = new Label();
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        valueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));
        valueLabel.AddThemeFontSizeOverride("font_size", 10);
        valueLabel.Text = GetDebuffValueText(effect);
        container.AddChild(valueLabel);

        _iconContainer.AddChild(container);
        _buffIcons.Add(icon);
        _buffValueLabels.Add(valueLabel);
    }

    private string GetBuffIconPath(string effectName)
    {
        return effectName.ToLower() switch
        {
            "strength" => "res://Assets/Icons/buff_attack.png",
            "defense" => "res://Assets/Icons/buff_defense.png",
            "regeneration" => "res://Assets/Icons/buff_heal.png",
            "thorns" => "res://Assets/Icons/buff_thorns.png",
            "fury" => "res://Assets/Icons/buff_fury.png",
            _ => ""
        };
    }

    private string GetDebuffIconPath(string effectName)
    {
        return effectName.ToLower() switch
        {
            "weak" => "res://Assets/Icons/debuff_weak.png",
            "vulnerable" => "res://Assets/Icons/debuff_vulnerable.png",
            "poison" => "res://Assets/Icons/debuff_poison.png",
            "slow" => "res://Assets/Icons/debuff_slow.png",
            "silence" => "res://Assets/Icons/debuff_silence.png",
            _ => ""
        };
    }

    private string GetBuffValueText(StatusEffect effect)
    {
        if (effect is StrengthBuff sb)
            return $"+{sb.AttackBonus}";
        if (effect is DefenseBuff db)
            return $"+{db.DefenseBonus}";
        if (effect is RegenerationBuff rb)
            return $"+{rb.HealPerTurn}";
        if (effect is ThornsBuff tb)
            return $"+{tb.DamageReflect}";
        return $"{effect.RemainingDuration}";
    }

    private string GetDebuffValueText(StatusEffect effect)
    {
        if (effect is WeakDebuff wd)
            return $"-{wd.AttackReduction}";
        if (effect is VulnerableDebuff vd)
            return $"+{vd.DamageIncrease}";
        if (effect is PoisonDebuff pd)
            return $"{pd.DamagePerTurn}";
        if (effect is SlowDebuff sd)
            return $"-{sd.EnergyReduction}";
        return $"{effect.RemainingDuration}";
    }

    public void ClearDisplay()
    {
        foreach (var child in _iconContainer.GetChildren())
        {
            child.QueueFree();
        }
        _buffIcons.Clear();
        _buffValueLabels.Clear();
    }
}
