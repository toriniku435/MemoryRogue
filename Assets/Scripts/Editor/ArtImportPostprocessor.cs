using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Editor
{
    // Assets/Art/ に置いた画像を UI 用スプライトとして取り込む。
    // 初回インポート時（.meta が無いとき）だけ設定するので、Inspector で手直しした設定や、
    // 同じ名前で画像を上書きしたときの設定はそのまま残る。
    // 画像は表示サイズの2倍で描いてある（Image.pixelsPerUnitMultiplier = 2 で使う）
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/";

        // 9-slice の枠（元画像のピクセル単位。左, 下, 右, 上）。
        // ArtSource/assets/ui.py で角の大きさを変えたら、ここも合わせる
        private static readonly Dictionary<string, Vector4> SpriteBorders = new Dictionary<string, Vector4>
        {
            { "ui_panel", new Vector4(36, 36, 36, 36) },
            { "ui_tray", new Vector4(40, 40, 40, 40) },
            { "ui_slot", new Vector4(30, 30, 30, 30) },
            // 平行四辺形のボタン。斜めの辺を左右の枠に収め、縦は伸ばさない（高さ＝表示の高さで使う）
            { "ui_button_primary", new Vector4(100, 100, 100, 100) },
            { "ui_button_outline", new Vector4(100, 100, 100, 100) },
            { "ui_button_mask", new Vector4(100, 100, 100, 100) },
            { "ui_bar_bg", new Vector4(24, 22, 24, 22) },
            { "ui_bar_fill", new Vector4(24, 22, 24, 22) },
            { "ui_status_pill", new Vector4(30, 30, 30, 30) },
            { "ui_target_frame", new Vector4(70, 70, 70, 70) },
            { "ui_glow_frame", new Vector4(40, 40, 40, 40) },
            { "ui_toast_panel", new Vector4(36, 30, 30, 30) },
            // 画面の上端・下端のバー。左右の警告色の部分を残し、真ん中だけを画面幅まで伸ばす（縦は伸ばさない）
            { "ui_hazard_bar", new Vector4(400, 0, 400, 0) },
            // シーン遷移の帯。両端の縁だけ残して縦横に伸ばす
            { "ui_transition_slice", new Vector4(12, 4, 12, 4) },
            // 一覧の1行（中身と縁）。左端の帯まで含めて左の枠に収める
            { "ui_row_fill", new Vector4(40, 36, 40, 36) },
            { "ui_row_frame", new Vector4(40, 36, 40, 36) },
            { "ui_tag", new Vector4(22, 22, 22, 22) },
            // 見出しの下線。左の太線と刻みは残し、細い線だけを横に伸ばす（縦は伸ばさない）
            { "ui_section_line", new Vector4(90, 0, 20, 0) },
        };

        // Image.Type.Tiled で敷き詰めるもの（テクスチャ自体を繰り返せるようにしておく）
        private static readonly HashSet<string> TiledSprites = new HashSet<string>
        {
            "ui_mem_pins",
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetImporter.importSettingsMissing) return;
            Apply((TextureImporter)assetImporter);
        }

        public static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            string name = System.IO.Path.GetFileNameWithoutExtension(importer.assetPath);
            importer.wrapMode = TiledSprites.Contains(name) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            if (SpriteBorders.TryGetValue(name, out var border))
            {
                importer.spriteBorder = border;
            }
        }
    }
}
