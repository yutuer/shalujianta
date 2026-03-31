using System;
using Godot;

namespace FishEatFish.Battle.HexMap
{
    public class HexEventManager
    {
        private static HexEventManager _instance;
        public static HexEventManager Instance => _instance;

        public System.Action<string, string> OnEventTriggered;
        public System.Action<string, int> OnDamageDealt;
        public System.Action<string, int> OnHealingApplied;
        public System.Action<string, int> OnBlackMarkGained;
        public System.Action<HexEventType, string> OnBattleTriggered;

        public HexEventManager()
        {
            _instance = this;
        }

        public void ProcessEvent(HexTile tile, HexMapController controller)
        {
            if (tile == null || controller == null) return;

            switch (tile.EventType)
            {
                case HexEventType.Empty:
                    ProcessEmpty(tile, controller);
                    break;

                case HexEventType.BattleNormal:
                    ProcessBattle(tile, controller, false);
                    break;

                case HexEventType.BattleElite:
                    ProcessBattle(tile, controller, true);
                    break;

                case HexEventType.BattleBoss:
                    ProcessBattle(tile, controller, true);
                    break;

                case HexEventType.Swamp:
                    ProcessSwamp(tile, controller);
                    break;

                case HexEventType.GainBlackMark:
                    ProcessGainBlackMark(tile, controller);
                    break;

                case HexEventType.Shop:
                    ProcessShop(tile, controller);
                    break;

                case HexEventType.Heal:
                    ProcessHeal(tile, controller);
                    break;

                case HexEventType.TwoWayTeleport:
                    ProcessTeleport(tile, controller);
                    break;

                case HexEventType.OneDirectionTele:
                    ProcessOneWayTeleport(tile, controller);
                    break;

                case HexEventType.Hole:
                    ProcessHole(tile, controller);
                    break;

                default:
                    GD.Print($"[HexEventManager] 未处理的事件类型: {tile.EventType}");
                    break;
            }
        }

        private void ProcessEmpty(HexTile tile, HexMapController controller)
        {
            GD.Print($"[HexEventManager] 空格子: {tile.Coord}");
            OnEventTriggered?.Invoke("empty", tile.Coord.ToString());
        }

        private void ProcessBattle(HexTile tile, HexMapController controller, bool isElite)
        {
            var battleType = isElite ? "精英战斗" : "普通战斗";
            GD.Print($"[HexEventManager] {battleType}: {tile.Coord}, 配置: {tile.EnemyConfig}");

            HexEventType eventType = isElite ? HexEventType.BattleElite : HexEventType.BattleNormal;
            string enemyConfig = tile.EnemyConfig ?? (isElite ? "elite_wave" : "normal_wave");

            OnEventTriggered?.Invoke(isElite ? "elite_battle" : "normal_battle", tile.Coord.ToString());
            OnBattleTriggered?.Invoke(eventType, enemyConfig);

            GD.Print($"[HexEventManager] 触发战斗场景切换: type={eventType}, config={enemyConfig}");
        }

        private float CalculateBattleDamage(HexTile tile, bool isElite)
        {
            int baseDamage = isElite ? 15 : 8;
            int variance = isElite ? 10 : 5;

            var random = new Random();
            return baseDamage + random.Next(variance);
        }

        private void ProcessSwamp(HexTile tile, HexMapController controller)
        {
            int damage = tile.Damage > 0 ? tile.Damage : 10;
            GD.Print($"[HexEventManager] 沼泽陷阱: {tile.Coord}, 伤害: {damage}");

            OnEventTriggered?.Invoke("swamp", tile.Coord.ToString());
            controller.DamagePlayer(damage);
            OnDamageDealt?.Invoke("swamp", damage);
        }

        private void ProcessGainBlackMark(HexTile tile, HexMapController controller)
        {
            int blackMarkGain = tile.BlackMarkGain > 0 ? tile.BlackMarkGain : 5;
            GD.Print($"[HexEventManager] 获得黑印: {tile.Coord}, 数量: {blackMarkGain}");

            OnEventTriggered?.Invoke("black_mark", tile.Coord.ToString());
            controller.AddBlackMark(blackMarkGain);
            OnBlackMarkGained?.Invoke(tile.Coord.ToString(), blackMarkGain);
        }

        private void ProcessShop(HexTile tile, HexMapController controller)
        {
            GD.Print($"[HexEventManager] 进入商店: {tile.Coord}");

            OnEventTriggered?.Invoke("shop", tile.Coord.ToString());
            controller.OpenShop();
        }

        private void ProcessHeal(HexTile tile, HexMapController controller)
        {
            int healAmount = tile.HealAmount > 0 ? tile.HealAmount : 20;
            GD.Print($"[HexEventManager] 生命之泉: {tile.Coord}, 治疗: {healAmount}");

            OnEventTriggered?.Invoke("heal", tile.Coord.ToString());
            controller.HealPlayer(healAmount);
            OnHealingApplied?.Invoke(tile.Coord.ToString(), healAmount);
        }

        private void ProcessTeleport(HexTile tile, HexMapController controller)
        {
            GD.Print($"[HexEventManager] 双向传送门: {tile.Coord}");
            OnEventTriggered?.Invoke("teleport_two_way", tile.Coord.ToString());
        }

        private void ProcessOneWayTeleport(HexTile tile, HexMapController controller)
        {
            GD.Print($"[HexEventManager] 单向传送门: {tile.Coord}, 角色: {tile.TeleportRole}");

            if (tile.TeleportRole != TeleportRole.Entry)
            {
                GD.Print($"[HexEventManager] 这是传送出口，不触发传送");
                return;
            }

            GD.Print($"[HexEventManager] 这是传送入口，准备传送");
            OnEventTriggered?.Invoke("teleport_one_way", tile.Coord.ToString());

            var targetTile = FindOneWayTeleportTarget(tile, controller);
            if (targetTile != null)
            {
                GD.Print($"[HexEventManager] 传送到: {targetTile.Coord}");
                controller.TeleportPlayerTo(targetTile);
            }
            else
            {
                GD.Print($"[HexEventManager] 未找到传送目标!");
            }
        }

        private HexTile FindOneWayTeleportTarget(HexTile sourceTile, HexMapController controller)
        {
            if (controller == null || controller.CurrentMap == null)
            {
                GD.PrintErr("[HexEventManager] controller or CurrentMap is null!");
                return null;
            }

            if (string.IsNullOrEmpty(sourceTile.TeleportPairId))
            {
                GD.PrintErr("[HexEventManager] TeleportPairId is null or empty!");
                return null;
            }

            foreach (var tile in controller.CurrentMap.GetAllTiles())
            {
                if (tile.TeleportPairId == sourceTile.TeleportPairId &&
                    tile.Coord != sourceTile.Coord &&
                    tile.TeleportRole == TeleportRole.Exit)
                {
                    return tile;
                }
            }

            GD.Print($"[HexEventManager] 未找到配对的出口传送门, TeleportPairId: {sourceTile.TeleportPairId}");
            return null;
        }

        private HexTile FindTeleportTarget(HexTile sourceTile, HexMapController controller)
        {
            if (controller == null || controller.CurrentMap == null)
            {
                GD.PrintErr("[HexEventManager] controller or CurrentMap is null!");
                return null;
            }

            if (!string.IsNullOrEmpty(sourceTile.TeleportPairId))
            {
                var allTiles = controller.CurrentMap.GetAllTiles();
                foreach (var tile in allTiles)
                {
                    if (tile != sourceTile &&
                        tile.EventType == HexEventType.OneDirectionTele &&
                        tile.TeleportPairId == sourceTile.TeleportPairId &&
                        !tile.IsDisappeared)
                    {
                        return tile;
                    }
                }
            }

            var hexMap = controller.CurrentMap;
            var targetRow = sourceTile.Coord.Q + 1;

            if (sourceTile.TeleportDirection == TeleportDirection.Forward)
            {
                targetRow = sourceTile.Coord.Q + 2;
            }
            else
            {
                targetRow = sourceTile.Coord.Q - 1;
            }

            var candidates = new System.Collections.Generic.List<HexTile>();
            foreach (var tile in hexMap.GetAllTiles())
            {
                if (tile.Coord.Q == targetRow && !tile.IsDisappeared && tile.CanEnter)
                {
                    candidates.Add(tile);
                }
            }

            if (candidates.Count > 0)
            {
                var random = new Random();
                return candidates[random.Next(candidates.Count)];
            }

            return null;
        }

        private void ProcessHole(HexTile tile, HexMapController controller)
        {
            GD.Print($"[HexEventManager] 洞穴触发: {tile.Coord}");
            OnEventTriggered?.Invoke("hole", tile.Coord.ToString());
        }

        public int CalculateDifficultyScaling(int playerLevel, int baseValue)
        {
            float scalingFactor = 1.0f + (playerLevel - 1) * 0.1f;
            return (int)(baseValue * scalingFactor);
        }

        public float CalculateDamageScaling(int playerLevel, float baseDamage)
        {
            float scalingFactor = 1.0f + (playerLevel - 1) * 0.08f;
            return baseDamage * scalingFactor;
        }

        public int CalculateBlackMarkReward(int playerLevel, int baseReward)
        {
            float scalingFactor = 1.0f + (playerLevel - 1) * 0.15f;
            return (int)(baseReward * scalingFactor);
        }
    }
}
