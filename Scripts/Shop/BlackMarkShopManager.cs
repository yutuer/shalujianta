using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FishEatFish.Shop
{
    public enum ShopItemType
    {
        Artifact,
        Engraving
    }

    public class ShopItem
    {
        public ShopItemType ItemType { get; set; }
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public bool Purchased { get; set; }

        public static ShopItem FromArtifact(ArtifactData artifact)
        {
            return new ShopItem
            {
                ItemType = ShopItemType.Artifact,
                ItemId = artifact.artifactId,
                Name = artifact.name,
                Icon = artifact.icon,
                Description = artifact.description,
                Price = artifact.price
            };
        }

        public static ShopItem FromEngraving(EngravingData engraving)
        {
            return new ShopItem
            {
                ItemType = ShopItemType.Engraving,
                ItemId = engraving.engravingId,
                Name = engraving.name,
                Icon = engraving.icon,
                Description = engraving.description,
                Price = engraving.price
            };
        }
    }

    public partial class BlackMarkShopManager : Node
    {
        private static BlackMarkShopManager _instance;
        public static BlackMarkShopManager Instance => _instance;

        private List<ArtifactData> _allArtifacts;
        private List<EngravingData> _allEngravings;

        private List<ArtifactData> _ownedArtifacts;
        private List<string> _engravedCardIds;

        private int _blackMarkCount;
        public int BlackMarkCount => _blackMarkCount;

        public int CurrentShopPrice { get; private set; }

        public System.Action<int> OnBlackMarkChanged;
        public System.Action OnShopClosed;

        public List<ShopItem> CurrentShopItems { get; private set; }
        public ShopItem PendingEngraving { get; private set; }

        public override void _Ready()
        {
            if (_instance != null && _instance != this)
            {
                QueueFree();
                return;
            }
            _instance = this;

            _ownedArtifacts = new List<ArtifactData>();
            _engravedCardIds = new List<string>();
            _blackMarkCount = 0;
            CurrentShopItems = new List<ShopItem>();

            LoadData();
        }

        private void LoadData()
        {
            LoadArtifacts();
            LoadEngravings();
        }

        private void LoadArtifacts()
        {
            var filePath = "res://Data/artifacts.json";
            if (!FileAccess.FileExists(filePath))
            {
                GD.PrintErr($"[BlackMarkShopManager] 文件不存在: {filePath}");
                _allArtifacts = new List<ArtifactData>();
                return;
            }

            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[BlackMarkShopManager] 无法读取文件: {filePath}");
                _allArtifacts = new List<ArtifactData>();
                return;
            }

            var jsonString = file.GetAsText();
            file.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"[BlackMarkShopManager] JSON解析失败: {parseResult}");
                _allArtifacts = new List<ArtifactData>();
                return;
            }

            var data = json.GetData();
            if (data.VariantType == Variant.Type.Nil)
            {
                GD.PrintErr("[BlackMarkShopManager] JSON数据为空");
                _allArtifacts = new List<ArtifactData>();
                return;
            }

            var dataDict = data.AsGodotDictionary();
            if (dataDict == null || !dataDict.ContainsKey("artifacts"))
            {
                GD.PrintErr("[BlackMarkShopManager] 无法解析artifacts数据");
                _allArtifacts = new List<ArtifactData>();
                return;
            }

            var artifactsArray = dataDict["artifacts"].AsGodotArray();
            _allArtifacts = new List<ArtifactData>();

            foreach (var item in artifactsArray)
            {
                var artifactDict = item.AsGodotDictionary();
                if (artifactDict != null)
                {
                    var artifact = ParseArtifact(artifactDict);
                    if (artifact != null)
                    {
                        _allArtifacts.Add(artifact);
                    }
                }
            }

            GD.Print($"[BlackMarkShopManager] 加载了 {_allArtifacts.Count} 个造物");
        }

        private ArtifactData ParseArtifact(Godot.Collections.Dictionary dict)
        {
            try
            {
                var artifact = new ArtifactData
                {
                    artifactId = dict.ContainsKey("artifactId") ? dict["artifactId"].ToString() : "",
                    name = dict.ContainsKey("name") ? dict["name"].ToString() : "",
                    icon = dict.ContainsKey("icon") ? dict["icon"].ToString() : "",
                    description = dict.ContainsKey("description") ? dict["description"].ToString() : "",
                    price = dict.ContainsKey("price") ? (int)(float)dict["price"] : 0,
                    effect = new ArtifactEffect()
                };

                if (dict.ContainsKey("effect"))
                {
                    var effectDict = dict["effect"].AsGodotDictionary();
                    if (effectDict != null)
                    {
                        artifact.effect.type = effectDict.ContainsKey("type") ? effectDict["type"].ToString() : "";
                        artifact.effect.value = effectDict.ContainsKey("value") ? (float)effectDict["value"] : 0f;
                    }
                }

                return artifact;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[BlackMarkShopManager] 解析造物数据失败: {e.Message}");
                return null;
            }
        }

        private void LoadEngravings()
        {
            var filePath = "res://Data/BuffEngraving.json";
            if (!FileAccess.FileExists(filePath))
            {
                GD.PrintErr($"[BlackMarkShopManager] 文件不存在: {filePath}");
                _allEngravings = new List<EngravingData>();
                return;
            }

            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[BlackMarkShopManager] 无法读取文件: {filePath}");
                _allEngravings = new List<EngravingData>();
                return;
            }

            var jsonString = file.GetAsText();
            file.Close();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"[BlackMarkShopManager] JSON解析失败: {parseResult}");
                _allEngravings = new List<EngravingData>();
                return;
            }

            var data = json.GetData();
            if (data.VariantType == Variant.Type.Nil)
            {
                GD.PrintErr("[BlackMarkShopManager] JSON数据为空");
                _allEngravings = new List<EngravingData>();
                return;
            }

            var dataDict = data.AsGodotDictionary();
            if (dataDict == null || !dataDict.ContainsKey("buffEngravings"))
            {
                GD.PrintErr("[BlackMarkShopManager] 无法解析buffEngravings数据");
                _allEngravings = new List<EngravingData>();
                return;
            }

            var engravingsArray = dataDict["buffEngravings"].AsGodotArray();
            _allEngravings = new List<EngravingData>();

            foreach (var item in engravingsArray)
            {
                var engravingDict = item.AsGodotDictionary();
                if (engravingDict != null)
                {
                    var engraving = ParseEngraving(engravingDict);
                    if (engraving != null)
                    {
                        _allEngravings.Add(engraving);
                    }
                }
            }

            GD.Print($"[BlackMarkShopManager] 加载了 {_allEngravings.Count} 个刻印");
        }

        private EngravingData ParseEngraving(Godot.Collections.Dictionary dict)
        {
            try
            {
                var engraving = new EngravingData
                {
                    engravingId = dict.ContainsKey("engravingId") ? dict["engravingId"].ToString() : "",
                    name = dict.ContainsKey("name") ? dict["name"].ToString() : "",
                    icon = dict.ContainsKey("icon") ? dict["icon"].ToString() : "",
                    description = dict.ContainsKey("description") ? dict["description"].ToString() : "",
                    price = 25,
                    effect = new EngravingEffect()
                };

                if (dict.ContainsKey("effect"))
                {
                    var effectDict = dict["effect"].AsGodotDictionary();
                    if (effectDict != null)
                    {
                        engraving.effect.type = effectDict.ContainsKey("type") ? effectDict["type"].ToString() : "";
                        engraving.effect.value = effectDict.ContainsKey("value") ? (float)effectDict["value"] : 0f;
                    }
                }

                return engraving;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[BlackMarkShopManager] 解析刻印数据失败: {e.Message}");
                return null;
            }
        }

        public void AddBlackMark(int amount)
        {
            _blackMarkCount += amount;
            OnBlackMarkChanged?.Invoke(_blackMarkCount);
            GD.Print($"[BlackMarkShopManager] 获得 {amount} 黑印，当前: {_blackMarkCount}");
        }

        public bool SpendBlackMark(int amount)
        {
            if (_blackMarkCount < amount)
            {
                GD.Print($"[BlackMarkShopManager] 黑印不足: 需要 {amount}，现有 {_blackMarkCount}");
                return false;
            }

            _blackMarkCount -= amount;
            OnBlackMarkChanged?.Invoke(_blackMarkCount);
            GD.Print($"[BlackMarkShopManager] 花费 {amount} 黑印，剩余: {_blackMarkCount}");
            return true;
        }

        public void OpenShop()
        {
            GD.Print($"[BlackMarkShopManager] 商店已开放，当前黑印: {_blackMarkCount}");
        }

        public void LoadShopItems(List<ShopItem> shopItems)
        {
            CurrentShopItems.Clear();
            if (shopItems != null && shopItems.Count > 0)
            {
                foreach (var item in shopItems)
                {
                    CurrentShopItems.Add(item);
                }
                GD.Print($"[BlackMarkShopManager] 加载商店物品: {CurrentShopItems.Count} 个");
            }
            else
            {
                GenerateShopItems();
            }
        }

        public List<ShopItem> SaveShopItems()
        {
            return new List<ShopItem>(CurrentShopItems);
        }

        private void GenerateShopItems()
        {
            if (_allArtifacts.Count == 0 || _allEngravings.Count == 0)
            {
                GD.PrintErr("[BlackMarkShopManager] 无法生成商店: 物品数据为空");
                return;
            }

            var random = new Random();

            var availableArtifacts = _allArtifacts.Where(a => !_ownedArtifacts.Contains(a)).ToList();
            var shuffledArtifacts = availableArtifacts.OrderBy(_ => random.Next()).ToList();
            int artifactCount = Math.Min(2, shuffledArtifacts.Count);
            for (int i = 0; i < artifactCount; i++)
            {
                var artifact = shuffledArtifacts[i];
                CurrentShopItems.Add(ShopItem.FromArtifact(artifact));
                GD.Print($"[BlackMarkShopManager] 添加造物到商店: {artifact.name}");
            }

            var availableEngravings = _allEngravings.ToList();
            var shuffledEngravings = availableEngravings.OrderBy(_ => random.Next()).ToList();
            if (shuffledEngravings.Count > 0)
            {
                var engraving = shuffledEngravings[0];
                CurrentShopItems.Add(ShopItem.FromEngraving(engraving));
                GD.Print($"[BlackMarkShopManager] 添加刻印到商店: {engraving.name}");
            }

            GD.Print($"[BlackMarkShopManager] 生成商店物品: {CurrentShopItems.Count} 个");
        }

        public bool CanAfford(ShopItem item)
        {
            return _blackMarkCount >= item.Price;
        }

        public bool PurchaseArtifact(ShopItem item)
        {
            if (item.ItemType != ShopItemType.Artifact)
            {
                GD.PrintErr("[BlackMarkShopManager] 物品类型不匹配");
                return false;
            }

            if (item.Purchased)
            {
                GD.Print($"[BlackMarkShopManager] 物品已购买: {item.Name}");
                return false;
            }

            if (!CanAfford(item))
            {
                GD.Print($"[BlackMarkShopManager] 黑印不足，无法购买: {item.Name}");
                return false;
            }

            if (!SpendBlackMark(item.Price))
            {
                return false;
            }

            var artifact = _allArtifacts.FirstOrDefault(a => a.artifactId == item.ItemId);
            if (artifact != null)
            {
                _ownedArtifacts.Add(artifact);
                item.Purchased = true;
                GD.Print($"[BlackMarkShopManager] 购买成功: {item.Name}，物品保留在商店但标记为已售");
                return true;
            }

            return false;
        }

        public bool StartEngravingPurchase(ShopItem item)
        {
            if (item.ItemType != ShopItemType.Engraving)
            {
                GD.PrintErr("[BlackMarkShopManager] 物品类型不匹配");
                return false;
            }

            if (!CanAfford(item))
            {
                GD.Print($"[BlackMarkShopManager] 黑印不足，无法购买刻印: {item.Name}");
                return false;
            }

            CurrentShopPrice = item.Price;
            PendingEngraving = item;
            GD.Print($"[BlackMarkShopManager] 准备刻印: {item.Name}");
            return true;
        }

        public bool ConfirmEngraving(string cardId)
        {
            if (PendingEngraving == null)
            {
                GD.PrintErr("[BlackMarkShopManager] 没有待刻印项目");
                return false;
            }

            if (PendingEngraving.Purchased)
            {
                GD.Print($"[BlackMarkShopManager] 刻印已购买: {PendingEngraving.Name}");
                return false;
            }

            if (!SpendBlackMark(PendingEngraving.Price))
            {
                return false;
            }

            _engravedCardIds.Add(cardId);
            PendingEngraving.Purchased = true;
            PendingEngraving = null;
            CurrentShopPrice = 0;

            GD.Print($"[BlackMarkShopManager] 刻印成功: {cardId}，刻印保留在商店但标记为已售");
            return true;
        }

        public void CancelEngraving()
        {
            PendingEngraving = null;
            CurrentShopPrice = 0;
        }

        public bool IsCardEngraved(string cardId)
        {
            return _engravedCardIds.Contains(cardId);
        }

        public List<ArtifactData> GetOwnedArtifacts()
        {
            return new List<ArtifactData>(_ownedArtifacts);
        }

        public void CloseShop()
        {
            PendingEngraving = null;
            CurrentShopPrice = 0;
            CurrentShopItems.Clear();
            OnShopClosed?.Invoke();
        }

        public void RegenerateShopItems()
        {
            GD.Print($"[BlackMarkShopManager] RegenerateShopItems called");
            CurrentShopItems.Clear();
            GenerateShopItems();
            GD.Print($"[BlackMarkShopManager] RegenerateShopItems: regenerated {CurrentShopItems.Count} items");
        }

        public void ResetForNewRun()
        {
            _ownedArtifacts.Clear();
            _engravedCardIds.Clear();
            _blackMarkCount = 0;
            CurrentShopItems.Clear();
            PendingEngraving = null;
            CurrentShopPrice = 0;
            GD.Print("[BlackMarkShopManager] 已重置，为新一轮探险做准备");
        }
    }
}
