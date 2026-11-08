using System.Collections.Generic;
using System.Reflection;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class EnemyDataTests
    {
        // Pickerに何が渡されたかを記録するだけのスタブ
        private sealed class SpyPicker : IEnemyCardPicker
        {
            public IReadOnlyList<CardData> ReceivedCards;
            public int ReceivedMin;
            public int ReceivedMax;
            public EnemyPickContext ReceivedContext;
            public IReadOnlyList<CardData> Result = new CardData[0];

            public IReadOnlyList<CardData> Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)
            {
                ReceivedContext = context;
                ReceivedCards = cards;
                ReceivedMin = minActionCount;
                ReceivedMax = maxActionCount;
                return Result;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static PoisonResistanceDefinition NewPoisonResistanceDefinition(int value)
        {
            var definition = new PoisonResistanceDefinition();
            SetPrivateField(definition, "_resistanceValue", value);
            return definition;
        }

        private static CardData NewCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            return card;
        }

        private static EnemyData NewEnemy(
            CardData[] cards,
            IEnemyCardPicker picker,
            int min = 1,
            int max = 2,
            StatusEffectDefinitionBase[] initialStatusEffectDefinitions = null)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(enemy, "_id", "Slime");
            SetPrivateField(enemy, "_maxHp", 30);
            SetPrivateField(enemy, "_minActionCount", min);
            SetPrivateField(enemy, "_maxActionCount", max);
            SetPrivateField(enemy, "_cards", cards);
            SetPrivateField(enemy, "_cardPicker", picker);
            SetPrivateField(enemy, "_initialStatusEffectDefinitions", initialStatusEffectDefinitions);
            return enemy;
        }

        [Test]
        public void PickCards_PassesItsOwnCardsAndActionCountsToThePicker()
        {
            var cards = new[] { NewCard("Attack"), NewCard("Block") };
            var picker = new SpyPicker();
            var enemy = NewEnemy(cards, picker, min: 1, max: 3);

            enemy.PickCards();

            Assert.AreEqual(2, picker.ReceivedCards.Count);
            Assert.AreEqual(1, picker.ReceivedMin);
            Assert.AreEqual(3, picker.ReceivedMax);
        }

        // Inspectorで空欄のままの要素をPicker側に持ち込ませない
        [Test]
        public void PickCards_PassesTheContextToThePicker()
        {
            var picker = new SpyPicker();
            var enemy = NewEnemy(new[] { NewCard("Attack") }, picker);

            enemy.PickCards(new EnemyPickContext(currentHp: 10, maxHp: 40, livingAllyCount: 1, turnNumber: 3, turnsAlone: 2));

            Assert.AreEqual(25, picker.ReceivedContext.HpPercent);
            Assert.AreEqual(1, picker.ReceivedContext.LivingAllyCount);
            Assert.AreEqual(3, picker.ReceivedContext.TurnNumber);
            Assert.AreEqual(2, picker.ReceivedContext.TurnsAlone);
        }

        [Test]
        public void PickCards_DropsNullEntriesBeforeCallingThePicker()
        {
            var card = NewCard("Attack");
            var picker = new SpyPicker();
            var enemy = NewEnemy(new[] { card, null }, picker);

            enemy.PickCards();

            Assert.AreEqual(1, picker.ReceivedCards.Count);
            Assert.AreSame(card, picker.ReceivedCards[0]);
        }

        // min > max のまま保存されていてもPicker側が破綻しないように寄せてから渡す
        [Test]
        public void PickCards_MinGreaterThanMax_ClampsMinToMax()
        {
            var picker = new SpyPicker();
            var enemy = NewEnemy(new[] { NewCard("Attack") }, picker, min: 5, max: 2);

            enemy.PickCards();

            Assert.AreEqual(2, picker.ReceivedMin);
            Assert.AreEqual(2, picker.ReceivedMax);
        }

        [Test]
        public void PickCards_ReturnsWhatThePickerReturned()
        {
            var expected = new[] { NewCard("Attack") };
            var picker = new SpyPicker { Result = expected };
            var enemy = NewEnemy(new[] { NewCard("Attack") }, picker);

            Assert.AreSame(expected, enemy.PickCards());
        }

        // Directorがそのままforeachできるよう、これらのケースでもnullを返さない
        [Test]
        public void PickCards_WithoutPicker_ReturnsEmptyNotNull()
        {
            var enemy = NewEnemy(new[] { NewCard("Attack") }, null);

            Assert.IsNotNull(enemy.PickCards());
            Assert.IsEmpty(enemy.PickCards());
        }

        [Test]
        public void PickCards_PickerReturnedNull_ReturnsEmptyNotNull()
        {
            var picker = new SpyPicker { Result = null };
            var enemy = NewEnemy(new[] { NewCard("Attack") }, picker);

            Assert.IsNotNull(enemy.PickCards());
            Assert.IsEmpty(enemy.PickCards());
        }

        // 呼ぶたびに生成されるので、戦闘中の値の変化がアセット側(定義)に残らない
        [Test]
        public void CreateInitialStatusEffects_CreatesAFreshInstanceEachCall()
        {
            var definition = NewPoisonResistanceDefinition(2);
            var enemy = NewEnemy(new CardData[0], null, initialStatusEffectDefinitions: new[] { definition });

            var first = enemy.CreateInitialStatusEffects();
            Assert.AreEqual(1, first.Count);
            Assert.IsInstanceOf<PoisonResistanceStatusEffect>(first[0]);
            Assert.AreEqual(2, first[0].Value);

            first[0].OnDuplicated(5); // 戦闘中に値が動いたとみなす
            Assert.AreEqual(5, first[0].Value); // 永続効果なので大きい方が残る

            var second = enemy.CreateInitialStatusEffects();
            Assert.AreNotSame(first[0], second[0]);
            Assert.AreEqual(2, second[0].Value); // 定義側は汚れていない
        }

        [Test]
        public void CreateInitialStatusEffects_DropsNullEntries()
        {
            var enemy = NewEnemy(new CardData[0], null,
                initialStatusEffectDefinitions: new StatusEffectDefinitionBase[] { NewPoisonResistanceDefinition(1), null });

            Assert.AreEqual(1, enemy.CreateInitialStatusEffects().Count);
        }

        [Test]
        public void CreateInitialStatusEffects_WithoutStatusEffects_ReturnsEmptyNotNull()
        {
            var enemy = NewEnemy(new CardData[0], null);

            Assert.IsNotNull(enemy.CreateInitialStatusEffects());
            Assert.IsEmpty(enemy.CreateInitialStatusEffects());
        }

        [Test]
        public void Accessors_ExposeSerializedValues()
        {
            var enemy = NewEnemy(new[] { NewCard("Attack") }, new RandomEnemyCardPicker(), min: 1, max: 2);

            Assert.AreEqual("Slime", enemy.Id);
            Assert.AreEqual(30, enemy.MaxHp);
            Assert.AreEqual(1, enemy.MinActionCount);
            Assert.AreEqual(2, enemy.MaxActionCount);
            Assert.AreEqual(1, enemy.Cards.Count);
        }
    }
}
