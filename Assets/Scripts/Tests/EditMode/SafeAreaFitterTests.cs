using Live2DCardRogue.Common.Views;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class SafeAreaFitterTests
    {
        // ノッチの無い画面では画面全体（置いた物の位置が変わらない）
        [Test]
        public void CalculateAnchors_IsTheWholeScreenWithoutInsets()
        {
            SafeAreaFitter.CalculateAnchors(new Rect(0, 0, 1920, 1080), new Vector2Int(1920, 1080), out var min, out var max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        // 横持ちで左にノッチ（120px）、下にジェスチャーバー（40px）
        [Test]
        public void CalculateAnchors_CutsTheInsets()
        {
            SafeAreaFitter.CalculateAnchors(new Rect(120, 40, 2280, 1040), new Vector2Int(2400, 1080), out var min, out var max);

            Assert.AreEqual(0.05f, min.x, 1e-5f);
            Assert.AreEqual(40f / 1080f, min.y, 1e-5f);
            Assert.AreEqual(1f, max.x, 1e-5f);
            Assert.AreEqual(1f, max.y, 1e-5f);
        }

        [Test]
        public void CalculateAnchors_FallsBackToTheWholeScreenForAnEmptyScreen()
        {
            SafeAreaFitter.CalculateAnchors(new Rect(0, 0, 0, 0), Vector2Int.zero, out var min, out var max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
