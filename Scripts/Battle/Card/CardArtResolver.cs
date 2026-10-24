using System.Collections.Generic;
using Godot;

namespace FishEatFish.Battle.Card
{
	/// <summary>
	/// **卡面插画查找**（一份实现，多处共用）。
	///
	/// 查找顺序：
	/// ① 通用卡：<c>cards.json</c> 里的 <c>spritePath</c>
	/// ② 角色专属卡：按 cardId 推导（<c>res://Assets/Cards/card_&lt;cardId&gt;.svg</c>，专属卡不在 cards.json 里）
	/// ③ 兜底：旧的「按卡名硬编码映射」（<c>ResourceManager.CardTextures</c>）
	///
	/// ⚠️ 之前手牌与详情面板各抄了一份（BattleManager 里一份、界面里再一份）——
	/// 抄第二份就一定会漏字段。统一走这里。
	/// </summary>
	public static class CardArtResolver
	{
		/// <summary>
		/// 取卡面插画。
		/// </summary>
		/// <param name="card">卡牌</param>
		/// <param name="legacyTextures">旧映射表（可为 null）</param>
		/// <returns>找不到返回 null</returns>
		public static Texture2D Resolve(Card card, Dictionary<string, Texture2D> legacyTextures = null)
		{
			if (card == null) return null;

			Texture2D texture = null;

			CardData cardData = CardConfigLoader.TryGetCardData(card.CardId);
			if (cardData != null && !string.IsNullOrEmpty(cardData.SpritePath) && ResourceLoader.Exists(cardData.SpritePath))
			{
				texture = ResourceLoader.Load<Texture2D>(cardData.SpritePath);
			}

			if (texture == null && !string.IsNullOrEmpty(card.CardId))
			{
				string derived = $"res://Assets/Cards/card_{card.CardId}.svg";
				if (ResourceLoader.Exists(derived)) texture = ResourceLoader.Load<Texture2D>(derived);
			}

			// ②-b 插画借用（衍生卡没有专属插画 → 借别的卡的立绘，如圣心借净世之路）
			if (texture == null && !string.IsNullOrEmpty(card.ArtCardId))
			{
				string borrowed = $"res://Assets/Cards/card_{card.ArtCardId}.svg";
				if (ResourceLoader.Exists(borrowed)) texture = ResourceLoader.Load<Texture2D>(borrowed);
			}

			if (texture == null && legacyTextures != null && legacyTextures.TryGetValue(card.Name, out Texture2D legacy))
			{
				texture = legacy;
			}

			return texture;
		}
	}
}
