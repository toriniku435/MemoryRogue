using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class AutoDeckBuilderTests
    {
        private static CardData Card(string id, CardRarity rarity, int cost, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = id;
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_rarity", rarity);
            SetPrivateField(card, "_cost", cost);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CardData Attack(CardRarity rarity = CardRarity.Normal, int cost = 3) => Card("Attack", rarity, cost, new BasicAttackCardEffect());
        private static CardData Block(CardRarity rarity = CardRarity.Normal, int cost = 3) => Card("Block", rarity, cost, new BasicBlockCardEffect());
        private static CardData Heal(CardRarity rarity = CardRarity.Rare, int cost = 3) => Card("Heal", rarity, cost, new BasicHealCardEffect());

        [Test]
        public void RoleOf_ClassifiesByEffectCategory()
        {
            Assert.AreEqual(CardRole.Attack, AutoDeckBuilder.RoleOf(Attack()));
            Assert.AreEqual(CardRole.Attack, AutoDeckBuilder.RoleOf(Card("Poison", CardRarity.Rare, 4, new BasicPoisonCardEffect())));
            Assert.AreEqual(CardRole.Defense, AutoDeckBuilder.RoleOf(Block()));
            Assert.AreEqual(CardRole.Support, AutoDeckBuilder.RoleOf(Heal()));
            Assert.AreEqual(CardRole.Support, AutoDeckBuilder.RoleOf(Card("Stun", CardRarity.Epic, 6, new StunCardEffect())));
        }

        // 攻撃と回復を併せ持つなら攻撃、ブロックと手札減少なら防御（主な役割で数える）
        [Test]
        public void RoleOf_MixedCardsCountAsTheirMainRole()
        {
            Assert.AreEqual(CardRole.Attack, AutoDeckBuilder.RoleOf(Card("Siphon", CardRarity.Normal, 4, new BasicAttackCardEffect(), new BasicHealCardEffect())));
            Assert.AreEqual(CardRole.Defense, AutoDeckBuilder.RoleOf(Card("Firewall", CardRarity.Rare, 5, new BasicBlockCardEffect(), new HandSizeDownCardEffect())));
        }

        [Test]
        public void Build_FillsEachRoleUpToItsCount()
        {
            var owned = Enumerable.Range(0, 10).Select(_ => Attack())
                .Concat(Enumerable.Range(0, 10).Select(_ => Block()))
                .Concat(Enumerable.Range(0, 10).Select(_ => Heal()))
                .ToArray();

            var deck = AutoDeckBuilder.Build(owned, new AutoDeckRule(5, 3, 2));

            Assert.AreEqual(10, deck.Length);
            Assert.AreEqual(5, deck.Count(card => AutoDeckBuilder.RoleOf(card) == CardRole.Attack));
            Assert.AreEqual(3, deck.Count(card => AutoDeckBuilder.RoleOf(card) == CardRole.Defense));
            Assert.AreEqual(2, deck.Count(card => AutoDeckBuilder.RoleOf(card) == CardRole.Support));
        }

        // 役割の中では、レアリティが高いもの → コストが低いものを優先する
        [Test]
        public void Build_PrefersHigherRarityThenLowerCost()
        {
            var normalCheap = Attack(CardRarity.Normal, 2);
            var normalExpensive = Attack(CardRarity.Normal, 5);
            var rare = Attack(CardRarity.Rare, 6);

            var deck = AutoDeckBuilder.Build(new[] { normalExpensive, normalCheap, rare }, new AutoDeckRule(2, 0, 0));

            CollectionAssert.AreEquivalent(new[] { rare, normalCheap }, deck);
        }

        // 足りない役割の枠は、残りの良いもので埋める（デッキの枚数はそろえる）
        [Test]
        public void Build_FillsShortRolesWithTheBestOfTheRest()
        {
            var owned = Enumerable.Range(0, 8).Select(_ => Attack()).Concat(new[] { Block() }).ToArray();

            var deck = AutoDeckBuilder.Build(owned, new AutoDeckRule(4, 3, 2));

            Assert.AreEqual(9, deck.Length);
            Assert.AreEqual(1, deck.Count(card => AutoDeckBuilder.RoleOf(card) == CardRole.Defense));
        }

        // 同じコマンドの複数枚は別々に数える（1つの参照を何枚でも入れられる）
        [Test]
        public void Build_CountsCopiesOfTheSameAssetSeparately()
        {
            var attack = Attack();
            var deck = AutoDeckBuilder.Build(new[] { attack, attack, attack }, new AutoDeckRule(2, 0, 0));

            Assert.AreEqual(2, deck.Length);
            Assert.IsTrue(deck.All(card => card == attack));
        }

        [Test]
        public void Build_ReturnsWhatThereIsWhenOwnedIsShort()
        {
            var deck = AutoDeckBuilder.Build(new[] { Attack(), null, Block() }, new AutoDeckRule(10, 6, 4));

            Assert.AreEqual(2, deck.Length);
            CollectionAssert.AllItemsAreNotNull(deck);
        }

        [Test]
        public void Build_IsDeterministic()
        {
            var owned = new[] { Heal(), Attack(CardRarity.Rare), Block(), Attack(), Block(CardRarity.Epic), Attack() };
            var rule = new AutoDeckRule(2, 1, 1);

            CollectionAssert.AreEqual(AutoDeckBuilder.Build(owned, rule), AutoDeckBuilder.Build(owned, rule));
        }
    }
}
