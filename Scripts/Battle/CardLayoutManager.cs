using Godot;
using System.Collections.Generic;

public partial class CardLayoutManager
{
    private const float CARD_WIDTH = 140f;
    private const float CARD_HEIGHT = 200f;
    private const float OVERLAP_OFFSET = 105f;
    private const float HOVER_SPREAD_OFFSET = 125f;
    private const float HOVER_LIFT = 80f;
    private const float FAN_SPREAD_ANGLE = 12f;
    private const float FAR_FAN_ANGLE = 25f;
    private const float FAN_BASE_Y_OFFSET = 30f;

    public float CardWidth => CARD_WIDTH;
    public float CardHeight => CARD_HEIGHT;
    public float OverlapOffset => OVERLAP_OFFSET;
    public float HoverSpreadOffset => HOVER_SPREAD_OFFSET;

    public int CheckCardHover(
        List<CardUI> cardUIs,
        Control container,
        Vector2 mousePos,
        int currentHoveredIndex)
    {
        if (cardUIs.Count == 0) return -1;

        int totalCards = cardUIs.Count;
        float handWidth = OVERLAP_OFFSET * (totalCards - 1) + CARD_WIDTH;
        float startX = (container.Size.X - handWidth) / 2f;
        float baseY = container.Size.Y - CARD_HEIGHT - 10;

        Vector2 containerGlobalPos = container.GlobalPosition;
        int newHoveredIndex = -1;

        for (int i = 0; i < cardUIs.Count; i++)
        {
            float x = startX + i * OVERLAP_OFFSET;
            float y = baseY;

            Rect2 cardRect = new Rect2(
                containerGlobalPos.X + x,
                containerGlobalPos.Y + y,
                CARD_WIDTH,
                CARD_HEIGHT
            );

            if (cardRect.HasPoint(mousePos))
            {
                newHoveredIndex = i;
                break;
            }
        }

        return newHoveredIndex;
    }

    public void UpdateCardPositions(
        List<CardUI> cardUIs,
        Control container,
        int hoveredCardIndex)
    {
        if (cardUIs.Count == 0) return;

        int totalCards = cardUIs.Count;
        bool isMouseInHandArea = IsMouseInHandArea(container);
        bool isHovering = isMouseInHandArea && hoveredCardIndex >= 0;

        float currentOffset = isHovering ? HOVER_SPREAD_OFFSET : OVERLAP_OFFSET;
        float handWidth = currentOffset * (totalCards - 1) + CARD_WIDTH;
        float startX = (container.Size.X - handWidth) / 2f;
        float baseY = container.Size.Y - CARD_HEIGHT - 10;

        for (int i = 0; i < cardUIs.Count; i++)
        {
            CardUI cardUI = cardUIs[i];

            float x = startX + i * currentOffset;
            float y = baseY;
            float rotation = 0f;

            int centerIndex = totalCards / 2;
            float distFromCenter = i - centerIndex;

            if (isHovering)
            {
                float distFromHover = i - hoveredCardIndex;

                if (i == hoveredCardIndex)
                {
                    y -= HOVER_LIFT;
                    rotation = 0f;
                }
                else if (Mathf.Abs(distFromHover) <= 2)
                {
                    rotation = distFromHover * FAN_SPREAD_ANGLE * (Mathf.Pi / 180f);
                    y -= HOVER_LIFT * 0.3f * (1f - Mathf.Abs(distFromHover) / 3f);
                }
                else
                {
                    float sign = distFromHover > 0 ? 1f : -1f;
                    float angleDegrees = Mathf.Min(FAR_FAN_ANGLE, 5f * Mathf.Abs(distFromHover));
                    rotation = sign * angleDegrees * (Mathf.Pi / 180f);
                    y += FAN_BASE_Y_OFFSET * 0.5f;
                }

                cardUI.SetHovered(i == hoveredCardIndex);
            }
            else
            {
                rotation = distFromCenter * FAN_SPREAD_ANGLE * 0.3f * (Mathf.Pi / 180f);
                y -= distFromCenter * 5f;
                cardUI.SetHovered(false);
            }

            cardUI.OffsetLeft = x;
            cardUI.OffsetTop = y;
            cardUI.OffsetRight = x + CARD_WIDTH;
            cardUI.OffsetBottom = y + CARD_HEIGHT;

            cardUI.Rotation = rotation;

            int zIndex = i;
            if (i == hoveredCardIndex && isMouseInHandArea)
            {
                zIndex = totalCards + 10;
            }
            cardUI.ZIndex = zIndex;
        }
    }

    public bool IsMouseInHandArea(Control container)
    {
        Vector2 mousePos = container.GetGlobalMousePosition();
        Rect2 handAreaRect = new Rect2(
            container.GlobalPosition,
            container.Size
        );
        return handAreaRect.HasPoint(mousePos);
    }

    public Vector2 GetCardDrawStartPosition(Control container)
    {
        return new Vector2(
            container.Size.X / 2f - CARD_WIDTH / 2f,
            container.Size.Y + 100f
        );
    }

    public (float targetX, float targetY) GetCardDrawTargetPosition(Control container, int cardIndex, int totalCards)
    {
        float handWidth = OVERLAP_OFFSET * (totalCards - 1) + CARD_WIDTH;
        float targetX = (container.Size.X - handWidth) / 2f + cardIndex * OVERLAP_OFFSET;
        float targetY = container.Size.Y - CARD_HEIGHT - 10;
        return (targetX, targetY);
    }
}
