using Godot;
using System.Collections.Generic;

namespace FishEatFish.Battle.CharacterSystem;

public partial class CharacterDefinition : Resource
{
    [Export]
    public string CharacterId;

    [Export]
    public string Name;

    [Export]
    public string NameEn;

    /// <summary>
    /// 立绘资源路径（如 "res://Assets/Portraits/char_rat.svg"）。
    /// 战斗场景左下角的角色立绘用；为空时战斗场景不显示立绘。
    /// </summary>
    [Export]
    public string Portrait;

    /// <summary>
    /// 取立绘资源路径：优先用数据里的 <see cref="Portrait"/>；
    /// 没有（硬编码工厂造出来的角色）就按 characterId 推导。
    /// </summary>
    public string GetPortraitPath()
    {
        if (!string.IsNullOrEmpty(Portrait)) return Portrait;
        if (!string.IsNullOrEmpty(CharacterId)) return $"res://Assets/Portraits/char_{CharacterId}.svg";
        return "";
    }

    [Export]
    public CharacterRace Race = CharacterRace.Chaos;

    [Export]
    public CharacterArchetype Archetype = CharacterArchetype.Striker;

    [Export]
    public int BaseHealth = 30;

    [Export]
    public int BaseEnergy = 2;

    [Export]
    public int BaseDrawCount = 2;

    [Export]
    public int BaseAttack = 5;

    [Export]
    public int BaseDefense = 3;

    [Export]
    public int BaseConstitution = 10;

    /// <summary>
    /// 暴击率（百分数，5 = 5%）。
    /// ⚠️ 用 float —— 参考图「详细属性」面板给的是 **14.6%** 这种小数（wiki 升格5 原值同为 14.6%），
    /// int 存不下会四舍五入成 15，跟参考图对不上。
    /// </summary>
    [Export]
    public float BaseCritRate = 5f;

    /// <summary>
    /// 暴击伤害（百分数，50 = 50%）。
    /// ⚠️ 用 float —— 与暴击率同口径：wiki 的属性可能是小数，int 存不下会四舍五入丢精度。
    /// （当前 4 个可玩角色的 wiki 原值都是整数 50，但 12 个星座 BOSS 是 150~185。）
    /// </summary>
    [Export]
    public float BaseCritDamage = 150f;

    /// <summary>
    /// 死亡抵抗 %。
    /// ⚠️ 用 float —— wiki 珊的升格5 原值是 **61.6**（参考图珈伦 36.4% 同理），
    /// int 存不下会四舍五入成 62，属性面板就跟 wiki 对不上。
    /// </summary>
    [Export]
    public float BaseDeathResistance = 0f;

    /// <summary>
    /// 银钥充能等级。
    /// ⚠️ 用 float —— wiki 泰旖丝的原值是 **29.4**，int 存不下会截断成 29，
    /// 「详细属性」面板就跟 wiki 对不上了（面板按**小数点后一位**显示）。
    /// </summary>
    [Export]
    public float BaseSilverKeyCharge = 15f;

    /// <summary>
    /// 狂气回充等级（消耗狂气后按 <c>floor(消耗量 × 等级 / 10)</c> 回冲）。
    /// ⚠️ 用 float —— wiki 升格5（60 级 0 命）的原值是 **5.6**（珈伦），int 存不下。
    ///
    /// ⚠️ 默认值 = **0**（wiki 的通用值）。以前这里是 2f —— 那不是 wiki 值，
    /// 而是老代码遗留，导致**没显式赋值这个字段的角色都吃到 2**
    /// （珊就是：面板显示「狂气回充等级 2.0」，而 wiki 是 **0**）。
    /// </summary>
    [Export]
    public float BaseRageReturn = 0f;

    [Export]
    public int BaseMaxRage = 100;

    /// <summary>
    /// 界域精通（参考图「详细属性」面板）。
    /// ⚠️ 用 float —— 与其它副属性同口径，wiki 原值可能是小数，int 会丢精度。
    /// </summary>
    [Export]
    public float BaseDomainMastery = 0f;

    /// <summary>伤害强效 %（参考图「详细属性」面板）</summary>
    [Export]
    public float BaseDamageBonus = 0f;

    /// <summary>黑印掉落 %（参考图「详细属性」面板）</summary>
    [Export]
    public float BaseBlackMarkDrop = 0f;

    [Export]
    public string AttackCardId;

    [Export]
    public string DefenseCardId;

    [Export]
    public string[] SpecialCardIds = new string[3];

    [Export]
    public string UltimateSkillId;

    public CharacterAttributes GetAttributes()
    {
        var attributes = new CharacterAttributes();
        attributes.Attack = BaseAttack;
        attributes.Defense = BaseDefense;
        attributes.Constitution = BaseConstitution;
        attributes.CritRate = BaseCritRate;
        attributes.CritDamage = BaseCritDamage;
        attributes.DeathResistance = BaseDeathResistance;
        attributes.SilverKeyCharge = BaseSilverKeyCharge;
        attributes.RageReturn = BaseRageReturn;
        attributes.MaxRage = BaseMaxRage;
        attributes.Race = Race;
        return attributes;
    }

    public void ApplyRaceDefaults()
    {
        var raceConfig = RaceAttributeConfig.GetConfig(Race);
    }

    /// <summary>
    /// 波吕克斯（双子座）—— **可玩角色**。
    /// 属性照参考游戏「详细属性」面板：攻击 182 / 防御 156 / 体质 162 / 暴击率 14.6% / 暴击伤害 50%。
    /// 卡组：打击 + 防御 + 3 张专属（净世之路 / 明灯灼魂 / 离垢的审判）；
    /// 狂气爆发「无罪的圣裁」：全体敌人受 60 伤害。
    /// </summary>
    public static CharacterDefinition CreatePollux()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "pollux",
            Name = "波吕克斯",
            // 界域=超维（wiki 角色页「|界域=超维」）、唤醒体类型=伤害型
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 182,
            BaseDefense = 156,
            BaseConstitution = 162,
            // 暴击率 14.6%：参考图「详细属性」面板 + wiki 升格5 原值，两边一致
            BaseCritRate = 14.6f,
            BaseCritDamage = 50,
            BaseRageReturn = 0,
            BaseSilverKeyCharge = 15f,
            BaseDeathResistance = 0,
            BaseDamageBonus = 4.8f,
            AttackCardId = "pollux_attack",
            DefenseCardId = "pollux_defense",
            SpecialCardIds = new string[] { "pollux_special", "pollux_special2", "pollux_special3" },
            UltimateSkillId = "pollux_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    /// <summary>
    /// 珈伦 —— **可玩角色**（参考游戏《终末前夜 Morimens》，2026-10-25 主人提供截图）。
    ///
    /// 属性口径（主人 2026-11-01 拍板）：**统一用 wiki 的「60 级 0 命」数据**，
    /// 即「通用值 + wiki 升格5 那 2 条覆盖」（见 <c>tools/wiki-character.ps1</c>）：
    /// 攻击 96 / 防御 108 / 体质 112（wiki 60 级行）/
    /// 暴击率 5% / 暴击伤害 50% / 界域精通 0 / 伤害强效 0 / 基础狂气 100 /
    /// **狂气回充等级 5.6**（wiki 升格5）/ 银钥充能等级 15 /
    /// **死亡抵抗 28**（wiki 升格5）；定位**辅助型**。
    ///
    /// ⚠️ 参考图面板给的是 **10.4 / 36.4**（那是**含额外 10 命**的值）——
    /// 主人明说「先不要管命座的值，只使用 60 级 0 命的数据」，所以这里用 wiki 的裸值。
    ///
    /// 卡组：打击 + 防御 + 凸月之舞（灵知觉醒）/ 蟾蜍炖菜 / 静默守望；
    /// 狂气爆发「客官请慢用！」（狂气爆发）。
    /// </summary>
    public static CharacterDefinition CreateJialun()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "jialun",
            Name = "珈伦",
            NameEn = "Jialun",
            // 界域=混沌（wiki 角色页「|界域=混沌」）
            Race = CharacterRace.Chaos,
            // 辅助型（参考图「人物列表」右侧的类型标签）
            Archetype = CharacterArchetype.Debuffer,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 96,
            BaseDefense = 108,
            BaseConstitution = 112,
            BaseCritRate = 5,
            BaseCritDamage = 50,
            BaseDeathResistance = 28,
            BaseRageReturn = 5.6f,
            BaseSilverKeyCharge = 15f,
            BaseMaxRage = 100,
            AttackCardId = "jialun_attack",
            DefenseCardId = "jialun_defense",
            SpecialCardIds = new string[] { "jialun_special", "jialun_special2", "jialun_special3" },
            UltimateSkillId = "jialun_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    /// <summary>
    /// 珊（Sanga）—— **可玩角色**（灰机 wiki 角色页「珊」，2026-11-01 主人指定重现）。
    ///
    /// 属性口径（与珈伦一致）：**统一用 wiki 的「60 级 0 命」数据**。
    /// - 本体 3 项 = wiki 60 级行：体质 128 / 攻击 100 / 防御 152
    /// - 其余 9 项 = 通用值 + wiki 升格5(=60 级时) 那 2 条覆盖：
    ///   黑印掉落 12.3% / 死亡抵抗 61.6%（通用值其余 7 项：暴击率5 / 暴击伤害50 /
    ///   界域精通0 / 伤害强效0 / 基础狂气100 / 狂气回充0 / 银钥充能15）
    ///
    /// ⚠️ wiki 上**没有**生命 / 能量 / 抽牌三项 → 暂用 wiki 系角色默认值 30 / 2 / 2
    /// （与波吕克斯、珈伦一致），等主人补参考图再调。
    ///
    /// 定位**防御型**（wiki「唤醒体类型=防御型」）→ Tank → 三分类显示「防御型」。
    /// 界域**深海**（wiki「|界域=深海」）→ Race = Abyss。
    ///
    /// 卡组：打击 + 防御 + 忘我之境（灵知觉醒）/ 美的怜悯（技能1右）/ 封闭创作（技能2左）；
    /// 狂气爆发「失落的艺术」。
    ///
    /// ⚠️ **专属卡顺序 = 显示顺序**：打击(1) / 防御(2) / **灵知觉醒卡(3)** / 技能卡1(4) / 技能卡2(5)。
    /// 参考游戏《终末前夜》的「技能」页就是**灵知觉醒卡永远排在 5 张的正中间**（珈伦参考图：
    /// 打击 / 防御 / 凸月之舞(灵知觉醒) / 蟾蜍炖菜 / 静默守望）→ 所以 `specialCardIds[0]`
    /// **恒为灵知觉醒卡**。`tools/check-card-data.ps1` 有规则守着这条。
    /// </summary>
    public static CharacterDefinition CreateSanga()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "sanga",
            Name = "珊",
            NameEn = "Sanga",
            // 界域=深海（wiki 角色页「|界域=深海」）
            Race = CharacterRace.Abyss,
            // 防御型（wiki「唤醒体类型=防御型」）
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 100,
            BaseDefense = 152,
            BaseConstitution = 128,
            BaseCritRate = 5,
            BaseCritDamage = 50,
            // 狂气回充等级 0（wiki 通用值）—— 显式写，别吃默认值
            BaseRageReturn = 0f,
            BaseBlackMarkDrop = 12.3f,
            // 死亡抵抗 61.6%（wiki 升格5 原值）—— ⚠️ 以前这里写的是 62（int 字段四舍五入的），
            // 与 wiki / 参考图的小数口径对不上。
            BaseDeathResistance = 61.6f,
            BaseSilverKeyCharge = 15f,
            BaseMaxRage = 100,
            AttackCardId = "sanga_attack",
            DefenseCardId = "sanga_defense",
            // ⚠️ 顺序即显示顺序：[0] = 灵知觉醒卡（永远排 5 张的正中间）
            SpecialCardIds = new string[] { "sanga_special3", "sanga_special", "sanga_special2" },
            UltimateSkillId = "sanga_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    /// <summary>
    /// 泰旖丝（Thais）—— **可玩角色**（灰机 wiki 角色页「泰旖丝」，2026-11-01 主人指定重现）。
    ///
    /// 属性口径（与珈伦一致）：**统一用 wiki 的「60 级 0 命」数据**。
    /// - 本体 3 项 = wiki 60 级行：体质 140 / 攻击 135 / 防御 126
    ///   （⚠️ 泰旖丝有「内在灵格限定」体质16 / 攻击15 / 防御14 —— 按主人 2026-11-01 拍板
    ///   **只取 wiki 60 级 0 命裸值**，内在灵格不叠加）
    /// - 其余 9 项 = 通用值 + wiki 升格5(=60 级时) 那 2 条覆盖：
    ///   狂气回充等级 2.4 / 银钥充能等级 29.4（通用值其余 7 项：暴击率5 / 暴击伤害50 /
    ///   界域精通0 / 伤害强效0 / 基础狂气100 / 黑印掉落0 / 死亡抵抗0）
    ///
    /// ⚠️ wiki 上**没有**生命 / 能量 / 抽牌三项 → 暂用 wiki 系角色默认值 30 / 2 / 2。
    ///
    /// 定位**辅助型**（wiki「唤醒体类型=辅助型」）→ Healer → 三分类显示「辅助型」。
    /// 界域**血肉**（wiki「|界域=血肉」）→ Race = Flesh。
    ///
    /// 卡组：打击 + 防御 + 目见母亲（灵知觉醒）/ 古老的爱抚（技能1右）/ 圣血的本能（技能2左）
    /// + 衍生卡「圣洁之子」；狂气爆发「丰穰之仪」。
    ///
    /// ⚠️ **专属卡顺序 = 显示顺序**：打击(1) / 防御(2) / **灵知觉醒卡(3)** / 技能卡1(4) / 技能卡2(5)。
    /// 参考游戏《终末前夜》的「技能」页就是**灵知觉醒卡永远排在 5 张的正中间**（珈伦参考图：
    /// 打击 / 防御 / 凸月之舞(灵知觉醒) / 蟾蜍炖菜 / 静默守望）→ 所以 `specialCardIds[0]`
    /// **恒为灵知觉醒卡**。`tools/check-card-data.ps1` 有规则守着这条。
    /// </summary>
    public static CharacterDefinition CreateThais()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "thais",
            Name = "泰旖丝",
            NameEn = "Thais",
            // 界域=血肉（wiki 角色页「|界域=血肉」）
            Race = CharacterRace.Flesh,
            // 辅助型（wiki「唤醒体类型=辅助型」）
            Archetype = CharacterArchetype.Healer,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 135,
            BaseDefense = 126,
            BaseConstitution = 140,
            BaseCritRate = 5,
            BaseCritDamage = 50,
            BaseRageReturn = 2.4f,
            // 银钥充能等级 29.4（wiki 原值，小数）→ 面板显示 29.4
            BaseSilverKeyCharge = 29.4f,
            BaseMaxRage = 100,
            AttackCardId = "thais_attack",
            DefenseCardId = "thais_defense",
            // ⚠️ 顺序即显示顺序：[0] = 灵知觉醒卡（永远排 5 张的正中间）
            SpecialCardIds = new string[] { "thais_special3", "thais_special", "thais_special2" },
            UltimateSkillId = "thais_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    /// <summary>
    /// **菲茵特**（wiki 角色页，2026-11-07 抓取）—— 界域血肉 / 防御型。
    ///
    /// 属性口径（与珊 / 泰旖丝一致）：**统一用 wiki 的「60 级 0 命」数据**。
    /// - 本体 3 项 = wiki 60 级行：体质 104 / 攻击 80 / 防御 124
    ///   （⚠️ 菲茵特有「内在灵格限定」体质13 / 攻击10 / 防御16 —— 按主人 2026-11-01 拍板
    ///   **只取 wiki 60 级 0 命裸值**，内在灵格不叠加）
    /// - 其余 9 项 = 通用值 + wiki 升格5(=60 级时) 那 2 条覆盖：
    ///   黑印掉落 3.6 / 死亡抵抗 33.6（通用值其余 7 项：暴击率5 / 暴击伤害50 /
    ///   界域精通0 / 伤害强效0 / 基础狂气100 / 狂气回充等级0 / 银钥充能等级15）
    ///
    /// ⚠️ wiki 上**没有**生命 / 能量 / 抽牌三项 → 暂用 wiki 系角色默认值 30 / 2 / 2。
    ///
    /// 定位**防御型**（wiki「唤醒体类型=防御型」）→ Tank → 三分类显示「防御型」。
    /// 界域**血肉**（wiki「|界域=血肉」）→ Race = Flesh（与泰旖丝同界域）。
    ///
    /// 卡组：打击 + 防御 + 无边星彩（灵知觉醒）/ 养分汲取（技能1右）/ 幼芽萌生（技能2左）；
    /// 狂气爆发「星之摇篮」。
    ///
    /// ⚠️ **专属卡顺序 = 显示顺序**：打击(1) / 防御(2) / **灵知觉醒卡(3)** / 技能卡1(4) / 技能卡2(5)。
    /// → 所以 `specialCardIds[0]` **恒为灵知觉醒卡**（`tools/check-card-data.ps1` 有规则守着）。
    /// </summary>
    public static CharacterDefinition CreateFaint()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "faint",
            Name = "菲茵特",
            NameEn = "Faint",
            // 界域=血肉（wiki 角色页「|界域=血肉」）
            Race = CharacterRace.Flesh,
            // 防御型（wiki「唤醒体类型=防御型」）
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 30,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 80,
            BaseDefense = 124,
            BaseConstitution = 104,
            BaseCritRate = 5,
            BaseCritDamage = 50,
            BaseRageReturn = 0f,
            BaseSilverKeyCharge = 15f,
            BaseMaxRage = 100,
            // 黑印掉落 3.6% / 死亡抵抗 33.6%（wiki 升格5 原值，小数）→ 面板显示 3.6 / 33.6
            BaseBlackMarkDrop = 3.6f,
            BaseDeathResistance = 33.6f,
            AttackCardId = "faint_attack",
            DefenseCardId = "faint_defense",
            // ⚠️ 顺序即显示顺序：[0] = 灵知觉醒卡（永远排 5 张的正中间）
            SpecialCardIds = new string[] { "faint_special3", "faint_special", "faint_special2" },
            UltimateSkillId = "faint_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    /// <summary>
    /// **可玩角色**清单（卡组齐全的那些）。
    ///
    /// 为什么单独列一份：<see cref="GetAllCharacters"/> 里还有 12 个星座 BOSS ——
    /// 它们**不使用卡牌**（BOSS 卡已删），卡组是空的，进「角色一览」只会显示成空壳。
    /// 判定口径：该角色的打击卡 / 防御卡在 <c>character_cards.json</c> 里都能查到。
    /// </summary>
    public static List<CharacterDefinition> GetPlayableCharacters()
    {
        List<CharacterDefinition> playable = new List<CharacterDefinition>();

        foreach (CharacterDefinition character in GetAllCharacters())
        {
            if (character == null) continue;
            if (string.IsNullOrEmpty(character.AttackCardId) || string.IsNullOrEmpty(character.DefenseCardId)) continue;
            if (!CharacterCardLoader.TryGetCharacterCard(character.AttackCardId, out _)) continue;
            if (!CharacterCardLoader.TryGetCharacterCard(character.DefenseCardId, out _)) continue;
            playable.Add(character);
        }

        return playable;
    }

    public static CharacterDefinition[] GetAllCharacters()
    {
        return new CharacterDefinition[]
        {
            CreatePollux(),
            CreateJialun(),
            CreateSanga(),
            CreateThais(),
            CreateFaint(),
            CreateMu(),
            CreateAldebaran(),
            CreateSaga(),
            CreateDeathmask(),
            CreateAiolia(),
            CreateShaka(),
            CreateDochiki(),
            CreateMilo(),
            CreateAiolos(),
            CreateShura(),
            CreateCamus(),
            CreateAphrodite()
        };
    }

    public static CharacterDefinition CreateMu()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "mu",
            Name = "牡羊座·穆",
            NameEn = "Mu",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 50,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 8,
            BaseDefense = 6,
            BaseConstitution = 14,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "mu_attack",
            DefenseCardId = "mu_defense",
            SpecialCardIds = new string[] { "mu_special" },
            UltimateSkillId = "mu_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateAldebaran()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "aldebaran",
            Name = "金牛座·阿鲁迪巴",
            NameEn = "Aldebaran",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 55,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 10,
            BaseDefense = 7,
            BaseConstitution = 15,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "aldebaran_attack",
            DefenseCardId = "aldebaran_defense",
            SpecialCardIds = new string[] { "aldebaran_special" },
            UltimateSkillId = "aldebaran_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateSaga()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "saga",
            Name = "双子座·撒加",
            NameEn = "Saga",
            Race = CharacterRace.Chaos,
            Archetype = CharacterArchetype.Mage,
            BaseHealth = 45,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 9,
            BaseDefense = 4,
            BaseConstitution = 12,
            BaseCritRate = 8,
            BaseCritDamage = 165,
            AttackCardId = "saga_attack",
            DefenseCardId = "saga_defense",
            SpecialCardIds = new string[] { "saga_special" },
            UltimateSkillId = "saga_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateDeathmask()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "deathmask",
            Name = "巨蟹座·迪斯马斯克",
            NameEn = "Deathmask",
            Race = CharacterRace.Abyss,
            Archetype = CharacterArchetype.Debuffer,
            BaseHealth = 35,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 3,
            BaseConstitution = 10,
            BaseCritRate = 10,
            BaseCritDamage = 170,
            AttackCardId = "deathmask_attack",
            DefenseCardId = "deathmask_defense",
            SpecialCardIds = new string[] { "deathmask_special" },
            UltimateSkillId = "deathmask_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateAiolia()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "aiolia",
            Name = "狮子座·艾欧里亚",
            NameEn = "Aiolia",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 40,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 10,
            BaseDefense = 4,
            BaseConstitution = 11,
            BaseCritRate = 12,
            BaseCritDamage = 180,
            AttackCardId = "aiolia_attack",
            DefenseCardId = "aiolia_defense",
            SpecialCardIds = new string[] { "aiolia_special" },
            UltimateSkillId = "aiolia_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateShaka()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "shaka",
            Name = "处女座·沙加",
            NameEn = "Shaka",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Healer,
            BaseHealth = 40,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 6,
            BaseDefense = 5,
            BaseConstitution = 12,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "shaka_attack",
            DefenseCardId = "shaka_defense",
            SpecialCardIds = new string[] { "shaka_special" },
            UltimateSkillId = "shaka_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateDochiki()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "dochiki",
            Name = "天秤座·童虎",
            NameEn = "Dochiki",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 55,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 9,
            BaseDefense = 7,
            BaseConstitution = 15,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "dochiki_attack",
            DefenseCardId = "dochiki_defense",
            SpecialCardIds = new string[] { "dochiki_special" },
            UltimateSkillId = "dochiki_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateMilo()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "milo",
            Name = "天蝎座·米罗",
            NameEn = "Milo",
            Race = CharacterRace.Abyss,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 35,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 9,
            BaseDefense = 3,
            BaseConstitution = 10,
            BaseCritRate = 15,
            BaseCritDamage = 185,
            AttackCardId = "milo_attack",
            DefenseCardId = "milo_defense",
            SpecialCardIds = new string[] { "milo_special" },
            UltimateSkillId = "milo_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateAiolos()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "aiolos",
            Name = "射手座·艾俄洛斯",
            NameEn = "Aiolos",
            Race = CharacterRace.Hyperdimension,
            Archetype = CharacterArchetype.Striker,
            BaseHealth = 40,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 10,
            BaseDefense = 4,
            BaseConstitution = 11,
            BaseCritRate = 12,
            BaseCritDamage = 180,
            AttackCardId = "aiolos_attack",
            DefenseCardId = "aiolos_defense",
            SpecialCardIds = new string[] { "aiolos_special" },
            UltimateSkillId = "aiolos_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateShura()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "shura",
            Name = "摩羯座·修罗",
            NameEn = "Shura",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Tank,
            BaseHealth = 50,
            BaseEnergy = 2,
            BaseDrawCount = 1,
            BaseAttack = 10,
            BaseDefense = 6,
            BaseConstitution = 14,
            BaseCritRate = 5,
            BaseCritDamage = 150,
            AttackCardId = "shura_attack",
            DefenseCardId = "shura_defense",
            SpecialCardIds = new string[] { "shura_special" },
            UltimateSkillId = "shura_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateCamus()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "camus",
            Name = "水瓶座·卡妙",
            NameEn = "Camus",
            Race = CharacterRace.Abyss,
            Archetype = CharacterArchetype.Mage,
            BaseHealth = 45,
            BaseEnergy = 3,
            BaseDrawCount = 2,
            BaseAttack = 8,
            BaseDefense = 5,
            BaseConstitution = 12,
            BaseCritRate = 8,
            BaseCritDamage = 165,
            AttackCardId = "camus_attack",
            DefenseCardId = "camus_defense",
            SpecialCardIds = new string[] { "camus_special" },
            UltimateSkillId = "camus_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }

    public static CharacterDefinition CreateAphrodite()
    {
        var charDef = new CharacterDefinition
        {
            CharacterId = "aphrodite",
            Name = "双鱼座·阿布罗狄",
            NameEn = "Aphrodite",
            Race = CharacterRace.Flesh,
            Archetype = CharacterArchetype.Poison,
            BaseHealth = 35,
            BaseEnergy = 2,
            BaseDrawCount = 2,
            BaseAttack = 7,
            BaseDefense = 3,
            BaseConstitution = 10,
            BaseCritRate = 10,
            BaseCritDamage = 175,
            AttackCardId = "aphrodite_attack",
            DefenseCardId = "aphrodite_defense",
            SpecialCardIds = new string[] { "aphrodite_special" },
            UltimateSkillId = "aphrodite_ultimate"
        };
        charDef.ApplyRaceDefaults();
        return charDef;
    }
}
