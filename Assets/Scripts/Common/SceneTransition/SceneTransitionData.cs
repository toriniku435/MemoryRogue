using System;

namespace Live2DCardRogue.Common.SceneTransition
{
    // シーン遷移で渡すデータの基底。行き先のシーン名（Destination）を自分で持つので、
    // 呼び出し側は SceneTransitionUseCase.LoadScene にこれを渡すだけでよい（行き先とデータの組み合わせを間違えられない）。
    //
    // どの型も行き先は1つに決まっていて、自分のコンストラクタで基底に SceneName を渡す。
    // 行き先が複数ありうるときは、行き先ごとに型を分ける（戦闘の結果は、マップへは MapSceneTransitionData に、
    // チャレンジの敗北でリザルトへは ChallengeResultTransitionData にする。どちらにするかは戦闘を呼んだ側が決める）。
    // 渡すものが無い遷移にも型を用意する（TitleSceneTransitionData など）。どのシーンへ何を持って入るかを、型の一覧で見渡せるようにするため
    //
    // インタフェースでなく抽象クラスなのは、行き先が必ずあることを基底のコンストラクタ1箇所で保証するため
    public abstract class SceneTransitionData
    {
        /// <summary>遷移先のシーン名（SceneName の値）。Build Settings に登録されている必要がある</summary>
        public string Destination { get; }

        protected SceneTransitionData(string destination)
        {
            if (string.IsNullOrEmpty(destination)) throw new ArgumentException("遷移先のシーン名が空です", nameof(destination));
            Destination = destination;
        }
    }
}
