using System;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ウインドウのプレハブに付けるコンポジションルート（シーンの Initializer のウインドウ版）。
    // WindowUseCase が生成した直後に Initialize を呼び、ウインドウはそこを起点に Presenter と View を組み立てる
    public interface IWindowInitializer : IDisposable
    {
        /// <summary>Initialize で組み立てた、ウインドウを動かす Presenter（Handler はこれ越しに開閉する）</summary>
        IWindowPresenterBase Presenter { get; }

        void Initialize(WindowInitDataBase data);
    }
}
