using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 対象選択中に、選んだカードのすぐ上へ対象の候補一覧を出す。
    // バトルフィールドのユニットまでカーソルを運ばずに対象を選べるようにするためのもの（ユニットを直接クリックしても選べる）。
    // 候補にカーソルを合わせると、そのユニットを「指している」ことにして、ユニット側の表示で強調させる
    public sealed class BattleTargetPickerPresenter : PresenterBase
    {
        private readonly CardSelectionModel _selectionModel;
        private readonly TargetFocusModel _focusModel;
        private readonly BattleTargetPickerView _view;
        private readonly IReadOnlyList<BattleTargetCandidate> _candidates;

        // 選べるかどうかの判定と、選んだことの報告先。ユニットのクリックと同じもの（見た目と挙動がずれないように）
        private readonly Func<UnitStatusModel, bool> _canBeTargeted;
        private readonly Action<UnitStatusModel> _onTargetPicked;

        // 一覧を出す目印（選んだ手札のカード）。手札のPresenterが知っている
        private readonly Func<RectTransform> _pendingCardAnchor;

        public BattleTargetPickerPresenter(
            CardSelectionModel selectionModel,
            TargetFocusModel focusModel,
            BattleTargetPickerView view,
            IReadOnlyList<BattleTargetCandidate> candidates,
            Func<UnitStatusModel, bool> canBeTargeted,
            Action<UnitStatusModel> onTargetPicked,
            Func<RectTransform> pendingCardAnchor)
        {
            _selectionModel = selectionModel;
            _focusModel = focusModel;
            _view = view;
            _candidates = candidates;
            _canBeTargeted = canBeTargeted;
            _onTargetPicked = onTargetPicked;
            _pendingCardAnchor = pendingCardAnchor;
        }

        protected override void OnInitialize()
        {
            _selectionModel.OnPendingCardChanged += HandlePendingCardChanged;
            _view.Hide();
        }

        protected override void OnDispose()
        {
            _selectionModel.OnPendingCardChanged -= HandlePendingCardChanged;
        }

        // 表示しているのは立ち絵とHPの数字だけで、ローカライズする文字は無い
        protected override void OnLocaleChanged()
        {
        }

        private void HandlePendingCardChanged(CardData pendingCard)
        {
            if (pendingCard == null || _pendingCardAnchor.Invoke() == null)
            {
                _view.Hide();
                return;
            }

            var targets = new List<BattleTargetCandidate>();
            foreach (var candidate in _candidates)
            {
                if (_canBeTargeted.Invoke(candidate.Status)) targets.Add(candidate);
            }

            if (targets.Count == 0)
            {
                _view.Hide();
                return;
            }

            _view.SetOptionCount(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                _view.GetOption(i).Bind(
                    target.Portrait,
                    $"{target.Status.CurrentHp}/{target.Status.MaxHp}",
                    onClicked: () => _onTargetPicked.Invoke(target.Status),
                    onHoverChanged: isHovered => HandleHoverChanged(target.Status, isHovered));
            }

            // 同じカードが手札に複数あると、別の1枚へ選び直しても PendingCard は変わらず通知も来ない。
            // なので目印は固定で渡さず、選んでいるカードを毎フレーム聞き直させる（選び直せば一覧もそちらへ移る）
            _view.Show(_pendingCardAnchor);
        }

        private void HandleHoverChanged(UnitStatusModel target, bool isHovered)
        {
            if (isHovered) _focusModel.Focus(target);
            else _focusModel.Unfocus(target);
        }
    }
}
