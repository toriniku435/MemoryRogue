using UnityEngine;
using UnityEngine.SceneManagement;

namespace Live2DCardRogue.Common.SceneTransition
{
    // シーン遷移。引き継ぎデータの置き場所(SceneTransitionDataStore)とロードの順序をここに閉じ込めるので、
    // 呼び出し側は「何を渡すか」だけを書けばよい（行き先はデータ自身が持っている）。
    // 遷移はグリッチの演出（SceneTransitionEffect）で覆って行い、読み込みで固まる瞬間を見せない
    public static class SceneTransitionUseCase
    {
        /// <param name="data">遷移先のInitializerが受け取るデータ。行き先は data.Destination</param>
        public static void LoadScene(SceneTransitionData data)
        {
            if (data == null)
            {
                Debug.LogError("遷移先のデータが null です");
                return;
            }

            // 遷移の途中に呼ばれても（連打など）重ねない。引き継ぎデータも上書きしない
            if (SceneTransitionEffect.IsTransitioning) return;

            SceneTransitionDataStore.Set(data);
            if (!SceneTransitionEffect.TryLoad(data.Destination))
            {
                SceneManager.LoadScene(data.Destination);
            }
        }
    }
}
