using Godot;

public partial class CardUI : Panel
{
    private Label nameLabel;
    private TextureRect cardImage;
    private Label costLabel;
    private Label descLabel;
    private Button playButton;
    private TextureRect typeIcon;
    private Label hoverHint;

    private Card cardData;
    private int cardIndex;
    private bool isInteractable = true;
    private bool isHovered = false;

    private Card pendingCard;
    private int pendingIndex;
    private bool pendingCanPlay;
    private Texture2D pendingTexture;
    private System.Action pendingCallback;

    private Tween hoverTween;
    private Vector2 baseScale = Vector2.One;
    private Vector2 hoverScale = new Vector2(1.1f, 1.1f);

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        cardImage = GetNode<TextureRect>("VBoxContainer/ImageContainer/CardImage");
        costLabel = GetNode<Label>("VBoxContainer/CostLabel");
        descLabel = GetNode<Label>("VBoxContainer/DescLabel");
        playButton = GetNode<Button>("VBoxContainer/PlayButton");
        typeIcon = GetNode<TextureRect>("VBoxContainer/TypeIcon");
        hoverHint = GetNode<Label>("VBoxContainer/HoverHint");

        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        costLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1f));
        descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));

        Scale = baseScale;
        hoverHint.Modulate = new Color(1f, 1f, 1f, 0f);

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
        Color glowColor = new Color(0.4f, 0.4f, 0.5f, 0f);
        string iconPath = "";

        if (cardData.IsAttack)
        {
            bgColor = new Color(0.2f, 0.1f, 0.1f, 1f);
            borderColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            glowColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);
            iconPath = "res://Assets/Icons/sword.png";
        }
        else if (cardData.ShieldGain > 0)
        {
            bgColor = new Color(0.1f, 0.15f, 0.2f, 1f);
            borderColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            glowColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
            iconPath = "res://Assets/Icons/shield.png";
        }
        else if (cardData.EnergyGain > 0 || cardData.DrawCount > 0)
        {
            bgColor = new Color(0.15f, 0.12f, 0.2f, 1f);
            borderColor = new Color(0.6f, 0.3f, 0.8f, 1f);
            glowColor = new Color(0.6f, 0.3f, 0.8f, 0.3f);
            iconPath = "res://Assets/Icons/star.png";
        }

        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = bgColor;
        style.BorderColor = borderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(5);
        style.SetShadowColor(glowColor);
        style.SetShadowSize(4);

        AddThemeStyleboxOverride("panel", style);

        if (typeIcon != null && !string.IsNullOrEmpty(iconPath))
        {
            Texture2D iconTex = ResourceLoader.Load<Texture2D>(iconPath);
            if (iconTex != null)
            {
                typeIcon.Texture = iconTex;
                typeIcon.Visible = true;
            }
        }
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;
        isHovered = hovered;

        if (hoverTween != null && hoverTween.IsValid())
        {
            hoverTween.Kill();
        }

        hoverTween = CreateTween();
        hoverTween.SetParallel(true);

        if (hovered)
        {
            hoverTween.TweenProperty(this, "scale", hoverScale, 0.15f);
            ShowHoverHint();
            UpdateGlowIntensity(0.6f);
        }
        else
        {
            hoverTween.TweenProperty(this, "scale", baseScale, 0.15f);
            HideHoverHint();
            UpdateGlowIntensity(0f);
        }

        hoverTween.Play();
    }

    private void ShowHoverHint()
    {
        if (hoverHint == null || !isInteractable) return;
        hoverHint.Text = "右键快速使用";
        hoverTween = CreateTween();
        hoverTween.TweenProperty(hoverHint, "modulate:a", 1f, 0.2f);
    }

    private void HideHoverHint()
    {
        if (hoverHint == null) return;
        hoverTween = CreateTween();
        hoverTween.TweenProperty(hoverHint, "modulate:a", 0f, 0.15f);
    }

    private void UpdateGlowIntensity(float intensity)
    {
        if (cardData == null) return;

        Color glowColor = new Color(0f, 0f, 0f, 0f);
        if (cardData.IsAttack)
        {
            glowColor = new Color(0.8f, 0.3f, 0.3f, intensity);
        }
        else if (cardData.ShieldGain > 0)
        {
            glowColor = new Color(0.3f, 0.5f, 0.8f, intensity);
        }
        else if (cardData.EnergyGain > 0 || cardData.DrawCount > 0)
        {
            glowColor = new Color(0.6f, 0.3f, 0.8f, intensity);
        }

        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = cardData.IsAttack ? new Color(0.2f, 0.1f, 0.1f) :
                        cardData.ShieldGain > 0 ? new Color(0.1f, 0.15f, 0.2f) :
                        new Color(0.15f, 0.15f, 0.2f);
        style.BorderColor = cardData.IsAttack ? new Color(0.8f, 0.3f, 0.3f) :
                           cardData.ShieldGain > 0 ? new Color(0.3f, 0.5f, 0.8f) :
                           new Color(0.6f, 0.3f, 0.8f);
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(5);
        style.SetShadowColor(glowColor);
        style.SetShadowSize((int)(8 * intensity));

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
