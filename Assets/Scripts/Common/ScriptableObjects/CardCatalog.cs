using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // ゲームに存在するすべてのコマンド（カード）の一覧。セーブデータは CardData.Id で持つので、
    // 読み込むときにここから実物を引き直す。コマンドを足したらここにも足す（CardCatalogTests が漏れを見つける）
    [CreateAssetMenu(fileName = "CardCatalog", menuName = "Cards/CardCatalog")]
    public sealed class CardCatalog : ScriptableObject
    {
        [SerializeField] private CardData[] _cards;

        private Dictionary<string, CardData> _byId;

        public IReadOnlyList<CardData> Cards => _cards ?? new CardData[0];

        /// <summary>ID のコマンド。無ければ null（消した・名前を変えたコマンドのセーブを読んだとき）</summary>
        public CardData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, CardData>();
                foreach (var card in Cards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.Id)) _byId[card.Id] = card;
                }
            }
            return _byId.TryGetValue(id, out var found) ? found : null;
        }
    }
}
