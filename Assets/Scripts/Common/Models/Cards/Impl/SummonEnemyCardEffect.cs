using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 敵専用。味方の敵（_enemy）を、最大HP _hp で戦場に出す（ランサムウェアのボットネット生成）。
    // カードの効果は2体のユニットしか知らないので、実際に出すのは実行する側（BattleCommandExecuteUseCase）で、
    // ここは何を・どれだけのHPで出すかを持つだけ。出せる枠は戦闘の始めに用意され、倒されたら同じ枠から出し直す
    [Serializable]
    public sealed class SummonEnemyCardEffect : CardEffectBase
    {
        [SerializeField] private EnemyData _enemy;
        [SerializeField][Min(1)] private int _hp = 25;

        public EnemyData Enemy => _enemy;
        public int Hp => _hp;

        // 相手には作用しない（自分の側に味方を出す）
        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
        }

        // 説明文の {summonHp}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["summonHp"] = _hp;
        }
    }
}
