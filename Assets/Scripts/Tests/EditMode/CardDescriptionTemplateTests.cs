using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // カードの説明文は Smart String で数値の名前（{damage} など）を書き、カードの効果が持つ数値を差し込む。
    // これで数値を調整すれば説明文も追従する。その約束が崩れていないかを、全カード・全言語で確かめる:
    //   - 説明文に数字を直接書いていない（書くと、数値を変えても説明文が古いまま残る）
    //   - 説明文の名前はすべて、そのカードの効果が差し出している（無い名前は画面でエラーになる）
    //   - カードの数値はすべて説明文に出ている（出ていない数値は、変えても誰も気付けない）
    //   - 1枚のカードの中で、効果どうしの名前がぶつかっていない
    public sealed class CardDescriptionTemplateTests
    {
        // {name} と {name:plural:...} の両方を拾う。名前は : の前まで
        private static readonly Regex Placeholder = new Regex(@"\{(\w+)(?::[^{}]*)?\}");

        private static IEnumerable<CardData> AllCards()
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        private static IEnumerable<(CardData card, string locale, string text, bool isSmart)> AllDescriptions()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.CardDescriptionTable);
            foreach (var card in AllCards())
            {
                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(card.Id);
                    if (entry == null) continue; // 登録漏れは CardLocalizationTests が落とす
                    yield return (card, table.LocaleIdentifier.Code, entry.Value, entry.IsSmart);
                }
            }
        }

        [Test]
        public void EveryDescription_IsSmart()
        {
            foreach (var (card, locale, _, isSmart) in AllDescriptions())
            {
                Assert.IsTrue(isSmart, $"{card.Id}/{locale} が Smart String になっていない（数値が差し込まれない）");
            }
        }

        [Test]
        public void NoDescription_WritesNumbersDirectly()
        {
            foreach (var (card, locale, text, _) in AllDescriptions())
            {
                var withoutPlaceholders = Placeholder.Replace(text, "");
                Assert.IsFalse(Regex.IsMatch(withoutPlaceholders, "[0-9０-９]"),
                    $"{card.Id}/{locale} に数字が直接書かれている（{{damage}} のような名前にする）: {text}");
            }
        }

        [Test]
        public void EveryPlaceholder_IsProvidedByTheCard()
        {
            foreach (var (card, locale, text, _) in AllDescriptions())
            {
                var values = card.CreateDescriptionValues();
                foreach (Match m in Placeholder.Matches(text))
                {
                    Assert.IsTrue(values.ContainsKey(m.Groups[1].Value),
                        $"{card.Id}/{locale} の {{{m.Groups[1].Value}}} を差し出す効果が無い: {text}");
                }
            }
        }

        [Test]
        public void EveryValue_AppearsInTheDescription()
        {
            foreach (var (card, locale, text, _) in AllDescriptions())
            {
                var used = new HashSet<string>(Placeholder.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value));
                foreach (var key in card.CreateDescriptionValues().Keys)
                {
                    Assert.IsTrue(used.Contains(key), $"{card.Id}/{locale} に {{{key}}} が出ていない（変えても説明文に表れない）: {text}");
                }
            }
        }

        [Test]
        public void EffectValueNames_DoNotCollideWithinACard()
        {
            foreach (var card in AllCards())
            {
                var seen = new HashSet<string>();
                foreach (var effect in card.Effects.Where(e => e != null))
                {
                    var values = new Dictionary<string, object>();
                    effect.AddDescriptionValues(values);
                    foreach (var key in values.Keys)
                    {
                        Assert.IsTrue(seen.Add(key), $"{card.Id} で {{{key}}} を2つの効果が差し出している（片方が上書きされる）");
                    }
                }
            }
        }

        // 実際に差し込んで、名前が残らず埋まることを確かめる
        [Test]
        public void EveryDescription_RendersWithoutLeftoverPlaceholders()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.CardDescriptionTable);
            foreach (var card in AllCards())
            {
                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(card.Id);
                    if (entry == null) continue;
                    var rendered = entry.GetLocalizedString(new object[] { card.CreateDescriptionValues() });
                    Assert.IsFalse(rendered.Contains("{") || rendered.Contains("}"), $"{card.Id}/{table.LocaleIdentifier.Code}: {rendered}");
                }
            }
        }
    }
}
