using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ウインドウはシーンに置かず、シーンの初期化時にここで生成する。
    // 呼び出し元は初期化データを渡すだけで、開閉の口（Handler）が返る。生成したプレハブの IWindowInitializer に
    // データを渡して初期化させ、閉じた状態で置いておく。開くのは各呼び出し元が Handler の Show で行う
    public static class WindowUseCase
    {
        private const string CatalogPath = "WindowCatalog";

        private static WindowCatalog _catalog;

        /// <param name="parent">シーンの PopupCanvas</param>
        public static WindowHandler Create(WindowInitData data, Transform parent)
        {
            var handler = new WindowHandler();
            Build(data, parent, handler, presenter => presenter is IWindowPresenter, nameof(IWindowPresenter));
            return handler;
        }

        /// <param name="parent">シーンの PopupCanvas</param>
        public static WindowHandler<TShowData> Create<TShowData>(WindowInitData<TShowData> data, Transform parent) where TShowData : WindowShowData
        {
            var handler = new WindowHandler<TShowData>();
            Build(data, parent, handler, presenter => presenter is IWindowPresenter<TShowData>, $"IWindowPresenter<{typeof(TShowData).Name}>");
            return handler;
        }

        // 開くときに Handler が Presenter を IWindowPresenter(<TShowData>) として呼ぶので、合わなければ生成の時点で止める
        private static void Build(WindowInitDataBase data, Transform parent, WindowHandlerBase handler, Func<IWindowPresenterBase, bool> canShow, string expected)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var prefab = Catalog.Find(data.Id);
            if (prefab == null) throw new InvalidOperationException($"WindowCatalog に {data.Id} のプレハブがありません");

            var instance = Object.Instantiate(prefab, parent, false);
            var initializer = instance.GetComponentInChildren<IWindowInitializer>(true);
            if (initializer == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"{prefab.name}（{data.Id}）に IWindowInitializer がありません");
            }

            initializer.Initialize(data);
            if (!canShow.Invoke(initializer.Presenter))
            {
                initializer.Dispose();
                Object.Destroy(instance);
                throw new InvalidOperationException($"{prefab.name}（{data.Id}）の Presenter が {expected} を実装していません");
            }
            handler.Bind(instance, initializer);
        }

        private static WindowCatalog Catalog
        {
            get
            {
                if (_catalog == null) _catalog = Resources.Load<WindowCatalog>(CatalogPath);
                if (_catalog == null) throw new InvalidOperationException($"Resources/{CatalogPath} がありません");
                return _catalog;
            }
        }
    }
}
