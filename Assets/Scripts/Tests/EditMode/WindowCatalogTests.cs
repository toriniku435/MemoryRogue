using System;
using System.Linq;
using System.Runtime.Serialization;
using Live2DCardRogue.Common.WindowSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // カタログの抜けや取り違えは、そのウインドウを生成したとき（シーンを開いたとき）まで分からないので、ここで先に確かめる
    public sealed class WindowCatalogTests
    {
        private const string CatalogPath = "Assets/Resources/WindowCatalog.asset";

        private static WindowCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WindowCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"{CatalogPath} がありません（WindowUseCase が Resources から読む）");
            return catalog;
        }

        [Test]
        public void EveryWindowId_HasAPrefab()
        {
            var catalog = LoadCatalog();
            foreach (WindowId id in Enum.GetValues(typeof(WindowId)))
            {
                Assert.IsNotNull(catalog.Find(id), $"WindowCatalog に {id} のプレハブがありません");
            }
        }

        [Test]
        public void NoWindowId_IsRegisteredTwice()
        {
            var entries = new SerializedObject(LoadCatalog()).FindProperty("_entries");
            var ids = Enumerable.Range(0, entries.arraySize)
                .Select(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("Id").enumValueIndex)
                .ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "WindowCatalog に同じ WindowId が2つあります（先のものしか使われない）");
        }

        // プレハブの Initializer が受け取る初期化データの Id が、カタログでそのプレハブを引く Id と同じこと。
        // 食い違うと、生成したときに型の違うデータを渡すことになる
        [Test]
        public void EveryPrefab_HasAnInitializerForItsWindowId()
        {
            var catalog = LoadCatalog();
            foreach (WindowId id in Enum.GetValues(typeof(WindowId)))
            {
                var prefab = catalog.Find(id);
                if (prefab == null) continue;

                var initializers = prefab.GetComponentsInChildren<IWindowInitializer>(true);
                Assert.AreEqual(1, initializers.Length, $"{prefab.name}（{id}）には IWindowInitializer が1つだけ要ります");
                Assert.AreSame(prefab, ((Component)initializers[0]).gameObject, $"{prefab.name}（{id}）の IWindowInitializer はプレハブのルートに付けます");

                var dataType = InitDataTypeOf(initializers[0].GetType());
                // Id は定数を返すだけなので、コンストラクタの依存を用意せずに読む
                var data = (WindowInitDataBase)FormatterServices.GetUninitializedObject(dataType);
                Assert.AreEqual(id, data.Id, $"{prefab.name} の {initializers[0].GetType().Name} は {dataType.Name}（{data.Id}）を受け取ります");
            }
        }

        // ウインドウの View への参照（SerializeField）が抜けていると、Initialize で Presenter を組み立てたときに落ちる
        [Test]
        public void EveryInitializer_HasItsReferencesAssigned()
        {
            var catalog = LoadCatalog();
            foreach (WindowId id in Enum.GetValues(typeof(WindowId)))
            {
                var prefab = catalog.Find(id);
                var initializer = prefab != null ? prefab.GetComponent<IWindowInitializer>() as MonoBehaviour : null;
                if (initializer == null) continue;

                var property = new SerializedObject(initializer).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                    Assert.IsNotNull(property.objectReferenceValue, $"{prefab.name} の {initializer.GetType().Name}.{property.name} が未設定です");
                }
            }
        }

        private static Type InitDataTypeOf(Type initializerType)
        {
            for (var t = initializerType; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(WindowInitializerBase<,>)) return t.GetGenericArguments()[0];
            }
            throw new InvalidOperationException($"{initializerType.Name} は WindowInitializerBase<TData, TPresenter> を継承していません");
        }
    }
}
