namespace Live2DCardRogue.Common.Models
{
    // タイトルの「チャレンジ」で選べるもの。階層数は ChallengeCatalog が決める（エンドレスは終わり無し）。
    // セーブには名前で残すので、並びを変えても古いデータは読める
    public enum ChallengeType
    {
        Normal,
        Hard,
        Master,
        Endless,
    }
}
