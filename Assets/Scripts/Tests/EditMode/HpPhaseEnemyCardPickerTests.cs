using System.Collections.Generic;
using System.Reflection;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // ボスの行動の選び方: HPのしきい値で重みが切り替わること、生成は味方がいないときだけ決まったターンに先頭で使うこと
    public sealed class HpPhaseEnemyCardPickerTests
    {
        private CardData _attack;
        private CardData _heal;
        private CardData _summon;
        private CardData[] _cards;

        [SetUp]
        public void SetUp()
        {
            _attack = NewCard("Attack");
            _heal = NewCard("Heal");
            _summon = NewCard("Summon");
            _cards = new[] { _attack, _heal, _summon };
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static CardData NewCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            return card;
        }

        private static HpPhaseEnemyCardPicker.Entry NewEntry(CardData card, int high, int low)
        {
            var entry = new HpPhaseEnemyCardPicker.Entry();
            SetPrivateField(entry, "_card", card);
            SetPrivateField(entry, "_weightHigh", high);
            SetPrivateField(entry, "_weightLow", low);
            return entry;
        }

        // しきい値以上では攻撃だけ、未満では回復だけが出る重みにしておく
        private HpPhaseEnemyCardPicker NewPicker()
        {
            var picker = new HpPhaseEnemyCardPicker();
            SetPrivateField(picker, "_thresholdPercent", 50);
            SetPrivateField(picker, "_entries", new[] { NewEntry(_attack, 1, 0), NewEntry(_heal, 0, 1) });
            SetPrivateField(picker, "_summonCard", _summon);
            SetPrivateField(picker, "_summonAfterAloneTurns", 3);
            return picker;
        }

        private static EnemyPickContext Context(int hp, int allies = 1, int turn = 2, int turnsAlone = 0)
        {
            return new EnemyPickContext(hp, 100, allies, turn, turnsAlone);
        }

        [Test]
        public void AboveThreshold_UsesTheHighWeights()
        {
            var picked = NewPicker().Pick(_cards, 2, 2, Context(hp: 50));
            CollectionAssert.AreEqual(new[] { _attack, _attack }, picked);
        }

        [Test]
        public void BelowThreshold_UsesTheLowWeights()
        {
            var picked = NewPicker().Pick(_cards, 2, 2, Context(hp: 49));
            CollectionAssert.AreEqual(new[] { _heal, _heal }, picked);
        }

        // 生成は1ターン目に味方がいなければ、そのターンの最初の行動になる（行動の数は変わらない）
        [Test]
        public void FirstTurnWithoutAllies_SummonsFirst()
        {
            var picked = NewPicker().Pick(_cards, 2, 2, Context(hp: 100, allies: 0, turn: 1));
            CollectionAssert.AreEqual(new[] { _summon, _attack }, picked);
        }

        [Test]
        public void AloneForTheGivenTurns_SummonsAgain()
        {
            var picker = NewPicker();
            CollectionAssert.DoesNotContain(picker.Pick(_cards, 2, 2, Context(hp: 100, allies: 0, turn: 5, turnsAlone: 2)), _summon);
            Assert.AreEqual(_summon, picker.Pick(_cards, 2, 2, Context(hp: 100, allies: 0, turn: 6, turnsAlone: 3))[0]);
        }

        [Test]
        public void WithALivingAlly_NeverSummons()
        {
            var picked = NewPicker().Pick(_cards, 2, 2, Context(hp: 100, allies: 1, turn: 1, turnsAlone: 9));
            CollectionAssert.DoesNotContain(picked, _summon);
        }

        // 重みを付け忘れても、生成以外のカードから行動する
        [Test]
        public void WithoutWeights_FallsBackToTheOtherCards()
        {
            var picker = NewPicker();
            SetPrivateField(picker, "_entries", new HpPhaseEnemyCardPicker.Entry[0]);

            var picked = picker.Pick(_cards, 2, 2, Context(hp: 100));

            Assert.AreEqual(2, picked.Count);
            CollectionAssert.DoesNotContain(picked, _summon);
        }

        [Test]
        public void NoCandidates_ReturnsEmptyNotNull()
        {
            Assert.IsNotNull(NewPicker().Pick(null, 2, 2, default));
            Assert.IsEmpty(NewPicker().Pick(new List<CardData>(), 2, 2, default));
        }
    }
}
