using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    // 画面は横持ちだけ。左右どちらに倒しても回る（SafeAreaFitter が Update でノッチの左右の入れ替わりを追うのはこのため）。
    // 縦持ちを許すと、1920x1080 を基準に組んだ画面がすべて崩れる
    public sealed class ScreenOrientationSettingsTests
    {
        [Test]
        public void OnlyLandscapeIsAllowed()
        {
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
        }
    }
}
