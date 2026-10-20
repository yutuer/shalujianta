using Godot;
using FishEatFish.Battle.Effects.Buffs;

namespace FishEatFish.UI
{
    /// <summary>
    /// 状态（Buff / Debuff）图标工厂。
    ///
    /// 玩家侧（BuffDebuffDisplay）与敌人侧（EnemyUI）共用同一套渲染逻辑，
    /// 避免两边各写一份「图标 + 数值」代码导致表现不一致。
    /// 纯逻辑类：不挂节点、不需要 .tscn。
    ///
    /// 图标来源：StatusEffect.IconPath（由 EffectRegistry 按 buffId 配置）
    /// 无真实图标资源时退化成 26×26 纯色方块（蓝=增益 / 红=减益）
    /// </summary>
    public static class StatusIconFactory
    {
        /// <summary>图标边长（像素）</summary>
        public const int IconSize = 26;

        /// <summary>数值文字字号</summary>
        public const int ValueFontSize = 11;

        /// <summary>图标节点名（外部可按名查找）</summary>
        public const string IconNodeName = "Icon";

        /// <summary>数值文字节点名（外部可按名查找并刷新数值）</summary>
        public const string ValueNodeName = "ValueLabel";

        /// <summary>没有图标资源时的增益占位色（蓝）</summary>
        public static readonly Color BuffFallbackColor = new Color(0.4f, 0.7f, 1f);

        /// <summary>没有图标资源时的减益占位色（红）</summary>
        public static readonly Color DebuffFallbackColor = new Color(1f, 0.35f, 0.35f);

        /// <summary>增益数值文字色</summary>
        public static readonly Color BuffValueColor = new Color(0.4f, 0.85f, 1f);

        /// <summary>减益数值文字色</summary>
        public static readonly Color DebuffValueColor = new Color(1f, 0.5f, 0.5f);

        // 纯色方块贴图缓存（增益 / 减益各一张）
        private static ImageTexture _buffFallbackTexture;
        private static ImageTexture _debuffFallbackTexture;

        /// <summary>
        /// 生成一个「图标 + 数值」的状态图标控件。
        /// </summary>
        /// <param name="effect">状态效果；为 null 时返回 null（不抛异常）</param>
        /// <param name="isDebuff">true = 减益（红），false = 增益（蓝）</param>
        /// <returns>
        /// VBoxContainer（子节点：TextureRect 图标「Icon」+ Label 数值「ValueLabel」）；
        /// effect 为 null 时返回 null
        /// </returns>
        public static Control CreateStatusIcon(StatusEffect effect, bool isDebuff)
        {
            if (effect == null)
            {
                GD.PrintErr("[StatusIconFactory] CreateStatusIcon 收到空状态效果，已跳过");
                return null;
            }

            VBoxContainer container = new VBoxContainer();
            container.Alignment = VBoxContainer.AlignmentMode.Center;

            TextureRect icon = new TextureRect
            {
                Name = IconNodeName,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };

            string iconPath = effect.IconPath;
            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }
            else
            {
                // 没有真实图标资源时退化成纯色方块（蓝=增益 / 红=减益）
                icon.Texture = GetFallbackTexture(isDebuff);
            }

            container.AddChild(icon);

            Label valueLabel = new Label
            {
                Name = ValueNodeName,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            valueLabel.AddThemeColorOverride("font_color", isDebuff ? DebuffValueColor : BuffValueColor);
            valueLabel.AddThemeFontSizeOverride("font_size", ValueFontSize);
            valueLabel.Text = BuildValueText(effect, isDebuff);
            container.AddChild(valueLabel);

            return container;
        }

        /// <summary>
        /// 数值文本；层数 &gt; 1 时追加「 ×N」。
        /// 未知效果类型退化为剩余回合数，不会抛异常。
        /// </summary>
        /// <param name="effect">状态效果；为 null 时返回空字符串</param>
        /// <param name="isDebuff">true = 减益口径，false = 增益口径</param>
        public static string BuildValueText(StatusEffect effect, bool isDebuff)
        {
            if (effect == null)
            {
                return "";
            }

            string value = isDebuff ? GetDebuffValueText(effect) : GetBuffValueText(effect);
            if (effect.StackCount > 1)
            {
                value += $" ×{effect.StackCount}";
            }
            return value;
        }

        /// <summary>增益数值口径：攻击力 / 防御力 / 治疗 / 反伤，其余用剩余回合数</summary>
        private static string GetBuffValueText(StatusEffect effect)
        {
            if (effect is StrengthBuff sb) return $"+{sb.AttackBonus}";
            if (effect is DefenseBuff db) return $"+{db.DefenseBonus}";
            if (effect is RegenerationBuff rb) return $"+{rb.HealPerTurn}";
            if (effect is ThornsBuff tb) return $"+{tb.DamageReflect}";
            return $"{effect.RemainingDuration}";
        }

        /// <summary>减益数值口径：易伤为 +N（受到的伤害变多），其余减益为 -N，未知用剩余回合数</summary>
        private static string GetDebuffValueText(StatusEffect effect)
        {
            if (effect is WeakDebuff wd) return $"-{wd.AttackReduction}";
            if (effect is VulnerableDebuff vd) return $"+{vd.DamageIncrease}";
            if (effect is PoisonDebuff pd) return $"-{pd.DamagePerTurn}";
            if (effect is SlowDebuff sd) return $"-{sd.EnergyReduction}";
            return $"{effect.RemainingDuration}";
        }

        /// <summary>取纯色方块贴图（带缓存，避免每次刷新都新建贴图）</summary>
        private static ImageTexture GetFallbackTexture(bool isDebuff)
        {
            if (isDebuff)
            {
                if (_debuffFallbackTexture == null || !GodotObject.IsInstanceValid(_debuffFallbackTexture))
                {
                    _debuffFallbackTexture = CreateSolidTexture(DebuffFallbackColor);
                }
                return _debuffFallbackTexture;
            }

            if (_buffFallbackTexture == null || !GodotObject.IsInstanceValid(_buffFallbackTexture))
            {
                _buffFallbackTexture = CreateSolidTexture(BuffFallbackColor);
            }
            return _buffFallbackTexture;
        }

        /// <summary>生成一张纯色贴图（IconSize × IconSize）</summary>
        private static ImageTexture CreateSolidTexture(Color color)
        {
            Image image = Image.CreateEmpty(IconSize, IconSize, false, Image.Format.Rgba8);
            image.Fill(color);
            return ImageTexture.CreateFromImage(image);
        }
    }
}
