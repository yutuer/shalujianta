using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Card;

[Tool]
public partial class HandCardPreview : Control
{
	[ExportGroup("Editor Preview")]
	[Export]
	private bool _editorPreviewHand = true;
	[Export(PropertyHint.Range, "1,15,1")]
	private int _editorPreviewCardCount = 6;
	[Export]
	private string _editorPreviewCardIds = "strike,defend,whirlwind,twin_strike,iron_wave,shrug_it_off";

	[ExportGroup("Editor Hover Test")]
	[Export(PropertyHint.Range, "-1,9,1")]
	private int _editorHoverCardIndex = -1;
	[Export]
	private bool _editorSimulateHover = false;

	[ExportGroup("Hand Layout")]
	[Export(PropertyHint.Range, "0,60,0.5")]
	private float _handArcAngleMinDegrees = 5f;
	[Export(PropertyHint.Range, "0,60,0.5")]
	private float _handArcAngleMaxDegrees = 12f;
	[Export(PropertyHint.Range, "100,1000,1")]
	private float _handArcRadiusMin = 200f;
	[Export(PropertyHint.Range, "100,1000,1")]
	private float _handArcRadiusMax = 350f;
	[Export(PropertyHint.Range, "-20,20,0.1")]
	private float _handArcPhaseOffsetDegrees = 0f;
	[Export(PropertyHint.Range, "-400,400,1")]
	private float _handArcCenterOffsetX = 0f;
	[Export(PropertyHint.Range, "-80,120,1")]
	private float _handBottomPadding = 6f;
	[Export(PropertyHint.Range, "0.5,1.2,0.01")]
	private float _handPivotYOffsetFactor = 0.92f;
	[Export(PropertyHint.Range, "0,80,1")]
	private float _handHoverLift = 30f;
	[Export(PropertyHint.Range, "0,80,1")]
	private float _handHoverNeighborPush = 20f;
	[Export(PropertyHint.Range, "0,40,1")]
	private float _handHoverSecondaryPush = 8f;
	[Export(PropertyHint.Range, "1.0,1.5,0.01")]
	private float _handHoverScale = 1.12f;

	[ExportGroup("Hand Debug")]
	[Export]
	private bool _showHandDebugOverlay = false;

	private string _editorPreviewSignature = string.Empty;
	private bool _editorPreviewLayoutPending;
	private bool _editorPreviewRefreshPending;
	private CardLayoutManager _linkedCardLayoutManager;
	private string _cardLayoutManagerPath = string.Empty;

	[ExportGroup("Link to CardLayoutManager")]
	[Export]
	private bool _autoSyncFromCardLayoutManager = true;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			SetupEditorPreview();
			return;
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			UpdateEditorPreview();
			RequestEditorPreviewLayout();
		}
	}

	public override void _ExitTree()
	{
		if (Engine.IsEditorHint())
		{
			ClearEditorPreviewHand();
		}
	}

	private void SetupEditorPreview()
	{
		if (!IsInstanceValid(this))
		{
			return;
		}

		SetAnchorsPreset(LayoutPreset.FullRect);
		CustomMinimumSize = new Vector2(1200, 400);
		Size = new Vector2(1200, 400);

		FindCardLayoutManager();
		RequestEditorPreviewRefresh();
	}

	private void FindCardLayoutManager()
	{
		_linkedCardLayoutManager = null;
		_cardLayoutManagerPath = string.Empty;

		var root = GetTree()?.EditedSceneRoot;
		if (root != null)
		{
			var cardLayoutManager = root.GetNodeOrNull<CardLayoutManager>("CardLayoutManager");
			if (cardLayoutManager != null)
			{
				_linkedCardLayoutManager = cardLayoutManager;
				_cardLayoutManagerPath = root.GetPathTo(cardLayoutManager);
				GD.Print($"[HandCardPreview] Found CardLayoutManager at: {_cardLayoutManagerPath}");
			}
		}

		if (_linkedCardLayoutManager == null)
		{
			var parent = GetParent();
			while (parent != null)
			{
				var sibling = parent.GetNodeOrNull<CardLayoutManager>("CardLayoutManager");
				if (sibling != null)
				{
					_linkedCardLayoutManager = sibling;
					_cardLayoutManagerPath = parent.GetPathTo(sibling);
					GD.Print($"[HandCardPreview] Found CardLayoutManager at sibling: {_cardLayoutManagerPath}");
					break;
				}
				parent = parent.GetParent();
			}
		}
	}

	private void UpdateEditorPreview(bool forceRefresh = false)
	{
		if (!Engine.IsEditorHint() || !IsInstanceValid(this))
		{
			return;
		}

		if (Size.X <= 1f || Size.Y <= 1f)
		{
			RequestEditorPreviewRefresh();
			return;
		}

		SyncFromCardLayoutManager();

		var signature = string.Join("|",
			_editorPreviewHand,
			_editorPreviewCardCount,
			_editorPreviewCardIds,
			_handArcAngleMinDegrees,
			_handArcAngleMaxDegrees,
			_handArcRadiusMin,
			_handArcRadiusMax,
			_handBottomPadding,
			_handPivotYOffsetFactor,
			_handHoverLift,
			_handHoverNeighborPush,
			_handHoverSecondaryPush,
			_handHoverScale,
			_showHandDebugOverlay,
			_editorHoverCardIndex,
			_editorSimulateHover,
			Size.X,
			Size.Y);

		if (!forceRefresh && signature == _editorPreviewSignature)
		{
			return;
		}

		_editorPreviewSignature = signature;

		if (!_editorPreviewHand)
		{
			ClearEditorPreviewHand();
			return;
		}

		RebuildEditorPreviewHand();
	}

	private void SyncFromCardLayoutManager()
	{
		if (!_autoSyncFromCardLayoutManager || _linkedCardLayoutManager == null)
		{
			return;
		}

		if (!IsInstanceValid(_linkedCardLayoutManager))
		{
			FindCardLayoutManager();
			return;
		}

		_handArcAngleMinDegrees = _linkedCardLayoutManager.GetArcAngleMin();
		_handArcAngleMaxDegrees = _linkedCardLayoutManager.GetArcAngleMax();
		_handArcRadiusMin = _linkedCardLayoutManager.GetArcRadiusMin();
		_handArcRadiusMax = _linkedCardLayoutManager.GetArcRadiusMax();
		_handBottomPadding = _linkedCardLayoutManager.GetBottomPadding();
		_handPivotYOffsetFactor = _linkedCardLayoutManager.GetPivotYOffsetFactor();
		_handHoverLift = _linkedCardLayoutManager.GetHoverLift();
		_handHoverNeighborPush = _linkedCardLayoutManager.GetHoverNeighborPush();
		_handHoverSecondaryPush = _linkedCardLayoutManager.GetHoverSecondaryPush();
		_handHoverScale = _linkedCardLayoutManager.GetHoverScale();
	}

	private void RebuildEditorPreviewHand()
	{
		ClearEditorPreviewHand();

		CardConfigLoader.LoadCards();
		var ids = _editorPreviewCardIds
			.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

		var count = Mathf.Clamp(_editorPreviewCardCount, 1, 15);
		for (var i = 0; i < count; i++)
		{
			var cardId = ids[i % ids.Length];
			var cardData = CardConfigLoader.GetCardData(cardId);
			var card = CreatePreviewCard(i, cardData);
			AddChild(card);
		}

		RequestEditorPreviewLayout();
	}

	private void ClearEditorPreviewHand()
	{
		if (!IsInstanceValid(this))
		{
			return;
		}

		foreach (Node child in GetChildren())
		{
			if (child is Panel)
			{
				child.QueueFree();
			}
		}
	}

	private Control CreatePreviewCard(int index, CardData cardData)
	{
		var card = new Panel();
		card.Name = $"PreviewCard_{index}";
		card.CustomMinimumSize = new Vector2(140f, 200f);
		card.ZIndex = 100;

		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0.2f, 0.2f, 0.3f, 1f);
		styleBox.BorderColor = new Color(0.5f, 0.5f, 0.6f, 1f);
		styleBox.BorderWidthLeft = 2;
		styleBox.BorderWidthTop = 2;
		styleBox.BorderWidthRight = 2;
		styleBox.BorderWidthBottom = 2;
		styleBox.CornerRadiusTopLeft = 8;
		styleBox.CornerRadiusTopRight = 8;
		styleBox.CornerRadiusBottomLeft = 8;
		styleBox.CornerRadiusBottomRight = 8;
		card.AddThemeStyleboxOverride("panel", styleBox);

		var vbox = new VBoxContainer();
		vbox.AnchorsPreset = (int)LayoutPreset.FullRect;
		vbox.OffsetLeft = 5;
		vbox.OffsetTop = 5;
		vbox.OffsetRight = -5;
		vbox.OffsetBottom = -5;
		card.AddChild(vbox);

		var typeText = cardData != null ? (cardData.IsAttack ? "攻击" : "防御") : GetCardTypeName(index);
		var typeLabel = new Label { Text = typeText, HorizontalAlignment = HorizontalAlignment.Center };
		typeLabel.AddThemeFontSizeOverride("font_size", 16);
		vbox.AddChild(typeLabel);

		var nameText = cardData?.Name ?? $"卡牌 {index + 1}";
		var nameLabel = new Label { Text = nameText, HorizontalAlignment = HorizontalAlignment.Center };
		nameLabel.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(nameLabel);

		var descText = cardData?.Description ?? "这张卡牌用于\n预览扇形展开效果";
		var descLabel = new Label
		{
			Text = descText,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		descLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(descLabel);

		var costText = cardData != null ? $"能量: {cardData.Cost}" : $"[{index}]";
		var costLabel = new Label { Text = costText, HorizontalAlignment = HorizontalAlignment.Center };
		costLabel.AddThemeFontSizeOverride("font_size", 18);
		costLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0f, 1f));
		vbox.AddChild(costLabel);

		return card;
	}

	private string GetCardTypeName(int index)
	{
		string[] types = { "攻击", "防御", "技能", "必杀", "普通", "特殊" };
		return types[index % types.Length];
	}

	private void RequestEditorPreviewLayout()
	{
		if (!Engine.IsEditorHint() || _editorPreviewLayoutPending)
		{
			return;
		}

		_editorPreviewLayoutPending = true;
		CallDeferred(nameof(DeferredEditorPreviewLayout));
	}

	private void RequestEditorPreviewRefresh()
	{
		if (!Engine.IsEditorHint() || _editorPreviewRefreshPending)
		{
			return;
		}

		_editorPreviewRefreshPending = true;
		CallDeferred(nameof(DeferredEditorPreviewRefresh));
	}

	private void DeferredEditorPreviewRefresh()
	{
		_editorPreviewRefreshPending = false;
		if (!Engine.IsEditorHint() || !IsInstanceValid(this))
		{
			return;
		}

		UpdateEditorPreview(forceRefresh: true);
		RequestEditorPreviewLayout();
	}

	private void DeferredEditorPreviewLayout()
	{
		_editorPreviewLayoutPending = false;
		if (!Engine.IsEditorHint() || !IsInstanceValid(this))
		{
			return;
		}

		var cards = new List<Control>();
		foreach (Node node in GetChildren())
		{
			if (node is Panel panel)
			{
				cards.Add(panel);
			}
		}

		if (cards.Count == 0)
		{
			return;
		}

		var poses = CalculateHandCardPoses(cards);
		for (var i = 0; i < cards.Count; i++)
		{
			var pose = poses[i];
			cards[i].Position = pose.LocalPosition;
			cards[i].RotationDegrees = pose.RotationDegrees;
			cards[i].Scale = pose.Scale;
			cards[i].ZIndex = pose.ZIndex;
		}
	}

	private readonly record struct HandCardPose(Vector2 LocalPosition, float RotationDegrees, Vector2 Scale, int ZIndex);

	private List<HandCardPose> CalculateHandCardPoses(List<Control> cards)
	{
		var count = cards.Count;
		if (count == 0)
		{
			return new List<HandCardPose>();
		}

		var cardSize = new Vector2(140f, 200f);
		var poses = new List<HandCardPose>(count);

		GetHandArcMetrics(count, cardSize, out var centerX, out var pivotOffset, out var pivotBaseY, out var maxAngle, out var radius, out var startAngle, out var angleStep);

		for (var i = 0; i < count; i++)
		{
			var angle = count <= 1 ? 0f : startAngle + angleStep * i;
			angle += Mathf.DegToRad(_handArcPhaseOffsetDegrees);
			var pivotX = centerX + Mathf.Sin(angle) * radius;
			var pivotY = pivotBaseY + (1f - Mathf.Cos(angle)) * radius;
			var rot = Mathf.RadToDeg(angle);

			var pivotPosition = new Vector2(pivotX, pivotY);
			var scale = Vector2.One;
			var finalRot = rot;
			var liftOffset = Vector2.Zero;

			if (_editorSimulateHover && _editorHoverCardIndex >= 0 && _editorHoverCardIndex < count)
			{
				var hoverIndex = _editorHoverCardIndex;
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

				var distFactor = absDist / (float)count;
				var neighborPushAngle = _handHoverNeighborPush * 0.015f * (1f - distFactor);
				var secondaryPushAngle = _handHoverSecondaryPush * 0.008f * (1f - distFactor);
				var rotationPush = distFromHover > 0 ? neighborPushAngle : -neighborPushAngle;

				if (absDist == 1)
				{
					rotationPush = distFromHover > 0 ? neighborPushAngle : -neighborPushAngle;
				}
				else if (absDist >= 2)
				{
					rotationPush = distFromHover > 0 ? secondaryPushAngle : -secondaryPushAngle;
				}

				finalRot += rotationPush;
			}

			var pivotTopLeft = ConvertPivotToTopLeft(pivotPosition + liftOffset, pivotOffset, finalRot, scale);
			poses.Add(new HandCardPose(pivotTopLeft, finalRot, scale, i));
		}

		return poses;
	}

	private void GetHandArcMetrics(int count, Vector2 cardSize, out float centerX, out Vector2 pivotOffset, out float pivotBaseY, out float maxAngle, out float radius, out float startAngle, out float angleStep)
	{
		centerX = Size.X * 0.5f + _handArcCenterOffsetX;
		pivotOffset = new Vector2(cardSize.X * 0.5f, cardSize.Y * _handPivotYOffsetFactor);
		pivotBaseY = Size.Y - (cardSize.Y - pivotOffset.Y) - _handBottomPadding;
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
}
