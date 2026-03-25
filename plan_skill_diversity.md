# 角色技能系统重构

## 一、目标
为12个十二生肖角色 + 12个黄金圣斗士设计符合其定位的**普通卡牌**和**大招**，并配置化。

---

## 二、卡牌数值系统（已实现）

### 2.1 核心设计理念
卡牌数值分为两大类，支持动态计算和实时更新：

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| **属性关联型** | 数值 = 角色属性 × 系数 | 伤害、护盾、治疗、反击 |
| **固定增加值型** | 数值 = 基础值 + (每级增加值 × (等级-1)) | 怒气、能量、抽牌数 |

### 2.2 数值计算公式

#### 属性关联型
```
系数 = 基础系数 × (1 + 20% × (等级 - 1))
数值 = 角色属性 × 系数
```

| 属性类型 | 关联计算 |
|----------|----------|
| 攻击力 | 伤害、反击 |
| 防御力 | 护盾 |
| 体质 | 治疗 |

#### 怒气固定增加值
```
怒气值 = 基础值(5) + (每级增加值 × (等级 - 1))
```

### 2.3 数值计算示例（角色攻击力=15）

| 等级 | 系数 | 伤害值(攻击×0.6×系数) | 怒气值 |
|------|------|----------------------|--------|
| 1 | 1.00 | 9 | 5 |
| 2 | 1.20 | 11 | 6 |
| 3 | 1.40 | 13 | 7 |
| 5 | 1.80 | 16 | 9 |
| 6 (MAX) | 2.00 | 18 | 10 |

### 2.4 卡牌配置字段

```csharp
// 基础数值（配置值）
[Export] public int Damage { get; set; } = 0;
[Export] public int ShieldGain { get; set; } = 0;
[Export] public int HealAmount { get; set; } = 0;

// 系数配置（决定升级成长）
[Export] public float DamageBaseCoefficient { get; set; } = 0.6f;  // 基础系数
[Export] public float ShieldBaseCoefficient { get; set; } = 0.6f;
[Export] public float HealBaseCoefficient { get; set; } = 0.6f;
[Export] public float CounterBaseCoefficient { get; set; } = 0.5f;

// 固定增长配置
[Export] public int RageGainPerLevel { get; set; } = 0;
[Export] public int EnergyGainPerLevel { get; set; } = 0;
[Export] public int DrawCountPerLevel { get; set; } = 0;
[Export] public int BuffValuePerLevel { get; set; } = 0;

// 等级系统
[Export] public int Level { get; set; } = 1;
[Export] public int MaxLevel { get; set; } = 6;

// 计算属性（实时更新）
public int CalculatedDamage => CardLevelSystem.CalculateAttributeLinkedValue(Level, Damage, DamageBaseCoefficient);
public int CalculatedShield => CardLevelSystem.CalculateAttributeLinkedValue(Level, ShieldGain, ShieldBaseCoefficient);
public int CalculatedRageGain => CardLevelSystem.CalculateRageGain(Level, RageGainPerLevel);
```

### 2.5 实时更新机制

```
角色属性变化 (Buff/Debuff)
        ↓
触发 OnValuesChanged 事件
        ↓
CardUI 订阅事件并刷新描述
        ↓
显示新的计算数值
```

```csharp
// 事件定义
public event System.Action<ICardLevelable> OnLevelChanged;
public event System.Action<Card> OnValuesChanged;

// UI 订阅
card.OnValuesChanged += (card) => {
    descLabel.Text = DescriptionGenerator.GenerateCardDescription(card, attributes);
};
```

### 2.6 动态描述生成

```csharp
// 根据当前等级和角色属性生成描述
string desc = DescriptionGenerator.GenerateCardDescription(card, attributes);
// 输出: "造成 12 点伤害，获得 10 怒气"

// 升级预览
string preview = DescriptionGenerator.GenerateUpgradePreview(card, attributes);
// 输出:
// 升级后效果:
//   伤害: 12 → 14
//   怒气: 5 → 6
```

### 2.7 升级系统接口

```csharp
public interface ICardLevelable
{
    int Level { get; set; }
    int MaxLevel { get; }
    CardUpgradeInfo UpgradeInfo { get; }
    void LevelUp();
    void SetLevel(int level);
    void AddExp(int exp);
    event System.Action<ICardLevelable> OnLevelChanged;
}

public class CardUpgradeInfo
{
    public int CurrentLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 6;
    public int UpgradeCost { get; set; } = 100;
    public int CurrentExp { get; set; } = 0;
    public int ExpToNextLevel { get; set; } = 100;

    public void AddExp(int exp) { ... }
    public static int CalculateExpForLevel(int level) => 100 * level * level;
    public static int CalculateUpgradeCost(int level) => 100 * level;
}
```

### 2.8 大招数值系统

与卡牌使用相同的计算系统：
- `DamageBaseCoefficient` - 伤害属性关联
- `HealBaseCoefficient` - 治疗属性关联
- `ShieldBaseCoefficient` - 护盾属性关联
- `DrawCountPerLevel` - 抽牌数固定增长
- `BuffValuePerLevel` - buff值固定增长

```csharp
// 计算属性
public int CalculatedDamage => CardLevelSystem.CalculateAttributeLinkedValue(Level, Damage, DamageBaseCoefficient);
public int CalculatedHeal => CardLevelSystem.CalculateAttributeLinkedValue(Level, Heal, HealBaseCoefficient);
public int CalculatedShield => CardLevelSystem.CalculateAttributeLinkedValue(Level, Shield, ShieldBaseCoefficient);
```

## 二、角色总览

### 2.1 十二生肖（原有）
| 角色 | 定位 | 种族 |
|------|------|------|
| 鼠 | 追击者 | 混沌 |
| 牛 | 坦克 | 血肉 |
| 虎 | 追击者 | 混沌 |
| 兔 | 追击者 | 超维 |
| 龙 | 法师 | 深海 |
| 蛇 | 毒师 | 深海 |
| 马 | 追击者 | 超维 |
| 羊 | 治疗者 | 血肉 |
| 猴 | 削弱者 | 混沌 |
| 鸡 | 追击者 | 超维 |
| 狗 | 坦克 | 血肉 |
| 猪 | 反击者 | 血肉 |

### 2.2 黄金圣斗士（新增）
| 角色 | 定位 | 种族 |
|------|------|------|
| 牡羊座·穆 | 坦克 | 混沌 |
| 金牛座·阿鲁迪巴 | 坦克 | 混沌 |
| 双子座·撒加 | 法师 | 混沌 |
| 巨蟹座·迪斯马斯克 | 削弱者 | 深海 |
| 狮子座·艾欧里亚 | 追击者 | 超维 |
| 处女座·沙加 | 治疗者 | 超维 |
| 天秤座·童虎 | 坦克 | 血肉 |
| 天蝎座·米罗 | 追击者 | 深海 |
| 射手座·艾俄洛斯 | 追击者 | 超维 |
| 摩羯座·修罗 | 坦克 | 血肉 |
| 水瓶座·卡妙 | 法师 | 深海 |
| 双鱼座·阿布罗狄 | 毒师 | 血肉 |

### 2.3 种族分布
| 种族 | 生肖 | 黄金圣斗士 |
|------|------|-----------|
| **混沌** | 鼠、虎、猴 | 穆、阿鲁迪巴、撒加 |
| **深海** | 龙、蛇 | 迪斯马斯克、米罗、卡妙 |
| **血肉** | 牛、羊、狗、猪 | 童虎、修罗、阿布罗狄 |
| **超维** | 兔、马、鸡 | 艾欧里亚、沙加、艾俄洛斯 |

## 三、黄金圣斗士技能设计

### 牡羊座·穆 (坦克/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 星光灭绝 | 2 | 造成15点伤害 |
| 防御 | 水晶墙 | 1 | 获得12点护盾，本回合无法被攻击 |
| 特殊 | 瞬间移动 | 1 | 获得8点护盾，+1能量 |
| 大招 | 星光灭绝冲击 | 100怒 | 对所有敌人造成40伤害 |

### 金牛座·阿鲁迪巴 (坦克/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 巨型号角 | 2 | 造成18点伤害 |
| 防御 | 黄金之壁 | 1 | 获得15点护盾 |
| 特殊 | 蛮力冲撞 | 2 | 造成10点伤害，附加2层虚弱 |
| 大招 | 泰坦新星 | 100怒 | 对最前方敌人造成60伤害 |

### 双子座·撒加 (法师/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 银河星爆 | 2 | 造成15点伤害 |
| 防御 | 精神封印 | 1 | 获得8点护盾，敌人下回合无法攻击 |
| 特殊 | 双重人格 | 1 | 造成6点伤害，+2能量 |
| 大招 | 罪恶降临 | 100怒 | 对所有敌人造成35伤害，附加2层虚弱 |

### 巨蟹座·迪斯马斯克 (削弱者/深海)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 积尸气冥苍波 | 2 | 造成12点伤害，附加2层虚弱 |
| 防御 | 死亡假面 | 1 | 获得8点护盾 |
| 特殊 | 积尸气魂葬波 | 2 | 附加3层虚弱到所有敌人 |
| 大招 | 死亡极限 | 100怒 | 所有敌人附加5层虚弱，-1能量 |

### 狮子座·艾欧里亚 (追击者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 闪电光速拳 | 1 | 造成8点伤害×2次 |
| 防御 | 闪电护盾 | 1 | 获得6点护盾 |
| 特殊 | 等离子光速拳 | 2 | 造成12点伤害×2次 |
| 大招 | 等离子闪电波 | 100怒 | 对所有敌人造成50伤害 |

### 处女座·沙加 (治疗者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 天舞宝轮 | 2 | 造成10点伤害，附加1层虚弱 |
| 防御 | 六道轮回 | 1 | 获得8点护盾，回复5点生命 |
| 特殊 | 天魔降伏 | 2 | 全体队友回复8点生命，获得5点护盾 |
| 大招 | 八感觉醒 | 100怒 | 全体队友回复20生命，清除所有debuff |

### 天秤座·童虎 (坦克/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 庐山百龙霸 | 2 | 造成15点伤害 |
| 防御 | 庐山真龙霸 | 1 | 获得15点护盾 |
| 特殊 | 亢龙有悔 | 1 | 获得10点护盾，本回合+2能量 |
| 大招 | 庐山龙飞翔 | 100怒 | 对所有敌人造成45伤害 |

### 天蝎座·米罗 (追击者/深海)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 猩红毒针 | 1 | 造成8点伤害，附加2层中毒 |
| 防御 | 深红毒针 | 1 | 获得5点护盾，附加1层中毒到敌人 |
| 特殊 | 安达里士 | 2 | 造成15点伤害，附加3层中毒 |
| 大招 | 猩红毒针·真红之星 | 100怒 | 对所有敌人造成25伤害，附加5层中毒 |

### 射手座·艾俄洛斯 (追击者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 圣衣流星拳 | 1 | 造成8点伤害×2次 |
| 防御 | 黄金之箭 | 1 | 获得6点护盾 |
| 特殊 | 射手光芒 | 1 | 造成6点伤害，+1能量 |
| 大招 | 黄金之箭·天马 | 100怒 | 对所有敌人造成40伤害，+3能量 |

### 摩羯座·修罗 (坦克/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 圣剑拔刃 | 2 | 造成16点伤害 |
| 防御 | 修罗功 | 1 | 获得12点护盾 |
| 特殊 | 圣剑乱舞 | 2 | 造成10点伤害×2次 |
| 大招 | 圣剑·最终奥义 | 100怒 | 对最前方敌人造成70伤害 |

### 水瓶座·卡妙 (法师/深海)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 曙光女神之宽恕 | 2 | 造成14点伤害，附加2层冰冻 |
| 防御 | 冰墙 | 1 | 获得10点护盾 |
| 特殊 | 冰晶之壁 | 1 | 获得8点护盾，附加1层冰冻到敌人 |
| 大招 | 极光处刑 | 100怒 | 对所有敌人造成30伤害，附加3层冰冻 |

### 双鱼座·阿布罗狄 (毒师/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 食人鱼玫瑰 | 1 | 造成6点伤害，附加2层中毒 |
| 防御 | 白玫瑰 | 1 | 获得6点护盾 |
| 特殊 | 血腥曼陀罗 | 2 | 附加4层中毒到所有敌人 |
| 大招 | 魔宫玫瑰 | 100怒 | 对所有敌人造成20伤害，附加6层中毒 |

## 四、十二生肖技能设计

### 鼠 (追击者/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 鼠牙乱咬 | 1 | 造成5点伤害×2次 |
| 防御 | 狡兔三窟 | 1 | 获得6点护盾 |
| 特殊 | 鼠目寸光 | 1 | 造成10点伤害，抽1张 |
| 大招 | 鼠群来袭 | 100怒 | 全体敌人受20伤害，抽3张 |

### 牛 (坦克/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 野蛮冲撞 | 2 | 造成12点伤害 |
| 防御 | 牛皮韧性 | 1 | 获得10点护盾 |
| 特殊 | 牛气冲天 | 1 | 获得8点护盾，本回合+3能量 |
| 大招 | 震天蛮牛冲 | 100怒 | 对最前方敌人造成50伤害 |

### 虎 (追击者/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 猛虎下山 | 2 | 造成15点伤害 |
| 防御 | 虎踞龙盘 | 1 | 获得8点护盾 |
| 特殊 | 虎啸山林 | 2 | 造成10点伤害×2次 |
| 大招 | 猛虎灭世斩 | 100怒 | 全体敌人受30伤害 |

### 兔 (追击者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 蹬鹰一脚 | 1 | 造成8点伤害 |
| 防御 | 狡兔三窟 | 1 | 抽2张牌 |
| 特殊 | 疾如风 | 0 | 本回合下次攻击伤害+50% |
| 大招 | 疾风连蹬 | 100怒 | 随机敌人攻击4次，各10伤害 |

### 龙 (法师/深海)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 龙息 | 2 | 造成12点伤害，附加2层中毒 |
| 防御 | 龙鳞护体 | 1 | 获得8点护盾 |
| 特殊 | 龙吟 | 2 | 全体敌人受5点伤害，附加1层中毒 |
| 大招 | 龙王咆哮 | 100怒 | 全体敌人受25伤害，附加3层中毒 |

### 蛇 (毒师/深海)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 毒牙咬 | 1 | 造成6点伤害，附加2层中毒 |
| 防御 | 蜕皮再生 | 1 | 获得5点护盾，回复3点生命 |
| 特殊 | 蛇毒蔓延 | 2 | 附加3层中毒到所有敌人 |
| 大招 | 万蛇噬咬 | 100怒 | 全体敌人受15伤害，附加5层中毒 |

### 马 (追击者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 踏雪无痕 | 1 | 造成8点伤害 |
| 防御 | 马到功成 | 1 | 获得5点护盾，+1能量 |
| 特殊 | 一马当先 | 1 | 造成6点伤害，下回合+2能量 |
| 大招 | 踏雪飞驹 | 100怒 | 对敌人造成20伤害，+2能量 |

### 羊 (治疗者/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 羊角顶撞 | 1 | 造成5点伤害 |
| 防御 | 羊毛护体 | 1 | 获得6点护盾 |
| 特殊 | 羊灵祝福 | 2 | 回复10点生命，获得5点护盾 |
| 大招 | 羊灵祝福 | 100怒 | 全体队友回复15生命，加10护盾 |

### 猴 (削弱者/混沌)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 猴拳 | 1 | 造成6点伤害 |
| 防御 | 猴毛护体 | 1 | 获得5点护盾 |
| 特殊 | 猴王戏耍 | 2 | 附加2层虚弱到所有敌人 |
| 大招 | 齐天大圣 | 100怒 | 随机抽3张牌，本回合能量不限 |

### 鸡 (追击者/超维)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 啄击 | 1 | 造成7点伤害，暴击率+10% |
| 防御 | 羽毛护体 | 1 | 获得4点护盾 |
| 特殊 | 雄鸡报晓 | 1 | 造成5点伤害，下回合+1能量 |
| 大招 | 雄鸡一唱天下白 | 100怒 | 敌人下回合无法行动 |

### 狗 (坦克/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 犬牙撕咬 | 2 | 造成10点伤害 |
| 防御 | 忠犬护主 | 1 | 获得12点护盾 |
| 特殊 | 狗急跳墙 | 1 | 获得8点护盾，本回合+2能量 |
| 大招 | 忠犬护主 | 100怒 | 全体队友加25护盾 |

### 猪 (反击者/血肉)
| 卡牌 | 名称 | 费用 | 效果 |
|------|------|------|------|
| 攻击 | 猪突猛进 | 2 | 造成10点伤害 |
| 防御 | 皮糙肉厚 | 1 | 获得10点护盾 |
| 特殊 | 哼哼唧唧 | 1 | 获得6点护盾，回复5点生命 |
| 大招 | 猪刚鬣冲击 | 100怒 | 对最前方敌人造成35伤害，回10血 |

## 五、角色定位分布

### 5.1 按定位统计
| 定位 | 数量 | 角色 |
|------|------|------|
| 追击者 | 8 | 鼠、虎、兔、马、鸡、艾欧里亚、米罗、艾俄洛斯 |
| 坦克 | 7 | 牛、狗、穆、阿鲁迪巴、童虎、修罗 |
| 法师 | 3 | 龙、撒加、卡妙 |
| 治疗者 | 2 | 羊、沙加 |
| 削弱者 | 2 | 猴、迪斯马斯克 |
| 毒师 | 2 | 蛇、阿布罗狄 |
| 反击者 | 1 | 猪 |

### 5.2 按种族统计
| 种族 | 数量 | 角色 |
|------|------|------|
| 混沌 | 6 | 鼠、虎、猴、穆、阿鲁迪巴、撒加 |
| 超维 | 6 | 兔、马、鸡、艾欧里亚、沙加、艾俄洛斯 |
| 血肉 | 6 | 牛、羊、狗、猪、童虎、修罗、阿布罗狄 |
| 深海 | 6 | 龙、蛇、迪斯马斯克、米罗、卡妙 |

## 六、配置结构

### 6.1 卡牌配置 (character_cards.json)
```json
{
    "cards": [
        {
            "cardId": "mu_starlight_extinction",
            "characterId": "mu",
            "name": "星光灭绝",
            "style": "tank",
            "description": "造成15点伤害",
            "cost": 2,
            "damage": 15
        }
    ]
}
```

### 6.2 大招配置 (ultimates.json)
```json
{
    "ultimates": [
        {
            "skillId": "mu_ultimate",
            "characterId": "mu",
            "name": "星光灭绝冲击",
            "description": "对所有敌人造成40伤害",
            "rageCost": 100,
            "damage": 40,
            "isAreaAttack": true
        }
    ]
}
```

## 七、实现步骤

### Phase 1: 创建配置
1. 创建 `Data/character_cards.json` - 24个角色专属卡牌（每角色3张）
2. 创建 `Data/ultimates.json` - 24个大招配置
3. 创建 `CharacterCardLoader.cs` - 角色卡牌加载器
4. 创建 `UltimateLoader.cs` - 大招加载器

### Phase 2: 更新代码
1. 更新 `Card.cs` 添加 characterId 字段
2. 更新 `UltimateSkill.cs` 使用配置加载
3. 更新 `CharacterDefinition.cs` 添加黄金圣斗士角色
4. 更新 `CharacterLoader.cs` 加载24个角色

### Phase 3: 验证
1. 验证编译
2. 测试卡牌加载

## 八、文件清单

### 已创建（卡牌数值系统）
- `Scripts/Battle/Card/CardLevelSystem.cs` - 核心数值计算系统
- `Scripts/Battle/Card/DescriptionGenerator.cs` - 动态描述生成器

### 已修改
- `Scripts/Battle/Card/Card.cs` - 添加等级、系数、计算属性、事件系统
- `Scripts/Battle/CharacterSystem/UltimateSkill.cs` - 添加等级、系数、计算属性、事件系统
- `Scripts/UI/CardUI.cs` - 添加实时更新、等级显示

### 待创建
- `Data/character_cards.json` - 24角色×3卡牌 = 72张卡
- `Data/ultimates.json` - 24个大招
- `Scripts/Battle/CharacterSystem/CharacterCardLoader.cs`
- `Scripts/Battle/CharacterSystem/UltimateLoader.cs`
- `Scripts/Battle/CharacterSystem/CharacterDefinition.cs` - 添加12黄金圣斗士（待完成）

---

## 九、使用示例

### 创建带等级的卡牌
```csharp
Card card = new Card();
card.Damage = 10;
card.DamageBaseCoefficient = 0.6f;
card.RageGainPerLevel = 1;
card.Level = 5;
card.LinkedAttributes = character.Attributes;

int damage = card.CalculatedDamage;  // 16
int rage = card.CalculatedRageGain;   // 9
```

### 升级卡牌
```csharp
card.LevelUp();  // 等级+1，触发OnLevelChanged事件

// 监听升级
card.OnLevelChanged += (levelable) => {
    GD.Print($"卡牌升级到 {levelable.Level} 级");
};
```

### 刷新Buff后的描述
```csharp
// 当角色获得Attack+5的Buff时
character.Attributes.Attack += 5;
card.RefreshCalculatedValues();  // 触发OnValuesChanged

// UI自动更新描述
// "造成 19 点伤害" → "造成 22 点伤害"
```

### 升级入口（预留接口）
```csharp
public void OnEnemyDefeated(int expReward)
{
    foreach (var card in playerHand)
    {
        card.AddExp(expReward / playerHand.Count);
    }
}
```
