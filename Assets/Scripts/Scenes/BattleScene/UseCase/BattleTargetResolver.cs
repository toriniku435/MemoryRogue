using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // コマンドの対象を決める: 選ぶ余地が無いときの対象、クリックだけで使うときの狙い先、選べる対象か、倒れていたときの振り直し。
    // 敵の並びを知っているのは EnemyPartyModel だけなので、ここは Func で聞くだけで敵を保持しない
    public sealed class BattleTargetResolver
    {
        private readonly UnitStatusModel _player;
        private readonly Func<UnitStatusModel, UnitStatusModel> _findAlternativeTarget;
        // 渡されなければ全体攻撃も選んだ1体にだけ当たる（テストなどで敵の並びを用意しない場合）
        private readonly Func<IReadOnlyList<UnitStatusModel>> _getLivingOpponents;
        private readonly Func<UnitStatusModel> _getPriorityOpponent;

        public BattleTargetResolver(UnitStatusModel player, Func<UnitStatusModel, UnitStatusModel> findAlternativeTarget,
            Func<IReadOnlyList<UnitStatusModel>> getLivingOpponents, Func<UnitStatusModel> getPriorityOpponent)
        {
            _player = player;
            _findAlternativeTarget = findAlternativeTarget;
            _getLivingOpponents = getLivingOpponents;
            _getPriorityOpponent = getPriorityOpponent;
        }

        /// <summary>生きている相手（手前から）。敵の並びを聞けないときは null</summary>
        public IReadOnlyList<UnitStatusModel> LivingOpponents => _getLivingOpponents?.Invoke();

        /// <summary>
        /// 選ぶ余地が無いときの対象。自分に効くカードならプレイヤー、相手に効くカードは、
        /// 1体を選ぶ効果が無い（全体に効く）か、生きている敵が1体だけならその敵。選ぶ必要があれば null
        /// </summary>
        public UnitStatusModel FindAutoTarget(CardData cardData)
        {
            if (!cardData.TargetsOpponent) return _player;

            var living = LivingOpponents;
            if (living == null || living.Count == 0) return null;
            if (living.Count == 1) return living[0];

            foreach (var effect in cardData.Effects)
            {
                if (effect != null && effect.TargetsOpponent && !effect.TargetsAllOpponents) return null;
            }
            return living[0];
        }

        /// <summary>クリックだけで使うときの狙い先: 優先ターゲットが生きていればそれ、無ければ一番手前の敵</summary>
        public UnitStatusModel FindFrontOpponent()
        {
            var living = LivingOpponents;
            if (living == null || living.Count == 0) return null;

            var priority = _getPriorityOpponent?.Invoke();
            if (priority != null)
            {
                foreach (var unit in living)
                {
                    if (unit == priority) return priority;
                }
            }
            return living[0];
        }

        /// <summary>
        /// そのカードの対象としてそのユニットを選べるか。自分に効くカードならプレイヤー自身だけ、相手に効くカードならプレイヤー以外だけ。
        /// 倒れたユニットは選べない
        /// </summary>
        public bool CanTarget(CardData cardData, UnitStatusModel target)
        {
            if (target.CurrentHp == 0) return false;
            return cardData.TargetsOpponent != (target == _player);
        }

        /// <summary>
        /// 倒れた相手に撃ち込まないよう、実行の直前に対象を確かめる。
        /// 代わりが見つからない場合（全滅済みなど）は元の対象のまま撃つ（Director がその前にループを抜ける）
        /// </summary>
        public UnitStatusModel Resolve(UnitStatusModel target)
        {
            if (target.CurrentHp > 0) return target;

            var alternative = _findAlternativeTarget?.Invoke(target);
            if (alternative == null) return target;

            Debug.Log("--- 対象が倒れているため、次の敵に切り替え ---");
            return alternative;
        }

        /// <summary>生きているユニットすべて（プレイヤーが先頭）。敵の並びを聞けないとき（テストなど）は、対象だけを足す</summary>
        public List<UnitStatusModel> AllLivingUnits(UnitStatusModel target)
        {
            var units = new List<UnitStatusModel> { _player };
            var living = LivingOpponents;
            if (living != null) units.AddRange(living);
            else if (target != null && target != _player) units.Add(target);
            return units;
        }
    }
}
