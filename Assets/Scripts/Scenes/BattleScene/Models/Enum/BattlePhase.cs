namespace Live2DCardRogue.BattleScene.Models
{
    public enum BattlePhase
    {
        Draw,

        /// <summary>プレイヤーの入力待ち。手札のクリックと対象選択ができるのはこの間だけ</summary>
        Player,

        /// <summary>実行ボタンを押したあと、積んだカードを順に処理している間。入力は受け付けない</summary>
        PlayerExecution,

        Enemy
    }
}
