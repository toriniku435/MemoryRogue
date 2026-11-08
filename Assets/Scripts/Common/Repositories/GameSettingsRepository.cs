using System;

namespace Live2DCardRogue.Common.Repositories
{
    // ゲーム全体の設定。シーンをまたいで残る。
    // 起動をまたいで残すのは GameSettingsSaveService（settings.json）で、起動時に読み込み、オプションで反映したときに書き出す。
    // 画面モードと言語は、値を持つだけで画面には当てない（当てるのは Common/Settings/GameSettingsApplier）
    public static class GameSettingsRepository
    {
        public const float MinBattleSpeed = 0.5f;
        public const float MaxBattleSpeed = 3f;
        public const float DefaultBattleSpeed = 1f;

        public const float DefaultVolume = 1f;
        public const bool DefaultFullscreen = true;

        /// <summary>戦闘の進行の速さ。1 が標準で、2 なら行動の間の待ち時間が半分になる</summary>
        public static float BattleSpeed { get; private set; } = DefaultBattleSpeed;

        /// <summary>BGM の音量（0〜1）。音ごとの音量と全体の調整（SoundLibrary）に掛ける</summary>
        public static float BgmVolume { get; private set; } = DefaultVolume;

        /// <summary>効果音の音量（0〜1）</summary>
        public static float SeVolume { get; private set; } = DefaultVolume;

        /// <summary>言語（Locale のコード。"ja"・"en"）。空ならローカライズの既定の選び方に任せる</summary>
        public static string LocaleCode { get; private set; } = "";

        /// <summary>フルスクリーンか（false ならウインドウ）</summary>
        public static bool Fullscreen { get; private set; } = DefaultFullscreen;

        /// <summary>チュートリアルを一度でも見たか。見たことがあれば、新しく始めたときに「見ますか？」を聞かない</summary>
        public static bool HasSeenTutorial { get; private set; }

        public static void SetBattleSpeed(float speed)
        {
            BattleSpeed = Math.Clamp(speed, MinBattleSpeed, MaxBattleSpeed);
        }

        public static void SetBgmVolume(float volume)
        {
            BgmVolume = Math.Clamp(volume, 0f, 1f);
        }

        public static void SetSeVolume(float volume)
        {
            SeVolume = Math.Clamp(volume, 0f, 1f);
        }

        public static void SetLocaleCode(string code)
        {
            LocaleCode = code ?? "";
        }

        public static void SetFullscreen(bool fullscreen)
        {
            Fullscreen = fullscreen;
        }

        public static void SetHasSeenTutorial(bool seen)
        {
            HasSeenTutorial = seen;
        }
    }
}
