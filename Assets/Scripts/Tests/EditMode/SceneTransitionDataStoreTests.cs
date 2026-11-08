using Live2DCardRogue.Common.SceneTransition;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 静的な受け渡し口なので、テストの実行順で中身が残らないよう毎回空にしてから始める
    public sealed class SceneTransitionDataStoreTests
    {
        private sealed class DummyParameter : SceneTransitionData
        {
            public int Value;

            public DummyParameter() : base("Dummy")
            {
            }
        }

        private sealed class OtherParameter : SceneTransitionData
        {
            public OtherParameter() : base("Other")
            {
            }
        }

        [SetUp]
        [TearDown]
        public void Clear()
        {
            SceneTransitionDataStore.Clear();
        }

        [Test]
        public void TryConsume_ReturnsWhatWasSet()
        {
            var parameter = new DummyParameter { Value = 7 };
            SceneTransitionDataStore.Set(parameter);

            Assert.IsTrue(SceneTransitionDataStore.TryConsume<DummyParameter>(out var consumed));
            Assert.AreSame(parameter, consumed);
        }

        // 取り出しは1度きり。次のシーンに古いデータが残らないことがこの仕組みの肝
        [Test]
        public void TryConsume_Twice_ReturnsNothingTheSecondTime()
        {
            SceneTransitionDataStore.Set(new DummyParameter());

            Assert.IsTrue(SceneTransitionDataStore.TryConsume<DummyParameter>(out _));
            Assert.IsFalse(SceneTransitionDataStore.TryConsume<DummyParameter>(out var second));
            Assert.IsNull(second);
        }

        [Test]
        public void TryConsume_WithoutAnythingSet_ReturnsFalse()
        {
            Assert.IsFalse(SceneTransitionDataStore.TryConsume<DummyParameter>(out var parameter));
            Assert.IsNull(parameter);
        }

        // 型違いの空振りでは捨てない。複数種類の結果を順に試すシーンがあるため
        [Test]
        public void TryConsume_WithWrongType_ReturnsFalse_AndKeepsTheParameter()
        {
            var parameter = new DummyParameter();
            SceneTransitionDataStore.Set(parameter);

            Assert.IsFalse(SceneTransitionDataStore.TryConsume<OtherParameter>(out _));
            Assert.IsTrue(SceneTransitionDataStore.TryConsume<DummyParameter>(out var consumed));
            Assert.AreSame(parameter, consumed);
        }

        [Test]
        public void Set_Null_IsTreatedAsNothingToConsume()
        {
            SceneTransitionDataStore.Set(null);

            Assert.IsFalse(SceneTransitionDataStore.TryConsume<DummyParameter>(out _));
        }
    }
}
