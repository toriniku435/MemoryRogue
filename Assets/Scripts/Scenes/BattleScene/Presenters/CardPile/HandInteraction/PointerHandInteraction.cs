using System.Threading;
using Cysharp.Threading.Tasks;
using Live2DCardRogue.BattleScene.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // マウスでの手札の操作: 乗った瞬間に浮かせて詳細を出し、少し止まったら候補パネルを開き、クリックで使う。
    // 選んだカードからも候補パネルからも離れたら、少し待って選択を外す（カードから候補へ移る間の隙間では外さない）
    internal sealed class PointerHandInteraction : IHandInteraction
    {
        private const int LeaveGraceMs = 180;

        private readonly IHandOperations _hand;
        private BattleCardView _hoveredView;
        private bool _isPointerOnPicker;
        private CancellationTokenSource _leaveCancellation;

        public PointerHandInteraction(IHandOperations hand)
        {
            _hand = hand;
        }

        // 合わせている間は、カードの詳細を大きく出す（選べないカードでも、どのフェーズでも）
        public void OnCardPointerChanged(BattleCardView view, bool isOver)
        {
            if (isOver)
            {
                _hoveredView = view;
                _hand.ShowPreview(view);

                // 乗った瞬間に浮かせる（素早く手札をなぞっても追いつくように）
                if (_hand.CanOperate(view) && !_hand.IsChoosingPurge && view != _hand.PendingView && view != _hand.InspectedView)
                {
                    _hand.Inspect(view);
                }
            }
            else
            {
                if (_hoveredView == view) _hoveredView = null;
                _hand.HidePreview(view);
                _hand.Lower(view);
            }
            RefreshLeaveTimer();
        }

        // 少し止まったら、敵が複数いて1体を選ぶカードは対象の候補パネルを開く（別の敵を選びたいとき用）。
        // パージするバッファを選んでいる間は、合わせただけでは何も変えない
        public void OnCardDwelled(BattleCardView view)
        {
            if (!_hand.CanOperate(view) || view == _hand.PendingView || _hand.IsChoosingPurge) return;
            if (!_hand.NeedsTargetChoice(view)) return;

            _hand.Select(view);
            RefreshLeaveTimer();
        }

        // クリックで使う（候補パネルが開いていても優先ターゲットへ。別の敵にしたいときはパネルで選ぶ）
        public void OnCardClicked(BattleCardView view)
        {
            if (!_hand.CanOperate(view)) return;

            if (_hand.IsChoosingPurge) _hand.TogglePurgeMark(view);
            else _hand.Play(view);
        }

        public void OnPickerPointerChanged(bool isOver)
        {
            _isPointerOnPicker = isOver;
            RefreshLeaveTimer();
        }

        // 選択が外れていれば、待つ必要も無くなる
        public void OnLiftedChanged(BattleCardView lifted)
        {
            RefreshLeaveTimer();
        }

        public void Dispose()
        {
            StopLeaveTimer();
        }

        private void RefreshLeaveTimer()
        {
            var pending = _hand.PendingView;
            bool shouldRelease = pending != null && _hoveredView != pending && !_isPointerOnPicker;
            if (!shouldRelease)
            {
                StopLeaveTimer();
                return;
            }
            if (_leaveCancellation != null) return;

            _leaveCancellation = new CancellationTokenSource();
            ReleaseAfterGraceAsync(_leaveCancellation.Token).Forget();
        }

        private void StopLeaveTimer()
        {
            if (_leaveCancellation == null) return;
            _leaveCancellation.Cancel();
            _leaveCancellation.Dispose();
            _leaveCancellation = null;
        }

        private async UniTaskVoid ReleaseAfterGraceAsync(CancellationToken token)
        {
            bool cancelled = await UniTask.Delay(LeaveGraceMs, ignoreTimeScale: true, cancellationToken: token).SuppressCancellationThrow();
            if (cancelled) return;

            _leaveCancellation?.Dispose();
            _leaveCancellation = null;
            _hand.CancelTargetSelection();
        }
    }
}
