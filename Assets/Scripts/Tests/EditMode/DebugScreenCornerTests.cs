#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Live2DCardRogue.Common.DebugMenu;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 画面 1920x1080、隅は短い辺の 10%（108px 四方）。座標は左下が原点
    public sealed class DebugScreenCornerTests
    {
        private const float Width = 1920f;
        private const float Height = 1080f;
        private const float Ratio = 0.1f;

        private static char Of(float x, float y) => DebugScreenCorner.Of(x, y, Width, Height, Ratio);

        [Test]
        public void Of_TellsEachCorner()
        {
            Assert.AreEqual(DebugScreenCorner.TopLeft, Of(10f, 1070f));
            Assert.AreEqual(DebugScreenCorner.TopRight, Of(1910f, 1070f));
            Assert.AreEqual(DebugScreenCorner.BottomLeft, Of(10f, 10f));
            Assert.AreEqual(DebugScreenCorner.BottomRight, Of(1910f, 10f));
        }

        // 端ぎりぎりでなくても、隅の大きさの内側なら隅
        [Test]
        public void Of_IncludesTheWholeCornerSquare()
        {
            Assert.AreEqual(DebugScreenCorner.TopLeft, Of(100f, 980f));
        }

        [Test]
        public void Of_IsNoneOutsideTheCorners()
        {
            Assert.AreEqual(DebugScreenCorner.None, Of(960f, 540f));
            // 上の辺の真ん中は、上でも隅ではない
            Assert.AreEqual(DebugScreenCorner.None, Of(960f, 1070f));
            Assert.AreEqual(DebugScreenCorner.None, Of(120f, 1070f));
        }

        // 縦長の画面でも、短い辺（横幅）に対する割合で同じ形の正方形になる
        [Test]
        public void Of_UsesTheShorterSideOnAPortraitScreen()
        {
            Assert.AreEqual(DebugScreenCorner.TopLeft, DebugScreenCorner.Of(100f, 1820f, 1080f, 1920f, Ratio));
            Assert.AreEqual(DebugScreenCorner.None, DebugScreenCorner.Of(100f, 1700f, 1080f, 1920f, Ratio));
        }
    }
}
#endif
