using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    // 持ち主のターンを決まった回数だけ効いて消える効果の基底。Value は残りターン数。
    //
    // 数えるのは「効いたまま始まった持ち主のターン」だけ。付与されたターンは数えない。
    // こうしておくと、自分のターン中に自分へ付けた効果（手札の増減など）も、
    // 相手のターン中に付けられた効果（停止・回復反転など）も、「次の持ち主のターンから Value ターン」と同じ意味になる。
    //
    // 効きの大きさ（Amount）は付与した時のまま。重ねがけは残りターンの長い方を残すだけで、足し合わせない
    // （停止を重ねて相手を永久に止める、といった積み上げを防ぐため）
    public abstract class TimedStatusEffectBase : StatusEffectBase
    {
        /// <summary>効きの大きさ（メモリーを何増やすか、など）。残りターン数は Value</summary>
        public int Amount { get; }

        private bool _isActiveThisTurn;

        protected TimedStatusEffectBase(int turns, int amount) : base(turns)
        {
            Amount = amount;
        }

        public override void OnDuplicated(int value)
        {
            if (value > Value) Value = value;
        }

        public override void OnTurnStart(UnitStatusModel owner)
        {
            _isActiveThisTurn = true;
        }

        public override void OnTurnEnd(UnitStatusModel owner)
        {
            if (!_isActiveThisTurn) return;

            _isActiveThisTurn = false;
            Value--;
            if (Value <= 0)
            {
                Value = 0;
                IsExpired = true;
            }
        }
    }
}
