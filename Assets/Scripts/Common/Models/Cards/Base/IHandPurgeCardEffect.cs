namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 積むときにプレイヤーがバッファ（手札）から選んだコマンドをパージし、その数で効く効果（バッファオーバーフロー）。
    /// カードの効果はバッファを知らないので、選ばせるのも預かるのもパージするのも実行する側（BattleCommandQueueUseCase / BattleCommandExecuteUseCase）で、
    /// ここには実際にパージした数を渡して ExecuteWithPurged を呼ぶ（Execute の代わり）。
    /// 敵が使うなどバッファの無い場面では、普通に Execute が呼ばれる
    /// </summary>
    public interface IHandPurgeCardEffect
    {
        /// <summary>パージできるバッファの数の上限</summary>
        int HandPurgeCount { get; }

        /// <param name="purgedCount">実際にパージした数（0..HandPurgeCount）</param>
        void ExecuteWithPurged(UnitStatusModel user, UnitStatusModel target, int purgedCount);
    }
}
