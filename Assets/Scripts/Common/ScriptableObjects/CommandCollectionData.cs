using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // コレクション（コマンド）に並べるもの。プレイヤーが手に入れられるコマンドだけを並べ、敵専用のコマンドは入れない。
    //   _packs:      絞り込みのタブにするチップ（並べた順）。中身がそのタブのコマンドになる
    //   _otherCards: チップからは出ず、ステージの報酬などでだけ手に入るコマンド（「すべて」にだけ並ぶ）
    // 「すべて」はその両方を、レアリティの低い順（同じなら最初に出てきた順）に並べる
    [CreateAssetMenu(fileName = "CommandCollection", menuName = "Collection/CommandCollectionData")]
    public sealed class CommandCollectionData : ScriptableObject
    {
        [SerializeField] private CardPackData[] _packs = new CardPackData[0];
        [SerializeField] private CardData[] _otherCards = new CardData[0];

        /// <summary>絞り込みのタブにするチップ（空欄は除く）</summary>
        public IReadOnlyList<CardPackData> Packs
        {
            get
            {
                var packs = new List<CardPackData>();
                foreach (var pack in _packs ?? new CardPackData[0])
                {
                    if (pack != null) packs.Add(pack);
                }
                return packs;
            }
        }

        /// <summary>そのチップから出るコマンド（重複と空欄は除く。チップに並べた順）</summary>
        public static IReadOnlyList<CardData> CardsOf(CardPackData pack)
        {
            var cards = new List<CardData>();
            if (pack == null) return cards;
            foreach (var card in pack.Cards)
            {
                if (card != null && !cards.Contains(card)) cards.Add(card);
            }
            return cards;
        }

        /// <summary>並べるコマンドすべて（重複と空欄は除く。レアリティの低い順、同じなら最初に出てきた順）</summary>
        public IReadOnlyList<CardData> CreateAllCards()
        {
            var cards = new List<CardData>();
            foreach (var pack in Packs)
            {
                foreach (var card in CardsOf(pack))
                {
                    if (!cards.Contains(card)) cards.Add(card);
                }
            }
            foreach (var card in _otherCards ?? new CardData[0])
            {
                if (card != null && !cards.Contains(card)) cards.Add(card);
            }

            // 安定した並べ替え（同じレアリティの中では元の順を保つ）
            var order = new Dictionary<CardData, int>();
            for (int i = 0; i < cards.Count; i++) order[cards[i]] = i;
            cards.Sort((a, b) => a.Rarity != b.Rarity ? a.Rarity.CompareTo(b.Rarity) : order[a].CompareTo(order[b]));
            return cards;
        }
    }
}
