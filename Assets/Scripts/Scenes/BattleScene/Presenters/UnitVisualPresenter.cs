using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // ユニットの動く見た目を立ち絵の枠に作り、そのユニットがカードを使ったら行動アニメを始める。
    // 被弾と倒れた表示は、HP の変化を受けた UnitPresenter → BattleUnitStatusView がそのまま見た目へ流す
    public sealed class UnitVisualPresenter : PresenterBase
    {
        private readonly UnitStatusModel _unit;
        private readonly BattleUnitStatusView _view;
        private readonly UnitVisualDefinition _definition;
        private readonly CurrentActionModel _currentAction;

        public UnitVisualPresenter(UnitStatusModel unit, BattleUnitStatusView view, UnitVisualDefinition definition, CurrentActionModel currentAction)
        {
            _unit = unit;
            _view = view;
            _definition = definition;
            _currentAction = currentAction;
        }

        protected override void OnInitialize()
        {
            _view.ApplyVisual(_definition);
            _view.SetDefeated(_unit.CurrentHp == 0);
            _currentAction.OnActionStarted += HandleActionStarted;
        }

        protected override void OnDispose()
        {
            _currentAction.OnActionStarted -= HandleActionStarted;
        }

        // 表示する文字を持たない
        protected override void OnLocaleChanged() { }

        private void HandleActionStarted(UnitStatusModel user, CardData card)
        {
            if (user == _unit) _view.PlayAction();
        }
    }
}
