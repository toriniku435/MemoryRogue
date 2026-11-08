namespace Live2DCardRogue.Common.Statuses
{
    /// <summary>
    /// HP変動の種別。UnitStatusModel.ChangeHp と IStatusEffect.OnHpChange で使う。
    ///
    /// 「どの割り込みを通すか」だけを表し、増減方向は表さない（方向は変動量の符号が持つ）。
    /// HP変動の入口を増やすときは、ここに値を1つ足すだけでよい
    /// </summary>
    public enum HpChangeType
    {
        /// <summary>攻撃によるダメージ</summary>
        TakeAttackDamage,

        /// <summary>毒によるダメージ。攻撃と区別して毒にだけ効く効果を作れるようにするためTakeAttackDamageとは別</summary>
        TakePoisonDamage,

        /// <summary>回復</summary>
        Heal,

        /// <summary>
        /// ブロックを貫通する攻撃のダメージ。ブロックが TakeAttackDamage だけを相殺するので、別の種別にするだけで貫通になる
        /// </summary>
        TakePiercingDamage,

        /// <summary>カードの代償として自分で払うHP。攻撃ではないので、ブロックでも耐性でも減らない</summary>
        PayHp,

        /// <summary>即死（今のHPぶんを減らす）。ブロックでは防げず、即死無効の効果だけがこれを打ち消す</summary>
        InstantKill,
    }
}
