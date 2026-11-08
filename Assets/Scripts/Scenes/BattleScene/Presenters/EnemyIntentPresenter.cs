using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Interaction;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Statuses;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 敵1体の次の行動を予告スロットに出す。停止で行わない分は暗くし、倒れたら隠す。
    // スロットにカーソルを乗せると、そのカードの詳細を詳細パネルに出す（手札と同じ CardPreviewModel）。
    // タッチではタップで出し、別のものをタップするまで出し続ける（カーソルの出入りは使わない）
    public sealed class EnemyIntentPresenter : PresenterBase
    {
        private readonly BattleEnemy _enemy;
        private readonly BattleEnemyIntentView _view;
        private readonly CardPreviewModel _previewModel;
        private readonly bool _isTouch;

        // 詳細パネルの持ち主の目印。スロットごとに1つ（同じカードが並んでも区別できるように）
        private readonly List<object> _slotOwners = new List<object>();

        public EnemyIntentPresenter(BattleEnemy enemy, BattleEnemyIntentView view, CardPreviewModel previewModel, InteractionMode interactionMode)
        {
            _isTouch = interactionMode == InteractionMode.Touch;
            _enemy = enemy;
            _view = view;
            _previewModel = previewModel;
        }

        protected override void OnInitialize()
        {
            for (int i = 0; i < _view.SlotCount; i++) _slotOwners.Add(new object());

            _enemy.OnPlanChanged += Sync;
            _enemy.Status.OnStatusEffectsChanged += Sync;
            _enemy.Status.OnHpChanged += HandleHpChanged;
            _view.OnSlotHovered += HandleSlotHovered;
            _view.OnSlotUnhovered += HandleSlotUnhovered;
            _view.OnSlotClicked += HandleSlotClicked;
            Sync();
        }

        protected override void OnDispose()
        {
            _enemy.OnPlanChanged -= Sync;
            _enemy.Status.OnStatusEffectsChanged -= Sync;
            _enemy.Status.OnHpChanged -= HandleHpChanged;
            _view.OnSlotHovered -= HandleSlotHovered;
            _view.OnSlotUnhovered -= HandleSlotUnhovered;
            _view.OnSlotClicked -= HandleSlotClicked;
        }

        protected override void OnLocaleChanged() => Sync();

        private void HandleHpChanged(int hp, int maxHp) => Sync();

        private void Sync()
        {
            var plan = _enemy.PlannedCards;
            if (_enemy.IsDefeated || plan.Count == 0)
            {
                _view.SetVisible(false);
                return;
            }

            // 停止などで行動回数が減っていれば、先頭からその数だけ行う（BattlePhaseDirector と同じ数え方）
            int actionCount = Mathf.Clamp(_enemy.Status.CalculateLimit(StatLimitType.ActionCount, plan.Count), 0, plan.Count);
            var actions = new List<(string, Sprite, bool)>(plan.Count);
            for (int i = 0; i < plan.Count; i++)
            {
                actions.Add((LocalizationHelper.GetCardName(plan[i]), plan[i].Illustration, i < actionCount));
            }

            _view.SetVisible(true);
            _view.SetHeader(LocalizationHelper.GetUiText(UiTextKey.IntentHeader));
            _view.SetActions(actions);
        }

        private void HandleSlotHovered(int index)
        {
            if (!_isTouch) ShowSlot(index);
        }

        private void HandleSlotClicked(int index)
        {
            if (_isTouch) ShowSlot(index);
        }

        private void ShowSlot(int index)
        {
            var plan = _enemy.PlannedCards;
            if (index < 0 || index >= plan.Count) return;
            _previewModel.Show(_slotOwners[index], plan[index]);
        }

        private void HandleSlotUnhovered(int index)
        {
            if (_isTouch) return;
            if (index < 0 || index >= _slotOwners.Count) return;
            _previewModel.Hide(_slotOwners[index]);
        }
    }
}
