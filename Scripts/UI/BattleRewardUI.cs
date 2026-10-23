using Godot;
using System.Collections.Generic;
using FishEatFish.Battle;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI;

/// <summary>
/// 战斗胜利后的**奖励选择界面**（tasks.md 1.3，对照参考图 R-10 / R-11）。
///
/// 流程：
///   战斗胜利 → 本界面（三选一：造物 或 刻印）
///   - 选造物 → 点「确定」→ 造物进背包 → 回到地图
///   - 选刻印 → 点「确定」→ 再选一张卡镶嵌 → 回到地图
///   - 点「跳过」→ 不要奖励，直接回到地图
///
/// 数据来源：<see cref="BattleTransitionManager"/>（autoload，跨场景带数据）。
/// </summary>
public partial class BattleRewardUI : Control
{
    /// <summary>奖励选项卡场景</summary>
    [Export]
    public PackedScene ChoiceCardScene;

    private Label _titleLabel;
    private Label _hintLabel;
    private HBoxContainer _choiceRow;
    private Button _confirmButton;
    private Button _skipButton;

    private readonly List<BattleRewardChoiceCard> _cards = new List<BattleRewardChoiceCard>();
    private List<BattleRewardChoice> _choices = new List<BattleRewardChoice>();

    private BattleRewardChoiceCard _selectedCard;

    /// <summary>选择完毕（带被选中的奖励；跳过时为 null）</summary>
    public System.Action<BattleRewardChoice> OnRewardSelected;

    /// <summary>候选奖励（供测试断言用）</summary>
    public List<BattleRewardChoice> Choices => _choices;

    /// <summary>当前选中的选项卡（供测试断言用）</summary>
    public BattleRewardChoiceCard SelectedCard => _selectedCard;

    public override void _Ready()
    {
        GD.Print("[BattleRewardUI] _Ready called");

        _titleLabel = GetNodeOrNull<Label>("Margin/VBox/TitleLabel");
        _hintLabel = GetNodeOrNull<Label>("Margin/VBox/HintLabel");
        _choiceRow = GetNodeOrNull<HBoxContainer>("Margin/VBox/ChoiceRow");
        _confirmButton = GetNodeOrNull<Button>("Margin/VBox/ButtonRow/ConfirmButton");
        _skipButton = GetNodeOrNull<Button>("Margin/VBox/ButtonRow/SkipButton");

        if (_choiceRow == null) GD.PrintErr("[BattleRewardUI] 未找到 Margin/VBox/ChoiceRow 节点");
        if (_confirmButton == null) GD.PrintErr("[BattleRewardUI] 未找到 ConfirmButton 节点");
        if (_skipButton == null) GD.PrintErr("[BattleRewardUI] 未找到 SkipButton 节点");

        // 订阅放在 _Ready（生命周期开始处），不要放在每次显示处 ——
        // 否则每打开一次就多一个订阅者，点一次确认会触发多次（踩坑记录 §27）
        if (_confirmButton != null)
        {
            _confirmButton.Pressed += OnConfirmPressed;
        }
        if (_skipButton != null)
        {
            _skipButton.Pressed += OnSkipPressed;
        }

        // 奖励选完（或跳过）→ 回到结算流程
        OnRewardSelected += OnRewardChosen;

        BindPendingChoices();

        GD.Print("[BattleRewardUI] _Ready completed");
    }

    /// <summary>
    /// 奖励选定后的收尾：回到「调查完成」结算界面。
    /// </summary>
    private void OnRewardChosen(BattleRewardChoice choice)
    {
        GD.Print($"[BattleRewardUI] OnRewardChosen called: {choice?.Name ?? "（跳过）"}");

        BattleTransitionManager manager = BattleTransitionManager.Instance;
        if (manager != null)
        {
            manager.CompleteBattleReward();
        }
        else
        {
            GD.PrintErr("[BattleRewardUI] BattleTransitionManager 不可用，直接回地图");
            GetTree().ChangeSceneToFile("res://Scenes/HexMapScene.tscn");
        }
    }

    /// <summary>从 BattleTransitionManager 取待选的奖励并铺到界面上</summary>
    private void BindPendingChoices()
    {
        BattleTransitionManager manager = BattleTransitionManager.Instance;
        List<BattleRewardChoice> pending = manager?.PendingRewardChoices;

        if (pending == null || pending.Count == 0)
        {
            GD.PrintErr("[BattleRewardUI] 没有待选奖励，显示空界面");
            return;
        }

        ShowChoices(pending);
    }

    /// <summary>铺开奖励候选（测试与复用都可直接调）</summary>
    public void ShowChoices(List<BattleRewardChoice> choices)
    {
        GD.Print("[BattleRewardUI] ShowChoices called");

        _choices = choices ?? new List<BattleRewardChoice>();
        _selectedCard = null;

        foreach (BattleRewardChoiceCard card in _cards)
        {
            if (card != null) card.QueueFree();
        }
        _cards.Clear();

        if (_choiceRow != null && ChoiceCardScene != null)
        {
            foreach (BattleRewardChoice choice in _choices)
            {
                BattleRewardChoiceCard card = ChoiceCardScene.Instantiate<BattleRewardChoiceCard>();
                if (card == null) continue;

                _choiceRow.AddChild(card);
                card.SetChoice(choice);
                card.OnClicked += OnChoiceClicked;
                _cards.Add(card);
            }
        }

        bool isEngraving = _choices.Count > 0 && _choices[0].Kind == BattleRewardKind.Engraving;
        if (_titleLabel != null)
        {
            _titleLabel.Text = isEngraving ? "选择一张卡牌镶嵌刻印" : "选择 1 个造物";
        }
        if (_hintLabel != null)
        {
            _hintLabel.Text = isEngraving
                ? "选择一种刻印，点确定后再选择要镶嵌的卡牌"
                : "点击卡片选择";
        }
        if (_confirmButton != null)
        {
            _confirmButton.Disabled = true;
        }

        GD.Print($"[BattleRewardUI] ShowChoices completed: {_cards.Count} 张候选卡");
    }

    private void OnChoiceClicked(BattleRewardChoiceCard card)
    {
        GD.Print("[BattleRewardUI] OnChoiceClicked called");

        if (card == null) return;

        foreach (BattleRewardChoiceCard other in _cards)
        {
            if (other != null) other.SetSelected(other == card);
        }

        _selectedCard = card;
        if (_confirmButton != null)
        {
            _confirmButton.Disabled = false;
        }
    }

    /// <summary>点「确定」：造物直接发；刻印进入选卡流程</summary>
    private void OnConfirmPressed()
    {
        GD.Print("[BattleRewardUI] OnConfirmPressed called");

        if (_selectedCard == null || _selectedCard.Choice == null)
        {
            GD.Print("[BattleRewardUI] OnConfirmPressed: 尚未选择奖励");
            return;
        }

        BattleRewardChoice choice = _selectedCard.Choice;

        if (choice.Kind == BattleRewardKind.Artifact)
        {
            BattleRewardManager manager = BattleRewardManager.Instance;
            if (manager != null)
            {
                manager.ApplyArtifactChoice(choice);
            }
            else
            {
                GD.PrintErr("[BattleRewardUI] BattleRewardManager 不可用，造物未发放");
            }

            GD.Print($"[BattleRewardUI] OnConfirmPressed completed: 发放造物 {choice.Name}");
            OnRewardSelected?.Invoke(choice);
            return;
        }

        // 刻印：进入「选择目标卡牌」流程
        GD.Print("[BattleRewardUI] 刻印奖励：进入选卡流程");
        ShowCardPicker(choice);
    }

    /// <summary>
    /// 刻印的选卡流程：把待选的刻印与候选卡牌交给刻印选择界面处理。
    /// </summary>
    private void ShowCardPicker(BattleRewardChoice choice)
    {
        List<Card> candidates = GetEngravableCards();

        if (candidates.Count == 0)
        {
            GD.Print("[BattleRewardUI] 没有可镶嵌的卡牌，本次刻印放弃");
            OnRewardSelected?.Invoke(null);
            return;
        }

        BattleTransitionManager manager = BattleTransitionManager.Instance;
        if (manager != null)
        {
            manager.SetPendingRewardEngraving(choice);
            manager.SetPendingRewardTargetCards(candidates);
        }

        GD.Print($"[BattleRewardUI] ShowCardPicker: {candidates.Count} 张可选卡");
        GetTree().ChangeSceneToFile("res://Scenes/UI/EngravingRewardCardSelectionUI.tscn");
    }

    /// <summary>取玩家可镶嵌刻印的卡牌（队伍的攻击/防御/特殊技）</summary>
    private List<Card> GetEngravableCards()
    {
        List<Card> cards = new List<Card>();

        FishEatFish.Battle.CharacterSystem.CharacterDefinition[] team = FishEatFish.Scenes.GlobalData.SelectedCharacters;
        if (team == null) return cards;

        FishEatFish.Battle.EngravingSystem.EngravingManager engravingManager =
            FishEatFish.Battle.EngravingSystem.EngravingManager.Instance;

        foreach (FishEatFish.Battle.CharacterSystem.CharacterDefinition character in team)
        {
            if (character == null) continue;

            AddCardIfEngravable(cards, character.AttackCardId, character, engravingManager);
            AddCardIfEngravable(cards, character.DefenseCardId, character, engravingManager);

            if (character.SpecialCardIds != null)
            {
                foreach (string specialId in character.SpecialCardIds)
                {
                    AddCardIfEngravable(cards, specialId, character, engravingManager);
                }
            }
        }

        return cards;
    }

    private void AddCardIfEngravable(
        List<Card> cards,
        string cardId,
        FishEatFish.Battle.CharacterSystem.CharacterDefinition character,
        FishEatFish.Battle.EngravingSystem.EngravingManager engravingManager)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        if (engravingManager != null && engravingManager.IsCardEngraved(cardId)) return;

        // ⚠️ 用 CreateInstance（独立副本 + 挂上该角色属性）：
        // 共享模板既算不出百分比数值，也会和别的界面共享同一个实例。
        Card card = FishEatFish.Battle.CharacterSystem.CharacterCardLoader.CreateInstance(
            cardId,
            character != null ? character.GetAttributes() : null);
        if (card == null) return;

        cards.Add(card);
    }

    private void OnSkipPressed()
    {
        GD.Print("[BattleRewardUI] OnSkipPressed called");
        GD.Print("[BattleRewardUI] OnSkipPressed completed");
        OnRewardSelected?.Invoke(null);
    }
}
