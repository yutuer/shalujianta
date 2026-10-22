using Godot;
using System.Collections.Generic;
using System.Linq;
using FishEatFish.Battle.Effects.Buffs;
using FishEatFish.UI;

/// <summary>
/// 战斗中的 Buff / Debuff 图标层（对照参考图 R-08 角色区下方那排图标）
///
/// 图标来源：StatusEffect.IconPath（由 EffectRegistry 按 buffId 配置）
/// 图标渲染：统一走 StatusIconFactory（与敌人侧 EnemyUI 共用同一套逻辑）
/// 层数：同名效果叠加时显示 ×N
/// 过期：RefreshStatus 传入的状态里消失的图标只淡出消失的那几个
/// </summary>
public partial class BuffDebuffDisplay : Control
{
    private HBoxContainer _iconContainer;

    private const int MaxDisplayIcons = 8;

    /// <summary>
    /// 状态图标的排布对齐（0=左 1=居中 2=右）。
    /// 玩家区的血条右移后，图标跟着靠右排，和血条对齐。
    /// </summary>
    [Export]
    public int IconAlignment { get; set; } = 1;

    /// <summary>当前显示中的状态图标：状态唯一键 → 图标控件</summary>
    private readonly Dictionary<string, Control> _iconNodes = new Dictionary<string, Control>();

    /// <summary>上一次刷新的状态快照：状态唯一键 → 层数</summary>
    private readonly Dictionary<string, int> _lastSnapshot = new Dictionary<string, int>();

    public override void _Ready()
    {
        GD.Print("[BuffDebuffDisplay] _Ready called");

        _iconContainer = GetNodeOrNull<HBoxContainer>("IconContainer");
        if (_iconContainer == null)
        {
            GD.PrintErr("[BuffDebuffDisplay] 未找到 IconContainer 节点");
            return;
        }

        _iconContainer.Alignment = (HBoxContainer.AlignmentMode)IconAlignment;
        GD.Print("[BuffDebuffDisplay] _Ready completed");
    }

    /// <summary>
    /// 全量重绘：清空后按「buff 在前、debuff 在后」重建全部状态图标
    /// </summary>
    public void DisplayAllStatus(List<StatusEffect> allStatus)
    {
        GD.Print("[BuffDebuffDisplay] DisplayAllStatus called");

        if (_iconContainer == null)
        {
            GD.PrintErr("[BuffDebuffDisplay] 未找到 IconContainer 节点，无法显示状态图标");
            return;
        }

        ClearDisplay();

        List<StatusEffect> buffList = SplitStatus(allStatus, true);
        List<StatusEffect> debuffList = SplitStatus(allStatus, false);

        foreach (StatusEffect buff in buffList.Take(MaxDisplayIcons))
        {
            AddStatusIcon(buff, false);
        }

        foreach (StatusEffect debuff in debuffList.Take(MaxDisplayIcons))
        {
            AddStatusIcon(debuff, true);
        }

        GD.Print($"[BuffDebuffDisplay] DisplayAllStatus completed: buff={buffList.Count} debuff={debuffList.Count}");
    }

    /// <summary>
    /// 用新状态集合刷新：与上次相比消失的状态图标淡出，新出现的直接显示，仍存在的只刷新数值
    /// </summary>
    public void RefreshStatus(List<StatusEffect> allStatus)
    {
        GD.Print("[BuffDebuffDisplay] RefreshStatus called");

        if (_iconContainer == null)
        {
            GD.PrintErr("[BuffDebuffDisplay] 未找到 IconContainer 节点，无法刷新状态图标");
            return;
        }

        List<StatusEffect> buffList = SplitStatus(allStatus, true);
        List<StatusEffect> debuffList = SplitStatus(allStatus, false);

        // 目标顺序：buff 在前、debuff 在后
        List<StatusEffect> ordered = new List<StatusEffect>();
        ordered.AddRange(buffList.Take(MaxDisplayIcons));
        ordered.AddRange(debuffList.Take(MaxDisplayIcons));

        Dictionary<string, StatusEffect> current = new Dictionary<string, StatusEffect>();
        foreach (StatusEffect effect in ordered)
        {
            current[BuildStatusKey(effect)] = effect;
        }

        // 1) 消失的状态 → 只淡出消失的那几个
        List<string> expiredKeys = _iconNodes.Keys.Where(key => !current.ContainsKey(key)).ToList();
        foreach (string key in expiredKeys)
        {
            Control expiredNode = _iconNodes[key];
            _iconNodes.Remove(key);
            _lastSnapshot.Remove(key);
            FadeOutAndRemove(expiredNode);
        }

        // 2) 仍存在的 → 刷新数值；新出现的 → 新建图标
        int added = 0;
        int updated = 0;
        foreach (StatusEffect effect in ordered)
        {
            string key = BuildStatusKey(effect);
            bool isDebuff = effect.EffectType == StatusEffectType.Debuff;

            if (_iconNodes.TryGetValue(key, out Control existingNode))
            {
                UpdateStatusIconValue(existingNode, effect, isDebuff);
                _lastSnapshot[key] = effect.StackCount;
                updated++;
            }
            else
            {
                AddStatusIcon(effect, isDebuff);
                added++;
            }
        }

        // 3) 按目标顺序重排（buff 在前、debuff 在后）
        ReorderIcons(ordered);

        GD.Print($"[BuffDebuffDisplay] RefreshStatus completed: buff={buffList.Count} debuff={debuffList.Count} added={added} updated={updated} expired={expiredKeys.Count}");
    }

    /// <summary>
    /// 全部状态过期时用：淡出当前所有图标并移除
    /// </summary>
    public void PlayExpireAnimation()
    {
        GD.Print("[BuffDebuffDisplay] PlayExpireAnimation called");

        if (_iconContainer == null)
        {
            GD.PrintErr("[BuffDebuffDisplay] 未找到 IconContainer 节点，无法播放过期动画");
            return;
        }

        foreach (Node child in _iconContainer.GetChildren())
        {
            if (child is CanvasItem item)
            {
                Tween tween = item.CreateTween();
                tween.TweenProperty(item, "modulate:a", 0f, 0.25f);
            }
        }

        _iconNodes.Clear();
        _lastSnapshot.Clear();
        GD.Print("[BuffDebuffDisplay] PlayExpireAnimation completed");
    }

    /// <summary>
    /// 清空所有状态图标
    /// </summary>
    public void ClearDisplay()
    {
        if (_iconContainer == null) return;

        foreach (Node child in _iconContainer.GetChildren())
        {
            // 先摘出再释放，避免同一帧内新旧图标重叠
            _iconContainer.RemoveChild(child);
            child.QueueFree();
        }

        _iconNodes.Clear();
        _lastSnapshot.Clear();
    }

    /// <summary>新建一个状态图标并登记到跟踪表</summary>
    private Control AddStatusIcon(StatusEffect effect, bool isDebuff)
    {
        if (effect == null) return null;

        Control iconNode = StatusIconFactory.CreateStatusIcon(effect, isDebuff);
        if (iconNode == null)
        {
            GD.PrintErr($"[BuffDebuffDisplay] 状态图标创建失败：{effect.EffectName}");
            return null;
        }

        string key = BuildStatusKey(effect);
        iconNode.Name = $"StatusIcon_{key}";
        _iconContainer.AddChild(iconNode);

        _iconNodes[key] = iconNode;
        _lastSnapshot[key] = effect.StackCount;
        return iconNode;
    }

    /// <summary>刷新已存在图标的数值文字</summary>
    private void UpdateStatusIconValue(Control iconNode, StatusEffect effect, bool isDebuff)
    {
        if (iconNode == null) return;
        if (!GodotObject.IsInstanceValid(iconNode)) return;

        Label valueLabel = iconNode.GetNodeOrNull<Label>(StatusIconFactory.ValueNodeName);
        if (valueLabel == null)
        {
            GD.PrintErr($"[BuffDebuffDisplay] 状态图标 {iconNode.Name} 内未找到数值 Label");
            return;
        }

        valueLabel.Text = StatusIconFactory.BuildValueText(effect, isDebuff);
    }

    /// <summary>淡出并移除单个图标（0.25s）</summary>
    private void FadeOutAndRemove(Control iconNode)
    {
        if (iconNode == null) return;
        if (!GodotObject.IsInstanceValid(iconNode)) return;

        Tween tween = iconNode.CreateTween();
        tween.TweenProperty(iconNode, "modulate:a", 0f, 0.25f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(iconNode))
            {
                iconNode.QueueFree();
            }
        }));
    }

    /// <summary>把存活图标按目标顺序重排</summary>
    private void ReorderIcons(List<StatusEffect> ordered)
    {
        int targetIndex = 0;
        foreach (StatusEffect effect in ordered)
        {
            string key = BuildStatusKey(effect);
            if (!_iconNodes.TryGetValue(key, out Control node)) continue;
            if (!GodotObject.IsInstanceValid(node)) continue;
            if (node.GetParent() != _iconContainer) continue;

            if (node.GetIndex(false) != targetIndex)
            {
                _iconContainer.MoveChild(node, targetIndex);
            }
            targetIndex++;
        }
    }

    /// <summary>按增益 / 减益拆分状态列表（过滤空元素）</summary>
    private static List<StatusEffect> SplitStatus(List<StatusEffect> allStatus, bool isBuff)
    {
        List<StatusEffect> result = new List<StatusEffect>();
        if (allStatus == null) return result;

        foreach (StatusEffect effect in allStatus)
        {
            if (effect == null) continue;
            bool effectIsBuff = effect.EffectType == StatusEffectType.Buff;
            if (effectIsBuff == isBuff)
            {
                result.Add(effect);
            }
        }
        return result;
    }

    /// <summary>状态唯一键：效果类名 + 效果名（同一效果刷新前后保持稳定）</summary>
    private static string BuildStatusKey(StatusEffect effect)
    {
        if (effect == null) return "";
        return $"{effect.GetType().Name}_{effect.EffectName}";
    }
}
