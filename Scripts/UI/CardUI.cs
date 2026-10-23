using Godot;
using FishEatFish.Battle.Card;
using FishEatFish.Battle.CharacterSystem;

public partial class CardUI : Panel
{
    private Label nameLabel;
    private TextureRect cardImage;
    private Label costLabel;
    private Label descLabel;
    private TextureRect typeIcon;
    private Label hoverHint;
    private Label levelLabel;
    private TextureRect engravingIcon;

    private Card cardData;
    private int cardIndex;
    private bool isInteractable = true;
    private bool isHovered = false;

    private Card pendingCard;
    private int pendingIndex;
    private bool pendingCanPlay;
    private Texture2D pendingTexture;
    private System.Action pendingCallback;

    private CharacterAttributes _linkedAttributes;

    // ==================== 拖拽出牌 ====================

    /// <summary>拖拽阶段</summary>
    public enum DragPhase
    {
        /// <summary>没按</summary>
        None,
        /// <summary>按住了（可能还没移动）</summary>
        Pressed,
        /// <summary>按住了并在往上拖</summary>
        Dragging,
        /// <summary>上移越过阈值，松手就出牌</summary>
        Ready
    }

    /// <summary>往上拖多少像素算「明显脱离牌堆」（松手即出牌）</summary>
    public const float DragReadyThreshold = 110f;

    /// <summary>按住时卡牌的放大倍数 —— 放大后牌面文字（名称/类型/描述/等级）才读得清</summary>
    private const float DragScale = 1.5f;

    /// <summary>刚按住时的最小抬起量（哪怕鼠标没动，卡牌也要明显高出牌堆）</summary>
    private const float PressedMinLift = 40f;

    /// <summary>横向拖动上限（防止把牌拖飞）</summary>
    private const float DragMaxHorizontal = 90f;

    /// <summary>
    /// 拖拽放大 / 复原的过渡速度（每秒推进多少）。
    /// 14 ≈ 0.07 秒走完全程，够快但不是"啪"地一下跳过去。
    /// </summary>
    private const float DragBlendSpeed = 14f;

    // ==================== 长按放大详情 ====================

    /// <summary>按住多久算「长按」（秒）</summary>
    private const float LongPressDelay = 0.35f;

    /// <summary>长按期间允许的鼠标抖动（像素）—— 超过就当成「要拖拽」</summary>
    private const float LongPressMoveTolerance = 8f;

    /// <summary>长按时卡牌放大到多少倍（比拖拽的 1.5 倍更大，文字才读得清）</summary>
    private const float LongPressScale = 2.2f;

    /// <summary>长按放大时卡牌往上抬多少（像素）—— 不然放大后下半截会掉出屏幕</summary>
    private const float LongPressLift = 150f;

    /// <summary>短按（没拖动、没长按）时触发 —— 打开卡牌详情界面</summary>
    public System.Action<Card> OnCardClicked;

    private float _pressElapsed;
    private bool _longPressTriggered;
    private bool _pressMoved;
    private float _longPressBlend;

    private DragPhase _dragPhase = DragPhase.None;
    private Vector2 _pressMouseGlobal;
    private Vector2 _dragDelta = Vector2.Zero;
    private float _dragBlend = 0f;

    /// <summary>手牌扇形布局给这张牌算出的「原位」</summary>
    private Vector2 _homeTopLeft = Vector2.Zero;
    private float _homeRotationDeg = 0f;
    private float _homeScale = 1f;

    /// <summary>卡牌未缩放时的尺寸（165x235，来自 Card.tscn）</summary>
    private readonly Vector2 _baseSize = new Vector2(165f, 235f);

    /// <summary>当前是否处于「按住」状态（拖拽中或按住未松）</summary>
    public bool IsDragging => _dragPhase != DragPhase.None;

    /// <summary>当前是否已经上移到「松手即出牌」的状态</summary>
    public bool IsDragReady => _dragPhase == DragPhase.Ready;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("VBoxContainer/HeaderRow/NameLabel");
        cardImage = GetNode<TextureRect>("VBoxContainer/ImageContainer/CardImage");
        costLabel = GetNode<Label>("CostBadge/CostLabel");
        descLabel = GetNode<Label>("VBoxContainer/DescLabel");
        typeIcon = GetNode<TextureRect>("VBoxContainer/HeaderRow/TypeIcon");
        hoverHint = GetNode<Label>("VBoxContainer/HoverHint");

        Node levelNode = GetNodeOrNull("VBoxContainer/LevelLabel");
        if (levelNode != null)
        {
            levelLabel = levelNode as Label;
        }

        engravingIcon = GetNodeOrNull<TextureRect>("EngravingIcon");
        if (engravingIcon == null)
        {
            GD.PrintErr("[CardUI] 未找到 EngravingIcon 节点");
        }

        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        costLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.9f, 1f));
        descLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.82f, 0.84f));

        // 费用角标：深色圆底 + 亮色数字（对照参考图 R-08 的左上角圆标）
        var badge = GetNodeOrNull<PanelContainer>("CostBadge");
        if (badge != null)
        {
            var badgeStyle = new StyleBoxFlat();
            badgeStyle.BgColor = new Color(0.08f, 0.1f, 0.16f, 0.95f);
            badgeStyle.BorderColor = new Color(0.45f, 0.62f, 0.85f, 0.9f);
            badgeStyle.SetBorderWidthAll(2);
            badgeStyle.SetCornerRadiusAll(17);
            badge.AddThemeStyleboxOverride("panel", badgeStyle);
        }

        hoverHint.Modulate = new Color(1f, 1f, 1f, 0f);
        hoverHint.Text = "按住上移，松手使用";

        // 牌面内部的容器都设为 IGNORE，事件统一由卡面本身（Panel）接收，
        // 否则点描述/插画会被子控件吃掉，按住拖不动牌。
        SetChildrenIgnoreMouse(this);

        ApplyPendingSetup();
    }

    /// <summary>把牌内所有子控件的鼠标过滤设为 IGNORE（只留牌面本体接收事件）</summary>
    private static void SetChildrenIgnoreMouse(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control)
            {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
            SetChildrenIgnoreMouse(child);
        }
    }

    public override void _Process(double delta)
    {
        // 拖拽状态**不靠轮询判断松手** —— 按下之后 Godot 会把鼠标焦点锁在这张牌上，
        // 移动与松手事件都会继续送到这里（标准拖拽模式）。
        // 之前用 Input.IsMouseButtonPressed 轮询，在某些情况下会误判成"已松手"把拖拽掐掉。
        if (_dragPhase != DragPhase.None)
        {
            // ⚠️ 放大过渡**必须每帧推进**：
            // 之前只在"收到鼠标移动事件"时推进 → 鼠标按住不动就永远推不到 1，
            // 卡牌就一直是半大不大的样子，看起来"放大非常缓慢"（其实是卡住了）。
            _dragBlend = Mathf.MoveToward(_dragBlend, 1f, (float)delta * DragBlendSpeed);

            // 长按检测：**按住不动**超过阈值 → 就地放大，把卡面详情放大出来（松手复原）。
            // 一旦鼠标开始移动（超过容差）就当作「要拖拽」，长按作废 —— 拖拽出牌不能被长按吃掉。
            if (_dragPhase == DragPhase.Pressed && !_longPressTriggered)
            {
                _pressElapsed += (float)delta;
                if (_pressElapsed >= LongPressDelay &&
                    GetGlobalMousePosition().DistanceTo(_pressMouseGlobal) <= LongPressMoveTolerance)
                {
                    _longPressTriggered = true;
                    GD.Print($"[CardUI] 长按触发，放大详情：{cardData?.CardId}（{cardData?.Name}）");
                }
            }

            _longPressBlend = Mathf.MoveToward(_longPressBlend, _longPressTriggered ? 1f : 0f, (float)delta * DragBlendSpeed);
            ApplyTransform();
            return;
        }

        // 取消出牌后：卡牌平滑滑回牌堆（_dragBlend 与 _dragDelta 一起归零）
        if (_dragBlend > 0.001f || _longPressBlend > 0.001f)
        {
            _dragBlend = Mathf.Max(0f, _dragBlend - (float)delta * DragBlendSpeed);
            _dragDelta = _dragDelta.Lerp(Vector2.Zero, Mathf.Min(1f, (float)delta * 12f));
            _longPressBlend = Mathf.Max(0f, _longPressBlend - (float)delta * DragBlendSpeed);
            ApplyTransform();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!isInteractable) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                if (_dragPhase != DragPhase.None)
                {
                    // 已经在拖了，再点一下 = 取消出牌
                    FinishDrag(false);
                }
                else
                {
                    BeginPress(GetGlobalMousePosition());
                }
            }
            else
            {
                // 松手：越过阈值就出牌，否则取消
                FinishDrag(_dragPhase == DragPhase.Ready);
            }
            AcceptEvent();
        }
        else if (@event is InputEventMouseMotion && _dragPhase != DragPhase.None)
        {
            // 按住之后鼠标焦点锁在这张牌上，移动事件会一直送到这里
            UpdateDrag(GetProcessDeltaTime(), GetGlobalMousePosition());
            AcceptEvent();
        }
    }

    /// <summary>按下：进入「按住」状态，卡牌开始脱离牌堆并放大</summary>
    /// <param name="globalMouse">按下的全局坐标</param>
    public void BeginPress(Vector2 globalMouse)
    {
        if (_dragPhase != DragPhase.None) return;

        _dragPhase = DragPhase.Pressed;
        _pressMouseGlobal = globalMouse;
        _dragDelta = Vector2.Zero;
        _pressElapsed = 0f;
        _longPressTriggered = false;
        _pressMoved = false;
        ZIndex = 1000;

        UpdateHint();
        UpdateGlowIntensity(0.75f);
    }

    /// <summary>拖拽中：卡牌跟手，上移越过阈值就进入 Ready</summary>
    /// <param name="delta">帧间隔</param>
    /// <param name="globalMouse">当前全局鼠标坐标</param>
    public void UpdateDrag(double delta, Vector2 globalMouse)
    {
        _dragDelta = globalMouse - _pressMouseGlobal;

        // 动过了 → 不算长按（长按只在「按住不动」时成立），拖拽出牌优先
        if (_dragDelta.Length() > LongPressMoveTolerance)
        {
            _pressMoved = true;
            _longPressTriggered = false;
        }

        // 只允许往上拖（横向限制在小范围内）
        if (_dragDelta.Y > 0f) _dragDelta.Y = 0f;
        _dragDelta.X = Mathf.Clamp(_dragDelta.X, -DragMaxHorizontal, DragMaxHorizontal);

        // 刚按住时（鼠标还没动）也要明显高出牌堆
        if (_dragDelta.Y > -PressedMinLift) _dragDelta.Y = -PressedMinLift;


        bool ready = -_dragDelta.Y >= DragReadyThreshold;
        if (ready)
        {
            _dragPhase = DragPhase.Ready;
        }
        else if (_dragPhase == DragPhase.Ready)
        {
            _dragPhase = DragPhase.Dragging;
        }
        else if (_dragPhase == DragPhase.Pressed && (globalMouse - _pressMouseGlobal).LengthSquared() > 4f)
        {
            _dragPhase = DragPhase.Dragging;
        }

        UpdateHint();
        UpdateGlowIntensity(ready ? 1f : 0.75f);

        // 当帧就把变换应用上（布局器每帧也会重算一次，这里先落一次，拖起来才跟手）
        ApplyTransform();
    }

    /// <summary>松手收尾</summary>
    /// <param name="commit">true = 越过阈值，出牌</param>
    public void FinishDrag(bool commit)
    {
        if (_dragPhase == DragPhase.None) return;

        bool wasLongPress = _longPressTriggered;
        bool moved = _pressMoved || GetGlobalMousePosition().DistanceTo(_pressMouseGlobal) > LongPressMoveTolerance;

        _dragPhase = DragPhase.None;

        if (commit)
        {
            // 出牌：直接触发回调（卡牌随后会被手牌刷新移除）
            _dragBlend = 0f;
            _dragDelta = Vector2.Zero;
            UpdateHint();
            UpdateGlowIntensity(0f);
            Play();
            return;
        }

        // 短按松手（没拖动、也没长按）= 「点击这张卡」→ 打开卡牌详情界面
        if (!wasLongPress && !moved && cardData != null)
        {
            GD.Print($"[CardUI] 点击卡牌：{cardData.CardId}（{cardData.Name}）");
            OnCardClicked?.Invoke(cardData);
        }

        // 取消：_dragBlend / _dragDelta 在 _Process 里平滑归零 → 卡牌滑回牌堆
        UpdateHint();
        UpdateGlowIntensity(isHovered ? 0.6f : 0f);
    }

    /// <summary>出牌（右键快捷出牌也走这里）</summary>
    public void Play()
    {
        if (pendingCallback == null) return;
        pendingCallback();
    }

    /// <summary>
    /// 由手牌布局器每帧调用：写入这张牌在扇形里的「原位」。
    /// </summary>
    public void SetHomeTransform(Vector2 topLeft, float rotationDeg, float scale)
    {
        _homeTopLeft = topLeft;
        _homeRotationDeg = rotationDeg;
        _homeScale = scale;
    }

    /// <summary>
    /// 由手牌布局器每帧调用：把「原位」和「拖拽位移」混合成最终变换。
    /// 拖拽中：脱离牌堆、摆正、放大；松手取消：平滑滑回。
    /// </summary>
    public void ApplyTransform()
    {
        float baseScale = Mathf.Lerp(_homeScale, DragScale, _dragBlend);
        // 长按放大：比拖拽的 1.5 倍更大，卡面文字才读得清（这就是「把详情放大出来」）
        float s = Mathf.Lerp(baseScale, LongPressScale, _longPressBlend);
        Vector2 homeCenter = _homeTopLeft + _baseSize * 0.5f;
        Vector2 center = homeCenter + _dragDelta * _dragBlend;
        // 长按放大时整张牌往上抬 —— 不抬的话放大的下半截会掉出屏幕
        center -= new Vector2(0f, LongPressLift * _longPressBlend);
        Vector2 topLeft = center - _baseSize * 0.5f * s;

        OffsetLeft = topLeft.X;
        OffsetTop = topLeft.Y;
        OffsetRight = topLeft.X + _baseSize.X;
        OffsetBottom = topLeft.Y + _baseSize.Y;

        Rotation = Mathf.Lerp(Mathf.DegToRad(_homeRotationDeg), 0f, _dragBlend);
        Scale = new Vector2(s, s);
    }

    /// <summary>全局坐标点是否落在这张牌上（考虑旋转与缩放）</summary>
    public bool ContainsGlobalPoint(Vector2 globalPos)
    {
        if (!IsInsideTree()) return false;
        Vector2 local = GetGlobalTransform().AffineInverse() * globalPos;
        return new Rect2(Vector2.Zero, Size).HasPoint(local);
    }

    /// <summary>这张牌当前能不能出（回合中且算力够）</summary>
    public bool IsInteractive => isInteractable;

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
    }

    public void Setup(Card card, int index, bool canPlay)
    {
        if (cardData != null)
        {
            cardData.OnValuesChanged -= OnCardValuesChanged;
            cardData.OnLevelChanged -= OnCardLevelChanged;
        }

        cardData = card;
        cardIndex = index;
        isInteractable = canPlay;

        pendingCard = card;
        pendingIndex = index;
        pendingCanPlay = canPlay;

        cardData.OnValuesChanged += OnCardValuesChanged;
        cardData.OnLevelChanged += OnCardLevelChanged;

        if (IsInsideTree())
        {
            ApplySetup(card, index, canPlay);
        }
    }

    public void SetLinkedAttributes(CharacterAttributes attributes)
    {
        _linkedAttributes = attributes;
        if (cardData != null)
        {
            cardData.LinkedAttributes = attributes;
        }
        RefreshDescription();
    }

    private void OnCardValuesChanged(Card card)
    {
        RefreshDescription();
    }

    private void OnCardLevelChanged(ICardLevelable card)
    {
        UpdateLevelDisplay();
    }

    private void RefreshDescription()
    {
        if (cardData == null || descLabel == null) return;
        descLabel.Text = DescriptionGenerator.GenerateCardDescription(cardData, _linkedAttributes);
    }

    private void UpdateLevelDisplay()
    {
        if (cardData == null || levelLabel == null) return;
        levelLabel.Text = DescriptionGenerator.GenerateLevelInfo(cardData);
        UpdateLevelStyle();
    }

    private void UpdateLevelStyle()
    {
        if (cardData == null || levelLabel == null) return;

        if (cardData.Level >= cardData.MaxLevel)
        {
            levelLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        }
        else if (cardData.Level >= 5)
        {
            levelLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
        }
        else
        {
            levelLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        }
    }

    private void ApplySetup(Card card, int index, bool canPlay)
    {
        Name = $"Card_{index}";
        nameLabel.Text = card.Name;
        costLabel.Text = card.Cost.ToString();
        descLabel.Text = DescriptionGenerator.GenerateCardDescription(card, _linkedAttributes);

        UpdateLevelDisplay();
        UpdateEngravingBadge(card);
        UpdateCardStyle();
    }

    /// <summary>
    /// 刻印角标：卡上刻了印就在右上角显示刻印图标（没刻则隐藏）。
    /// 图标资源必须真实存在，否则不显示（不显示占位图，避免误导）。
    /// </summary>
    private void UpdateEngravingBadge(Card card)
    {
        if (engravingIcon == null) return;

        if (card == null || !card.IsEngraved || card.EngravingEffect == null)
        {
            engravingIcon.Visible = false;
            engravingIcon.Texture = null;
            return;
        }

        string iconPath = card.EngravingEffect.IconPath;
        if (string.IsNullOrEmpty(iconPath) || !ResourceLoader.Exists(iconPath))
        {
            GD.PrintErr($"[CardUI] 刻印图标不存在: {iconPath}");
            engravingIcon.Visible = false;
            engravingIcon.Texture = null;
            return;
        }

        Texture2D texture = ResourceLoader.Load<Texture2D>(iconPath);
        if (texture == null)
        {
            engravingIcon.Visible = false;
            engravingIcon.Texture = null;
            return;
        }

        engravingIcon.Texture = texture;
        engravingIcon.Visible = true;
        engravingIcon.TooltipText = card.EngravingEffect.Name;
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
    }

    public void SetPlayCallback(System.Action callback)
    {
        pendingCallback = callback;
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
            iconPath = "res://Assets/Icons/intent_attack.svg";
        }
        else if (cardData.IsDefenseCard())
        {
            bgColor = new Color(0.1f, 0.15f, 0.2f, 1f);
            borderColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            glowColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
            iconPath = "res://Assets/Icons/intent_defend.svg";
        }
        else if (cardData.EnergyGain > 0 || cardData.DrawCount > 0)
        {
            bgColor = new Color(0.15f, 0.12f, 0.2f, 1f);
            borderColor = new Color(0.6f, 0.3f, 0.8f, 1f);
            glowColor = new Color(0.6f, 0.3f, 0.8f, 0.3f);
            iconPath = "res://Assets/Icons/intent_buff.svg";
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

    /// <summary>
    /// 悬停：只做「轻微抬高」由布局器负责，这里只管辉光与提示文字。
    /// ⚠️ 不要在这里改 Scale —— 缩放归布局器统一管，否则两处会打架。
    /// </summary>
    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;
        isHovered = hovered;

        if (_dragPhase != DragPhase.None) return;

        if (hovered)
        {
            UpdateHint();
            UpdateGlowIntensity(0.6f);
        }
        else
        {
            UpdateHint();
            UpdateGlowIntensity(0f);
        }
    }

    /// <summary>按当前交互阶段刷新提示文字</summary>
    private void UpdateHint()
    {
        if (hoverHint == null) return;

        string text;
        float alpha;

        // 长按放大详情中：提示改成「松手还原」（这时拖拽提示会误导）
        if (_longPressTriggered)
        {
            text = "松手还原";
            alpha = 1f;

            hoverHint.Text = text;
            hoverHint.Visible = alpha > 0.01f;
            hoverHint.Modulate = new Color(1f, 1f, 1f, alpha);
            return;
        }

        switch (_dragPhase)
        {
            case DragPhase.Ready:
                text = "松手使用！";
                alpha = 1f;
                break;
            case DragPhase.Pressed:
            case DragPhase.Dragging:
                text = "继续上移，松手使用";
                alpha = 1f;
                break;
            default:
                text = isHovered && isInteractable ? "按住上移，松手使用" : "按住上移，松手使用";
                alpha = isHovered && isInteractable ? 1f : 0f;
                break;
        }

        if (!isInteractable)
        {
            text = "算力不足";
            alpha = isHovered ? 1f : 0f;
        }

        hoverHint.Text = text;
        hoverHint.Visible = alpha > 0.01f;
        hoverHint.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    private void UpdateGlowIntensity(float intensity)
    {
        if (cardData == null) return;

        Color glowColor = new Color(0f, 0f, 0f, 0f);
        if (cardData.IsAttack)
        {
            glowColor = new Color(0.8f, 0.3f, 0.3f, intensity);
        }
        else if (cardData.IsDefenseCard())
        {
            glowColor = new Color(0.3f, 0.5f, 0.8f, intensity);
        }
        else if (cardData.EnergyGain > 0 || cardData.DrawCount > 0)
        {
            glowColor = new Color(0.6f, 0.3f, 0.8f, intensity);
        }

        StyleBoxFlat style = new StyleBoxFlat();
        style.BgColor = cardData.IsAttack ? new Color(0.2f, 0.1f, 0.1f) :
                        cardData.IsDefenseCard() ? new Color(0.1f, 0.15f, 0.2f) :
                        new Color(0.15f, 0.15f, 0.2f);
        style.BorderColor = cardData.IsAttack ? new Color(0.8f, 0.3f, 0.3f) :
                           cardData.IsDefenseCard() ? new Color(0.3f, 0.5f, 0.8f) :
                           new Color(0.6f, 0.3f, 0.8f);
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(5);
        style.SetShadowColor(glowColor);
        style.SetShadowSize((int)(8 * intensity));

        AddThemeStyleboxOverride("panel", style);
    }

    public Card GetCardData()
    {
        return cardData;
    }

    public int GetCardIndex()
    {
        return cardIndex;
    }

    public override void _Notification(int what)
    {
        if (what == (int)GodotObject.NotificationPredelete)
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (cardData != null)
        {
            cardData.OnValuesChanged -= OnCardValuesChanged;
            cardData.OnLevelChanged -= OnCardLevelChanged;
        }
    }
}
