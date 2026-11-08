using System.Collections;
using System.Reflection;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

namespace Live2DCardRogue.Tests.PlayMode
{
    // LocalizationHelperの取得はLocalizationSettingsの初期化(Addressables経由)を必要とし、
    // Edit Modeでは解決できないためここに置いている(EditMode/PlayModeの使い分けはCLAUDE.mdを参照)。
    // テーブルのキーがCardData.Idそのものであることも、このテストが実質的な検証になっている。
    public sealed class LocalizationHelperPlayModeTests
    {
        private Locale _originalLocale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LocalizationSettings.InitializationOperation;
            _originalLocale = LocalizationSettings.SelectedLocale;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 選択ロケールはグローバル状態なので、他のテストに影響しないよう必ず戻す
            LocalizationSettings.SelectedLocale = _originalLocale;
            yield return null;
        }

        private static CardData NewCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            var field = typeof(CardData).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(card, id);
            return card;
        }

        [UnityTest]
        public IEnumerator GetCardName_ReturnsEntryOfSelectedLocale()
        {
            var card = NewCard("Attack");

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
            yield return null;
            Assert.AreEqual("アタック", LocalizationHelper.GetCardName(card));

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            yield return null;
            Assert.AreEqual("Attack", LocalizationHelper.GetCardName(card));
        }

        // 説明文もカード名と同じくCardData.Idがキー(テーブルだけが違う)
        [UnityTest]
        public IEnumerator GetCardDescription_ReturnsEntryOfSelectedLocale()
        {
            var card = NewCard("Attack");

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
            yield return null;
            Assert.AreEqual("6ダメージを与える", LocalizationHelper.GetCardDescription(card));

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            yield return null;
            Assert.AreEqual("Deal 6 damage", LocalizationHelper.GetCardDescription(card));
        }

        // UITextテーブルはキーがUiTextKeyのenum名。enumとテーブルの対応そのものはUiTextKeyTests(EditMode)で見ている
        [UnityTest]
        public IEnumerator GetUiText_ReturnsEntryOfSelectedLocale()
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
            yield return null;
            Assert.AreEqual("山札", LocalizationHelper.GetUiText(UiTextKey.DeckPile));
            Assert.AreEqual("捨て札", LocalizationHelper.GetUiText(UiTextKey.DiscardPile));

            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            yield return null;
            Assert.AreEqual("Deck", LocalizationHelper.GetUiText(UiTextKey.DeckPile));
            Assert.AreEqual("Discard", LocalizationHelper.GetUiText(UiTextKey.DiscardPile));
        }
    }
}
