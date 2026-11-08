using Live2DCardRogue.BattleScene.Directors;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 誰かがカードを使った・ターンが区切れた、を CurrentActionModel に書き、敵の行動には効果音を鳴らす。
    // （使い手の行動アニメは CurrentActionModel を見る UnitVisualPresenter が始める）
    // （プレイヤーのコマンドの実行音は実行リストの Presenter が鳴らしている）
    public sealed class BattleActionPresenter : PresenterBase
    {
        private readonly BattleCommandExecuteUseCase _executeUseCase;
        private readonly BattlePhaseDirector _director;
        private readonly UnitStatusModel _player;
        private readonly CurrentActionModel _currentAction;

        public BattleActionPresenter(BattleCommandExecuteUseCase executeUseCase, BattlePhaseDirector director, UnitStatusModel player, CurrentActionModel currentAction)
        {
            _executeUseCase = executeUseCase;
            _director = director;
            _player = player;
            _currentAction = currentAction;
        }

        protected override void OnInitialize()
        {
            _executeUseCase.OnCardExecuting += HandleCardExecuting;
            _director.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _director.OnEnemyTurnStarted += _currentAction.Clear;
            _director.OnUnitTurnEnding += HandleUnitTurnEnding;
        }

        protected override void OnDispose()
        {
            _executeUseCase.OnCardExecuting -= HandleCardExecuting;
            _director.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            _director.OnEnemyTurnStarted -= _currentAction.Clear;
            _director.OnUnitTurnEnding -= HandleUnitTurnEnding;
        }

        // 表示する文字を持たない
        protected override void OnLocaleChanged() { }

        private void HandleCardExecuting(UnitStatusModel user, CardData card, UnitStatusModel target)
        {
            _currentAction.Set(user, card);
            if (user != _player) AudioUseCase.PlaySe(SeId.EnemyAction);
        }

        private void HandlePlayerTurnStarted(int turn) => _currentAction.Clear();

        private void HandleUnitTurnEnding(UnitStatusModel unit) => _currentAction.Clear();
    }
}
