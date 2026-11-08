using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 一つ前に実行したコマンドと同じ効果をもう一度出す（リドゥ）。
    // 何を繰り返すかは実行する側（BattleCommandExecuteUseCase。覚えておくのは CommandHistoryModel）が覚えていて、このコマンドを見つけたら代わりにそれを実行する。
    // この効果そのものは何もしない（事情の分からない場面では何も起きない）
    [Serializable]
    public sealed class RedoCardEffect : CardEffectBase
    {
        // 対象は選ばない（繰り返すコマンドの対象に向ける）
        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
        }

        // 数値は無い（説明文に数字を書かない）
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
        }
    }
}
