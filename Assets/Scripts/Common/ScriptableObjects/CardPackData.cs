using UnityEngine;
using Live2DCardRogue.Common.Models.Cards;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // カードパック1種類分。封入カードと、レアリティごとの当たりやすさを持つ。
    // 表示名は _id をキーに CardPackName テーブルから引く（CardDataと同じ規約）
    [CreateAssetMenu(fileName = "NewCardPackData", menuName = "Cards/CardPackData")]
    public class CardPackData : ScriptableObject
    {
        [SerializeField] private string _id;

        /// <summary>1パックの価格（トークン）</summary>
        [SerializeField][Min(0)] private int _price;

        /// <summary>1パックから出る枚数</summary>
        [SerializeField][Min(1)] private int _cardCount = 3;

        /// <summary>封入カード。ここに無いレアリティは重みを付けても出ない</summary>
        [SerializeField] private CardData[] _cards;

        /// <summary>レアリティごとの当たりやすさ</summary>
        [SerializeField] private RarityWeight[] _rarityWeights;

        /// <summary>一覧と購入パネルに出すパックの絵。未設定でもよい（絵の枠が空くだけ）</summary>
        [SerializeField] private Sprite _illustration;

        public string Id => _id;
        public int Price => _price;
        public int CardCount => _cardCount;
        public Sprite Illustration => _illustration;

        /// <summary>封入カード（空欄の要素も含めてそのまま。未設定なら空）</summary>
        public System.Collections.Generic.IReadOnlyList<CardData> Cards => _cards ?? new CardData[0];

        /// <summary>
        /// パックを指定数だけ開けて、出たカードを返す。
        /// 封入が空だったり重みが全部0だったりしても例外にはせず空配列を返すので、
        /// 呼び出し側はnullチェックも件数チェックも要らない
        /// </summary>
        public CardData[] Open(int packCount, System.Random random)
        {
            if (packCount <= 0) return new CardData[0];

            return OpenCards(packCount * _cardCount, random);
        }

        /// <summary>
        /// このチップの封入と当たりやすさで、ちょうど cardCount 個を引く（チップ何個分かに割り切れない数でもよい）。
        /// ファーストビルドのように、一部を確定枠にして残りだけを抽選するときに使う
        /// </summary>
        public CardData[] OpenCards(int cardCount, System.Random random)
        {
            if (cardCount <= 0) return new CardData[0];

            return CardPackOpener.Open(_cards, _rarityWeights, cardCount, random);
        }
    }
}
