namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// スタミナ変動の種別。UnitStatusModel.ChangeStamina と IStatusEffect.OnStaminaChange で使う。
    ///
    /// HP変動とはenumもフックも分けてある。1つにまとめるとChangeHpにスタミナの種別を渡せてしまい、
    /// 実行時チェックが必要になるため（分けておけばコンパイル時に弾ける）
    /// </summary>
    public enum StaminaChangeType
    {
        /// <summary>獲得・回復</summary>
        Gain,

        /// <summary>消費</summary>
        Consume,
    }
}
