using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.Common.Models
{
    // バトルログの1件。何が起きたかをデータのまま持ち、文章にはしない
    // （表示の言語が変わっても引き直せるように。文章は BattleLogFormatter が作る）
    public sealed class BattleLogEntry
    {
        public BattleLogKind Kind { get; }

        /// <summary>誰の行動の結果か（色分けに使う）。ターンの区切りでは null</summary>
        public UnitStatusModel Actor { get; }

        /// <summary>主語のユニット（カードの使用者、ダメージを受けた側、状態効果が付いた側など）</summary>
        public UnitStatusModel Subject { get; }

        /// <summary>カードの対象（CardUsed のみ。使用者自身なら null）</summary>
        public UnitStatusModel Target { get; }

        public CardData Card { get; }

        public IStatusEffect Effect { get; }

        /// <summary>ダメージ量・回復量・状態効果の値や増減、ターン数など、種類ごとの数値</summary>
        public int Amount { get; }

        /// <summary>
        /// StatusDetail のみ: 時間制の効果の効きの大きさ（TimedStatusEffectBase.Amount）。Amount には値（残りターン）が入る。
        /// 効果のインスタンスは戦闘中に値が変わるので、記録した時点の値をここに控えておく
        /// </summary>
        public int EffectAmount { get; }

        public BattleLogEntry(BattleLogKind kind, UnitStatusModel actor = null, UnitStatusModel subject = null, UnitStatusModel target = null, CardData card = null, IStatusEffect effect = null, int amount = 0, int effectAmount = 0)
        {
            Kind = kind;
            Actor = actor;
            Subject = subject;
            Target = target;
            Card = card;
            Effect = effect;
            Amount = amount;
            EffectAmount = effectAmount;
        }
    }
}
