using System;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Window.EpisodeSelect.Models
{
    // どのエピソードを選べるかの決まり。エピソード1はいつでも、エピソード N（2以上）はエピソード N-1 をクリアしたら選べる。
    // クリアしたかどうかと中身があるかどうかは渡してもらう（保存ファイルもアセットも知らない）
    public static class EpisodeUnlockRule
    {
        /// <param name="index">エピソード（0始まり）</param>
        /// <param name="hasCleared">そのエピソード（0始まり）をクリアしたことがあるか</param>
        /// <param name="isPlayable">そのエピソード（0始まり）の中身（ステージ）があるか</param>
        public static EpisodeState GetState(int index, Func<int, bool> hasCleared, Func<int, bool> isPlayable)
        {
            if (index > 0 && !hasCleared.Invoke(index - 1)) return EpisodeState.Locked;
            return isPlayable.Invoke(index) ? EpisodeState.Playable : EpisodeState.NotReady;
        }

        /// <summary>選べるようになるために、クリアしなければならないエピソード（0始まり）。条件が無ければ -1</summary>
        public static int RequiredEpisode(int index)
        {
            return index > 0 ? index - 1 : -1;
        }
    }
}
