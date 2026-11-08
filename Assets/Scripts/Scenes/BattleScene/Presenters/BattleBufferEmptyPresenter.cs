using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // バッファ（手札）が空で、まだ何も積んでいないプレイヤーの手番には、手札の場所に理由とどうすればよいかを出す
    // （何も無い場所が「表示が壊れた」ように見えないように）。バッファロックが付いていれば、それが理由。
    // 積んだ結果として空になったときは、理由が画面に見えているので出さない
    public sealed class BattleBufferEmptyPresenter : PresenterBase
    {
        private readonly BattleBufferEmptyView _view;
        private readonly CardPileModel _hand;
        private readonly CardCommandModel _commandModel;
        private readonly BattlePhaseModel _phaseModel;
        private readonly UnitStatusModel _playerStatus;

        public BattleBufferEmptyPresenter(BattleBufferEmptyView view, CardPileModel hand, CardCommandModel commandModel, BattlePhaseModel phaseModel, UnitStatusModel playerStatus)
        {
            _view = view;
            _hand = hand;
            _commandModel = commandModel;
            _phaseModel = phaseModel;
            _playerStatus = playerStatus;
        }

        protected override void OnInitialize()
        {
            _hand.OnCardAdded += HandleHandChanged;
            _hand.OnCardRemoved += HandleHandChanged;
            _hand.OnCleared += Refresh;
            _commandModel.OnChanged += Refresh;
            _phaseModel.OnPhaseChanged += HandlePhaseChanged;
            _playerStatus.OnStatusEffectsChanged += Refresh;
            Refresh();
        }

        protected override void OnDispose()
        {
            _hand.OnCardAdded -= HandleHandChanged;
            _hand.OnCardRemoved -= HandleHandChanged;
            _hand.OnCleared -= Refresh;
            _commandModel.OnChanged -= Refresh;
            _phaseModel.OnPhaseChanged -= HandlePhaseChanged;
            _playerStatus.OnStatusEffectsChanged -= Refresh;
        }

        protected override void OnLocaleChanged()
        {
            Refresh();
        }

        private void HandleHandChanged(CardData _) => Refresh();

        private void HandlePhaseChanged(BattlePhase _) => Refresh();

        private void Refresh()
        {
            bool empty = _phaseModel.CurrentPhase == BattlePhase.Player && _hand.Count == 0 && _commandModel.Count == 0;
            if (!empty)
            {
                _view.Hide();
                return;
            }

            bool locked = false;
            foreach (var effect in _playerStatus.StatusEffects)
            {
                if (effect is BufferLockStatusEffect) locked = true;
            }
            _view.Show(
                LocalizationHelper.GetUiText(locked ? UiTextKey.BufferEmptyLocked : UiTextKey.BufferEmpty),
                LocalizationHelper.GetUiText(UiTextKey.BufferEmptyHint));
        }
    }
}
