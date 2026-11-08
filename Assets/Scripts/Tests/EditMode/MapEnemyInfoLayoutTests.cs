using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // マップの敵詳細ウインドウで、説明の文が枠に収まり切るか。
    // 枠をはみ出すと下が切れて最後まで読めないので、テーブルの全文・全言語を実際のフォント設定で測って確かめる。
    //   - 敵の特徴（EnemyDescription）→ ウインドウの Detail/Trait
    //   - 敵が使うコマンドの効果の説明（CardDescription、実際の数値を差し込んだもの）→ コマンドの行の Description
    // 文を長くしたり、レイアウトを変えたりして落ちたら、文を縮めるか枠を広げる
    public sealed class MapEnemyInfoLayoutTests
    {
        private const string WindowPrefabPath = "Assets/Prefabs/EnemyDetailWindow.prefab";
        private const string CommandRowPrefabPath = "Assets/Prefabs/EnemyCommandRow.prefab";

        // 測るために実体化したもの（TearDown で消す）
        private readonly List<GameObject> _instances = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in _instances) Object.DestroyImmediate(instance);
            _instances.Clear();
        }

        // プレハブのアセットのままでは TMP が初期化されず、どの文も同じ高さに測れてしまう（落ちるべきときに落ちない）。
        // 実体化して、途中の非アクティブな親（ウインドウの Dialog など）も有効にしてから測る
        private TMP_Text LoadText(string prefabPath, string childPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, prefabPath + " が見つからない");
            var instance = Object.Instantiate(prefab);
            _instances.Add(instance);
            var child = instance.transform.Find(childPath);
            Assert.IsNotNull(child, prefabPath + " に " + childPath + " が無い");
            for (var t = child; t != null; t = t.parent) t.gameObject.SetActive(true);
            return child.GetComponent<TMP_Text>();
        }

        private static void AssertFits(TMP_Text text, string value, string label)
        {
            var box = ((RectTransform)text.transform).sizeDelta;
            float height = text.GetPreferredValues(value, box.x, 0f).y;
            Assert.LessOrEqual(height, box.y, $"{label} が枠（{box.x}x{box.y}）に収まらない: 必要な高さ {height} 「{value}」");
        }

        [Test]
        public void EveryEnemyDescription_FitsInWindowTraitBox()
        {
            var trait = LoadText(WindowPrefabPath, "Dialog/Window/Detail/Trait");
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.EnemyDescriptionTable);
            Assert.IsNotNull(collection, LocalizationHelper.EnemyDescriptionTable + " テーブルが見つからない");

            foreach (var table in collection.StringTables)
            {
                foreach (var entry in table.Values)
                {
                    AssertFits(trait, entry.Value, $"{table.LocaleIdentifier.Code} の {entry.Key} の特徴");
                }
            }
        }

        [Test]
        public void EveryEnemyCommandDescription_FitsInCommandRow()
        {
            var description = LoadText(CommandRowPrefabPath, "Description");
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.CardDescriptionTable);
            Assert.IsNotNull(collection, LocalizationHelper.CardDescriptionTable + " テーブルが見つからない");

            foreach (var card in EnemyCards())
            {
                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(card.Id);
                    Assert.IsNotNull(entry, $"{table.LocaleIdentifier.Code} に {card.Id} の説明が無い");
                    string rendered = entry.GetLocalizedString(new object[] { card.CreateDescriptionValues() });
                    AssertFits(description, rendered, $"{table.LocaleIdentifier.Code} の {card.Id} の説明");
                }
            }
        }

        // どれかの敵が使うコマンド（重複なし）
        private static IEnumerable<CardData> EnemyCards()
        {
            return AssetDatabase.FindAssets("t:EnemyData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(enemy => enemy != null && enemy.Cards != null)
                .SelectMany(enemy => enemy.Cards)
                .Where(card => card != null)
                .Distinct();
        }
    }
}
