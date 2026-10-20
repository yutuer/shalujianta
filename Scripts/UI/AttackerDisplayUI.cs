using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.CharacterSystem;

/// <summary>
/// 攻击体角色显示UI组件
/// 功能：展示4个角色的头像和名称，配合圆形怒气进度条
/// 位置：战斗场景顶部中央
/// </summary>
public partial class AttackerDisplayUI : Control
{
    private HBoxContainer _characterContainer;
    private List<CharacterSlotUI> _characterSlots = new List<CharacterSlotUI>();
    private Label _teamStatsLabel;

    public override void _Ready()
    {
        SetupLayout();
        GD.Print("[AttackerDisplayUI] 攻击体角色显示UI初始化完成");
    }

    private void SetupLayout()
    {
        _teamStatsLabel = GetNode<Label>("VBoxContainer/TeamStatsLabel");
        _characterContainer = GetNode<HBoxContainer>("VBoxContainer/CharacterContainer");

        if (_characterContainer != null)
        {
            for (int i = 0; i < 4; i++)
            {
                string slotPath = $"VBoxContainer/CharacterContainer/CharacterSlot{i}";
                if (HasNode(slotPath))
                {
                    CharacterSlotUI slot = GetNode<Panel>(slotPath) as CharacterSlotUI;
                    if (slot != null)
                    {
                        _characterSlots.Add(slot);
                    }
                }
            }
        }

        if (_characterSlots.Count == 0)
        {
            GD.PrintErr("[AttackerDisplayUI] 未找到角色槽位节点");
        }
    }

    /// <summary>
    /// 设置攻击体数据并更新UI
    /// </summary>
    /// <param name="attacker">攻击体实例</param>
    public void SetAttacker(Attacker attacker)
    {
        if (attacker == null || attacker.Characters == null)
        {
            GD.PrintErr("[AttackerDisplayUI] Attacker无效");
            return;
        }

        for (int i = 0; i < 4 && i < attacker.Characters.Length; i++)
        {
            CharacterDefinition character = attacker.Characters[i];
            if (i < _characterSlots.Count)
            {
                _characterSlots[i].SetCharacter(character, i);
            }
        }

        UpdateTeamStats(attacker);
        GD.Print($"[AttackerDisplayUI] 已设置攻击体，共{attacker.GetCharacterCount()}个角色");
    }

    private void UpdateTeamStats(Attacker attacker)
    {
        if (_teamStatsLabel != null)
        {
            _teamStatsLabel.Text = $"总属性: 生命 {attacker.TotalHealth} | 攻击 {attacker.TotalAttack} | 防御 {attacker.TotalDefense} | 能量 {attacker.TotalEnergy}";
        }
    }

    /// <summary>
    /// 更新指定角色的怒气状态显示
    /// </summary>
    /// <param name="slotIndex">角色槽位索引 (0-3)</param>
    /// <param name="rageValue">当前怒气值 (0-100)</param>
    /// <param name="canUseUltimate">是否可以使用大招</param>
    public void UpdateCharacterStatus(int slotIndex, int rageValue, bool canUseUltimate)
    {
        if (slotIndex >= 0 && slotIndex < _characterSlots.Count)
        {
            _characterSlots[slotIndex].UpdateRageStatus(rageValue, canUseUltimate);
        }
    }

    public void SetCharacterSlotClickable(int slotIndex, bool clickable)
    {
        if (slotIndex >= 0 && slotIndex < _characterSlots.Count)
        {
            _characterSlots[slotIndex].SetClickable(clickable);
        }
    }

    /// <summary>
    /// 设置角色槽位点击回调（点击可释放大招的槽位时触发）
    /// </summary>
    /// <param name="onSlotClicked">回调参数为槽位索引 (0-3)，传 null 表示清除回调</param>
    public void SetUltimateClickCallback(System.Action<int> onSlotClicked)
    {
        GD.Print("[AttackerDisplayUI] SetUltimateClickCallback called");

        for (int i = 0; i < _characterSlots.Count; i++)
        {
            int slotIndex = i;
            CharacterSlotUI slot = _characterSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (onSlotClicked != null)
            {
                slot.SetClickCallback(() => onSlotClicked(slotIndex));
            }
            else
            {
                slot.SetClickCallback(null);
            }
        }

        GD.Print("[AttackerDisplayUI] SetUltimateClickCallback completed");
    }

    /// <summary>
    /// 高亮显示怒气已满的角色（可用于大招释放）
    /// </summary>
    public void HighlightCharacterWithFullRage()
    {
        for (int i = 0; i < _characterSlots.Count; i++)
        {
            if (_characterSlots[i].CanUseUltimate())
            {
                _characterSlots[i].SetHighlight(true);
            }
        }
    }
}
