using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Repositories
{
    public enum RunMode
    {
        // ストーリー（エピソードのステージを順に踏破する）
        Campaign,
        // チャレンジ（ノーマル / ハード / マスター / エンドレス。敵の編成をランダムに組み、負けたら終わり）
        Challenge,
    }

    // 今遊んでいるのがストーリーかチャレンジか、チャレンジならどれか（とその階層数）。タイトルが決め、マップがそれを見てルールを選ぶ。
    // デッキ・トークン・ユニット・進行のリポジトリは両方のモードで共用し、どちらのモードかだけをここに持つ。
    // チャレンジの敵の編成は「シード + 階層」から毎回同じものを組み直すので、保存するのもシードだけでよい。
    // 階層数は始めたときの値を持つ（途中で ChallengeCatalog を調整しても、遊んでいるランの長さは変わらない）
    public static class RunModeRepository
    {
        public static RunMode Mode { get; private set; } = RunMode.Campaign;

        /// <summary>遊んでいるチャレンジ（ストーリーでは使わない）</summary>
        public static ChallengeType Challenge { get; private set; }

        /// <summary>チャレンジの階層数。0 なら終わりが無い（エンドレス）</summary>
        public static int ChallengeLayerCount { get; private set; }

        /// <summary>チャレンジの敵の編成・報酬を組むシード（ストーリーでは使わない）</summary>
        public static int ChallengeSeed { get; private set; }

        public static bool IsChallenge => Mode == RunMode.Challenge;

        public static void StartCampaign()
        {
            Mode = RunMode.Campaign;
            Challenge = ChallengeType.Normal;
            ChallengeLayerCount = 0;
            ChallengeSeed = 0;
        }

        public static void StartChallenge(ChallengeType challenge, int layerCount, int seed)
        {
            Mode = RunMode.Challenge;
            Challenge = challenge;
            ChallengeLayerCount = layerCount < 0 ? 0 : layerCount;
            ChallengeSeed = seed;
        }
    }
}
