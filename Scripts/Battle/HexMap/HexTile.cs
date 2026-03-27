using Godot;

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
        OneDirectionTele
    }

    public enum TeleportDirection
    {
        Forward,
        Backward
    }

    public partial class HexTile
    {
        public const int InfiniteTriggers = -1;

        public HexCoord Coord { get; }
        public HexEventType EventType { get; set; }
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public bool IsVisited { get; set; }
        public bool IsDisappeared { get; set; }
        public int TriggerCount { get; set; }

        public int Damage { get; set; }
        public int HealAmount { get; set; }
        public int BlackMarkGain { get; set; }
        public string EnemyConfig { get; set; }
        public string TeleportPairId { get; set; }
        public bool HasTriggeredThisVisit { get; set; }
        public TeleportDirection TeleportDirection { get; set; }

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
            return new HexTile(Coord, EventType)
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
                DisplayName = DisplayName
            };
        }
    }
}
