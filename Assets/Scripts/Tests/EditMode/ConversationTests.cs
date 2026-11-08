using System.Collections.Generic;
using System.Text;
using Live2DCardRogue.Common.Conversation;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore.LowLevel;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class ConversationRunnerTests
    {
        [Test]
        public void Next_GoesLineByLine_ThenFinishes()
        {
            var runner = new ConversationRunner(3);
            Assert.AreEqual(0, runner.Index);
            Assert.IsFalse(runner.IsFinished);

            Assert.IsTrue(runner.Next());
            Assert.IsTrue(runner.Next());
            Assert.AreEqual(2, runner.Index);

            Assert.IsFalse(runner.Next(), "最後の行の次で読み終わる");
            Assert.IsTrue(runner.IsFinished);
            Assert.IsFalse(runner.Next(), "読み終わった後は進まない");
        }

        [Test]
        public void NoLines_IsFinishedFromTheStart()
        {
            Assert.IsTrue(new ConversationRunner(0).IsFinished);
        }
    }

    // 会話アセットはローカライズの表の外にある文なので、FontCoverageTests が見ない。ここで同じように確かめる
    public sealed class ConversationDataTests
    {
        private const string FontAssetPath = "Assets/Fonts/DotGothic16/DotGothic16-Regular SDF.asset";

        private static IEnumerable<ConversationData> AllConversations()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ConversationData"))
                yield return AssetDatabase.LoadAssetAtPath<ConversationData>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [Test]
        public void EveryConversation_HasAnIdAndNoEmptyLine()
        {
            foreach (var conversation in AllConversations())
            {
                Assert.IsFalse(string.IsNullOrEmpty(conversation.Id), $"{conversation.name}: ID が空");
                for (int i = 0; i < conversation.Lines.Count; i++)
                    Assert.IsFalse(string.IsNullOrEmpty(conversation.Lines[i]?.Text), $"{conversation.name}: {i + 1}行目の台詞が空");
            }
        }

        [Test]
        public void ConversationIds_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (var conversation in AllConversations())
                Assert.IsTrue(seen.Add(conversation.Id), $"会話の ID {conversation.Id} が重なっている（{conversation.name}）");
        }

        [Test]
        public void EveryCharacter_ExistsInDotGothic16()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            FontEngine.InitializeFontEngine();
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(font.sourceFontFile, (int)font.faceInfo.pointSize));

            var missing = new SortedSet<string>();
            foreach (var conversation in AllConversations())
            {
                foreach (var line in conversation.Lines)
                {
                    foreach (var ch in (line?.Speaker ?? "") + (line?.Text ?? ""))
                    {
                        if (char.IsControl(ch) || char.IsWhiteSpace(ch)) continue;
                        if (FontEngine.TryGetGlyphWithUnicodeValue(ch, GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph) && glyph.index != 0) continue;
                        missing.Add($"'{ch}' (U+{(int)ch:X4}) in {conversation.name}");
                    }
                }
            }

            var message = new StringBuilder("DotGothic16 に無い文字:\n");
            foreach (var line in missing) message.AppendLine(line);
            Assert.IsEmpty(missing, message.ToString());
        }

        // エピソード1（キャンペーンの既定のステージ並び）は、始まりと全ステージに会話が付いている
        [Test]
        public void DefaultCampaign_HasAnOpeningAndAClearConversationPerStage()
        {
            var balance = AssetDatabase.LoadAssetAtPath<CampaignBalance>("Assets/ScriptableObjects/CampaignBalance.asset");
            var campaign = balance.GetEpisode(0);
            Assert.IsNotNull(campaign.OpeningConversation, "エピソードの始まりの会話が空欄");
            foreach (var stage in campaign.CreateStages())
                Assert.IsNotNull(stage.ClearConversation, $"{stage.name}: クリア後の会話が空欄");
        }
    }
}
