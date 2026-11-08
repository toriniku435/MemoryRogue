using System;

namespace Live2DCardRogue.Common.Save
{
    // 設定ファイル（settings.json）の中身。JsonUtility で読み書きするので、フィールドは public のまま。
    // 項目を足すときは、既定値を書いておけば古いファイルを読んでもその値になる（JsonUtility は無い項目を触らない）
    [Serializable]
    public sealed class GameSettingsData
    {
        // 2: 音量・言語・画面モード・チュートリアルを見たか、を足した（1 のファイルはそれぞれ既定値で読める）
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        /// <summary>戦闘の進行の速さ（1 が標準）</summary>
        public float battleSpeed = 1f;

        /// <summary>BGM・効果音の音量（0〜1）</summary>
        public float bgmVolume = 1f;
        public float seVolume = 1f;

        /// <summary>言語（Locale のコード）。空なら既定の選び方</summary>
        public string locale = "";

        public bool fullscreen = true;

        /// <summary>チュートリアルを一度でも見たか（ユーザーデータの削除で戻る）</summary>
        public bool tutorialSeen;
    }
}
