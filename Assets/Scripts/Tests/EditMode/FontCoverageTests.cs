using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.TextCore.LowLevel;

namespace Live2DCardRogue.Tests.EditMode
{
    // UI の文字はすべて DotGothic16 で描き、日本語の代わりに描いてくれるフォールバックフォントは持たない。
    // 字形の無い文字は、エラーも出ずに空白として描かれるだけなので、ローカライズ表に入っている文字を
    // すべて元のフォントファイルで引いてみて、無いものをここで見つける。
    // （フォントアセットに文字を足すと、アセットが書き換わってしまうので、元の .ttf を直接見る）
    public sealed class FontCoverageTests
    {
        private const string FontAssetPath = "Assets/Fonts/DotGothic16/DotGothic16-Regular SDF.asset";

        [Test]
        public void EveryLocalizedCharacter_ExistsInDotGothic16()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Assert.IsNotNull(font, FontAssetPath + " が見つからない");
            Assert.IsNotNull(font.sourceFontFile, "DotGothic16 の元のフォントファイルが設定されていない");

            FontEngine.InitializeFontEngine();
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(font.sourceFontFile, (int)font.faceInfo.pointSize));

            var missing = new SortedSet<string>();
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                foreach (var table in collection.StringTables)
                {
                    foreach (var entry in table.Values)
                    {
                        if (string.IsNullOrEmpty(entry.Value)) continue;

                        foreach (var ch in entry.Value)
                        {
                            if (char.IsControl(ch) || char.IsWhiteSpace(ch)) continue;
                            if (FontEngine.TryGetGlyphWithUnicodeValue(ch, GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph) && glyph.index != 0) continue;

                            missing.Add($"'{ch}' (U+{(int)ch:X4}) in {collection.TableCollectionName}/{table.LocaleIdentifier.Code}/{entry.Key}");
                        }
                    }
                }
            }

            var message = new StringBuilder("DotGothic16 に無い文字:\n");
            foreach (var line in missing) message.AppendLine(line);
            Assert.IsEmpty(missing, message.ToString());
        }
    }
}
