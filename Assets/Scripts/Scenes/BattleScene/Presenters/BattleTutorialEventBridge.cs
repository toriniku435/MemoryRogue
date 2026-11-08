using System;
using Live2DCardRogue.BattleScene.Directors;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Tutorial;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 戦闘の出来事を、チュートリアルの出来事（TutorialEvents）として流す橋渡し。
    // 戦闘の本体（UseCase・Director・Model）はチュートリアルを知らず、ここが外から見張って名前を投げる。
    // チュートリアルが無い戦闘でも作ってよい（誰も聞いていなければ何も起きない）
    public sealed class BattleTutorialEventBridge : IDisposable
    {
        private readonly CardCommandModel _commandModel;
        private readonly BattlePhaseModel _phaseModel;
        private readonly BattlePhaseDirector _director;
        private int _lastCommandCount;

        public BattleTutorialEventBridge(CardCommandModel commandModel, BattlePhaseModel phaseModel, BattlePhaseDirector director)
        {
            _commandModel = commandModel;
            _phaseModel = phaseModel;
            _director = director;

            _lastCommandCount = _commandModel.Count;
            _commandModel.OnChanged += HandleCommandsChanged;
            _phaseModel.OnPhaseChanged += HandlePhaseChanged;
            _commandModel.OnExecuteRequested += HandleExecuteRequested;
            _director.OnEnemyTurnStarted += HandleEnemyTurnStarted;
        }

        public void Dispose()
        {
            _commandModel.OnChanged -= HandleCommandsChanged;
            _phaseModel.OnPhaseChanged -= HandlePhaseChanged;
            _commandModel.OnExecuteRequested -= HandleExecuteRequested;
            _director.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
        }

        // 実行リストが増えた = コマンドを積んだ（戻した・実行したときは減るので数えない）
        private void HandleCommandsChanged()
        {
            int count = _commandModel.Count;
            if (count > _lastCommandCount) TutorialEvents.Raise(TutorialEventNames.CardQueued);
            _lastCommandCount = count;
        }

        // 引き終わって操作できるようになった
        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Player) TutorialEvents.Raise(TutorialEventNames.PlayerTurnStarted);
        }

        private void HandleExecuteRequested()
        {
            TutorialEvents.Raise(TutorialEventNames.ExecuteRequested);
        }

        private void HandleEnemyTurnStarted()
        {
            TutorialEvents.Raise(TutorialEventNames.EnemyTurnStarted);
        }
    }
}
