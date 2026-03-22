using Godot;

public partial class CardUI : Panel
{
    private Label nameLabel;
    private TextureRect cardImage;
    private Label costLabel;
    private Label descLabel;
    private Button playButton;

    private Card cardData;
    private int cardIndex;
    private bool isInteractable = true;

    private Card pendingCard;
    private int pendingIndex;
    private bool pendingCanPlay;
    private Texture2D pendingTexture;
    private System.Action pendingCallback;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        cardImage = GetNode<TextureRect>("VBoxContainer/ImageContainer/CardImage");
        costLabel = GetNode<Label>("VBoxContainer/CostLabel");
        descLabel = GetNode<Label>("VBoxContainer/DescLabel");
        playButton = GetNode<Button>("VBoxContainer/PlayButton");

        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        costLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1f));
        descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));

        ApplyPendingSetup();
    }

    private void ApplyPendingSetup()
    {
        if (pendingCard != null)
        {
            ApplySetup(pendingCard, pendingIndex, pendingCanPlay);
            if (pendingTexture != null && cardImage != null)
            {
                cardImage.Texture = pendingTexture;
            }
        }
        if (pendingCallback != null && playButton != null)
        {
            playButton.Pressed += pendingCallback;
        }
    }

    public void Setup(Card card, int index, bool canPlay)
    {
        cardData = card;
        cardIndex = index;
        isInteractable = canPlay;

        pendingCard = card;
        pendingIndex = index;
        pendingCanPlay = canPlay;

        if (IsInsideTree())
        {
            ApplySetup(card, index, canPlay);
        }
    }

    private void ApplySetup(Card card, int index, bool canPlay)
    {
        Name = $"Card_{index}";
        nameLabel.Text = card.Name;
        costLabel.Text = $"费用: {card.Cost}";
        descLabel.Text = card.Description;
        playButton.Disabled = !canPlay;

        UpdateCardStyle();
    }

    public void SetTexture(Texture2D texture)
    {
        pendingTexture = texture;
        if (cardImage != null && texture != null)
        {
            cardImage.Texture = texture;
        }
    }

    public void SetInteractable(bool canPlay)
    {
        isInteractable = canPlay;
        if (playButton != null)
        {
            playButton.Disabled = !canPlay;
        }
    }

    public void SetPlayCallback(System.Action callback)
    {
        pendingCallback = callback;
        if (playButton != null && callback != null)
        {
            playButton.Pressed += callback;
        }
    }

    public void UpdateCardStyle()
    {
        if (cardData == null) return;

        Color bgColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        Color borderColor = new Color(0.4f, 0.4f, 0.5f, 1f);

        if (cardData.IsAttack)
        {
            bgColor = new Color(0.2f, 0.1f, 0.1f, 1f);
            borderColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        }
        else if (cardData.ShieldGain > 0)
        {
            bgColor = new Color(0.1f, 0.15f, 0.2f, 1f);
            borderColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        }

        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = bgColor;
        style.BorderColor = borderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(5);

        AddThemeStyleboxOverride("panel", style);
    }

    public Button GetPlayButton()
    {
        return playButton;
    }

    public Card GetCardData()
    {
        return cardData;
    }

    public int GetCardIndex()
    {
        return cardIndex;
    }
}
