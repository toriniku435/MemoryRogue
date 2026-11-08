using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardCommandModelTests
    {
        private static CardCommand NewCommand()
        {
            return new CardCommand(
                ScriptableObject.CreateInstance<CardData>(),
                new UnitStatusModel(maxHp: 30, maxStamina: 3));
        }

        [Test]
        public void TryAdd_UpToMaxCount_Succeeds_ThenFails()
        {
            var model = new CardCommandModel(maxCount: 2);

            Assert.IsTrue(model.TryAdd(NewCommand()));
            Assert.IsTrue(model.TryAdd(NewCommand()));
            Assert.IsTrue(model.IsFull);
            Assert.IsFalse(model.TryAdd(NewCommand())); // 上限を超えて積めない
            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public void RemoveAt_ReturnsCommand_AndShrinksList()
        {
            var model = new CardCommandModel(maxCount: 3);
            var first = NewCommand();
            var second = NewCommand();
            model.TryAdd(first);
            model.TryAdd(second);

            var removed = model.RemoveAt(0);

            Assert.AreSame(first, removed);
            Assert.AreEqual(1, model.Count);
            Assert.AreSame(second, model.Commands[0]);
        }

        // 空リストへの実行要求をモデル側で吸収しているので、呼び出し側は件数を見なくてよい
        [Test]
        public void RemoveAt_OutOfRange_ReturnsNull_AndDoesNotNotify()
        {
            var model = new CardCommandModel(maxCount: 3);
            bool notified = false;
            model.OnChanged += () => notified = true;

            Assert.IsNull(model.RemoveAt(0));
            Assert.IsNull(model.RemoveAt(-1));
            Assert.IsFalse(notified);
        }

        [Test]
        public void Swap_ExchangesTwoCommands()
        {
            var model = new CardCommandModel(maxCount: 3);
            var first = NewCommand();
            var second = NewCommand();
            model.TryAdd(first);
            model.TryAdd(second);

            model.Swap(0, 1);

            Assert.AreSame(second, model.Commands[0]);
            Assert.AreSame(first, model.Commands[1]);
        }

        // 端のカードは片側が範囲外になる。Viewはボタンを出さないが、モデル側でも何もしない
        [Test]
        public void Swap_OutOfRange_DoesNothing()
        {
            var model = new CardCommandModel(maxCount: 3);
            var first = NewCommand();
            model.TryAdd(first);
            bool notified = false;
            model.OnChanged += () => notified = true;

            model.Swap(0, -1);
            model.Swap(0, 1);
            model.Swap(0, 0);

            Assert.AreSame(first, model.Commands[0]);
            Assert.IsFalse(notified);
        }

        [Test]
        public void Clear_EmptiesList_AndNotifiesOnlyWhenSomethingWasThere()
        {
            var model = new CardCommandModel(maxCount: 3);
            int notifiedCount = 0;
            model.OnChanged += () => notifiedCount++;

            model.Clear(); // 空のままなら通知しない
            Assert.AreEqual(0, notifiedCount);

            model.TryAdd(NewCommand());
            model.Clear();

            Assert.AreEqual(0, model.Count);
            Assert.AreEqual(2, notifiedCount); // TryAddの分とClearの分
        }
    }
}
