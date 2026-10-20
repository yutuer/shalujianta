using Godot;
using System.Collections.Generic;
using FishEatFish.Battle;
using FishEatFish.Battle.Card;

namespace FishEatFish.UI;

/// <summary>
/// 战斗奖励里的「刻印选卡」界面（tasks.md 1.3.4）。
///
/// 场景：战斗胜利 → 奖励三选一（选了刻印）→ 本界面选一张卡镶嵌 → 回结算。
///
/// 为什么单独做一个界面而不是复用商店的 <c>EngravingCardSelectionUI</c>：
/// 商店那条路是「花黑印买刻印」，本条路是「战斗奖励白拿刻印」，
/// 计费与数据来源都不同，混在一起会让两边都变复杂。
/// </summary>
public partial class EngravingRewardCardSelectionUI : Control
{
    private static readonly PackedScene CardSelectionItemScene = GD.Load<PackedScene>("res://Scenes/UI/CardSelectionItem.tscn");

    private Label _titleLabel;
    private GridContainer _cardGrid;
    private Button _confirmButton;
    private Button _cancelButton;
    private TextureRect _engravingIcon;
    private Label _engravingNameLabel;
    private Label _engravingEffectLabel;

    private readonly List<FishEatFish.UI.CardSelectionItem.CardSelectionItem> _cardItems =
        new List<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();

    private BattleRewardChoice _engravingChoice;
    private Card _selectedCard;

    public override void _Ready()
    {
        GD.Print("[EngravingRewardCardSelectionUI] _Ready called");

        _titleLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/TitleLabel");
        _cardGrid = GetNodeOrNull<GridContainer>("BackgroundPanel/VBoxContainer/CardScrollMargin/CardScrollContainer/CardGrid");
        _confirmButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ButtonRow/ConfirmButton");
        _cancelButton = GetNodeOrNull<Button>("BackgroundPanel/VBoxContainer/BottomContainer/ButtonRow/CancelButton");
        _engravingIcon = GetNodeOrNull<TextureRect>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingIcon");
        _engravingNameLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingNameLabel");
        _engravingEffectLabel = GetNodeOrNull<Label>("BackgroundPanel/VBoxContainer/BottomContainer/EngravingInfoContainer/EngravingEffectLabel");

        if (_cardGrid == null) GD.PrintErr("[EngravingRewardCardSelectionUI] 未找到 CardGrid 节点");
        if (_confirmButton == null) GD.PrintErr("[EngravingRewardCardSelectionUI] 未找到 ConfirmButton 节点");
        if (_cancelButton == null) GD.PrintErr("[EngravingRewardCardSelectionUI] 未找到 CancelButton 节点");

        // 订阅放在 _Ready，不要放在每次显示时（踩坑记录 §27）
        if (_confirmButton != null)
        {
            _confirmButton.Pressed += OnConfirmPressed;
        }
        if (_cancelButton != null)
        {
            _cancelButton.Pressed += OnCancelPressed;
        }

        Visible = false;

        // 场景切换进来的 → 自己取待选数据
        CallDeferred(nameof(BindPendingData));

        GD.Print("[EngravingRewardCardSelectionUI] _Ready completed");
    }

    /// <summary>
    /// 本界面是被场景切换进来的（不是被父节点添加的），
    /// 所以要自己从 <see cref="BattleTransitionManager"/> 取待选的刻印与候选卡。
    /// </summary>
    private void BindPendingData()
    {
        BattleTransitionManager manager = BattleTransitionManager.Instance;
        if (manager == null)
        {
            GD.PrintErr("[EngravingRewardCardSelectionUI] BattleTransitionManager 不可用");
            return;
        }

        BattleRewardChoice choice = manager.PendingRewardEngraving;
        List<Card> cards = manager.PendingRewardTargetCards;

        if (choice == null || cards == null || cards.Count == 0)
        {
            GD.PrintErr("[EngravingRewardCardSelectionUI] 没有待选的刻印或候选卡");
            return;
        }

        ShowCardSelection(choice, cards);
    }

    /// <summary>
    /// 显示选卡界面。
    /// </summary>
    /// <param name="engravingChoice">要镶嵌的刻印</param>
    /// <param name="availableCards">可选的卡牌</param>
    public void ShowCardSelection(BattleRewardChoice engravingChoice, List<Card> availableCards)
    {
        GD.Print($"[EngravingRewardCardSelectionUI] ShowCardSelection called: {engravingChoice?.Name}, cards={availableCards?.Count ?? 0}");

        _engravingChoice = engravingChoice;
        _selectedCard = null;

        foreach (var item in _cardItems)
        {
            if (item != null) item.QueueFree();
        }
        _cardItems.Clear();

        if (_cardGrid != null)
        {
            foreach (Node child in _cardGrid.GetChildren())
            {
                child.QueueFree();
            }

            if (availableCards != null)
            {
                foreach (Card card in availableCards)
                {
                    FishEatFish.UI.CardSelectionItem.CardSelectionItem cardItem =
                        CardSelectionItemScene.Instantiate<FishEatFish.UI.CardSelectionItem.CardSelectionItem>();
                    if (cardItem == null) continue;

                    cardItem.Card = card;
                    cardItem.OnSelected += OnCardItemSelected;

                    _cardGrid.AddChild(cardItem);
                    cardItem.SetCardData(card);
                    _cardItems.Add(cardItem);
                }
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.Disabled = true;
        }

        if (_titleLabel != null)
        {
            _titleLabel.Text = $"选择一张卡牌镶嵌刻印 - {_engravingChoice?.Name ?? "刻印"}";
        }

        UpdateEngravingInfo();

        Visible = true;
        GD.Print($"[EngravingRewardCardSelectionUI] ShowCardSelection completed: {_cardItems.Count} 张卡");
    }

    private void UpdateEngravingInfo()
    {
        if (_engravingChoice == null)
        {
            if (_engravingNameLabel != null) _engravingNameLabel.Text = "";
            if (_engravingEffectLabel != null) _engravingEffectLabel.Text = "";
            return;
        }

        if (_engravingNameLabel != null)
        {
            _engravingNameLabel.Text = _engravingChoice.Name ?? "未知刻印";
        }

        if (_engravingEffectLabel != null)
        {
            _engravingEffectLabel.Text = _engravingChoice.Description ?? "";
        }

        if (_engravingIcon != null && !string.IsNullOrEmpty(_engravingChoice.IconPath))
        {
            if (ResourceLoader.Exists(_engravingChoice.IconPath))
            {
                _engravingIcon.Texture = ResourceLoader.Load<Texture2D>(_engravingChoice.IconPath);
            }
            else
            {
                GD.PrintErr($"[EngravingRewardCardSelectionUI] 刻印图标不存在: {_engravingChoice.IconPath}");
                _engravingIcon.Texture = null;
            }
        }
    }

    private void OnCardItemSelected(FishEatFish.UI.CardSelectionItem.CardSelectionItem item)
    {
        GD.Print("[EngravingRewardCardSelectionUI] OnCardItemSelected called");

        foreach (var cardItem in _cardItems)
        {
            if (cardItem != null) cardItem.SetSelected(cardItem == item);
        }

        _selectedCard = item?.Card;
        if (_confirmButton != null)
        {
            _confirmButton.Disabled = false;
        }
    }

    private void OnConfirmPressed()
    {
        GD.Print("[EngravingRewardCardSelectionUI] OnConfirmPressed called");

        if (_selectedCard == null || _engravingChoice == null)
        {
            GD.Print("[EngravingRewardCardSelectionUI] OnConfirmPressed: 没有选卡或没有刻印");
            return;
        }

        BattleRewardManager manager = BattleRewardManager.Instance;
        if (manager != null)
        {
            manager.ApplyEngravingChoice(_engravingChoice, _selectedCard);
        }
        else
        {
            GD.PrintErr("[EngravingRewardCardSelectionUI] BattleRewardManager 不可用");
        }

        Visible = false;
        Finish();
        GD.Print("[EngravingRewardCardSelectionUI] OnConfirmPressed completed");
    }

    private void OnCancelPressed()
    {
        GD.Print("[EngravingRewardCardSelectionUI] OnCancelPressed called");
        Visible = false;
        Finish();
        GD.Print("[EngravingRewardCardSelectionUI] OnCancelPressed completed");
    }

    /// <summary>回到结算流程</summary>
    private void Finish()
    {
        BattleTransitionManager manager = BattleTransitionManager.Instance;
        if (manager != null)
        {
            manager.CompleteBattleReward();
        }
        else
        {
            GD.PrintErr("[EngravingRewardCardSelectionUI] BattleTransitionManager 不可用");
            GetTree().ChangeSceneToFile("res://Scenes/HexMapScene.tscn");
        }
    }
}
