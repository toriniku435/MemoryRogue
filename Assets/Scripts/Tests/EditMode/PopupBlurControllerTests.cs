using Live2DCardRogue.Common.Rendering;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 静的なカウンタなのでテスト間で状態が残る。前後で必ず0に戻しておく
    public sealed class PopupBlurControllerTests
    {
        [SetUp]
        public void SetUp()
        {
            PopupBlurController.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            PopupBlurController.Clear();
        }

        [Test]
        public void IsActive_NothingPushed_IsFalse()
        {
            Assert.IsFalse(PopupBlurController.IsActive);
        }

        [Test]
        public void IsActive_AfterPush_IsTrue()
        {
            PopupBlurController.Push();

            Assert.IsTrue(PopupBlurController.IsActive);
        }

        // ポップアップが重なっている間は、1つ閉じてもぼかしは続く
        [Test]
        public void IsActive_NestedPopups_StaysTrueUntilAllPopped()
        {
            PopupBlurController.Push();
            PopupBlurController.Push();

            PopupBlurController.Pop();
            Assert.IsTrue(PopupBlurController.IsActive);

            PopupBlurController.Pop();
            Assert.IsFalse(PopupBlurController.IsActive);
        }

        // Pushと対にならないPopでカウンタがマイナスに沈むと、
        // 次に開いたポップアップでぼかしが掛からなくなる
        [Test]
        public void Pop_MoreThanPushed_DoesNotGoNegative()
        {
            PopupBlurController.Pop();
            PopupBlurController.Pop();

            PopupBlurController.Push();

            Assert.IsTrue(PopupBlurController.IsActive);
        }

        // 閉じる動きでぼかしを抜き切ったら、パスを積む必要はない
        [Test]
        public void IsActive_StrengthZero_IsFalse()
        {
            PopupBlurController.Push();
            PopupBlurController.Strength = 0f;

            Assert.IsFalse(PopupBlurController.IsActive);
        }

        [Test]
        public void Strength_OutOfRange_IsClamped()
        {
            PopupBlurController.Strength = 2f;
            Assert.AreEqual(1f, PopupBlurController.Strength);

            PopupBlurController.Strength = -1f;
            Assert.AreEqual(0f, PopupBlurController.Strength);
        }

        // 抜き切った強さが残っていると、次に開いたポップアップがぼけずに始まる
        [Test]
        public void Strength_AfterLastPop_ResetsToFull()
        {
            PopupBlurController.Push();
            PopupBlurController.Strength = 0f;

            PopupBlurController.Pop();

            Assert.AreEqual(1f, PopupBlurController.Strength);
        }

        [Test]
        public void Clear_WhileSeveralPopupsOpen_TurnsBlurOff()
        {
            PopupBlurController.Push();
            PopupBlurController.Push();

            PopupBlurController.Clear();

            Assert.IsFalse(PopupBlurController.IsActive);
        }

        // 範囲の強さは 0..1 に収め、画面全体の強さとは別に持つ
        [Test]
        public void SetRegion_KeepsItsOwnClampedStrength()
        {
            PopupBlurController.SetRegion(new UnityEngine.Vector4(0.2f, 0.2f, 0.8f, 0.8f), 2f);

            Assert.IsTrue(PopupBlurController.HasRegion);
            Assert.AreEqual(1f, PopupBlurController.RegionStrength);
            Assert.AreEqual(1f, PopupBlurController.Strength);

            PopupBlurController.SetRegion(PopupBlurController.Region, -1f);
            Assert.AreEqual(0f, PopupBlurController.RegionStrength);
        }

        [Test]
        public void ClearRegion_AndClear_TurnTheRegionOff()
        {
            PopupBlurController.SetRegion(new UnityEngine.Vector4(0f, 0f, 1f, 1f), 0.5f);
            PopupBlurController.ClearRegion();
            Assert.IsFalse(PopupBlurController.HasRegion);

            PopupBlurController.SetRegion(new UnityEngine.Vector4(0f, 0f, 1f, 1f), 0.5f);
            PopupBlurController.Clear();
            Assert.IsFalse(PopupBlurController.HasRegion);
        }

        [Test]
        public void OpenCount_FollowsPushAndPop()
        {
            PopupBlurController.Push();
            PopupBlurController.Push();
            Assert.AreEqual(2, PopupBlurController.OpenCount);

            PopupBlurController.Pop();
            Assert.AreEqual(1, PopupBlurController.OpenCount);
        }
    }
}
