using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ファーストビルドのウインドウを開くときに渡すもの
    public sealed class FirstBuildShowData : WindowShowData
    {
        /// <summary>開けたチップから出たコマンド。空なら何も見せない</summary>
        public IReadOnlyList<CardData> Cards { get; }

        /// <summary>開けたチップの数（タイトルに出す）</summary>
        public int ChipCount { get; }

        /// <summary>自動でデッキにセットした数（閉じたときに伝える）</summary>
        public int DeckCount { get; }

        public FirstBuildShowData(IReadOnlyList<CardData> cards, int chipCount, int deckCount)
        {
            Cards = cards;
            ChipCount = chipCount;
            DeckCount = deckCount;
        }
    }
}
