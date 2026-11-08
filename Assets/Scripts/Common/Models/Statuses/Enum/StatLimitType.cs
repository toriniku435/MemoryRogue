namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// 状態効果が変えられる「上限・回数」の種別。UnitStatusModel.CalculateLimit と IStatusEffect.OnStatLimit で使う。
    ///
    /// HP・スタミナの増減（受ける側）や与える値（与える側）とは別の量なので、フックも別にしてある。
    /// 上限を持つ量が増えたら、ここに値を1つ足すだけでよい
    /// </summary>
    public enum StatLimitType
    {
        /// <summary>スタミナ（メモリー）の最大値</summary>
        MaxStamina,

        /// <summary>ターン開始時に手札を何枚まで引くか</summary>
        HandSize,

        /// <summary>1ターンに行動できる回数（敵の行動回数の上限。0 なら行動しない）</summary>
        ActionCount,

        /// <summary>次に実行するコマンドを何回繰り返すか（既定は1回。マルチスレッド）</summary>
        CommandRepeat,

        /// <summary>
        /// 引く手札の枚数の頭打ち（既定は制限なし＝int.MaxValue を渡す）。HandSize は「最低1枚」に丸められるが、
        /// こちらはその後に掛かるので 0 にできる（バッファオーバーフローのバッファロック）
        /// </summary>
        DrawCap,

        /// <summary>敵が行動するたびに、グリッチ（自滅の行動）が割り込む確率（%、既定は 0）</summary>
        GlitchChance,
    }
}
