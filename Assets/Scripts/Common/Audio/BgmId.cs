namespace Live2DCardRogue.Common.Audio
{
    // BGM の種類。生成スクリプト（SoundSource/music.py）の出力 bgm_<小文字のスネークケース>.wav と対応している。
    // SoundLibrary は値の名前で保存しているが、SceneBgm は数値で保存しているので、値は末尾に足すこと
    public enum BgmId
    {
        /// <summary>BGM を止める</summary>
        None,
        [SoundUsage("タイトル画面")] Title,
        [SoundUsage("キャンペーンマップ")] Map,
        [SoundUsage("バトル")] Battle,
        [SoundUsage("ボスバトル")] BossBattle,
    }
}
