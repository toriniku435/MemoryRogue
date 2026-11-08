using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // _chancePercent % の確率で _damage のダメージ。外れたら何も起きない（ブルートフォース）
    [Serializable]
    public sealed class ChanceAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Range(0, 100)] private int _chancePercent = 50;
        [SerializeField][Min(0)] private int _damage = 30;

        /// <summary>当たり外れを決める乱数（テストで差し替えられるように公開している）。Model なので UnityEngine.Random は使わない</summary>
        public static System.Random Random { get; set; } = new System.Random();

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target)
        {
            return Random.Next(100) < _chancePercent ? _damage : 0;
        }

        // 説明文の {chancePercent}, {chanceDamage}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["chancePercent"] = _chancePercent;
            values["chanceDamage"] = _damage;
        }
    }
}
