using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.WindowSystem
{
    // アーカイブ・パージの中身の一覧。どちらを見せるかは開くときに BattlePileShowData で渡す
    public sealed class BattlePileInitData : WindowInitData<BattlePileShowData>
    {
        public override WindowId Id => WindowId.BattlePile;

        /// <summary>アーカイブ（捨て札）</summary>
        public CardPileModel Archive { get; }

        /// <summary>パージ（除外）</summary>
        public CardPileModel Purge { get; }

        public BattlePileInitData(CardPileModel archive, CardPileModel purge)
        {
            Archive = archive;
            Purge = purge;
        }
    }
}
