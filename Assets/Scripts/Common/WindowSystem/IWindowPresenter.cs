namespace Live2DCardRogue.Common.WindowSystem
{
    // Handler がウインドウを動かすために Presenter に約束させること。
    // Handler は Common にあり、各ウインドウの Presenter（Window/）の型を知らないので、この形でだけ呼ぶ
    public interface IWindowPresenterBase
    {
        bool IsOpen { get; }

        /// <summary>開閉の動きで閉じる</summary>
        void Close();

        /// <summary>動きなしで閉じる。生成した直後に Handler が必ず呼ぶ</summary>
        void HideImmediate();
    }

    /// <summary>開くときに何も要らないウインドウ</summary>
    public interface IWindowPresenter : IWindowPresenterBase
    {
        void Open();
    }

    /// <summary>開くときに TArg が要るウインドウ</summary>
    public interface IWindowPresenter<in TArg> : IWindowPresenterBase
    {
        void Open(TArg arg);
    }
}
