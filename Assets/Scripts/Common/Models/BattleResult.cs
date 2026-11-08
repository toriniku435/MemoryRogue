namespace Live2DCardRogue.Common.Models
{
    // 戦闘の結果。戦闘はこれを作るだけで、どのシーンへどう持って行くかは知らない
    // （BattleSceneTransitionData.CreateReturnData が、戻り先のシーンの SceneTransitionData に包む）
    public sealed class BattleResult
    {
        public bool IsVictory { get; }

        /// <summary>勝利したときに受け取るトークンを何 % 増やすか（マイニング）。増やすのは受け取る側</summary>
        public int TokenBonusPercent { get; }

        /// <summary>戦闘中にコマンドで払ったトークン（勝っても負けても戻らない。差し引くのは受け取る側）</summary>
        public int TokensSpent { get; }

        public BattleResult(bool isVictory, int tokenBonusPercent = 0, int tokensSpent = 0)
        {
            IsVictory = isVictory;
            TokenBonusPercent = tokenBonusPercent < 0 ? 0 : tokenBonusPercent;
            TokensSpent = tokensSpent < 0 ? 0 : tokensSpent;
        }
    }
}
