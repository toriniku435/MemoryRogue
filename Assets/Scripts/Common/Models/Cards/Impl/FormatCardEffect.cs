using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 敵味方すべてのユニットから、コマンドで付いた効果をすべて消す（ブロックも。フォーマット）。
    // 敵が生まれつき持つ性質（無効化・耐性・階層ブースト。IStatusEffect.IsTrait）は消えない。
    // 対象は選ばない。プレイヤーが使うと、実行する側から生きているユニットすべてを受け取る。
    // 事情の分からない場面（敵が使う）では、対象と使用者から消す
    [Serializable]
    public sealed class FormatCardEffect : CardEffectBase, ICommandContextCardEffect
    {
        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            Clear(user);
            if (target != user) Clear(target);
        }

        public void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context)
        {
            foreach (var unit in context.AllUnits) Clear(unit);
        }

        private static void Clear(UnitStatusModel unit)
        {
            unit?.RemoveStatusEffects(effect => !effect.IsTrait);
        }

        // 数値は無い
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
        }
    }
}
