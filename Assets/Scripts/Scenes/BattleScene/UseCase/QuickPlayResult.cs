namespace Live2DCardRogue.BattleScene.UseCase
{
    /// <summary>BattleCommandQueueUseCase.TryPlayWithoutChoosing の結果</summary>
    public enum QuickPlayResult
    {
        /// <summary>積めない（理由のメッセージを出した）</summary>
        Refused,

        /// <summary>対象を決めて積んだ（パージするコマンドなら、パージの選択に入った）</summary>
        Played,

        /// <summary>敵が複数いて、1体を選ぶ必要がある</summary>
        NeedsTarget,
    }
}
