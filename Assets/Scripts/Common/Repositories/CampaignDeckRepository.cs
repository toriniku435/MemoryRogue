using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Repositories
{
    // キャンペーン中のデッキと所持カード。進行状況と同じく、シーンをまたいで残り続ける。
    //
    // デッキは DeckSlotCount 個まで用意でき、そのうちアクティブな1つ（ActiveDeckIndex）を戦闘で使う。
    // どのデッキも所持カードから組むだけで、デッキ同士で同じカードを取り合うことはない（重ねて入れてよい）。
    //
    // デッキ編集のウインドウへは「渡して、返してもらう」形にしてあるので、あのウインドウはここを知らない。
    // 保存の責任をここ1箇所に集めておくことで、別モードが別の保存先を使うこともできる。
    // ファイルへの保存と再開は CampaignSaveService が、ほかのキャンペーンのリポジトリとまとめて行う
    public static class CampaignDeckRepository
    {
        /// <summary>用意できるデッキの数</summary>
        public const int DeckSlotCount = 10;

        private static CardData[][] _decks = CreateEmptyDecks();

        /// <summary>デッキの一覧（DeckSlotCount 個。空のデッキは長さ0）。どれも1枚ずつ並べた形</summary>
        public static IReadOnlyList<CardData[]> Decks => _decks;

        /// <summary>アクティブな（戦闘で使う）デッキの番号</summary>
        public static int ActiveDeckIndex { get; private set; }

        /// <summary>アクティブな（戦闘で使う）デッキ。1枚ずつ並べた形</summary>
        public static CardData[] Deck => _decks[ActiveDeckIndex];

        /// <summary>所持カード。同じカードを複数枚持っているなら、その枚数分並んでいる</summary>
        public static CardData[] OwnedCards { get; private set; } = new CardData[0];

        /// <summary>
        /// マーケットのシングル購入で買ったコマンド（買った数だけ並ぶ）。1種類あたりの上限を数えるためだけに持つ。
        /// 買ったコマンドそのものは OwnedCards にも入っている
        /// </summary>
        public static CardData[] PurchasedSingles { get; private set; } = new CardData[0];

        /// <summary>まだ何も入っていないか（キャンペーン開始時の初期化が必要か）</summary>
        public static bool IsEmpty
        {
            get
            {
                if (OwnedCards.Length > 0) return false;
                foreach (var deck in _decks)
                {
                    if (deck.Length > 0) return false;
                }
                return true;
            }
        }

        /// <summary>デッキを1つだけ持って始める（1番目に入れてアクティブにし、残りは空）</summary>
        public static void Initialize(CardData[] deck, CardData[] ownedCards, CardData[] purchasedSingles = null)
        {
            Initialize(new[] { deck }, 0, ownedCards, purchasedSingles);
        }

        /// <summary>デッキの一覧とアクティブな番号を持って始める（足りない分は空、多い分は捨てる）</summary>
        public static void Initialize(IReadOnlyList<CardData[]> decks, int activeDeckIndex, CardData[] ownedCards, CardData[] purchasedSingles = null)
        {
            SetDecks(decks, activeDeckIndex);
            OwnedCards = ownedCards ?? new CardData[0];
            PurchasedSingles = purchasedSingles ?? new CardData[0];
        }

        /// <summary>シングル購入で買ったコマンドを記録する（所持に加えるのは AddOwnedCards）</summary>
        public static void AddPurchasedSingles(CardData[] cards)
        {
            if (cards == null || cards.Length == 0) return;

            var singles = new List<CardData>(PurchasedSingles);
            foreach (var card in cards)
            {
                if (card != null) singles.Add(card);
            }

            PurchasedSingles = singles.ToArray();
        }

        /// <summary>購入などで手に入れたカードを所持に加える</summary>
        public static void AddOwnedCards(CardData[] cards)
        {
            if (cards == null || cards.Length == 0) return;

            var owned = new List<CardData>(OwnedCards);
            foreach (var card in cards)
            {
                if (card != null) owned.Add(card);
            }

            OwnedCards = owned.ToArray();
        }

        /// <summary>デッキの一覧とアクティブな番号をまとめて置き換える（デッキ編集の保存）</summary>
        public static void SetDecks(IReadOnlyList<CardData[]> decks, int activeDeckIndex)
        {
            _decks = CreateEmptyDecks();
            if (decks != null)
            {
                for (int i = 0; i < decks.Count && i < DeckSlotCount; i++)
                {
                    _decks[i] = decks[i] ?? new CardData[0];
                }
            }
            ActiveDeckIndex = activeDeckIndex >= 0 && activeDeckIndex < DeckSlotCount ? activeDeckIndex : 0;
        }

        public static void Clear()
        {
            _decks = CreateEmptyDecks();
            ActiveDeckIndex = 0;
            OwnedCards = new CardData[0];
            PurchasedSingles = new CardData[0];
        }

        private static CardData[][] CreateEmptyDecks()
        {
            var decks = new CardData[DeckSlotCount][];
            for (int i = 0; i < decks.Length; i++) decks[i] = new CardData[0];
            return decks;
        }
    }
}
