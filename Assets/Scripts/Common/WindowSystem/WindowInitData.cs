namespace Live2DCardRogue.Common.WindowSystem
{
    // ウインドウを生成するときに渡すデータの基底。どのウインドウか（Id）と、そのウインドウの初期化に要るものを持つ
    // （SceneTransitionData がどのシーンへ何を持って入るかを持つのと同じ考え）。
    // 型は Common にだけ置き、Common の型だけで作る（Scenes に依存しないように）。
    //
    // Id を抽象プロパティにしてあるのは、インスタンスを作らなくても型から読めるようにするため（WindowCatalogTests が
    // プレハブの Initializer の受け取る型とカタログの Id を突き合わせる）。定数を返すだけにすること
    public abstract class WindowInitDataBase
    {
        public abstract WindowId Id { get; }
    }

    /// <summary>開くときに何も要らないウインドウの初期化データ。WindowUseCase.Create は WindowHandler を返す</summary>
    public abstract class WindowInitData : WindowInitDataBase
    {
    }

    /// <summary>開くときに TShowData が要るウインドウの初期化データ。WindowUseCase.Create は WindowHandler&lt;TShowData&gt; を返す</summary>
    public abstract class WindowInitData<TShowData> : WindowInitDataBase where TShowData : WindowShowData
    {
    }

    /// <summary>ウインドウを開くときに渡すデータの基底（Handler の Show に渡す）</summary>
    public abstract class WindowShowData
    {
    }
}
