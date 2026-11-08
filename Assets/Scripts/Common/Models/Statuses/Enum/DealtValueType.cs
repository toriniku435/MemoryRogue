namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// このユニットが生み出す値の種別。UnitStatusModel.CalculateDealtValue と IStatusEffect.OnDealtValue で使う。
    ///
    /// HpChangeType / StaminaChangeType が「受ける側」の割り込みなのに対し、こちらは「与える側」。
    /// 相手に与えるものも、自分に付与するもの（ブロックなど）も、生み出すのは同じユニットなのでここに含める
    /// </summary>
    public enum DealtValueType
    {
        /// <summary>与える攻撃ダメージ</summary>
        AttackDamage,

        /// <summary>得るブロック量</summary>
        Block,

        /// <summary>与える毒の量</summary>
        Poison,

        /// <summary>得る強化の量</summary>
        Strength,

        /// <summary>その場で回復するHPの量</summary>
        Heal,

        /// <summary>得る再生（継続回復）の量</summary>
        Regeneration,
    }
}
