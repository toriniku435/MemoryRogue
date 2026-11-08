using UnityEngine;

namespace Live2DCardRogue.Common.Audio
{
    // 音を鳴らす唯一の入口。View・Presenter・UIの部品はここだけを呼び、鳴らし方（SoundPlayer）は知らない
    // （MessageUseCase が MessageToast を包んでいるのと同じ関係）。
    // 再生中でなければ何もしない（EditMode のテストから呼ばれても DontDestroyOnLoad で落ちないように）
    public static class AudioUseCase
    {
        public static void PlaySe(SeId id)
        {
            if (!Application.isPlaying || id == SeId.None) return;
            SoundPlayer.Instance?.PlaySe(id);
        }

        /// <summary>鳴っている途中の効果音を止める（長く溜める音を、演出を飛ばしたときに切るため）</summary>
        public static void StopSe(SeId id)
        {
            if (!Application.isPlaying || id == SeId.None) return;
            SoundPlayer.Instance?.StopSe(id);
        }

        /// <summary>
        /// オプションで音量を動かしている間、設定（GameSettingsRepository）を変えずにその音量で鳴らす（0〜1）。
        /// 編集を終えたら EndVolumePreview で設定の値に戻す
        /// </summary>
        public static void PreviewVolumes(float bgm, float se)
        {
            SoundPlayer.SetVolumePreview(Mathf.Clamp01(bgm), Mathf.Clamp01(se));
        }

        public static void EndVolumePreview()
        {
            SoundPlayer.SetVolumePreview(null, null);
        }

        /// <summary>BGM を切り替える（クロスフェード）。同じ曲なら続きをそのまま流す。None で止める</summary>
        public static void PlayBgm(BgmId id)
        {
            if (!Application.isPlaying) return;
            SoundPlayer.Instance?.PlayBgm(id);
        }
    }
}
