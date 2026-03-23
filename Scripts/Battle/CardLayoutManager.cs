using Godot;
using System.Collections.Generic;

[Tool]
public partial class CardLayoutManager : Node
{
	[ExportGroup("Card Size")]
	[Export(PropertyHint.Range, "80,200,1")]
	private float _cardWidth = 140f;
	[Export(PropertyHint.Range, "100,300,1")]
	private float _cardHeight = 200f;

	[ExportGroup("Card Overlap")]
	[Export(PropertyHint.Range, "50,150,1")]
	private float _overlapOffset = 105f;
	[Export(PropertyHint.Range, "80,180,1")]
	private float _hoverSpreadOffset = 125f;

	[ExportGroup("Hand Layout")]
	[Export(PropertyHint.Range, "0,60,0.5")]
	private float _handArcAngleMinDegrees = 5f;
	[Export(PropertyHint.Range, "0,60,0.5")]
	private float _handArcAngleMaxDegrees = 12f;
	[Export(PropertyHint.Range, "100,1500,1")]
	private float _handArcRadiusMin = 720f;
	[Export(PropertyHint.Range, "100,1500,1")]
	private float _handArcRadiusMax = 1080f;
	[Export(PropertyHint.Range, "-400,400,1")]
	private float _handArcCenterOffsetX = 0f;
	[Export(PropertyHint.Range, "-80,120,1")]
	private float _handBottomPadding = 10f;
	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	private float _handPivotYOffsetFactor = 0.92f;

	[ExportGroup("Hover Effects")]
	[Export(PropertyHint.Range, "0,150,1")]
	private float _handHoverLift = 80f;
	[Export(PropertyHint.Range, "0,150,1")]
	private float _handHoverNeighborPush = 20f;
	[Export(PropertyHint.Range, "0,80,1")]
	private float _handHoverSecondaryPush = 8f;
	[Export(PropertyHint.Range, "1.0,1.5,0.01")]
	private float _handHoverScale = 1.12f;

	[ExportGroup("Use Advanced Arc Layout")]
	[Export]
	private bool _useAdvancedArcLayout = true;

	public float CardWidth => _cardWidth;
	public float CardHeight => _cardHeight;
	public float OverlapOffset => _overlapOffset;
	public float HoverSpreadOffset => _hoverSpreadOffset;

	public float GetArcAngleMin() => _handArcAngleMinDegrees;
	public float GetArcAngleMax() => _handArcAngleMaxDegrees;
	public float GetArcRadiusMin() => _handArcRadiusMin;
	public float GetArcRadiusMax() => _handArcRadiusMax;
	public float GetBottomPadding() => _handBottomPadding;
	public float GetPivotYOffsetFactor() => _handPivotYOffsetFactor;
	public float GetHoverLift() => _handHoverLift;
	public float GetHoverNeighborPush() => _handHoverNeighborPush;
	public float GetHoverSecondaryPush() => _handHoverSecondaryPush;
	public float GetHoverScale() => _handHoverScale;
	public float GetCenterOffsetX() => _handArcCenterOffsetX;
	public bool GetUseAdvancedArcLayout() => _useAdvancedArcLayout;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			return;
		}

		GD.Print("[CardLayoutManager] Editor mode initialized");
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			return;
		}
	}

	public int CheckCardHover(
		List<CardUI> cardUIs,
		Control container,
		Vector2 mousePos,
		int currentHoveredIndex)
	{
		if (cardUIs.Count == 0) return -1;

		int totalCards = cardUIs.Count;
		float handWidth = _overlapOffset * (totalCards - 1) + _cardWidth;
		float startX = (container.Size.X - handWidth) / 2f;
		float baseY = container.Size.Y - _cardHeight - 10;

		Vector2 containerGlobalPos = container.GlobalPosition;
		int newHoveredIndex = -1;

		for (int i = 0; i < cardUIs.Count; i++)
		{
			float x = startX + i * _overlapOffset;
			float y = baseY;

			Rect2 cardRect = new Rect2(
				containerGlobalPos.X + x,
				containerGlobalPos.Y + y,
				_cardWidth,
				_cardHeight
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

		float currentOffset = isHovering ? _hoverSpreadOffset : _overlapOffset;

		if (_useAdvancedArcLayout && totalCards > 0)
		{
			UpdateCardPositionsAdvanced(cardUIs, container, hoveredCardIndex, isHovering, isMouseInHandArea);
		}
		else
		{
			UpdateCardPositionsSimple(cardUIs, container, hoveredCardIndex, isHovering, isMouseInHandArea, currentOffset);
		}
	}

	private void UpdateCardPositionsAdvanced(
		List<CardUI> cardUIs,
		Control container,
		int hoveredCardIndex,
		bool isHovering,
		bool isMouseInHandArea)
	{
		int totalCards = cardUIs.Count;
		var cardSize = new Vector2(_cardWidth, _cardHeight);

		GetHandArcMetrics(container, totalCards, cardSize, out var centerX, out var pivotOffset, out var pivotBaseY, out var maxAngle, out var radius, out var startAngle, out var angleStep);

		for (int i = 0; i < totalCards; i++)
		{
			CardUI cardUI = cardUIs[i];
			float angle = totalCards <= 1 ? 0f : startAngle + angleStep * i;
			float pivotX = centerX + Mathf.Sin(angle) * radius;
			float pivotY = pivotBaseY + (1f - Mathf.Cos(angle)) * radius;
			float rot = Mathf.RadToDeg(angle);

			var pivotPosition = new Vector2(pivotX, pivotY);
			var scale = Vector2.One;
			var finalRot = rot;
			var liftOffset = Vector2.Zero;

			if (isHovering && hoveredCardIndex >= 0 && hoveredCardIndex < totalCards)
			{
				var hoverIndex = hoveredCardIndex;
				var distFromHover = i - hoverIndex;
				var absDist = Mathf.Abs(distFromHover);

				if (i == hoverIndex)
				{
					liftOffset = new Vector2(0, -_handHoverLift);
					scale = new Vector2(_handHoverScale, _handHoverScale);
				}
				else if (absDist == 1)
				{
					liftOffset = new Vector2(0, -_handHoverNeighborPush);
				}
				else if (absDist == 2)
				{
					liftOffset = new Vector2(0, -_handHoverSecondaryPush);
				}

				var distFactor = absDist / (float)totalCards;
				var neighborPushAngle = _handHoverNeighborPush * 0.015f * (1f - distFactor);
				var secondaryPushAngle = _handHoverSecondaryPush * 0.008f * (1f - distFactor);
				float rotationPush;

				if (absDist == 1)
				{
					rotationPush = distFromHover > 0 ? neighborPushAngle : -neighborPushAngle;
				}
				else if (absDist >= 2)
				{
					rotationPush = distFromHover > 0 ? secondaryPushAngle : -secondaryPushAngle;
				}
				else
				{
					rotationPush = 0f;
				}

				finalRot += rotationPush;
			}

			var pivotTopLeft = ConvertPivotToTopLeft(pivotPosition + liftOffset, pivotOffset, finalRot, scale);

			cardUI.OffsetLeft = pivotTopLeft.X;
			cardUI.OffsetTop = pivotTopLeft.Y;
			cardUI.OffsetRight = pivotTopLeft.X + _cardWidth;
			cardUI.OffsetBottom = pivotTopLeft.Y + _cardHeight;

			cardUI.Rotation = Mathf.DegToRad(finalRot);
			cardUI.Scale = scale;

			int zIndex = i;
			if (i == hoveredCardIndex && isMouseInHandArea)
			{
				zIndex = totalCards + 10;
			}
			cardUI.ZIndex = zIndex;

			cardUI.SetHovered(i == hoveredCardIndex && isMouseInHandArea);
		}
	}

	private void UpdateCardPositionsSimple(
		List<CardUI> cardUIs,
		Control container,
		int hoveredCardIndex,
		bool isHovering,
		bool isMouseInHandArea,
		float currentOffset)
	{
		int totalCards = cardUIs.Count;
		float handWidth = currentOffset * (totalCards - 1) + _cardWidth;
		float startX = (container.Size.X - handWidth) / 2f;
		float baseY = container.Size.Y - _cardHeight - 10;

		const float FAN_SPREAD_ANGLE = 12f;
		const float FAR_FAN_ANGLE = 25f;
		const float FAN_BASE_Y_OFFSET = 30f;

		for (int i = 0; i < totalCards; i++)
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
					y -= _handHoverLift;
					rotation = 0f;
				}
				else if (Mathf.Abs(distFromHover) <= 2)
				{
					rotation = distFromHover * FAN_SPREAD_ANGLE * (Mathf.Pi / 180f);
					y -= _handHoverLift * 0.3f * (1f - Mathf.Abs(distFromHover) / 3f);
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
			cardUI.OffsetRight = x + _cardWidth;
			cardUI.OffsetBottom = y + _cardHeight;

			cardUI.Rotation = rotation;

			int zIndex = i;
			if (i == hoveredCardIndex && isMouseInHandArea)
			{
				zIndex = totalCards + 10;
			}
			cardUI.ZIndex = zIndex;
		}
	}

	private void GetHandArcMetrics(Control container, int count, Vector2 cardSize, out float centerX, out Vector2 pivotOffset, out float pivotBaseY, out float maxAngle, out float radius, out float startAngle, out float angleStep)
	{
		centerX = container.Size.X * 0.5f + _handArcCenterOffsetX;
		pivotOffset = new Vector2(cardSize.X * 0.5f, cardSize.Y * _handPivotYOffsetFactor);
		pivotBaseY = container.Size.Y - (cardSize.Y - pivotOffset.Y) - _handBottomPadding;
		var spreadFactor = Mathf.Clamp((count - 1) / 7f, 0f, 1f);
		maxAngle = Mathf.DegToRad(Mathf.Lerp(_handArcAngleMinDegrees, _handArcAngleMaxDegrees, spreadFactor));
		radius = Mathf.Lerp(_handArcRadiusMax, _handArcRadiusMin, spreadFactor);
		startAngle = -maxAngle;
		angleStep = count <= 1 ? 0f : (maxAngle * 2f) / (count - 1);
	}

	private static Vector2 ConvertPivotToTopLeft(Vector2 pivotPosition, Vector2 pivotOffset, float rotationDegrees, Vector2 scale)
	{
		var uniformScale = (scale.X + scale.Y) * 0.5f;
		var scaledPivot = pivotOffset * uniformScale;
		var rotatedPivot = scaledPivot.Rotated(Mathf.DegToRad(rotationDegrees));
		return pivotPosition - rotatedPivot;
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
			container.Size.X / 2f - _cardWidth / 2f,
			container.Size.Y + 100f
		);
	}

	public (float targetX, float targetY) GetCardDrawTargetPosition(Control container, int cardIndex, int totalCards)
	{
		float handWidth = _overlapOffset * (totalCards - 1) + _cardWidth;
		float targetX = (container.Size.X - handWidth) / 2f + cardIndex * _overlapOffset;
		float targetY = container.Size.Y - _cardHeight - 10;
		return (targetX, targetY);
	}
}
