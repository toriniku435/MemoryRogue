using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    // マーケットの購入のきまり（チップとシングル購入）
    public sealed class CardShopModelTests
    {
        // ノーマル 40 / レア 90 / エピック 180、1種類 3 個まで
        private static readonly SinglePurchaseRule Rule = new SinglePurchaseRule(40, 90, 180, 3);

        private CardData _normal;
        private CardData _rare;
        private CardData _epic;
        private CardPackData _chipA;
        private CardPackData _chipB;

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CardData Card(string id, CardRarity rarity)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = id;
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_rarity", rarity);
            return card;
        }

        private static CardPackData Chip(int price, params CardData[] cards)
        {
            var pack = ScriptableObject.CreateInstance<CardPackData>();
            SetPrivateField(pack, "_price", price);
            SetPrivateField(pack, "_cardCount", 5);
            SetPrivateField(pack, "_cards", cards);
            var weight = new RarityWeight();
            SetPrivateField(weight, "_rarity", CardRarity.Normal);
            SetPrivateField(weight, "_weight", 1);
            SetPrivateField(pack, "_rarityWeights", new[] { weight });
            return pack;
        }

        [SetUp]
        public void SetUp()
        {
            _normal = Card("Normal", CardRarity.Normal);
            _rare = Card("Rare", CardRarity.Rare);
            _epic = Card("Epic", CardRarity.Epic);
            // 2つのチップに同じコマンドが入っていても、シングルの一覧には1回だけ並ぶ
            _chipA = Chip(100, _epic, _normal);
            _chipB = Chip(150, _rare, _normal, null);
        }

        private CardShopModel Create(int tokens, float multiplier = 1f, params CardData[] prior)
        {
            return new CardShopModel(new[] { _chipA, _chipB }, tokens, new System.Random(0), multiplier, Rule, prior);
        }

        // 所持数は、来る前から持っていた分と、この画面で買った分（チップから出た分も）の合計
        [Test]
        public void OwnedCount_IncludesWhatWasBoughtHere()
        {
            var model = new CardShopModel(new[] { _chipA, _chipB }, 1000, new System.Random(0), 1f, Rule, null, new[] { _normal, _normal, _rare });
            Assert.AreEqual(2, model.OwnedCountOf(_normal));
            Assert.AreEqual(0, model.OwnedCountOf(_epic));

            model.SelectSingle(_epic);
            model.Buy();
            model.SelectPack(_chipB);
            var drawn = model.Buy(); // 重みがノーマルだけなので、_chipB からはノーマルだけが出る

            Assert.AreEqual(1, model.OwnedCountOf(_epic));
            Assert.AreEqual(2 + drawn.Count(c => c == _normal), model.OwnedCountOf(_normal));
        }

        [Test]
        public void Singles_AreDistinctAndOrderedByRarity()
        {
            var model = Create(1000);

            CollectionAssert.AreEqual(new[] { _normal, _rare, _epic }, model.Singles.ToArray());
        }

        [Test]
        public void SinglePrice_FollowsRarityAndMultiplier()
        {
            var model = Create(1000, multiplier: 1.5f);

            Assert.AreEqual(60, model.PriceOfSingle(_normal));
            Assert.AreEqual(135, model.PriceOfSingle(_rare));
            Assert.AreEqual(270, model.PriceOfSingle(_epic));
        }

        // 数は残り（上限 − これまでに買った数）まで。買うとその数だけ手に入り、トークンが減る
        [Test]
        public void BuyingSingles_StopsAtTheLimit()
        {
            var model = Create(1000, 1f, _rare);
            model.SelectSingle(_rare);

            Assert.AreEqual(2, model.RemainingOf(_rare));
            model.IncreaseQuantity();
            Assert.IsFalse(model.CanIncreaseQuantity, "残り2つなので3つにはできない");

            var bought = model.Buy();

            CollectionAssert.AreEqual(new[] { _rare, _rare }, bought);
            Assert.AreEqual(1000 - 180, model.Tokens);
            Assert.AreEqual(0, model.RemainingOf(_rare));
            Assert.IsFalse(model.CanBuy, "売り切れは買えない");
            CollectionAssert.AreEqual(new[] { _rare, _rare }, model.PurchasedSingles.ToArray());
            CollectionAssert.AreEqual(new[] { _rare, _rare }, model.PurchasedCards.ToArray());
        }

        [Test]
        public void Singles_CannotBeBoughtWithoutEnoughTokens()
        {
            var model = Create(100);
            model.SelectSingle(_epic);

            Assert.IsFalse(model.CanBuy);
            Assert.AreEqual(0, model.Buy().Length);
            Assert.AreEqual(100, model.Tokens);
        }

        // チップを買った分は、シングル購入の残りに数えない
        [Test]
        public void BuyingAChip_DoesNotUseUpSingles()
        {
            var model = Create(1000);
            model.SelectPack(_chipA);

            var drawn = model.Buy();

            Assert.AreEqual(5, drawn.Length, "1チップ5コマンド");
            Assert.AreEqual(900, model.Tokens);
            Assert.AreEqual(0, model.PurchasedSingles.Count);
            Assert.AreEqual(3, model.RemainingOf(_normal));
        }

        // タブを変えたら、選んでいたものは外れる（見えていないものを買わないように）
        [Test]
        public void SwitchingTab_ClearsTheSelection()
        {
            var model = Create(1000);
            model.SelectPack(_chipA);

            model.SetTab(ShopTab.Singles);

            Assert.IsNull(model.SelectedPack);
            Assert.IsFalse(model.CanBuy);
        }

        [Test]
        public void SelectingASingle_ReplacesTheSelectedChip()
        {
            var model = Create(1000);
            model.SelectPack(_chipA);

            model.SelectSingle(_normal);

            Assert.IsNull(model.SelectedPack);
            Assert.AreEqual(_normal, model.SelectedSingle);
            Assert.AreEqual(40, model.TotalPrice);
        }
    }
}
