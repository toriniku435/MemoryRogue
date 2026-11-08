using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Editor
{
    // Assets/Audio/ に置いた音の取り込み設定。初回（.meta が無いとき）だけ設定するので、
    // Inspector で手直しした設定や、同じ名前で上書きしたときの設定はそのまま残る。
    //   SE : 短いので読み込み時に展開しておく（鳴らすときに待たない）
    //   BGM: 長いのでストリーミングで Vorbis 圧縮（メモリを食わない。ループの継ぎ目も切れない）
    public sealed class AudioImportPostprocessor : AssetPostprocessor
    {
        public const string AudioRoot = "Assets/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot) || !assetImporter.importSettingsMissing) return;
            Apply((AudioImporter)assetImporter);
        }

        public static void Apply(AudioImporter importer)
        {
            bool isBgm = importer.assetPath.Contains("/BGM/");
            var settings = importer.defaultSampleSettings;
            if (isBgm)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = isBgm;
        }
    }
}
