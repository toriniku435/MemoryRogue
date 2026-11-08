using UnityEngine;

namespace Live2DCardRogue.Common.Audio
{
    // 置いたシーンの BGM。シーンが始まったときに AudioUseCase へ切り替えを頼むだけ
    // （どのシーンが何を流すかをシーンの中で見て分かるように、Initializer ではなく部品にしてある）。
    //
    // 曲が遷移データで決まるシーン（ボス戦のバトル）は、Initializer が Override で差し替える。
    // 実行順を遅らせてあるのは、Initializer の Start（= Initialize）より後に鳴らすため。
    // 先に既定の曲を鳴らしてから差し替えると、クロスフェードの途中で前のシーンの曲が途切れる
    [DefaultExecutionOrder(100)]
    public sealed class SceneBgm : MonoBehaviour
    {
        [SerializeField] private BgmId _bgm;

        /// <summary>このシーンで流す曲を差し替える（Start より前に呼ぶ）</summary>
        public void Override(BgmId bgm)
        {
            _bgm = bgm;
        }

        private void Start()
        {
            AudioUseCase.PlayBgm(_bgm);
        }
    }
}
