using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Tutorial;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // チュートリアルの手順のアセットと、文言・目印・出来事の突き合わせ。
    // 手を差し込んだり、UI を作り直したりしたときに、画面で「文言が無い」「強調する場所が無い」「進まない」になる前に気付くためのもの
    public sealed class TutorialDataTests
    {
        // 実行時にシーンが登録する、動的な目印の接頭辞（TutorialAnchorRegistry.RegisterResolver）
        private static readonly HashSet<string> DynamicPrefixes = new HashSet<string> { "HandCard", "MapRoom" };

        private static IEnumerable<TutorialSequence> AllSequences()
        {
            return AssetDatabase.FindAssets("t:TutorialSequence")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TutorialSequence>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sequence => sequence != null);
        }

        [Test]
        public void EverySequence_HasSteps()
        {
            var sequences = AllSequences().ToArray();
            Assert.IsNotEmpty(sequences, "チュートリアルの手順のアセットが1つも無い");
            foreach (var sequence in sequences) Assert.IsNotEmpty(sequence.Steps, sequence.name + " に手が無い");
        }

        [Test]
        public void EveryTextKey_HasTextInEveryLocale()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.TutorialTextTable);
            Assert.IsNotNull(collection, LocalizationHelper.TutorialTextTable + " テーブルが見つからない");

            foreach (var sequence in AllSequences())
            {
                foreach (var step in sequence.Steps.Where(s => s.HasText))
                {
                    foreach (var table in collection.StringTables)
                    {
                        var entry = table.GetEntry(step.TextKey);
                        Assert.IsNotNull(entry, $"{sequence.name}: {table.LocaleIdentifier.Code} に {step.TextKey} が無い");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), $"{sequence.name}: {table.LocaleIdentifier.Code} の {step.TextKey} が空");
                    }
                }
            }
        }

        // 出来事を待つ手の名前は、どこかで投げられるもの（TutorialEventNames）でなければ、その手から先へ進めない
        [Test]
        public void EveryEventStep_WaitsForAKnownEvent()
        {
            var known = typeof(TutorialEventNames).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToHashSet();

            foreach (var sequence in AllSequences())
            {
                foreach (var step in sequence.Steps.Where(s => s.Advance == TutorialAdvance.Event))
                {
                    CollectionAssert.Contains(known, step.EventName, $"{sequence.name}: 知らない出来事 {step.EventName}");
                }
            }
        }

        // 待つだけの手（文言なし）は、クリックでは進めない（画面に何も出ないので、出来事で進むしかない）
        [Test]
        public void WaitOnlySteps_AdvanceByEvent()
        {
            foreach (var sequence in AllSequences())
            {
                foreach (var step in sequence.Steps.Where(s => !s.HasText))
                {
                    Assert.AreEqual(TutorialAdvance.Event, step.Advance, $"{sequence.name}: 文言の無い手がクリックで進むことになっている");
                }
            }
        }

        // シーンに置く目印は、そのシーンに同じ ID の TutorialAnchor がある。動的な目印は、知っている接頭辞のもの
        [Test]
        public void EveryAnchor_ExistsInItsScene()
        {
            foreach (var sequence in AllSequences())
            {
                var scenePath = EditorBuildSettings.scenes.Select(s => s.path)
                    .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == sequence.SceneName);
                Assert.IsNotNull(scenePath, $"{sequence.name}: シーン {sequence.SceneName} が Build Settings に無い");
                string sceneText = File.ReadAllText(scenePath);

                foreach (var step in sequence.Steps.Where(s => s.HasAnchor))
                {
                    int separator = step.AnchorId.IndexOf(TutorialAnchorRegistry.PrefixSeparator);
                    if (separator > 0)
                    {
                        CollectionAssert.Contains(DynamicPrefixes, step.AnchorId.Substring(0, separator), $"{sequence.name}: 知らない動的な目印 {step.AnchorId}");
                        continue;
                    }
                    StringAssert.Contains("_anchorId: " + step.AnchorId + "\n", sceneText.Replace("\r\n", "\n"),
                        $"{sequence.name}: {sequence.SceneName} に目印 {step.AnchorId} が無い");
                }
            }
        }

        // 練習の戦闘で「手札の○○」を指す手は、そのコマンドが固定のデッキに入っている
        [Test]
        public void BattleHandCardAnchors_AreInTheTutorialDeck()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CampaignTutorialData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<CampaignTutorialData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsNotNull(data.BattleSequence, data.name + " に戦闘の手順が無い");
                var deckIds = data.Deck.Where(c => c != null).Select(c => c.Id).ToHashSet();
                foreach (var step in data.BattleSequence.Steps.Where(s => s.HasAnchor && s.AnchorId.StartsWith("HandCard:")))
                {
                    CollectionAssert.Contains(deckIds, step.AnchorId.Substring("HandCard:".Length), $"{data.name}: {step.AnchorId} のコマンドがデッキに無い");
                }
            }
        }
    }
}
