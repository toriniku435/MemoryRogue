using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.SceneTransition
{
    // チャレンジが終わった（リザルトへ）。どのチャレンジか、到達した階層、踏破したか、それを記録した後の最高到達階層。
    // 記録は送る側（マップ。負けたときはマップが戦闘に渡した関数）が済ませてから作る。リザルトは見せるだけ
    // （ストーリーの成績をマップが確定させてからエンディングへ渡すのと同じ）
    public sealed class ChallengeResultTransitionData : ResultSceneTransitionDataBase
    {
        public ChallengeType Challenge { get; }

        /// <summary>到達した階層（1始まり）。負けたならその階層、踏破したなら最後の階層</summary>
        public int ReachedLayer { get; }

        /// <summary>最後の階層まで踏破したか</summary>
        public bool IsCleared { get; }

        /// <summary>今回を含めた最高到達階層</summary>
        public int BestLayer { get; }

        /// <summary>今回で最高記録を更新したか</summary>
        public bool IsNewRecord { get; }

        /// <param name="previousBest">今回の前の最高到達階層（まだ遊んでいなければ 0）</param>
        public ChallengeResultTransitionData(ChallengeType challenge, int reachedLayer, bool isCleared, int previousBest)
        {
            Challenge = challenge;
            ReachedLayer = reachedLayer < 1 ? 1 : reachedLayer;
            IsCleared = isCleared;
            IsNewRecord = ReachedLayer > previousBest;
            BestLayer = IsNewRecord ? ReachedLayer : previousBest;
        }
    }
}
