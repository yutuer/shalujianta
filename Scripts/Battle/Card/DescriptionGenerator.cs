using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FishEatFish.Battle.CharacterSystem;

namespace FishEatFish.Battle.Card;

public static class DescriptionGenerator
{
    public static string GenerateCardDescription(Card card, CharacterAttributes attributes = null)
    {
        if (attributes == null)
        {
            attributes = card.LinkedAttributes;
        }

        var parts = new List<string>();

        if (card.IsAttack && card.GetFinalDamage(attributes) > 0)
        {
            int damage = card.GetFinalDamage(attributes);
            // 命中次数：「造成 x 点伤害 2 次」（参考游戏：离垢的审判）
            string hitText = card.HitCount > 1 ? $" {card.HitCount} 次" : "";
            parts.Add($"造成 {damage} 点伤害{hitText}");
        }

        if (card.GetFinalShield(attributes) > 0)
        {
            int shield = card.GetFinalShield(attributes);
            parts.Add($"获得 {shield} 点护盾");
        }

        if (card.HealAmount > 0)
        {
            int heal = attributes != null
                ? card.CalculatedHeal
                : card.HealAmount;
            parts.Add($"回复 {heal} 点生命");
        }

        if (card.EnergyGain > 0 || card.EnergyGainBase > 0)
        {
            int energy = attributes != null
                ? card.CalculatedEnergyGain
                : (card.EnergyGain > 0 ? card.EnergyGain : card.EnergyGainBase);
            parts.Add($"获得 {energy} 点能量");
        }

        if (card.DrawCount > 0 || card.DrawCountBase > 0)
        {
            int draw = attributes != null
                ? card.CalculatedDrawCount
                : (card.DrawCount > 0 ? card.DrawCount : card.DrawCountBase);
            // 抽牌方式：普通随机抽 / 抽「算力消耗最低的指令卡」（参考游戏：明灯灼魂）
            parts.Add(card.DrawFilter == CardDrawFilter.LowestCostInstruction
                ? $"抽 {draw} 张算力消耗最低的指令卡"
                : $"抽 {draw} 张牌");
        }

        if (!string.IsNullOrEmpty(card.ApplyBuffName) && card.ApplyBuffDuration > 0)
        {
            int buffValue = attributes != null ? card.CalculatedBuffValue : card.BuffValueBase;
            string buffName = GetBuffDisplayName(card.ApplyBuffName);
            // 措辞按**实际作用对象**：卡牌增益目前落在敌人身上（见 BattleManager.ApplyCardEffects），
            // 所以写「目标获得」而不是「获得」——卡面不能声称给自己加 buffs。
            parts.Add($"目标获得 {buffName} +{buffValue}，持续 {card.ApplyBuffDuration} 回合");
        }

        if (!string.IsNullOrEmpty(card.ApplyDebuffName) && card.ApplyDebuffDuration > 0)
        {
            int debuffValue = attributes != null ? card.CalculatedDebuffValue : card.DebuffValueBase;
            string debuffName = GetDebuffDisplayName(card.ApplyDebuffName);
            parts.Add($"目标获得 {debuffName} -{debuffValue}，持续 {card.ApplyDebuffDuration} 回合");
        }

        // 怒气：按战斗实际发放的口径（RageManager）显示，别用卡牌等级系统那套 5+perLevel
        int cardRage = RageManager.GetRageForCard(card);
        if (cardRage > 0)
        {
            parts.Add($"获得 {cardRage} 怒气");
        }

        // ⚠️ 「保留」**不在这里单独加** —— 词缀标注统一由
        //    <see cref="BuildAffixMarkText"/> 追加（写成「⚠保留。」，与参考图一致）。
        //    两处都写会重复（"…，保留⚠保留。"）。

        if (parts.Count == 0)
        {
            return card.Description;
        }

        return string.Join("，", parts);
    }

    /// <summary>
    /// **卡面词缀标注**（照参考游戏《终末前夜 Morimens》的卡面）。
    ///
    /// 参考图里卡面正文**带词缀标记**（「⚠保留。」「⚠消耗。」「✕罪印」「⚠灵知觉醒」），
    /// 同时右侧还有一块「词缀说明」—— **两处都有**，不是二选一
    /// （见 docs/screenshots/人物_波吕克斯/卡牌3(灵知觉醒)_波吕克斯.jpg）。
    ///
    /// 规则：**正文里已经出现过的词缀名就不再重复标**
    /// （例如「×罪印」写在句中，末尾就不必再标一次），
    /// 没出现过的才补在末尾。
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <param name="bodyText">正文（用来判断某个词缀是不是已经写过了）</param>
    public static string BuildAffixMarkText(Card card, string bodyText = null)
    {
        if (card == null) return "";

        string body = bodyText ?? "";
        var marks = new List<string>();

        // 按卡配置的「正文要标哪些词缀」过滤：
        //   bodyAffixMarks == null（数据表没写这个字段）= 全部都标
        //   bodyAffixMarks == []（写了空数组）        = 一个都不标（正文由策划文案逐字负责）
        System.Func<string, bool> allowed = id =>
            card.BodyAffixMarks == null || card.BodyAffixMarks.Contains(id);

        // ① 通用词缀（保留 / 消耗 / 回合唯一）—— isRetain 也算保留（老数据没配 affixes）
        if ((card.IsRetain || card.Affixes.Contains(CardAffix.Retain)) &&
            allowed(CardAffix.Retain) && !body.Contains("保留"))
        {
            marks.Add("⚠保留。");
        }
        if (card.Affixes.Contains(CardAffix.Consume) &&
            allowed(CardAffix.Consume) && !body.Contains("消耗"))
        {
            marks.Add("⚠消耗。");
        }
        if (card.Affixes.Contains(CardAffix.UniquePerTurn) &&
            allowed(CardAffix.UniquePerTurn) && !body.Contains("回合唯一"))
        {
            marks.Add("回合唯一。");
        }

        // ② 卡牌特有的词缀（预备1 / 罪印…）—— 符号跟「词缀说明」区保持一致
        if (card.AffixNotes != null)
        {
            foreach (CardAffixNote note in card.AffixNotes)
            {
                if (note == null) continue;
                string name = note.Name;
                if (string.IsNullOrEmpty(name)) continue;
                if (body.Contains(name)) continue;   // 正文里已经写过 → 不重复标
                if (!allowed(note.Id)) continue;      // 这张卡不自动标这个词缀
                AffixInfo info = AffixLibrary.Get(note.Id);
                string symbol = info?.Symbol ?? "⚠";
                marks.Add($"{symbol}{name}。");
            }
        }

        return string.Join("", marks);
    }

    /// <summary>
    /// **卡面正文（含词缀标注）** —— 所有显示卡面正文的地方统一走这里，
    /// 免得有的地方带标注、有的地方不带。
    ///
    /// 正文来源优先级：传入的 bodyText → 数据表的 description（策划文案）→ 按数值自动生成。
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <param name="bodyText">正文（可空）</param>
    /// <param name="attributes">角色属性</param>
    public static string BuildCardBodyText(Card card, string bodyText = null, CharacterAttributes attributes = null)
    {
        if (card == null) return "";

        string body = bodyText;
        if (string.IsNullOrEmpty(body))
        {
            body = !string.IsNullOrEmpty(card.TemplateDescription)
                ? card.TemplateDescription
                : GenerateCardDescription(card, attributes);
        }

        // 末尾补个句号 —— 词缀标注是「⚠xxx。」这种短句，接上去更顺
        if (!string.IsNullOrEmpty(body) &&
            !body.EndsWith("。") && !body.EndsWith("！") && !body.EndsWith("？"))
        {
            body += "。";
        }

        // 文案里的数值占位符（{heal} / {damage} / {shield}）→ 换成**按当前等级算出的真值**。
        // 为什么需要：策划写的正文里带数字（「回复 17 点生命」）会**跟等级脱钩** ——
        // 卡牌总览显示满级、战斗里是 1 级，写死的数字两边对不上。
        // 用占位符后，卡面与战斗永远同源（同一条规则算出来的）。
        body = ReplaceValuePlaceholders(body, card, attributes);

        return body + BuildAffixMarkText(card, body);
    }

    /// <summary>把正文里的 <c>{heal}</c> / <c>{damage}</c> / <c>{shield}</c> 占位符换成实际数值</summary>
    private static string ReplaceValuePlaceholders(string body, Card card, CharacterAttributes attributes)
    {
        if (string.IsNullOrEmpty(body) || !body.Contains("{")) return body;

        // ⚠️ 和 GenerateCardDescription 一样：**没传 attributes 就回落到卡上挂的角色属性**。
        //    漏了这一步 → 卡面把「回复 X 点生命」算成 0（战斗里却是对的，两边不一致）。
        if (attributes == null && card != null)
        {
            attributes = card.LinkedAttributes;
        }

        if (body.Contains("{heal}"))
        {
            CardEffect heal = card.FindEffect(CardEffectType.Heal);
            int value = heal != null ? CardEffectCalculator.CalculateHeal(card, heal, attributes) : 0;
            body = body.Replace("{heal}", value.ToString());
        }
        if (body.Contains("{damage}"))
        {
            CardEffect dmg = card.FindEffect(CardEffectType.Damage);
            int value = dmg != null ? CardEffectCalculator.CalculateDamage(card, dmg, attributes) : 0;
            body = body.Replace("{damage}", value.ToString());
        }
        if (body.Contains("{shield}"))
        {
            CardEffect sh = card.FindEffect(CardEffectType.Shield);
            int value = sh != null ? CardEffectCalculator.CalculateShield(card, sh, attributes) : 0;
            body = body.Replace("{shield}", value.ToString());
        }
        if (body.Contains("{poison}"))
        {
            // 中毒层数 = 这张卡**第一个** poison 减益效果的计算值
            // （凸月之舞 = 固定 24 层；蟾蜍炖菜 = 攻击力×30%）
            int value = 0;
            foreach (CardEffect e in card.Effects)
            {
                if (e.Type == CardEffectType.Debuff && e.Debuff == "poison")
                {
                    value = CardEffectCalculator.CalculateDebuffValue(card, e, attributes);
                    break;
                }
            }
            body = body.Replace("{poison}", value.ToString());
        }
        return body;
    }

    /// <summary>
    /// **把卡面正文里出现的词缀按「词缀说明」区的颜色着色**（输出 BBCode）。
    ///
    /// 照参考游戏《终末前夜 Morimens》：卡面正文里的词缀**带颜色**——
    /// 「⚠保留」「⚠消耗」「⚠预备1」是橙色（#e8a34a），「✕罪印」是紫色（#c86bd8），
    /// 和右侧「词缀说明」区用的是**同一套颜色**（都取自 <c>Data/affixes.json</c>）。
    ///
    /// ⚠️ **只加颜色、不补符号** —— 参考图里「预备1」在正文里就是**没有符号**的
    /// （符号只在词缀说明区有），补符号会改掉文案。
    /// </summary>
    /// <param name="text">纯文本正文</param>
    /// <param name="card">卡牌（决定哪些词缀要着色）</param>
    /// <returns>带 <c>[color=…]</c> 标记的 BBCode 文本</returns>
    public static string ColorizeAffixesInBody(string text, Card card)
    {
        if (string.IsNullOrEmpty(text) || card == null) return text;

        // ① 这张卡带的词缀（通用词缀 + 卡牌特有词缀）—— 只给「这张卡真的有」的词缀上色
        var ids = new List<string>();
        foreach (string id in card.Affixes)
        {
            if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) ids.Add(id);
        }
        if (card.AffixNotes != null)
        {
            foreach (CardAffixNote note in card.AffixNotes)
            {
                if (note != null && !string.IsNullOrEmpty(note.Id) && !ids.Contains(note.Id)) ids.Add(note.Id);
            }
        }

        // ② 查词缀表拿「名称 / 符号 / 颜色」
        var byName = new Dictionary<string, AffixInfo>();
        foreach (string id in ids)
        {
            AffixInfo info = AffixLibrary.Get(id);
            if (info == null || string.IsNullOrEmpty(info.Name)) continue;
            if (byName.ContainsKey(info.Name)) continue;
            byName[info.Name] = info;
        }
        if (byName.Count == 0) return text;

        // ③ 一次扫描：把「符号? + 词缀名」整段包上颜色（名字长的先匹配，避免短的抢先）
        var names = new List<string>(byName.Keys);
        names.Sort((a, b) => b.Length.CompareTo(a.Length));
        string pattern = "(?:\u26A0|\u2715)?(?:" + string.Join("|", names.ConvertAll(Regex.Escape)) + ")";

        return Regex.Replace(text, pattern, match =>
        {
            string matched = match.Value;
            string name = matched;
            if (name.Length > 0 && (name[0] == '\u26A0' || name[0] == '\u2715')) name = name.Substring(1);
            if (!byName.TryGetValue(name, out AffixInfo info)) return matched;
            return $"[color={info.Color}]{matched}[/color]";
        });
    }

    /// <summary>
    /// **卡面正文（含词缀着色）** —— 给 <c>RichTextLabel</c> 用（BBCode）。
    /// 正文来源与 <see cref="BuildCardBodyText"/> 完全一致，只是多了一步词缀上色。
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <param name="bodyText">正文（可空）</param>
    /// <param name="attributes">角色属性</param>
    public static string BuildCardBodyBBCode(Card card, string bodyText = null, CharacterAttributes attributes = null)
    {
        return ColorizeAffixesInBody(BuildCardBodyText(card, bodyText, attributes), card);
    }

    /// <summary>
    /// **卡面正文（含词缀着色）· 按卡牌类型出文案那套** —— 卡牌总览 / 长按预览用。
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <param name="attributes">角色属性</param>
    public static string BuildCardFlavorBBCode(Card card, CharacterAttributes attributes = null)
    {
        return ColorizeAffixesInBody(GenerateCardFlavorText(card, attributes), card);
    }

    public static string GenerateUltimateDescription(UltimateSkill ultimate, CharacterAttributes attributes = null)
    {
        if (attributes == null)
        {
            attributes = ultimate.LinkedAttributes;
        }

        var parts = new List<string>();

        if (ultimate.Damage > 0)
        {
            int damage = attributes != null
                ? ultimate.CalculatedDamage
                : ultimate.Damage;
            parts.Add($"造成 {damage} 点伤害");
        }
        else if (ultimate.DamagePercent > 0 && attributes != null)
        {
            // 百分比口径（攻击力 × 百分比）—— 没有角色属性就不显示，别写「造成 0 点伤害」
            parts.Add($"造成 {ultimate.CalculatedDamage} 点伤害");
        }

        if (ultimate.Heal > 0 || ultimate.HealPercent > 0)
        {
            int heal = attributes != null
                ? ultimate.CalculatedHeal
                : ultimate.Heal;
            parts.Add($"回复 {heal} 点生命");
        }

        if (ultimate.Shield > 0)
        {
            int shield = attributes != null
                ? ultimate.CalculatedShield
                : ultimate.Shield;
            parts.Add($"获得 {shield} 点护盾");
        }

        if (ultimate.DrawCount > 0 || ultimate.DrawCountPerLevel > 0)
        {
            int draw = attributes != null
                ? ultimate.CalculatedDrawCount
                : ultimate.DrawCount;
            parts.Add($"抽 {draw} 张牌");
        }

        if (ultimate.BuffValue > 0 || ultimate.BuffValuePerLevel > 0)
        {
            int buffVal = attributes != null
                ? ultimate.CalculatedBuffValue
                : ultimate.BuffValue;
            if (ultimate.BuffDuration > 0)
            {
                parts.Add($"附加 {buffVal} 层效果，持续 {ultimate.BuffDuration} 回合");
            }
            else
            {
                parts.Add($"附加 {buffVal} 层效果");
            }
        }

        if (ultimate.BuffDuration == 1 && ultimate.Damage == 0 && ultimate.Heal == 0 && ultimate.Shield == 0)
        {
            parts.Add("目标下回合无法行动");
        }

        // 减益（中毒 / 虚弱…）：参考游戏「对所有敌人施加 47 层中毒」
        if (!string.IsNullOrEmpty(ultimate.ApplyDebuffName) && ultimate.DebuffValuePercent > 0 && attributes != null)
        {
            int layers = ultimate.CalculatedDebuffValue;
            parts.Add($"对所有敌人施加 {layers} 层{GetDebuffDisplayName(ultimate.ApplyDebuffName)}");
        }

        if (parts.Count == 0)
        {
            return ultimate.Description;
        }

        return string.Join("，", parts);
    }

    public static string GetBuffDisplayName(string buffName)
    {
        return buffName.ToLower() switch
        {
            "strength" => "攻击力",
            "defense" => "防御力",
            "regeneration" => "回复",
            "thorns" => "荆棘",
            "fury" => "狂暴",
            _ => buffName
        };
    }

    public static string GetDebuffDisplayName(string debuffName)
    {
        return debuffName.ToLower() switch
        {
            "weak" => "虚弱",
            "vulnerable" => "易伤",
            "poison" => "中毒",
            "slow" => "减速",
            "silence" => "沉默",
            _ => debuffName
        };
    }

    public static string GenerateLevelInfo(Card card)
    {
        if (card.Level >= card.MaxLevel)
        {
            return $"等级: {card.Level} (MAX)";
        }
        return $"等级: {card.Level}/{card.MaxLevel}";
    }

    /// <summary>
    /// **卡牌正文文案**（对照参考游戏《终末前夜 Morimens》卡牌总览里的卡面文字）。
    ///
    /// 格式按卡牌**类型**分三种（主人 2026-10-24 给的规格）：
    /// 指令（伤害型）：「造成 18 点伤害。波吕克斯获得 15 点怒气。」
    /// 指令（防御型）：「获得 16 点护盾。波吕克斯获得 10 点怒气。」
    /// 灵知觉醒：「波吕克斯获得 50 点怒气。{策划写的专属说明}」
    ///
    /// ⚠️ 这里的数值与**战斗结算同源**（都走 <c>GetFinalDamage</c> / <c>RageManager.GetRageForCard</c>），
    /// 不会出现「卡面写一套、实际给另一套」。
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <param name="attributes">角色属性（为空时取卡上挂的）</param>
    public static string GenerateCardFlavorText(Card card, CharacterAttributes attributes = null)
    {
        if (card == null) return "";
        if (attributes == null) attributes = card.LinkedAttributes;

        string owner = string.IsNullOrEmpty(card.OwnerName) ? "" : card.OwnerName + "获得";
        int rage = RageManager.GetRageForCard(card);
        var parts = new System.Collections.Generic.List<string>();

        if (card.CardType == CardType.Awakening)
        {
            // 灵知觉醒：先说怒气，再说策划写的专属说明
            if (rage > 0) parts.Add($"{owner} {rage} 点怒气。");
            if (!string.IsNullOrEmpty(card.TemplateDescription)) parts.Add(card.TemplateDescription);
        }
        else if (!string.IsNullOrEmpty(card.TemplateDescription))
        {
            // ⚠️ **策划写了正文就直接用**（2026-10-25 珈伦的卡牌）——
            //    「蟾蜍炖菜」这类「施加中毒 + 回复 + 虚弱」的复合效果没法用数值模板拼出来，
            //    自动生成的结果只剩「获得 X 点护盾」这种半截话。
            parts.Add(card.TemplateDescription);
        }
        else
        {
            int damage = card.GetFinalDamage(attributes);
            int shield = card.GetFinalShield(attributes);
            int draw = attributes != null ? card.CalculatedDrawCount : card.DrawCount;

            if (damage > 0)
            {
                string hitText = card.HitCount > 1 ? $" {card.HitCount} 次" : "";
                parts.Add(card.IsAreaAttack
                    ? $"对所有敌人造成 {damage} 点伤害{hitText}。"
                    : $"造成 {damage} 点伤害{hitText}。");
            }
            if (shield > 0) parts.Add($"获得 {shield} 点护盾。");
            if (draw > 0)
            {
                parts.Add(card.DrawFilter == CardDrawFilter.LowestCostInstruction
                    ? $"抽 {draw} 张算力消耗最低的指令卡。"
                    : $"抽 {draw} 张。");
            }
            if (rage > 0) parts.Add($"{owner} {rage} 点怒气。");
        }

        // 词缀标注（保留 / 消耗 / 回合唯一 + 特有词缀）——
        // 统一走 BuildAffixMarkText，正文里已经写过的词缀不重复标
        string body = string.Join("", parts);
        // 文案里的数值占位符（{heal} / {damage} / {shield} / {poison}）→ 换成实际数值
        body = ReplaceValuePlaceholders(body, card, attributes);
        return body + BuildAffixMarkText(card, body);
    }

    public static string GenerateUpgradePreview(Card card, CharacterAttributes attributes = null)
    {
        if (card.Level >= card.MaxLevel)
        {
            return "已达到最大等级";
        }

        if (attributes == null)
        {
            attributes = card.LinkedAttributes;
        }

        var preview = new StringBuilder();
        preview.AppendLine("升级后效果:");

        // 伤害：走百分比口径（角色攻击 × 百分比 × 等级系数）。
        // ⚠️ 不能只看 card.Damage > 0 —— 百分比化之后绝对值恒为 0，预览会整段空白。
        int currentDamage = card.GetFinalDamage(attributes, card.Level);
        int nextDamage = card.GetFinalDamage(attributes, card.Level + 1);
        if (currentDamage > 0 || nextDamage > 0)
        {
            preview.AppendLine($"  伤害: {currentDamage} → {nextDamage}");
        }
        else if (card.DamagePercent > 0)
        {
            preview.AppendLine($"  伤害: 角色攻击 × {card.DamagePercent}%");
        }

        int currentShield = card.GetFinalShield(attributes, card.Level);
        int nextShield = card.GetFinalShield(attributes, card.Level + 1);
        if (currentShield > 0 || nextShield > 0)
        {
            preview.AppendLine($"  护盾: {currentShield} → {nextShield}");
        }
        else if (card.ShieldPercent > 0)
        {
            preview.AppendLine($"  护盾: 角色防御 × {card.ShieldPercent}%");
        }

        if (card.HealAmount > 0)
        {
            int current = attributes != null ? card.CalculatedHeal : card.HealAmount;
            int next = CardLevelSystem.CalculateAttributeLinkedValue(
                card.Level + 1,
                card.HealAmount,
                card.HealBaseCoefficient);
            preview.AppendLine($"  治疗: {current} → {next}");
        }

        // 怒气：新口径 —— 唯一真相源 = effects 里的 rage 条目（value + perLevel × (等级 − 1)）
        CardEffect rageEffect = card.FindEffect(CardEffectType.Rage);
        if (rageEffect != null)
        {
            int current = CardLevelSystem.CalculateFixedValue(card.Level, rageEffect.Value, rageEffect.PerLevel);
            int next = CardLevelSystem.CalculateFixedValue(card.Level + 1, rageEffect.Value, rageEffect.PerLevel);
            preview.AppendLine($"  怒气: {current} → {next}");
        }

        return preview.ToString();
    }
}
