using Godot;
using System.Collections.Generic;

/// <summary>
/// 怒气值显示UI组件
/// 功能：集中展示4个角色的怒气状态，提供怒气获取动画反馈
/// 位置：战斗场景左侧
/// </summary>
public partial class RageDisplayUI : Control
{
    private Dictionary<int, RageSlotUI> _rageSlots = new Dictionary<int, RageSlotUI>();
    private Label _totalRageLabel;
    private VBoxContainer _mainContainer;

    public override void _Ready()
    {
        SetupUI();
        GD.Print("[RageDisplayUI] 怒气显示UI初始化完成");
    }

    private void SetupUI()
    {
        _mainContainer = GetNodeOrNull<VBoxContainer>("VBoxContainer");
        _totalRageLabel = GetNodeOrNull<Label>("VBoxContainer/TotalRageLabel");

        var slotsContainer = GetNodeOrNull<HBoxContainer>("VBoxContainer/RageSlotsContainer");
        if (slotsContainer != null)
        {
            for (int i = 0; i < 4; i++)
            {
                // 槽位实际路径是 VBoxContainer/RageSlotsContainer/RageSlotN（原路径少了一层 VBoxContainer/）
                string slotPath = $"VBoxContainer/RageSlotsContainer/RageSlot{i}";
                if (HasNode(slotPath))
                {
                    RageSlotUI slot = GetNodeOrNull<Panel>(slotPath) as RageSlotUI;
                    if (slot != null)
                    {
                        _rageSlots[i] = slot;
                    }
                }
            }
        }

        if (_rageSlots.Count == 0)
        {
            GD.PrintErr("[RageDisplayUI] 未找到怒气槽位节点");
        }
    }

    /// <summary>
    /// 设置单个角色的怒气值
    /// </summary>
    /// <param name="slotIndex">角色槽位索引 (0-3)</param>
    /// <param name="currentRage">当前怒气值</param>
    /// <param name="maxRage">最大怒气值，默认为100</param>
    public void SetCharacterRage(int slotIndex, int currentRage, int maxRage = 100)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].SetRage(currentRage, maxRage);
        }
    }

    /// <summary>
    /// 设置角色名称显示
    /// </summary>
    /// <param name="slotIndex">角色槽位索引</param>
    /// <param name="characterName">角色名称</param>
    /// <param name="nameColor">角色名称颜色</param>
    public void SetCharacterInfo(int slotIndex, string characterName, Color nameColor)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].SetCharacterName(characterName, nameColor);
        }
    }

    /// <summary>
    /// 批量更新所有角色的怒气值
    /// </summary>
    /// <param name="rageValues">怒气值字典 (key:槽位索引, value:怒气值)</param>
    public void UpdateAllRage(Dictionary<int, int> rageValues)
    {
        int totalRage = 0;
        foreach (var kvp in rageValues)
        {
            if (_rageSlots.ContainsKey(kvp.Key))
            {
                _rageSlots[kvp.Key].SetRage(kvp.Value);
                totalRage += kvp.Value;
            }
        }

        if (_totalRageLabel != null)
        {
            _totalRageLabel.Text = $"总怒气: {totalRage}";
        }
    }

    /// <summary>
    /// 高亮显示可释放狂气爆发的角色
    /// </summary>
    /// <param name="readyIndices">怒气已满的角色索引列表</param>
    public void HighlightReadyCharacters(List<int> readyIndices)
    {
        foreach (var kvp in _rageSlots)
        {
            if (readyIndices.Contains(kvp.Key))
            {
                kvp.Value.SetHighlight(true);
            }
            else
            {
                kvp.Value.SetHighlight(false);
            }
        }
    }

    /// <summary>
    /// 显示怒气获取动画
    /// </summary>
    /// <param name="slotIndex">角色槽位索引</param>
    /// <param name="gainedRage">获得的怒气值</param>
    public void ShowRageGainAnimation(int slotIndex, int gainedRage)
    {
        if (_rageSlots.ContainsKey(slotIndex))
        {
            _rageSlots[slotIndex].PlayGainAnimation(gainedRage);
        }
    }
}
