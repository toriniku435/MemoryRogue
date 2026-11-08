namespace Live2DCardRogue.Common.Models
{
    // キャンペーンの成績。下から順に並べてあるので、大小で比べられる（rank >= CampaignRank.S など）
    public enum CampaignRank
    {
        E,
        D,
        C,
        B,
        A,
        S,
        SS,
        SSS,
    }
}
