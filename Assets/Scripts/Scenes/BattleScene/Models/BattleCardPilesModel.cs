using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.BattleScene.Models
{
    // プレイヤーのコマンドが置かれる山: 山札・手札（バッファ）・捨て札（アーカイブ）・パージ・次の手札に戻る分。
    // 実行リストに積んだコマンドはどの山にも無い（CardCommandModel が持つ）
    public sealed class BattleCardPilesModel
    {
        public CardPileModel Deck { get; }
        public CardPileModel Hand { get; }
        public CardPileModel Discard { get; }

        // パージ（除外）されたコマンド。この戦闘の間は山札に戻らない
        public CardPileModel Purge { get; }

        // 次のターンの手札に戻るコマンド（ループバック）。画面には出さず、次に引くときにほかより先に手札へ入る
        public CardPileModel Returning { get; }

        public BattleCardPilesModel(CardPileModel deck, CardPileModel hand, CardPileModel discard, CardPileModel purge = null)
        {
            Deck = deck;
            Hand = hand;
            Discard = discard;
            Purge = purge ?? new CardPileModel(new CardData[0]);
            Returning = new CardPileModel(new CardData[0]);
        }

        /// <summary>
        /// 実行リストの外に、もう使えるコマンドが1つも無いか（山札・手札・捨て札・次の手札に戻る分が空で、何かがパージされている）。
        /// 実行リストに残っている分は呼び出し側が数える
        /// </summary>
        public bool HasOnlyPurged =>
            Deck.IsEmpty && Hand.IsEmpty && Discard.IsEmpty && Returning.IsEmpty && !Purge.IsEmpty;

        /// <summary>実行し終えたコマンドの行き先の山</summary>
        public CardPileModel PileOf(CommandDestination destination)
        {
            switch (destination)
            {
                case CommandDestination.Purge: return Purge;
                case CommandDestination.ReturnToHand: return Returning;
                default: return Discard;
            }
        }
    }
}
