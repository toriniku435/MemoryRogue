using System;
using Live2DCardRogue.Common.Presenters;
using UnityEngine;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ウインドウの Initializer の基底。型引数が、そのウインドウの初期化データと、組み立てる Presenter を表す
    // （SceneInitializerBase<TData> のウインドウ版）。
    //
    // WindowUseCase から渡されたデータを TData に解決して Initialize(TData) を呼び、返った Presenter を初期化してから、
    // 必ず HideImmediate する（生成したウインドウは閉じた状態から始まる。Presenter の初期化に任せない）。
    //
    // Initialize は生成した直後、プレハブの Start より前に呼ばれる。継承先は Start / Awake に頼らず、組み立てはすべて Initialize で行うこと
    public abstract class WindowInitializerBase<TData, TPresenter> : MonoBehaviour, IWindowInitializer
        where TData : WindowInitDataBase
        where TPresenter : PresenterBase, IWindowPresenterBase
    {
        private TPresenter _presenter;

        public IWindowPresenterBase Presenter => _presenter;

        void IWindowInitializer.Initialize(WindowInitDataBase data)
        {
            if (_presenter != null) throw new InvalidOperationException($"{GetType().Name} は初期化済みです");
            if (!(data is TData typed)) throw new ArgumentException($"{GetType().Name} は {typeof(TData).Name} で初期化します（渡されたのは {data?.GetType().Name ?? "null"}）", nameof(data));

            _presenter = Initialize(typed);
            _presenter.Initialize();
            _presenter.HideImmediate();
        }

        /// <summary>このウインドウの Presenter を、プレハブの View（SerializeField）と data から組み立てる。Initialize は基底が呼ぶ</summary>
        protected abstract TPresenter Initialize(TData data);

        public void Dispose()
        {
            _presenter?.Dispose();
            _presenter = null;
        }
    }
}
