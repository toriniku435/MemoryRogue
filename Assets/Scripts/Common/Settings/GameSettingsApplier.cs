using UnityEngine;
using UnityEngine.Localization.Settings;
using Live2DCardRogue.Common.Repositories;

namespace Live2DCardRogue.Common.Settings
{
    // 設定（GameSettingsRepository）のうち、エンジンの状態に当てる必要があるもの（画面モード・言語・フレームレート）を当てる入口。
    // 起動時（GameSettingsSaveService が読み込んだ直後）と、オプションで反映したときに呼ぶ。
    // 音量はここでは当てない（SoundPlayer が鳴らすたびに読む）
    public static class GameSettingsApplier
    {
        // ウインドウにしたときの大きさ。画面より一回り小さい中で、いちばん大きい 16:9
        private static readonly Vector2Int[] WindowedSizes =
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(1600, 900),
            new Vector2Int(1280, 720),
        };

        private const int BaseFrameRate = 60;

        /// <summary>
        /// 描画のフレームレートを決める。モバイルは vSyncCount を無視し、targetFrameRate が既定（-1）だと 30fps になって
        /// アニメーション（特にピクセル格子に刻む移動）がガタついて見えるため、明示する。
        /// 画面のリフレッシュレートが 60 の倍数なら 60（120Hz でも等間隔に出る）、そうでなければそのリフレッシュレート
        /// （90Hz に 60 を出すと間隔が揃わずガタつく）。デスクトップは vSyncCount が効いていて、この値は無視される
        /// </summary>
        public static void ApplyFrameRate()
        {
            int refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = refreshRate <= 0 || refreshRate % BaseFrameRate == 0
                ? BaseFrameRate
                : refreshRate;
        }

        /// <summary>
        /// 画面モードをゲームから変えられるか。ブラウザ（WebGL）はフルスクリーンをユーザー操作からしか許さず、
        /// Unity はその要求を「次のクリック」まで持ち越すため、起動時に当てると最初のクリックで突然フルスクリーンになる。
        /// WebGL ではページ側のフルスクリーンボタンに任せる
        /// </summary>
        public static bool CanChangeDisplay => Application.platform != RuntimePlatform.WebGLPlayer;

        /// <summary>フルスクリーン / ウインドウを当てる。エディタと WebGL では何もしない（Game ビューの大きさは変えられない）</summary>
        public static void ApplyDisplay()
        {
            if (Application.isEditor || !CanChangeDisplay) return;

            int displayWidth = Display.main.systemWidth;
            int displayHeight = Display.main.systemHeight;
            if (GameSettingsRepository.Fullscreen)
            {
                if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) return;
                Screen.SetResolution(displayWidth, displayHeight, FullScreenMode.FullScreenWindow);
                return;
            }

            if (Screen.fullScreenMode == FullScreenMode.Windowed) return;
            // 画面いっぱいの大きさのウインドウは枠がはみ出して扱いにくいので、一回り小さいものを選ぶ
            var size = WindowedSizes[WindowedSizes.Length - 1];
            foreach (var candidate in WindowedSizes)
            {
                if (candidate.x < displayWidth && candidate.y < displayHeight)
                {
                    size = candidate;
                    break;
                }
            }
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
        }

        /// <summary>
        /// 言語を当てる（オプションで反映したとき）。空なら何もしない。
        /// 起動時は当てない: ローカライズの初期化がまだなら、初期化の中で GameSettingsLocaleSelector が設定の言語を選ぶ
        /// （初期化の完了の通知から書き換えると、初期化を待つ呼び出しの途中で入れ子になって例外になる）
        /// </summary>
        public static void ApplyLocale()
        {
            string code = GameSettingsRepository.LocaleCode;
            if (string.IsNullOrEmpty(code) || !LocalizationSettings.InitializationOperation.IsDone) return;

            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null)
            {
                Debug.LogWarning($"言語 {code} がありません。今の言語のままにします");
                return;
            }
            if (LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
        }
    }
}
