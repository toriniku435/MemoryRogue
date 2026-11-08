using System.IO;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Live2DCardRogue.Tests.EditMode
{
    // 本物の保存先（persistentDataPath）は使わず、一時フォルダで読み書きする
    public sealed class CampaignSaveServiceTests
    {
        private string _directory;
        private CampaignSaveService _service;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "Live2DCardRogueSaveTests_" + System.Guid.NewGuid().ToString("N"));
            _service = new CampaignSaveService(new SaveFileStorage(_directory));
            ClearRepositories();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
            ClearRepositories();
        }

        private static void ClearRepositories()
        {
            CampaignProgressRepository.Reset();
            CampaignTokenRepository.Clear();
            CampaignUnitRepository.Clear();
            CampaignDeckRepository.Clear();
            RunModeRepository.StartCampaign();
        }

        private static CardCatalog LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptableObjects/CardCatalog.asset");
        }

        [Test]
        public void TryLoadProgress_IsFalse_WhenNothingIsSaved()
        {
            Assert.IsFalse(_service.TryLoadProgress(out _));
        }

        // 保存して、別の状態にしてから戻すと、元の状態がそっくり戻る
        [Test]
        public void SaveThenRestore_RoundTripsEveryRepository()
        {
            var catalog = LoadCatalog();
            var attack = catalog.Find("Attack");
            var block = catalog.Find("Block");
            CampaignProgressRepository.Restore(3, 2);
            CampaignTokenRepository.Initialize(820);
            CampaignUnitRepository.Initialize(160, 12);
            CampaignDeckRepository.Initialize(new[] { attack, attack, block }, new[] { attack, attack, attack, block }, new[] { attack, block, block });

            _service.SaveProgress(stageCount: 10);
            ClearRepositories();

            Assert.IsTrue(_service.TryLoadProgress(out var data));
            Assert.AreEqual(10, data.stageCount);
            CampaignSaveSnapshot.Restore(data, catalog);

            Assert.AreEqual(3, CampaignProgressRepository.CurrentStageIndex);
            Assert.AreEqual(2, CampaignProgressRepository.BackupBoostCount);
            Assert.AreEqual(820, CampaignTokenRepository.Tokens);
            Assert.AreEqual(160, CampaignUnitRepository.MaxHp);
            Assert.AreEqual(12, CampaignUnitRepository.MaxMemory);
            CollectionAssert.AreEqual(new[] { attack, attack, block }, CampaignDeckRepository.Deck);
            Assert.AreEqual(4, CampaignDeckRepository.OwnedCards.Length);
            CollectionAssert.AreEqual(new[] { attack, block, block }, CampaignDeckRepository.PurchasedSingles);
        }

        // 版 1（シングル購入を記録する前）のデータは、シングル購入をまだ何も買っていない扱いで読める
        [Test]
        public void Restore_Version1_HasNoPurchasedSingles()
        {
            var json = "{\"version\":1,\"stageIndex\":2,\"maxHp\":100,\"maxMemory\":10,\"deck\":[\"Attack\"],\"ownedCards\":[\"Attack\"]}";
            var data = JsonUtility.FromJson<CampaignSaveData>(json);

            CampaignSaveSnapshot.Restore(data, LoadCatalog());

            Assert.AreEqual(1, CampaignDeckRepository.Deck.Length);
            Assert.AreEqual(0, CampaignDeckRepository.PurchasedSingles.Length);
        }

        // デッキの一覧とアクティブな番号も、そっくり戻る
        [Test]
        public void SaveThenRestore_RoundTripsEveryDeckAndTheActiveIndex()
        {
            var catalog = LoadCatalog();
            var attack = catalog.Find("Attack");
            var block = catalog.Find("Block");
            CampaignDeckRepository.Initialize(new[] { new[] { attack }, new CardData[0], new[] { block, block } }, 2, new[] { attack, block, block });

            _service.SaveProgress(stageCount: 10);
            ClearRepositories();
            Assert.IsTrue(_service.TryLoadProgress(out var data));
            CampaignSaveSnapshot.Restore(data, catalog);

            Assert.AreEqual(CampaignDeckRepository.DeckSlotCount, CampaignDeckRepository.Decks.Count);
            Assert.AreEqual(2, CampaignDeckRepository.ActiveDeckIndex);
            CollectionAssert.AreEqual(new[] { attack }, CampaignDeckRepository.Decks[0]);
            Assert.AreEqual(0, CampaignDeckRepository.Decks[1].Length);
            CollectionAssert.AreEqual(new[] { block, block }, CampaignDeckRepository.Deck);
        }

        // 版 2（デッキが1つだけだった頃）のデータは、そのデッキを1番目のアクティブなデッキとして読む
        [Test]
        public void Restore_Version2_PutsTheDeckInTheFirstSlot()
        {
            var json = "{\"version\":2,\"stageIndex\":0,\"maxHp\":100,\"maxMemory\":10,\"deck\":[\"Attack\",\"Block\"],\"ownedCards\":[\"Attack\",\"Block\"]}";
            var data = JsonUtility.FromJson<CampaignSaveData>(json);

            CampaignSaveSnapshot.Restore(data, LoadCatalog());

            Assert.AreEqual(0, CampaignDeckRepository.ActiveDeckIndex);
            Assert.AreEqual(2, CampaignDeckRepository.Deck.Length);
            Assert.AreEqual(0, CampaignDeckRepository.Decks[1].Length);
        }

        // 消した・名前を変えたコマンドの ID は飛ばして、読める分だけ戻す
        [Test]
        public void Restore_SkipsUnknownCardIds()
        {
            var data = new CampaignSaveData { deck = new[] { "Attack", "NoSuchCommand" }, ownedCards = new[] { "Attack" }, maxHp = 100, maxMemory = 10 };

            LogAssert.ignoreFailingMessages = true;
            CampaignSaveSnapshot.Restore(data, LoadCatalog());
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, CampaignDeckRepository.Deck.Length);
        }

        [Test]
        public void DeleteProgress_RemovesTheSave()
        {
            _service.SaveProgress(10);
            _service.DeleteProgress();

            Assert.IsFalse(_service.TryLoadProgress(out _));
        }

        // 壊れたファイルは読まない（例外で止まらない）
        [Test]
        public void TryLoadProgress_IsFalse_ForACorruptedFile()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, CampaignSaveService.ProgressFileName), "{ this is not json");

            LogAssert.ignoreFailingMessages = true;
            Assert.IsFalse(_service.TryLoadProgress(out _));
            LogAssert.ignoreFailingMessages = false;
        }

        // 知らない新しい版のデータは、読み違えないよう読まない
        [Test]
        public void TryLoadProgress_IsFalse_ForANewerVersion()
        {
            Directory.CreateDirectory(_directory);
            var json = JsonUtility.ToJson(new CampaignSaveData { version = CampaignSaveData.CurrentVersion + 1 });
            File.WriteAllBytes(Path.Combine(_directory, CampaignSaveService.ProgressFileName), SaveCipher.Default.Encrypt(json));

            Assert.IsFalse(_service.TryLoadProgress(out _));
        }

        [Test]
        public void RecordClear_AppendsRecords()
        {
            var rule = new CampaignScoreRule(1, 1000, new[] { 0, 150, 350, 600, 900, 1200, 1600, 2000 }, CampaignRank.S, CampaignRank.B);
            _service.RecordClear(CampaignResultCalculator.Calculate(1500, 0, rule));
            _service.RecordClear(CampaignResultCalculator.Calculate(200, 1, rule));

            var clears = _service.LoadClearData().clears;
            Assert.AreEqual(2, clears.Count);
            Assert.AreEqual(1500, clears[0].score);
            Assert.AreEqual("S", clears[0].rank);
            Assert.AreEqual("Best", clears[0].route);
            Assert.AreEqual(1, clears[1].backupBoostCount);
        }

        // クリア記録には、どのエピソードをクリアしたかが残る（遊んでいるエピソードは進捗のリポジトリが持つ）
        [Test]
        public void RecordClear_RecordsTheEpisodeBeingPlayed()
        {
            var rule = new CampaignScoreRule(1, 1000, new[] { 0, 150, 350, 600, 900, 1200, 1600, 2000 }, CampaignRank.S, CampaignRank.B);
            CampaignProgressRepository.Reset(episodeIndex: 1);
            _service.RecordClear(CampaignResultCalculator.Calculate(500, 0, rule));

            var data = _service.LoadClearData();
            Assert.AreEqual(1, data.clears[0].episodeIndex);
            Assert.IsTrue(data.HasCleared(1));
            Assert.IsFalse(data.HasCleared(0));
            Assert.IsFalse(data.HasCleared(2));
        }

        // 版 1 のクリア記録（エピソードを持たない）は、エピソード1のクリアとして読める
        [Test]
        public void ClearData_Version1_CountsAsEpisode1()
        {
            var data = JsonUtility.FromJson<CampaignClearData>("{\"version\":1,\"clears\":[{\"score\":900,\"rank\":\"A\"}]}");

            Assert.IsTrue(data.HasCleared(0));
            Assert.IsFalse(data.HasCleared(1));
        }

        // 遊んでいるエピソードも、保存して戻すとそのまま戻る。版 3 以前の進捗はエピソード1として読める
        [Test]
        public void SaveThenRestore_KeepsTheEpisode()
        {
            CampaignProgressRepository.Reset(episodeIndex: 2);
            _service.SaveProgress(stageCount: 10);
            ClearRepositories();

            Assert.IsTrue(_service.TryLoadProgress(out var data));
            CampaignSaveSnapshot.Restore(data, LoadCatalog());
            Assert.AreEqual(2, CampaignProgressRepository.EpisodeIndex);

            var old = JsonUtility.FromJson<CampaignSaveData>("{\"version\":3,\"stageIndex\":4}");
            CampaignSaveSnapshot.Restore(old, LoadCatalog());
            Assert.AreEqual(0, CampaignProgressRepository.EpisodeIndex);
            Assert.AreEqual(4, CampaignProgressRepository.CurrentStageIndex);
        }

        // ユーザーデータの削除: クリア記録が消え、エピソードの解放も最初に戻る
        [Test]
        public void DeleteClearData_RemovesEveryRecord()
        {
            var rule = new CampaignScoreRule(1, 1000, new[] { 0, 150, 350, 600, 900, 1200, 1600, 2000 }, CampaignRank.S, CampaignRank.B);
            _service.RecordClear(CampaignResultCalculator.Calculate(500, 0, rule));
            Assert.IsTrue(_service.LoadClearData().HasCleared(0));

            _service.DeleteClearData();
            Assert.AreEqual(0, _service.LoadClearData().clears.Count);
            Assert.IsFalse(_service.LoadClearData().HasCleared(0));
        }

        // 保存したファイルは暗号化されていて、中身が平文では読めない
        [Test]
        public void SavedFile_IsNotPlainText()
        {
            CampaignTokenRepository.Initialize(820);
            _service.SaveProgress(10);

            var bytes = File.ReadAllBytes(Path.Combine(_directory, CampaignSaveService.ProgressFileName));
            StringAssert.DoesNotContain("tokens", System.Text.Encoding.UTF8.GetString(bytes));
        }

        // 1バイトでも書き換えられたファイルは読まない
        [Test]
        public void TryLoadProgress_IsFalse_ForATamperedFile()
        {
            _service.SaveProgress(10);
            var path = Path.Combine(_directory, CampaignSaveService.ProgressFileName);
            var bytes = File.ReadAllBytes(path);
            bytes[bytes.Length / 2] ^= 0x01;
            File.WriteAllBytes(path, bytes);

            LogAssert.ignoreFailingMessages = true;
            Assert.IsFalse(_service.TryLoadProgress(out _));
            LogAssert.ignoreFailingMessages = false;
        }

        // 暗号化する前の平文の .json も読め、次に保存したら暗号化した .sav へ移って .json は消える
        [Test]
        public void LegacyPlainJson_IsReadThenMigrated()
        {
            Directory.CreateDirectory(_directory);
            var legacy = Path.Combine(_directory, CampaignSaveService.LegacyProgressFileName);
            File.WriteAllText(legacy, JsonUtility.ToJson(new CampaignSaveData { tokens = 777, maxHp = 100, maxMemory = 10 }));

            Assert.IsTrue(_service.TryLoadProgress(out var data));
            Assert.AreEqual(777, data.tokens);

            _service.SaveProgress(10);
            Assert.IsFalse(File.Exists(legacy));
            Assert.IsTrue(File.Exists(Path.Combine(_directory, CampaignSaveService.ProgressFileName)));
        }

        // 暗号化して戻すと元の文字列になる。IV が毎回変わるので、同じ中身でも毎回違うバイト列になる
        [Test]
        public void Cipher_RoundTrips_WithAFreshIvEachTime()
        {
            var cipher = SaveCipher.Default;
            var a = cipher.Encrypt("{\"hello\":1}");
            var b = cipher.Encrypt("{\"hello\":1}");

            CollectionAssert.AreNotEqual(a, b);
            Assert.IsTrue(cipher.TryDecrypt(a, out var text));
            Assert.AreEqual("{\"hello\":1}", text);
        }

        // 別の鍵で作ったファイルは読めない
        [Test]
        public void Cipher_RejectsDataFromAnotherKey()
        {
            var other = new SaveCipher(new byte[] { 1, 2, 3, 4 });

            Assert.IsFalse(SaveCipher.Default.TryDecrypt(other.Encrypt("x"), out _));
        }

        // チャレンジの進捗は別のファイルに書き、ストーリーの進捗を上書きしない
        [Test]
        public void SaveProgress_Challenge_IsSeparateFromTheCampaign()
        {
            CampaignProgressRepository.Restore(2, 0);
            _service.SaveProgress(stageCount: 10);

            RunModeRepository.StartChallenge(ChallengeType.Hard, 60, 777);
            CampaignProgressRepository.Restore(36, 0);
            _service.SaveProgress(stageCount: 60);

            Assert.IsTrue(File.Exists(Path.Combine(_directory, CampaignSaveService.EndlessProgressFileName)));
            Assert.IsTrue(_service.TryLoadProgress(out var campaign));
            Assert.IsFalse(campaign.isEndless);
            Assert.AreEqual(2, campaign.stageIndex);
            Assert.IsTrue(_service.TryLoadProgress(out var challenge, RunMode.Challenge));
            Assert.IsTrue(challenge.isEndless);
            Assert.AreEqual(36, challenge.stageIndex);
            Assert.AreEqual(777, challenge.endlessSeed);
            Assert.AreEqual("Hard", challenge.challengeType);
            Assert.AreEqual(60, challenge.challengeLayerCount);
        }

        // 戻すと、モード・チャレンジ・階層数・シードも戻る（ストーリーの進捗を戻せばストーリーに戻る）
        [Test]
        public void Restore_SetsTheRunModeChallengeAndSeed()
        {
            RunModeRepository.StartChallenge(ChallengeType.Master, 100, 4242);
            CampaignProgressRepository.Restore(12, 0);
            _service.SaveProgress(stageCount: 100);
            RunModeRepository.StartCampaign();

            Assert.IsTrue(_service.TryLoadProgress(out var data, RunMode.Challenge));
            CampaignSaveSnapshot.Restore(data, LoadCatalog());

            Assert.IsTrue(RunModeRepository.IsChallenge);
            Assert.AreEqual(ChallengeType.Master, RunModeRepository.Challenge);
            Assert.AreEqual(100, RunModeRepository.ChallengeLayerCount);
            Assert.AreEqual(4242, RunModeRepository.ChallengeSeed);
            Assert.AreEqual(12, CampaignProgressRepository.CurrentStageIndex);

            CampaignSaveSnapshot.Restore(new CampaignSaveData { version = 4 }, LoadCatalog());
            Assert.IsFalse(RunModeRepository.IsChallenge);
        }

        // 版 5 のエンドレスの中断データ（チャレンジの種類が無い）は、終わりの無いエンドレスとして戻る
        [Test]
        public void Restore_Version5Endless_IsEndlessChallenge()
        {
            CampaignSaveSnapshot.Restore(new CampaignSaveData { version = 5, isEndless = true, endlessSeed = 9 }, LoadCatalog());

            Assert.IsTrue(RunModeRepository.IsChallenge);
            Assert.AreEqual(ChallengeType.Endless, RunModeRepository.Challenge);
            Assert.AreEqual(0, RunModeRepository.ChallengeLayerCount);
            Assert.AreEqual(9, RunModeRepository.ChallengeSeed);
        }

        // チャレンジを終えると、そのチャレンジの最高到達階層が上がり（下がりはしない）、中断データは消える。チャレンジごとに別に残る
        [Test]
        public void FinishChallengeRun_KeepsTheBestPerChallengeAndDeletesTheProgress()
        {
            RunModeRepository.StartChallenge(ChallengeType.Normal, 30, 1);
            _service.SaveProgress(stageCount: 30);

            Assert.AreEqual(0, _service.FinishChallengeRun(ChallengeType.Normal, 15, cleared: false));
            Assert.IsFalse(_service.TryLoadProgress(out _, RunMode.Challenge));
            Assert.AreEqual(15, _service.LoadClearData().FindChallenge("Normal").bestLayer);
            Assert.IsFalse(_service.LoadClearData().FindChallenge("Normal").cleared);

            Assert.AreEqual(15, _service.FinishChallengeRun(ChallengeType.Normal, 9, cleared: false));
            Assert.AreEqual(15, _service.LoadClearData().FindChallenge("Normal").bestLayer);

            Assert.AreEqual(15, _service.FinishChallengeRun(ChallengeType.Normal, 30, cleared: true));
            Assert.AreEqual(30, _service.LoadClearData().FindChallenge("Normal").bestLayer);
            Assert.IsTrue(_service.LoadClearData().FindChallenge("Normal").cleared);

            Assert.AreEqual(0, _service.FinishChallengeRun(ChallengeType.Hard, 4, cleared: false));
            Assert.AreEqual(4, _service.LoadClearData().FindChallenge("Hard").bestLayer);
        }

        // 版 5 以前のエンドレスの最高到達階層は、エンドレスの記録として読める
        [Test]
        public void LoadClearData_MovesTheOldEndlessBestIntoChallengeRecords()
        {
            var json = JsonUtility.ToJson(new CampaignClearData { version = 5, endlessBestLayer = 42 });
            new SaveFileStorage(_directory).WriteBytesAtomic(CampaignSaveService.ClearFileName, SaveCipher.Default.Encrypt(json));

            var data = _service.LoadClearData();

            Assert.AreEqual(42, data.FindChallenge("Endless").bestLayer);
        }

        // ストーリーの中断データを消しても、チャレンジの中断データは残る
        [Test]
        public void DeleteProgress_OnlyDeletesThatMode()
        {
            _service.SaveProgress(stageCount: 10);
            RunModeRepository.StartChallenge(ChallengeType.Endless, 0, 5);
            _service.SaveProgress(stageCount: 0);

            _service.DeleteProgress(RunMode.Campaign);

            Assert.IsFalse(_service.TryLoadProgress(out _));
            Assert.IsTrue(_service.TryLoadProgress(out _, RunMode.Challenge));
        }

        // 進捗を保存すると、所持コマンドが「一度でも手に入れた」記録に残る。所持から外れても記録は消えない
        [Test]
        public void SaveProgress_RecordsDiscoveredCommands()
        {
            var catalog = LoadCatalog();
            var attack = catalog.Find("Attack");
            var heal = catalog.Find("Heal");
            CampaignDeckRepository.Initialize(new[] { attack }, new[] { attack, heal });
            _service.SaveProgress(stageCount: 10);

            CampaignDeckRepository.Initialize(new[] { attack }, new[] { attack });
            _service.SaveProgress(stageCount: 10);

            CollectionAssert.AreEquivalent(new[] { "Attack", "Heal" }, _service.LoadClearData().discoveredCommands);
        }

        // 手に入れたことがあるコマンドには、記録を始める前からの中断データの所持コマンドも数える
        [Test]
        public void LoadDiscoveredCommandIds_IncludesSuspendedRunsOwnedCommands()
        {
            var catalog = LoadCatalog();
            _service.RecordDiscoveredCommands(new[] { catalog.Find("Heal") });
            var json = JsonUtility.ToJson(new CampaignSaveData { ownedCards = new[] { "Attack" } });
            new SaveFileStorage(_directory).WriteBytesAtomic(CampaignSaveService.ProgressFileName, SaveCipher.Default.Encrypt(json));

            var ids = _service.LoadDiscoveredCommandIds();

            Assert.IsTrue(ids.Contains("Heal"));
            Assert.IsTrue(ids.Contains("Attack"));
        }

        // 書き込みは一時ファイル経由。上書きしても一時ファイルは残らない
        [Test]
        public void WriteAtomic_ReplacesWithoutLeavingTempFiles()
        {
            var storage = new SaveFileStorage(_directory);
            storage.WriteAtomic("a.json", "1");
            storage.WriteAtomic("a.json", "2");

            Assert.IsTrue(storage.TryRead("a.json", out var text));
            Assert.AreEqual("2", text);
            Assert.IsFalse(File.Exists(Path.Combine(_directory, "a.json.tmp")));
        }
    }
}
