using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // 状態効果を1種類足してテーブルへの登録を忘れると、画面に "No translation found ..." が出るまで気付けない。
    // IStatusEffectの実装を型から全部拾って突き合わせておく
    // (テーブルアセットの検査はEditor APIなのでEditMode側)
    public sealed class StatusEffectLocalizationTests
    {
        // 状態効果は値付きコンストラクタしか持たないため、インスタンス化せずEffectId定数から取る
        private static IEnumerable<(string typeName, string id)> AllStatusEffectIds()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IStatusEffect>())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                var field = type.GetField("EffectId", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                yield return (type.Name, field == null ? null : (string)field.GetRawConstantValue());
            }
        }

        // Idはローカライズキーなので、型から辿れる定数として置いておく約束
        [Test]
        public void EveryStatusEffectType_DeclaresEffectIdConstant()
        {
            var types = AllStatusEffectIds().ToList();

            // 何も拾えていないと以降のテストが素通りしてしまうので、最低1件は在ることを担保する
            Assert.IsNotEmpty(types);

            foreach (var (typeName, id) in types)
            {
                Assert.IsFalse(string.IsNullOrEmpty(id), typeName + " に public const string EffectId が無い");
            }
        }

        [TestCase(LocalizationHelper.StatusEffectNameTable)]
        [TestCase(LocalizationHelper.StatusEffectDescriptionTable)]
        [TestCase(LocalizationHelper.StatusEffectLogDetailTable)]
        public void EveryStatusEffectId_HasTranslationInEveryLocale(string tableName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.IsNotNull(collection, tableName + " テーブルが見つからない");

            foreach (var (typeName, id) in AllStatusEffectIds())
            {
                if (string.IsNullOrEmpty(id)) continue; // 上のテストで別途落ちる

                Assert.IsTrue(collection.SharedData.Contains(id), tableName + " にキーが無い: " + id);

                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(id);
                    Assert.IsNotNull(entry, tableName + "/" + table.LocaleIdentifier.Code + " に " + id + " のエントリが無い");
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(entry.Value),
                        tableName + "/" + table.LocaleIdentifier.Code + " の " + id + " が空");
                }
            }
        }
    }
}
