using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Data
{
    // 実行リストに積まれたカード1枚分。「どのカードを誰に撃つか」だけを持つ。
    // 自分に効くカードも対象にはプレイヤー自身が入るので、Targetがnullになることはない
    public sealed class CardCommand
    {
        private static readonly CardData[] NoCards = new CardData[0];

        public CardData Card { get; }
        public UnitStatusModel Target { get; }

        /// <summary>
        /// 積むときにプレイヤーが選んだ、実行時にパージするバッファ（バッファオーバーフロー）。
        /// 積んだ時点でバッファから抜いて預かり、実行したらパージ、実行リストから戻したらバッファへ返す
        /// </summary>
        public IReadOnlyList<CardData> PurgeCards { get; }

        public CardCommand(CardData card, UnitStatusModel target, IReadOnlyList<CardData> purgeCards = null)
        {
            Card = card;
            Target = target;
            PurgeCards = purgeCards ?? NoCards;
        }
    }
}
