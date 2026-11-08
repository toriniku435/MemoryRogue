using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 使うのに _tokenCost トークンを払う（クラウドバースト）。払うのは積むときで、負けても戻らない。
    // 支払いは実行する側（BattleCommandQueueUseCase / BattleCommandExecuteUseCase）が行うので、実行したときには何もしない
    [Serializable]
    public sealed class PayTokensCardEffect : CardEffectBase, ITokenCostCardEffect
    {
        [SerializeField][Min(0)] private int _tokenCost = 100;

        public int TokenCost => _tokenCost;

        // 支払いは誰にも作用しない（対象はほかの効果が決める）
        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
        }

        // 説明文の {tokenCost}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["tokenCost"] = _tokenCost;
        }
    }
}
