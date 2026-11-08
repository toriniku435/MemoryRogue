using System;
using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models
{
    public enum ShopTab
    {
        // チップ（何が出るかはお楽しみ）
        Chips,
        // シングル購入（コマンドを選んで1つずつ買う）
        Singles,
    }

    // 購入画面の状態。所持トークン、今のタブ、選んでいるチップかコマンド、買う個数を持つ。
    // 「買えるか」の判定をここ1箇所に集めてあるので、ボタンの活性と実際の購入処理がズレない。
    // シングル購入は、1種類あたり SinglePurchaseRule.LimitPerCommand 個まで（これまでに買った分も数える）
    public sealed class CardShopModel
    {
        private readonly List<CardData> _purchasedCards = new List<CardData>();
        private readonly List<CardData> _purchasedSingles = new List<CardData>();
        private readonly Dictionary<CardData, int> _priorSingleCounts = new Dictionary<CardData, int>();
        private readonly Dictionary<CardData, int> _ownedCounts = new Dictionary<CardData, int>();
        private readonly System.Random _random;
        private readonly float _priceMultiplier;
        private readonly SinglePurchaseRule _singleRule;

        public event Action OnChanged;

        /// <param name="priceMultiplier">全チップとシングル購入の値段にかける係数（ゲームバランスの基礎係数）</param>
        /// <param name="singleRule">シングル購入の値段と上限。null なら既定値</param>
        /// <param name="priorSinglePurchases">これまでにシングル購入で買ったコマンド（買った数だけ並ぶ）</param>
        public CardShopModel(IReadOnlyList<CardPackData> packs, int tokens, System.Random random, float priceMultiplier = 1f,
            SinglePurchaseRule singleRule = null, IReadOnlyList<CardData> priorSinglePurchases = null, IReadOnlyList<CardData> ownedCards = null)
        {
            Packs = packs ?? new CardPackData[0];
            Tokens = tokens;
            _random = random;
            _priceMultiplier = Math.Max(0f, priceMultiplier);
            _singleRule = singleRule ?? SinglePurchaseRule.Default;
            Singles = CreateSingleList(Packs);

            Count(priorSinglePurchases, _priorSingleCounts);
            Count(ownedCards, _ownedCounts);
        }

        private static void Count(IReadOnlyList<CardData> cards, Dictionary<CardData, int> counts)
        {
            foreach (var card in cards ?? new CardData[0])
            {
                if (card == null) continue;
                counts.TryGetValue(card, out int count);
                counts[card] = count + 1;
            }
        }

        public IReadOnlyList<CardPackData> Packs { get; }

        /// <summary>シングル購入で売っているコマンド。並べているチップのどれかに入っているもの（重複なし、レアリティの低い順）</summary>
        public IReadOnlyList<CardData> Singles { get; }

        public int SingleLimit => _singleRule.LimitPerCommand;

        public ShopTab Tab { get; private set; } = ShopTab.Chips;

        /// <summary>残りトークン</summary>
        public int Tokens { get; private set; }

        /// <summary>選んでいるパック。未選択なら null（シングルを選んでいるときも null）</summary>
        public CardPackData SelectedPack { get; private set; }

        /// <summary>選んでいるシングルのコマンド。未選択なら null（チップを選んでいるときも null）</summary>
        public CardData SelectedSingle { get; private set; }

        /// <summary>購入する個数。1以上</summary>
        public int Quantity { get; private set; } = 1;

        /// <summary>選んでいるもの1つの値段</summary>
        public int UnitPrice => SelectedPack != null ? PriceOf(SelectedPack) : SelectedSingle != null ? PriceOfSingle(SelectedSingle) : 0;

        /// <summary>今の選択での合計価格</summary>
        public int TotalPrice => UnitPrice * Quantity;

        /// <summary>この画面で買ったカード（チップもシングルも）。戻るときにまとめて返す</summary>
        public IReadOnlyList<CardData> PurchasedCards => _purchasedCards;

        /// <summary>この画面でシングル購入で買ったコマンド</summary>
        public IReadOnlyList<CardData> PurchasedSingles => _purchasedSingles;

        private bool HasSelection => SelectedPack != null || SelectedSingle != null;

        // シングルは残り数を超えて買えない
        private int MaxQuantity => SelectedSingle != null ? RemainingOf(SelectedSingle) : int.MaxValue;

        public bool CanBuy => HasSelection && Quantity <= MaxQuantity && TotalPrice <= Tokens;

        /// <summary>個数を増やせるか（1個増やしてもトークンが足り、シングルなら残り数も足りるか）</summary>
        public bool CanIncreaseQuantity => HasSelection && Quantity + 1 <= MaxQuantity && UnitPrice * (Quantity + 1) <= Tokens;

        public bool CanDecreaseQuantity => Quantity > 1;

        /// <summary>そのチップ1つの値段（CardPackData の値段 × 係数、四捨五入）</summary>
        public int PriceOf(CardPackData pack)
        {
            return pack == null ? 0 : Multiply(pack.Price);
        }

        /// <summary>そのコマンドをシングル購入するときの1つの値段（レアリティごとの値段 × 係数、四捨五入）</summary>
        public int PriceOfSingle(CardData card)
        {
            return card == null ? 0 : Multiply(_singleRule.BasePriceOf(card.Rarity));
        }

        /// <summary>そのコマンドをあといくつシングル購入できるか（これまでとこの画面で買った分を引く）</summary>
        public int RemainingOf(CardData card)
        {
            if (card == null) return 0;
            _priorSingleCounts.TryGetValue(card, out int prior);
            int bought = _purchasedSingles.Count(c => c == card);
            return Math.Max(0, _singleRule.LimitPerCommand - prior - bought);
        }

        /// <summary>そのコマンドを今いくつ持っているか（来る前から持っていた分 + この画面で買った分。チップから出た分も含む）</summary>
        public int OwnedCountOf(CardData card)
        {
            if (card == null) return 0;
            _ownedCounts.TryGetValue(card, out int owned);
            return owned + _purchasedCards.Count(c => c == card);
        }

        private int Multiply(int price)
        {
            return (int)Math.Round(price * _priceMultiplier, MidpointRounding.AwayFromZero);
        }

        /// <summary>タブを切り替える。選んでいたものは外す（見えていないものを買わないように）</summary>
        public void SetTab(ShopTab tab)
        {
            if (Tab == tab) return;

            Tab = tab;
            SelectedPack = null;
            SelectedSingle = null;
            Quantity = 1;
            OnChanged?.Invoke();
        }

        public void SelectPack(CardPackData pack)
        {
            if (SelectedPack == pack && SelectedSingle == null) return;

            SelectedPack = pack;
            SelectedSingle = null;
            Quantity = 1; // 選び直したら個数も選び直し
            OnChanged?.Invoke();
        }

        public void SelectSingle(CardData card)
        {
            if (SelectedSingle == card && SelectedPack == null) return;

            SelectedSingle = card;
            SelectedPack = null;
            Quantity = 1;
            OnChanged?.Invoke();
        }

        public void IncreaseQuantity()
        {
            if (!CanIncreaseQuantity) return;

            Quantity++;
            OnChanged?.Invoke();
        }

        public void DecreaseQuantity()
        {
            if (!CanDecreaseQuantity) return;

            Quantity--;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 選んでいるものを個数分だけ買う。チップなら開けて出たカードを、シングルならそのコマンドを個数分返す。
        /// トークンを引き、購入済みに積む
        /// </summary>
        /// <returns>このとき手に入ったカード。買えなかった場合は空</returns>
        public CardData[] Buy()
        {
            if (!CanBuy) return new CardData[0];

            int price = TotalPrice;
            CardData[] bought;
            if (SelectedPack != null)
            {
                bought = SelectedPack.Open(Quantity, _random);
            }
            else
            {
                bought = Enumerable.Repeat(SelectedSingle, Quantity).ToArray();
                _purchasedSingles.AddRange(bought);
            }

            Tokens -= price;
            _purchasedCards.AddRange(bought);

            // 残りトークン（と残り数）で買える個数に収まるよう、選び直させる
            Quantity = 1;
            OnChanged?.Invoke();
            return bought;
        }

        // チップの中身を重複なく集め、レアリティの低い順に並べる（同じレアリティは最初に出てきた順のまま）
        private static IReadOnlyList<CardData> CreateSingleList(IReadOnlyList<CardPackData> packs)
        {
            var seen = new List<CardData>();
            foreach (var pack in packs)
            {
                if (pack == null) continue;
                foreach (var card in pack.Cards)
                {
                    if (card != null && !seen.Contains(card)) seen.Add(card);
                }
            }
            return seen.Select((card, index) => (card, index)).OrderBy(x => (int)x.card.Rarity).ThenBy(x => x.index).Select(x => x.card).ToList();
        }
    }
}
