using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // パージするバッファの選択で、選んだ数と「決定したい」がモデルを通ること
    // （実行ボタンが決定ボタンになり、パネルの決定ボタンと同じ流れで積む）
    public sealed class CardSelectionModelPurgeTests
    {
        private CardData _card;

        [SetUp]
        public void SetUp()
        {
            _card = ScriptableObject.CreateInstance<CardData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_card);
        }

        private CardSelectionModel BeginChoice()
        {
            var model = new CardSelectionModel();
            model.BeginPurgeChoice(_card, new UnitStatusModel(10, 0), capacity: 2);
            return model;
        }

        // 1個も選んでいなければ決定できず、決定を求めても伝わらない
        [Test]
        public void CannotConfirm_UntilSomethingIsChosen()
        {
            var model = BeginChoice();
            int requested = 0;
            model.OnPurgeConfirmRequested += () => requested++;

            Assert.IsFalse(model.CanConfirmPurge);
            Assert.IsFalse(model.RequestPurgeConfirm());
            Assert.AreEqual(0, requested);

            model.SetPurgeChosenCount(1);
            Assert.IsTrue(model.CanConfirmPurge);
            Assert.IsTrue(model.RequestPurgeConfirm());
            Assert.AreEqual(1, requested);
        }

        [Test]
        public void ChosenCount_RaisesOnlyOnChange_AndOnlyWhileChoosing()
        {
            var model = BeginChoice();
            int changed = 0;
            model.OnPurgeChosenCountChanged += () => changed++;

            model.SetPurgeChosenCount(1);
            model.SetPurgeChosenCount(1);
            Assert.AreEqual(1, changed);

            model.Clear();
            Assert.AreEqual(0, model.PurgeChosenCount, "選択を終えたら 0 に戻る");
            model.SetPurgeChosenCount(2);
            Assert.AreEqual(0, model.PurgeChosenCount, "選んでいないときは数を持たない");
            Assert.IsFalse(model.RequestPurgeConfirm());
        }

        // 選び直し（次のパージの選択）は 0 から
        [Test]
        public void NewChoice_StartsFromZero()
        {
            var model = BeginChoice();
            model.SetPurgeChosenCount(2);

            model.BeginPurgeChoice(_card, new UnitStatusModel(10, 0), capacity: 2);
            Assert.AreEqual(0, model.PurgeChosenCount);
            Assert.IsFalse(model.CanConfirmPurge);
        }
    }
}
