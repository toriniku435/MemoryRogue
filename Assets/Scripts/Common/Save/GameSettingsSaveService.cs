using System;
using System.IO;
using UnityEngine;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Settings;

namespace Live2DCardRogue.Common.Save
{
    // ゲーム全体の設定（GameSettingsRepository）を settings.json に読み書きする。
    // キャンペーンの進捗と違って改ざんされても困らないので、暗号化せず、人が読める JSON のまま置く。
    // 起動時に一度読み込み（LoadOnStartup）、オプションで「反映」したときに書き出す。
    // 保存先は Application.persistentDataPath/save（キャンペーンの保存と同じフォルダ）。テストでは別のフォルダを渡して作る
    public sealed class GameSettingsSaveService
    {
        public const string FileName = "settings.json";

        private static GameSettingsSaveService _default;

        private readonly SaveFileStorage _storage;

        public GameSettingsSaveService(SaveFileStorage storage)
        {
            _storage = storage;
        }

        /// <summary>ゲームが使う保存先（Application.persistentDataPath/save）</summary>
        public static GameSettingsSaveService Default =>
            _default ??= new GameSettingsSaveService(new SaveFileStorage(Path.Combine(Application.persistentDataPath, "save")));

        // どのシーンから始めても、最初のシーンの Initializer より先に設定が入っているように。
        // 画面モードは、読み込んだ値をここで当てる。言語はローカライズの初期化の中で GameSettingsLocaleSelector が選び、
        // 音量は鳴らすたびに読まれる
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadOnStartup()
        {
            Default.Load();
            GameSettingsApplier.ApplyDisplay();
            GameSettingsApplier.ApplyFrameRate();
        }

        /// <summary>
        /// 設定を読み込んでリポジトリへ入れる。ファイルが無い・壊れている・新しすぎるときは何もしない（今の値のまま）。
        /// 読み込めたら true
        /// </summary>
        public bool Load()
        {
            if (!_storage.TryRead(FileName, out var text)) return false;

            GameSettingsData data;
            try
            {
                data = JsonUtility.FromJson<GameSettingsData>(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"設定ファイルを読めませんでした（{FileName}）: {e.Message}");
                return false;
            }

            if (data == null || data.version > GameSettingsData.CurrentVersion) return false;

            GameSettingsRepository.SetBattleSpeed(data.battleSpeed);
            GameSettingsRepository.SetBgmVolume(data.bgmVolume);
            GameSettingsRepository.SetSeVolume(data.seVolume);
            GameSettingsRepository.SetLocaleCode(data.locale);
            GameSettingsRepository.SetFullscreen(data.fullscreen);
            GameSettingsRepository.SetHasSeenTutorial(data.tutorialSeen);
            return true;
        }

        /// <summary>今の設定を書き出す。失敗しても例外は投げない（ログに出して、次に反映したときにまた書く）</summary>
        public void Save()
        {
            var data = new GameSettingsData
            {
                battleSpeed = GameSettingsRepository.BattleSpeed,
                bgmVolume = GameSettingsRepository.BgmVolume,
                seVolume = GameSettingsRepository.SeVolume,
                locale = GameSettingsRepository.LocaleCode,
                fullscreen = GameSettingsRepository.Fullscreen,
                tutorialSeen = GameSettingsRepository.HasSeenTutorial,
            };
            try
            {
                _storage.WriteAtomic(FileName, JsonUtility.ToJson(data, prettyPrint: true));
            }
            catch (Exception e)
            {
                Debug.LogError($"設定を保存できませんでした（{FileName}）: {e.Message}");
            }
        }
    }
}
