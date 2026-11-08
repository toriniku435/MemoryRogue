using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 次に実行するコマンドを、使用後に捨て札ではなく次のターンの手札へ戻す（ループバック）
    [Serializable]
    public sealed class LoopbackCardEffect : SelfStatusCardEffectBase
    {
        protected override IStatusEffect CreateStatusEffect() => new LoopbackStatusEffect();

        // 数値を持たない（説明文に差し込むものが無い）
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
        }
    }
}
