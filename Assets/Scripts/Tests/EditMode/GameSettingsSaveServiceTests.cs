using System.IO;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 設定ファイル（settings.json）の読み書き（編集のほうは GameOptionsTests）
    public sealed class GameSettingsSaveServiceTests
    {
        private float _savedSpeed;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _savedSpeed = GameSettingsRepository.BattleSpeed;
            _folder = Path.Combine(Path.GetTempPath(), "L2CR_SettingsTest_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            GameSettingsRepository.SetBattleSpeed(_savedSpeed);
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [Test]
        public void SaveService_RoundTripsTheBattleSpeed()
        {
            var service = new GameSettingsSaveService(new SaveFileStorage(_folder));
            GameSettingsRepository.SetBattleSpeed(2.5f);
            service.Save();

            GameSettingsRepository.SetBattleSpeed(1f);
            Assert.IsTrue(service.Load());
            Assert.AreEqual(2.5f, GameSettingsRepository.BattleSpeed, 0.0001f);
            StringAssert.Contains("battleSpeed", File.ReadAllText(Path.Combine(_folder, GameSettingsSaveService.FileName)), "人が読める JSON のまま");
        }

        [Test]
        public void SaveService_LeavesTheSettingsAlone_WhenTheFileIsMissingOrBroken()
        {
            var service = new GameSettingsSaveService(new SaveFileStorage(_folder));
            GameSettingsRepository.SetBattleSpeed(1.5f);

            Assert.IsFalse(service.Load(), "ファイルが無い");

            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, GameSettingsSaveService.FileName), "{ not json");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("設定ファイルを読めませんでした"));
            Assert.IsFalse(service.Load(), "壊れている（エラーをログに出して、今の値のまま）");

            File.WriteAllText(Path.Combine(_folder, GameSettingsSaveService.FileName), "{\"version\":99,\"battleSpeed\":3}");
            Assert.IsFalse(service.Load(), "新しすぎる版は読まない");

            Assert.AreEqual(1.5f, GameSettingsRepository.BattleSpeed, 0.0001f);
        }

        [Test]
        public void SaveService_ClampsAnOutOfRangeValue()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, GameSettingsSaveService.FileName), "{\"version\":1,\"battleSpeed\":50}");
            var service = new GameSettingsSaveService(new SaveFileStorage(_folder));

            Assert.IsTrue(service.Load());
            Assert.AreEqual(GameSettingsRepository.MaxBattleSpeed, GameSettingsRepository.BattleSpeed, 0.0001f);
        }
    }
}
