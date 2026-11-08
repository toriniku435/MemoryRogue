namespace Live2DCardRogue.Common.Models
{
    // ランクで分かれるエンディング。どのランクがどのルートかは CampaignResultCalculator.RouteOf が決める
    public enum EndingRoute
    {
        Bad,
        Normal,
        Best,
    }
}
