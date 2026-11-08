using Live2DCardRogue.Common.Models;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class PopupStackModelTests
    {
        private sealed class Window { }

        [Test]
        public void Bring_PutsTheWindowInFront()
        {
            var model = new PopupStackModel<Window>();
            var a = new Window();
            var b = new Window();

            model.Bring(a);
            model.Bring(b);

            Assert.AreSame(b, model.Front);
            CollectionAssert.AreEqual(new[] { a, b }, model.Items);
        }

        // もう開いているウインドウを開き直したら、重複させずに手前へ移す
        [Test]
        public void Bring_AnOpenWindow_MovesItToTheFrontWithoutDuplicating()
        {
            var model = new PopupStackModel<Window>();
            var a = new Window();
            var b = new Window();
            model.Bring(a);
            model.Bring(b);

            model.Bring(a);

            CollectionAssert.AreEqual(new[] { b, a }, model.Items);
        }

        // 一番手前を閉じたら、その下が一番手前に戻る
        [Test]
        public void Remove_TheFront_GivesTheFrontBackToTheOneBelow()
        {
            var model = new PopupStackModel<Window>();
            var a = new Window();
            var b = new Window();
            model.Bring(a);
            model.Bring(b);

            Assert.IsTrue(model.Remove(b));

            Assert.AreSame(a, model.Front);
        }

        [Test]
        public void Remove_AWindowNotOpen_ReturnsFalse()
        {
            var model = new PopupStackModel<Window>();
            model.Bring(new Window());

            Assert.IsFalse(model.Remove(new Window()));
            Assert.IsFalse(model.Remove(null));
            Assert.AreEqual(1, model.Items.Count);
        }

        [Test]
        public void Front_WhenEmpty_IsNull()
        {
            Assert.IsNull(new PopupStackModel<Window>().Front);
        }
    }
}
