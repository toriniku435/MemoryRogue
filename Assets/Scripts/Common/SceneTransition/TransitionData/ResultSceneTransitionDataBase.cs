namespace Live2DCardRogue.Common.SceneTransition
{
    // リザルトへの入り方（ストーリーの成績とチャレンジの結果）の共通の基底。
    // Initializer はシーンごとに1つの型しか受け取らないので、2種類をこの型でまとめて受け、中で型を見て分ける
    public abstract class ResultSceneTransitionDataBase : SceneTransitionData
    {
        protected ResultSceneTransitionDataBase()
            : base(SceneName.Result)
        {
        }
    }
}
