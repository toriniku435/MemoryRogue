using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.ResultScene.Presenters;
using Live2DCardRogue.ResultScene.Views;

namespace Live2DCardRogue.ResultScene.Initializer
{
    // リザルトのコンポジションルート。「タイトルへ」でタイトルに戻る。
    //   ストーリー: マップで確定した成績（ResultSceneTransitionData）を受け取って見せるだけ
    //   チャレンジ: 負けると戦闘からそのまま、最後の階層まで踏破するとマップから、どちらも ChallengeResultTransitionData でここへ来る。
    //              どのチャレンジの何階層かは RunModeRepository と進行のリポジトリにあるので、ここで記録を更新し、中断データを消してから見せる
    public sealed class ResultSceneInitializer : SceneInitializerBase<ResultSceneTransitionDataBase>
    {
        [SerializeField] private ResultSceneViewContainer _viewContainer;

        [Header("このシーンを単体で再生したときの成績")]
        [SerializeField][Min(0)] private int _remainingTokensMock = 1500;
        [SerializeField][Min(0)] private int _backupBoostCountMock = 0;
        // モックの成績を計算するときのランクの閾値（キャンペーンと同じアセット）
        [SerializeField] private CampaignBalance _balance;
        // 単体で再生したときに、チャレンジのリザルトを見せるか（そのチャレンジと到達階層、踏破したか）。記録は書かない
        [SerializeField] private bool _endlessMock;
        [SerializeField] private ChallengeType _challengeMock = ChallengeType.Endless;
        [SerializeField][Min(1)] private int _endlessReachedLayerMock = 23;
        [SerializeField] private bool _challengeClearedMock;

        private PresenterBase _presenter;

        protected override void Initialize(ResultSceneTransitionDataBase data)
        {
            _presenter = CreatePresenter(data);
            _presenter.Initialize();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        private PresenterBase CreatePresenter(ResultSceneTransitionDataBase data)
        {
            switch (data)
            {
                case ResultSceneTransitionData story:
                    return new ResultPresenter(_viewContainer, story.Result, BackToTitle);
                // チャレンジが終わった（踏破ならマップから、負けなら戦闘から）。記録は送る側で済んでいる
                case ChallengeResultTransitionData challenge:
                    return new ChallengeResultPresenter(_viewContainer, challenge, BackToTitle);
            }

            if (_endlessMock)
            {
                Debug.Log("--- 引き継ぎデータが無いため、Inspectorのモック（チャレンジ）で開始します ---");
                return new ChallengeResultPresenter(_viewContainer, new ChallengeResultTransitionData(_challengeMock, _endlessReachedLayerMock, _challengeClearedMock, 0), BackToTitle);
            }

            Debug.Log("--- 引き継ぎデータが無いため、Inspectorのモック成績で開始します ---");
            var result = CampaignResultCalculator.Calculate(_remainingTokensMock, _backupBoostCountMock, _balance.CreateScoreRule());
            return new ResultPresenter(_viewContainer, result, BackToTitle);
        }

        // キャンペーンの状態は、次にタイトルでモードを選んだときに初期化される
        private void BackToTitle()
        {
            SceneTransitionUseCase.LoadScene(new TitleSceneTransitionData());
        }
    }
}
