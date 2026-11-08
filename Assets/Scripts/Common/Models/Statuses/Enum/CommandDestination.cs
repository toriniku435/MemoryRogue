namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// 実行し終えたコマンドの行き先。UnitStatusModel.CalculateCommandDestination と IStatusEffect.OnCommandDestination で使う。
    /// 行き先を変える効果が重なったときは、効果ごとに「どれより強いか」を決めて返す（パージは戻すより強い）
    /// </summary>
    public enum CommandDestination
    {
        /// <summary>捨て札（アーカイブ）へ。いつもの行き先</summary>
        Discard,

        /// <summary>次のターンの手札へ戻る（ループバック）</summary>
        ReturnToHand,

        /// <summary>パージ（除外）。この戦闘の間、山札に戻ってこない</summary>
        Purge,
    }
}
