namespace Live2DCardRogue.Common.SceneTransition
{
    // タイトルへ戻る。タイトルは何も受け取らない（中断データやクリア記録はセーブから読む）ので、中身は持たない
    public sealed class TitleSceneTransitionData : SceneTransitionData
    {
        public TitleSceneTransitionData()
            : base(SceneName.Title)
        {
        }
    }
}
