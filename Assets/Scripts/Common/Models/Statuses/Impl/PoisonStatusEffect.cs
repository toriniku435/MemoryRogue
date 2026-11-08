using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    // 持ち主が自分のターンを終えるたびにValue分のダメージを与える永続効果。
    // 発動しても値は減らず、解除手段が実装されるまで残り続ける
    public class PoisonStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Poison";

        public PoisonStatusEffect(int poisonValue) : base(poisonValue)
        {
        }

        public override string Id => EffectId;

        // 重ね掛けは大きい方を残して 1 増やす（強化・継続ダメージの決まり。StatusEffectBase.StackPlusOne）
        public override void OnDuplicated(int value) => StackPlusOne(value);


        // 発動は自分のターン終了時。BattlePhaseDirectorが各ユニットのターン末尾でEndTurn()を呼ぶ
        public override void OnTurnEnd(UnitStatusModel owner)
        {
            if (Value == 0) return;

            // 種別を渡すことで、毒用の割り込み（毒耐性など）だけが適用される
            owner.ChangeHp(HpChangeType.TakePoisonDamage, -Value);
        }
    }
}
