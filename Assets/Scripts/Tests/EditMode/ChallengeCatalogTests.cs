using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    // チャレンジの種類と階層数（ChallengeCatalog）
    public sealed class ChallengeCatalogTests
    {
        private const string CatalogPath = "Assets/ScriptableObjects/ChallengeCatalog.asset";

        // 実際のアセット: どのチャレンジも1つずつ並び、エンドレスだけが終わり無し（0）
        [Test]
        public void Asset_ListsEveryChallengeOnce_AndOnlyEndlessHasNoEnd()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);

            var seen = new HashSet<ChallengeType>();
            foreach (var entry in catalog.Challenges)
            {
                Assert.IsTrue(seen.Add(entry.Type), entry.Type.ToString());
                if (entry.Type == ChallengeType.Endless) Assert.AreEqual(0, entry.LayerCount);
                else Assert.Greater(entry.LayerCount, 0, entry.Type.ToString());
            }
            foreach (ChallengeType type in Enum.GetValues(typeof(ChallengeType)))
            {
                Assert.IsTrue(seen.Contains(type), type + " がカタログに無い");
            }
        }

        // 実際のアセット: 今の階層数（ノーマル 30・ハード 60・マスター 100）。調整したら、ここも合わせる
        [Test]
        public void Asset_HasTheAgreedLayerCounts()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(CatalogPath);

            Assert.AreEqual(30, catalog.LayerCountOf(ChallengeType.Normal));
            Assert.AreEqual(60, catalog.LayerCountOf(ChallengeType.Hard));
            Assert.AreEqual(100, catalog.LayerCountOf(ChallengeType.Master));
            Assert.AreEqual(0, catalog.LayerCountOf(ChallengeType.Endless));
        }

        // どのチャレンジにも名前のキーがあり、ほかと重ならない（キーそのものの翻訳は UiTextKeyTests が確かめる）
        [Test]
        public void EveryChallenge_HasItsOwnNameKey()
        {
            var keys = new HashSet<UiTextKey>();
            foreach (ChallengeType type in Enum.GetValues(typeof(ChallengeType)))
            {
                Assert.IsTrue(keys.Add(LocalizationHelper.ToUiTextKey(type)), type.ToString());
            }
        }
    }
}
