namespace Live2DCardRogue.Common.WindowSystem
{
    // 戦闘の山の一覧に出す山
    public enum BattlePileKind
    {
        Archive,
        Purge,
    }

    // 山の一覧を開くときに渡すもの
    public sealed class BattlePileShowData : WindowShowData
    {
        public BattlePileKind Pile { get; }

        public BattlePileShowData(BattlePileKind pile)
        {
            Pile = pile;
        }
    }
}
