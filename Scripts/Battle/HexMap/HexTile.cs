using Godot;
using System.Collections.Generic;
using FishEatFish.Shop;

namespace FishEatFish.Battle.HexMap
{
    public enum HexEventType
    {
        Empty,
        BattleNormal,
        BattleElite,
        BattleBoss,
        Swamp,
        GainBlackMark,
        Shop,
        Heal,
        TwoWayTeleport,
        Hole,
        OneDirectionTele,
        /// <summary>探照灯：打开开关可驱散周围的迷雾（扩大视野）</summary>
        Searchlight
    }

    public enum TeleportDirection
    {
        Forward,
        Backward
    }

    public enum TeleportRole
    {
        None,   // 非传送门
        Entry,  // 传送入口（可以传送）
        Exit    // 传送出口（不能传送）
    }

    public partial class HexTile
    {
        public const int InfiniteTriggers = -1;

        public HexCoord Coord { get; }
        public HexEventType EventType { get; set; }
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public bool IsVisited { get; set; }

        /// <summary>是否已被照亮（战争迷雾）。未照亮的格子显示为迷雾（黑色）。</summary>
        public bool IsRevealed { get; set; }

        /// <summary>
        /// 是否阻挡视线（墙体 / 巨石之类）。
        /// 目前地图生成器不会设置它，是给 FOV 规则预留的开关：
        /// 换上 BlockingFovRule 后，被标记的格子会挡住它身后的视野。
        /// </summary>
        public bool IsVisionBlocker { get; set; }

        public bool IsDisappeared { get; set; }
        public int TriggerCount { get; set; }

        public int Damage { get; set; }
        public int HealAmount { get; set; }
        public int BlackMarkGain { get; set; }
        public string EnemyConfig { get; set; }
        public string TeleportPairId { get; set; }
        public bool HasTriggeredThisVisit { get; set; }
        public TeleportDirection TeleportDirection { get; set; }
        public TeleportRole TeleportRole { get; set; }
        public int ShopRefreshCount { get; set; }
        public List<ShopItem> ShopItems { get; set; }

        public string IconPath { get; set; }
        public string DisplayName { get; set; }

        public System.Action<HexTile> OnEnter;
        public System.Action<HexTile> OnExit;

        public HexTile(HexCoord coord, HexEventType eventType = HexEventType.Empty)
        {
            Coord = coord;
            EventType = eventType;
            TriggerCount = 1;
            IsStart = false;
            IsEnd = false;
            IsVisited = false;
            IsDisappeared = false;
            HasTriggeredThisVisit = false;
            TeleportDirection = TeleportDirection.Forward;
            ShopRefreshCount = 2;
            ShopItems = new List<ShopItem>();
        }

        public bool CanEnter => !IsDisappeared;

        public bool ShouldShowTeleportPrompt
        {
            get
            {
                if (EventType != HexEventType.TwoWayTeleport && EventType != HexEventType.OneDirectionTele)
                    return false;

                if (HasTriggeredThisVisit)
                    return false;

                if (EventType == HexEventType.OneDirectionTele && TeleportRole != TeleportRole.Entry)
                    return false;

                return true;
            }
        }

        public bool CanTeleportTo(HexCoord fromCoord, HexCoord toCoord)
        {
            if (EventType == HexEventType.TwoWayTeleport)
                return true;

            if (EventType == HexEventType.OneDirectionTele)
            {
                return TeleportDirection == TeleportDirection.Forward;
            }

            return false;
        }

        public void OnPlayerEnter()
        {
            if (!CanEnter) return;

            IsVisited = true;
            OnEnter?.Invoke(this);

            if (EventType == HexEventType.TwoWayTeleport)
            {
                HasTriggeredThisVisit = true;
            }
        }

        public void OnPlayerExit()
        {
            OnExit?.Invoke(this);
        }

        public bool CanTrigger
        {
            get
            {
                if (IsDisappeared) return false;
                if (TriggerCount == 0) return false;
                return true;
            }
        }

        public void Trigger()
        {
            if (!CanTrigger) return;

            if (TriggerCount > 0)
            {
                TriggerCount--;
                if (TriggerCount == 0)
                {
                    EventType = HexEventType.Empty;
                }
            }
        }

        public void TriggerHole()
        {
            if (EventType != HexEventType.Hole) return;

            IsDisappeared = true;
            EventType = HexEventType.Empty;
            GD.Print($"[HexTile] 洞穴 {Coord} 已消失");
        }

        public void ResetVisitState()
        {
            IsVisited = false;
            HasTriggeredThisVisit = false;
        }

        public HexTile Clone()
        {
            var clone = new HexTile(Coord, EventType)
            {
                IsStart = IsStart,
                IsEnd = IsEnd,
                IsVisited = IsVisited,
                IsDisappeared = IsDisappeared,
                TriggerCount = TriggerCount,
                Damage = Damage,
                HealAmount = HealAmount,
                BlackMarkGain = BlackMarkGain,
                EnemyConfig = EnemyConfig,
                TeleportPairId = TeleportPairId,
                HasTriggeredThisVisit = HasTriggeredThisVisit,
                TeleportDirection = TeleportDirection,
                IconPath = IconPath,
                DisplayName = DisplayName,
                ShopRefreshCount = ShopRefreshCount
            };

            foreach (var item in ShopItems)
            {
                clone.ShopItems.Add(new ShopItem
                {
                    ItemType = item.ItemType,
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Icon = item.Icon,
                    Description = item.Description,
                    Price = item.Price,
                    Purchased = item.Purchased
                });
            }

            return clone;
        }
    }
}

