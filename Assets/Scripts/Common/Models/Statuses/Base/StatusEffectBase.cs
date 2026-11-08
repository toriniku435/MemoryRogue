using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    // 実行時専用の型。アセットへのオーサリングはStatusEffectDefinitionBase側が担うので、
    // ここはUnityEngineに依存しない素のModelのままでよい。
    //
    // 効果はIsExpiredをtrueにしない限り持続する（＝既定では永続）。
    // ワンショット効果は、役目を終えた時点で自分でIsExpiredを立てること（例: BlockStatusEffect）
    public abstract class StatusEffectBase : IStatusEffect
    {
        /// <summary>効果の識別子。ローカライズの見出し・説明を引くキーでもある</summary>
        public abstract string Id { get; }

        public virtual int MaxValue => int.MaxValue;

        public int Value { get; protected set; }

        public bool IsExpired { get; protected set; }

        protected StatusEffectBase(int value)
        {
            Value = value;
        }

        // 既定は素通し。関心のある type だけを見て値を変える
        public virtual int OnHpChange(HpChangeType type, int value) => value;
        public virtual int OnStaminaChange(StaminaChangeType type, int value) => value;
        public virtual int OnDealtValue(DealtValueType type, int value) => value;
        public virtual int OnStatLimit(StatLimitType type, int value) => value;

        public virtual void OnCommandStart() { }
        public virtual CommandDestination OnCommandDestination(CommandDestination destination) => destination;
        public virtual void OnCommandEnd() { }

        public virtual bool BlocksStatusEffect(IStatusEffect incoming) => false;

        public virtual void OnTurnStart(UnitStatusModel owner) { }
        public virtual void OnTurnEnd(UnitStatusModel owner) { }

        /// <summary>
        /// 重複時の既定の振る舞いは、既存の値と新しい値の大きい方を残す（MaxValueで頭打ち）。
        /// 永続効果は加算すると重ね掛けで際限なく伸びるので、これをゲーム全体の決まりにしている。
        /// 決まりの外にあるのは、消費されて尽きるブロック（加算）と、持続ターンを持つ時間制の効果
        /// （TimedStatusEffectBase。長い方の持続を残す）で、それぞれoverrideしている
        /// </summary>
        public virtual void OnDuplicated(int value)
        {
            if (value <= Value) return;

            Value = value > MaxValue ? MaxValue : value;
        }

        /// <summary>
        /// 強化・継続ダメージ（筋力・ウイルス・ワーム）の重ね掛け: 大きい方を残したうえで 1 増やす（MaxValue で頭打ち）。
        /// 重ねるたびに少しずつ伸びるが、加算ほどは伸びない（2026-10-04 のルール変更。それまでは大きい方を残すだけ）
        /// </summary>
        protected void StackPlusOne(int value)
        {
            long next = (long)(value > Value ? value : Value) + 1;
            Value = next > MaxValue ? MaxValue : (int)next;
        }

        /// <summary>既定はコマンドで付いた効果。生まれつきの性質（無効化・耐性など）は override して true にする</summary>
        public virtual bool IsTrait => false;

        public bool IsSameEffect(IStatusEffect other)
        {
            return other != null && Id == other.Id;
        }
    }
}
