using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.EndingScene.Presenters;
using Live2DCardRogue.EndingScene.Views;

namespace Live2DCardRogue.EndingScene.Initializer
{
    // エンディングのコンポジションルート。成績は受け取るだけで、読み終えたらそのままリザルトへ渡す
    public sealed class EndingSceneInitializer : SceneInitializerBase<EndingSceneTransitionData>
    {
        [SerializeField] private EndingSceneViewContainer _viewContainer;

        [Header("このシーンを単体で再生したときの成績")]
        [SerializeField][Min(0)] private int _remainingTokensMock = 1500;
        [SerializeField][Min(0)] private int _backupBoostCountMock = 0;
        // モックの成績を計算するときのランクの閾値（キャンペーンと同じアセット）
        [SerializeField] private CampaignBalance _balance;

        private EndingPresenter _presenter;
        private CampaignResult _result;

        protected override void Initialize(EndingSceneTransitionData data)
        {
            _result = data?.Result ?? CreateMockResult();
            Debug.Log($"--- エンディング（ルート{_result.Route} / ランク{_result.Rank}）---");

            _presenter = new EndingPresenter(_viewContainer, Continue);
            _presenter.Initialize();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        private CampaignResult CreateMockResult()
        {
            Debug.Log("--- 引き継ぎデータが無いため、Inspectorのモック成績で開始します ---");
            return CampaignResultCalculator.Calculate(_remainingTokensMock, _backupBoostCountMock, _balance.CreateScoreRule());
        }

        private void Continue()
        {
            SceneTransitionUseCase.LoadScene(new ResultSceneTransitionData(_result));
        }
    }
}
