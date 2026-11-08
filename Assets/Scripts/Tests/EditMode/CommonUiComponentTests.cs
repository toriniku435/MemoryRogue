using System;
using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Views;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Tests.EditMode
{
    // 画面上のテキスト・ボタンは必ずCommonText/CommonButtonを使う(素のTextMeshProUGUI/Buttonを直接置かない)。
    // 素のまま置いても見た目も挙動も変わらないため、混入に気付けるのはこのテストだけ。
    // プレハブはコンポーネントを直接見る。シーンはEditModeで開くと作業中のシーンを巻き込んでしまうので、
    // 代わりに「素のクラスのMonoScriptに依存していないか」をAssetDatabaseの依存関係で見ている。
    public sealed class CommonUiComponentTests
    {
        [Test]
        public void EveryPrefab_UsesCommonText()
        {
            AssertPrefabsUseCommonType(typeof(TextMeshProUGUI), typeof(CommonText));
        }

        [Test]
        public void EveryPrefab_UsesCommonButton()
        {
            AssertPrefabsUseCommonType(typeof(Button), typeof(CommonButton));
        }

        [Test]
        public void EveryScene_DoesNotReferenceRawUiScripts()
        {
            var rawScriptPaths = new[]
            {
                MonoScriptPath(typeof(TextMeshProUGUI)),
                MonoScriptPath(typeof(Button)),
            };

            foreach (var scenePath in AssetPaths("t:Scene"))
            {
                // 再帰なし。プレハブ内のコンポーネントはプレハブ側のテストが見るので、ここはシーン直置き分だけでよい
                var dependencies = AssetDatabase.GetDependencies(scenePath, false);

                foreach (var rawScriptPath in rawScriptPaths)
                {
                    Assert.IsFalse(
                        dependencies.Contains(rawScriptPath),
                        scenePath + " が " + rawScriptPath + " を参照している(素のコンポーネントが直接置かれている)");
                }
            }
        }

        private static void AssertPrefabsUseCommonType(Type rawType, Type commonType)
        {
            foreach (var prefabPath in AssetPaths("t:Prefab"))
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (root == null) continue;

                foreach (var component in root.GetComponentsInChildren(rawType, true))
                {
                    Assert.IsInstanceOf(
                        commonType,
                        component,
                        prefabPath + " の " + HierarchyPath(component.transform) + " が素の" + rawType.Name);
                }
            }
        }

        private static IEnumerable<string> AssetPaths(string filter)
        {
            return AssetDatabase.FindAssets(filter, new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        // 型からMonoScriptを引く公開APIが無いので、名前で絞ってから実際のクラスで突き合わせる
        private static string MonoScriptPath(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type) return path;
            }

            Assert.Fail(type.Name + " のMonoScriptが見つからない(テスト側の前提が壊れている)");
            return null;
        }

        private static string HierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
