using System.Collections;
using Live2DCardRogue.Common.Presenters;
using NUnit.Framework;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

namespace Live2DCardRogue.Tests.PlayMode
{
    // PresenterBaseのロケール購読/解除を検証する。LocalizationSettingsの初期化(Addressables経由)が要るためPlayMode。
    public sealed class PresenterBasePlayModeTests
    {
        private sealed class SpyPresenter : PresenterBase
        {
            public int InitializeCount;
            public int DisposeCount;
            public int LocaleChangedCount;

            protected override void OnInitialize()
            {
                InitializeCount++;
            }

            protected override void OnDispose()
            {
                DisposeCount++;
            }

            protected override void OnLocaleChanged()
            {
                LocaleChangedCount++;
            }
        }

        private Locale _originalLocale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LocalizationSettings.InitializationOperation;
            _originalLocale = LocalizationSettings.SelectedLocale;

            // 切り替え前の言語を固定しておく(同じロケールを代入しても変更イベントは飛ばないため)
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LocalizationSettings.SelectedLocale = _originalLocale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnLocaleChanged_RunsWhileInitialized_AndStopsAfterDispose()
        {
            var presenter = new SpyPresenter();
            presenter.Initialize();
            Assert.AreEqual(1, presenter.InitializeCount);
            Assert.AreEqual(0, presenter.LocaleChangedCount);

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            yield return null;
            Assert.AreEqual(1, presenter.LocaleChangedCount);

            presenter.Dispose();
            Assert.AreEqual(1, presenter.DisposeCount);

            // Disposeで購読解除できていないと、ここでカウントが増えてしまう
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
            yield return null;
            Assert.AreEqual(1, presenter.LocaleChangedCount);
        }

        [UnityTest]
        public IEnumerator InitializeAndDispose_AreIdempotent()
        {
            var presenter = new SpyPresenter();
            presenter.Initialize();
            presenter.Initialize();
            Assert.AreEqual(1, presenter.InitializeCount);

            presenter.Dispose();
            presenter.Dispose();
            Assert.AreEqual(1, presenter.DisposeCount);

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            yield return null;
            Assert.AreEqual(0, presenter.LocaleChangedCount);
        }
    }
}
