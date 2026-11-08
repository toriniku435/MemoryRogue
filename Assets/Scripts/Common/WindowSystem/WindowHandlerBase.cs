using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 生成した1つのウインドウを、呼び出し元から開閉するための口。WindowUseCase.Create が返し、シーンの OnDestroy で Dispose する。
    // 開く操作は、開くときに要るものの有無で WindowHandler / WindowHandler<TShowData> に分かれる。
    // 開閉の動き・ぼかし・重なり順はプレハブの中の WindowTransition / PopupLayerMember に任せ、ここでは扱わない
    public abstract class WindowHandlerBase : IDisposable
    {
        private GameObject _instance;
        private IWindowInitializer _initializer;

        internal void Bind(GameObject instance, IWindowInitializer initializer)
        {
            _instance = instance;
            _initializer = initializer;
        }

        public bool IsOpen => _initializer.Presenter.IsOpen;

        /// <summary>開閉の動きで閉じる</summary>
        public void Hide()
        {
            _initializer.Presenter.Close();
        }

        protected IWindowPresenterBase Presenter => _initializer.Presenter;

        public void Dispose()
        {
            if (_initializer == null) return;
            _initializer.Dispose();
            _initializer = null;
            // シーンを抜けるときはシーンと一緒に壊れているので、残っているときだけ
            if (_instance != null) Object.Destroy(_instance);
            _instance = null;
        }
    }

    /// <summary>開くときに何も要らないウインドウ</summary>
    public sealed class WindowHandler : WindowHandlerBase
    {
        public void Show()
        {
            ((IWindowPresenter)Presenter).Open();
        }
    }

    /// <summary>開くときに TShowData が要るウインドウ（確認の文言、再開するセーブデータ、流す会話など）</summary>
    public sealed class WindowHandler<TShowData> : WindowHandlerBase where TShowData : WindowShowData
    {
        public void Show(TShowData data)
        {
            ((IWindowPresenter<TShowData>)Presenter).Open(data);
        }
    }
}
