using System.IO;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.Models;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // オプション（タイトル・バトル共通）の編集（GameOptionsModel）と、増えた設定項目の settings.json への読み書き
    public sealed class GameOptionsTests
    {
        private static readonly string[] Locales = { "en", "ja" };

        private float _bgm, _se, _speed;
        private string _locale;
        private bool _fullscreen, _seen;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _bgm = GameSettingsRepository.BgmVolume;
            _se = GameSettingsRepository.SeVolume;
            _speed = GameSettingsRepository.BattleSpeed;
            _locale = GameSettingsRepository.LocaleCode;
            _fullscreen = GameSettingsRepository.Fullscreen;
            _seen = GameSettingsRepository.HasSeenTutorial;
            _folder = Path.Combine(Path.GetTempPath(), "L2CR_GameOptionsTest_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            GameSettingsRepository.SetBgmVolume(_bgm);
            GameSettingsRepository.SetSeVolume(_se);
            GameSettingsRepository.SetBattleSpeed(_speed);
            GameSettingsRepository.SetLocaleCode(_locale);
            GameSettingsRepository.SetFullscreen(_fullscreen);
            GameSettingsRepository.SetHasSeenTutorial(_seen);
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private static GameOptionsModel NewModel(string locale = "ja")
        {
            return new GameOptionsModel(1f, 1f, 1f, Locales, locale, true);
        }

        [Test]
        public void Model_IsDirtyOnlyAfterAChange_AndSnapsVolumes()
        {
            var model = NewModel();
            Assert.IsFalse(model.IsDirty);

            model.SetBgmVolume(0.42f);
            Assert.AreEqual(0.4f, model.BgmVolume, 0.0001f, "5% 刻みに丸める");
            Assert.IsTrue(model.IsDirty);

            model.SetBgmVolume(1f);
            Assert.IsFalse(model.IsDirty, "元に戻せば変更なし");

            model.SetFullscreen(false);
            Assert.IsTrue(model.IsDirty);
        }

        // 言語は端まで行くと反対の端へ戻る。知らない言語で開いたら先頭の言語として扱う
        [Test]
        public void Model_StepsThroughLocales()
        {
            var model = NewModel("ja");
            model.StepLocale(1);
            Assert.AreEqual("en", model.LocaleCode);
            model.StepLocale(-1);
            Assert.AreEqual("ja", model.LocaleCode);
            model.StepLocale(-1);
            Assert.AreEqual("en", model.LocaleCode);

            Assert.AreEqual("en", NewModel("fr").LocaleCode);
        }

        // 「デフォルトに戻す」は言語を変えない
        [Test]
        public void Model_ResetToDefault_KeepsTheLanguage()
        {
            var model = new GameOptionsModel(0.3f, 0.2f, 2f, Locales, "en", false);
            Assert.IsFalse(model.IsDefault);

            model.ResetToDefault();
            Assert.IsTrue(model.IsDefault);
            Assert.AreEqual(GameSettingsRepository.DefaultVolume, model.BgmVolume, 0.0001f);
            Assert.AreEqual(GameSettingsRepository.DefaultFullscreen, model.Fullscreen);
            Assert.AreEqual("en", model.LocaleCode);
        }

        [Test]
        public void Model_SnapsTheBattleSpeed_AndStaysInRange()
        {
            var model = NewModel();
            model.SetBattleSpeed(1.37f);
            Assert.AreEqual(1.25f, model.BattleSpeed, 0.0001f);
            model.SetBattleSpeed(10f);
            Assert.AreEqual(GameSettingsRepository.MaxBattleSpeed, model.BattleSpeed, 0.0001f);
            model.SetBattleSpeed(0f);
            Assert.AreEqual(GameSettingsRepository.MinBattleSpeed, model.BattleSpeed, 0.0001f);
        }

        // 編集しても、反映するまで設定そのものは変わらない
        [Test]
        public void Model_DoesNotTouchTheSettings()
        {
            GameSettingsRepository.SetBattleSpeed(2f);
            var model = new GameOptionsModel(1f, 1f, GameSettingsRepository.BattleSpeed, Locales, "ja", true);
            int changed = 0;
            model.OnChanged += () => changed++;

            model.ResetToDefault();
            model.SetBgmVolume(0.5f);

            Assert.AreEqual(2, changed);
            Assert.IsTrue(model.IsDirty);
            Assert.AreEqual(2f, GameSettingsRepository.BattleSpeed, 0.0001f);
            Assert.AreEqual(_bgm, GameSettingsRepository.BgmVolume, 0.0001f);
        }

        [Test]
        public void Settings_RoundTripTheNewFields()
        {
            var service = new GameSettingsSaveService(new SaveFileStorage(_folder));
            GameSettingsRepository.SetBgmVolume(0.35f);
            GameSettingsRepository.SetSeVolume(0.6f);
            GameSettingsRepository.SetLocaleCode("en");
            GameSettingsRepository.SetFullscreen(false);
            GameSettingsRepository.SetHasSeenTutorial(true);
            service.Save();

            GameSettingsRepository.SetBgmVolume(1f);
            GameSettingsRepository.SetSeVolume(1f);
            GameSettingsRepository.SetLocaleCode("");
            GameSettingsRepository.SetFullscreen(true);
            GameSettingsRepository.SetHasSeenTutorial(false);
            Assert.IsTrue(service.Load());

            Assert.AreEqual(0.35f, GameSettingsRepository.BgmVolume, 0.0001f);
            Assert.AreEqual(0.6f, GameSettingsRepository.SeVolume, 0.0001f);
            Assert.AreEqual("en", GameSettingsRepository.LocaleCode);
            Assert.IsFalse(GameSettingsRepository.Fullscreen);
            Assert.IsTrue(GameSettingsRepository.HasSeenTutorial);
        }

        // 版 1 の設定ファイル（戦闘スピードだけ）は、増えた項目を既定値で読む
        [Test]
        public void Settings_Version1_ReadsDefaultsForTheNewFields()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, GameSettingsSaveService.FileName), "{\"version\":1,\"battleSpeed\":2}");
            GameSettingsRepository.SetBgmVolume(0.1f);
            GameSettingsRepository.SetHasSeenTutorial(true);

            Assert.IsTrue(new GameSettingsSaveService(new SaveFileStorage(_folder)).Load());
            Assert.AreEqual(2f, GameSettingsRepository.BattleSpeed, 0.0001f);
            Assert.AreEqual(1f, GameSettingsRepository.BgmVolume, 0.0001f);
            Assert.AreEqual("", GameSettingsRepository.LocaleCode);
            Assert.IsTrue(GameSettingsRepository.Fullscreen);
            Assert.IsFalse(GameSettingsRepository.HasSeenTutorial);
        }

        [Test]
        public void Repository_ClampsVolumes()
        {
            GameSettingsRepository.SetBgmVolume(1.5f);
            GameSettingsRepository.SetSeVolume(-1f);
            Assert.AreEqual(1f, GameSettingsRepository.BgmVolume);
            Assert.AreEqual(0f, GameSettingsRepository.SeVolume);
        }
    }
}
