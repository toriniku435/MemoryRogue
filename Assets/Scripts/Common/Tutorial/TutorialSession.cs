using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    // 1つの手順を流している間の、手順（TutorialRunner）と画面（TutorialOverlayView）のつなぎ。
    // 出来事（TutorialEvents）を手順へ渡し、手が変わるたびに文言を引いて画面に出す。
    // 終わったら（最後まで / スキップ）onEnded を呼び、画面を片付ける。
    // スキップは confirmSkip があれば確かめてから（尋ねている間は画面を引っ込める）
    public sealed class TutorialSession : PresenterBase
    {
        private readonly TutorialRunner _runner;
        private readonly TutorialOverlayView _view;
        private readonly Action<bool> _onEnded;
        private readonly Action<Action, Action> _confirmSkip;
        private bool _confirmingSkip;
        private bool _disposed;

        public TutorialSession(TutorialSequence sequence, TutorialOverlayView view, Action<bool> onEnded, Action<Action, Action> confirmSkip = null)
        {
            _runner = new TutorialRunner(sequence.Steps);
            _view = view;
            _onEnded = onEnded;
            _confirmSkip = confirmSkip;
        }

        public bool IsFinished => _runner.IsFinished;

        protected override void OnInitialize()
        {
            _runner.OnStepChanged += ShowStep;
            _runner.OnFinished += HandleFinished;
            _view.OnNextRequested += _runner.Next;
            _view.OnSkipRequested += HandleSkipRequested;
            TutorialEvents.OnRaised += _runner.HandleEvent;
            ApplyLabels();
            _runner.Start();
        }

        protected override void OnDispose()
        {
            _runner.OnStepChanged -= ShowStep;
            _runner.OnFinished -= HandleFinished;
            _view.OnNextRequested -= _runner.Next;
            _disposed = true;
            _view.OnSkipRequested -= HandleSkipRequested;
            TutorialEvents.OnRaised -= _runner.HandleEvent;
            if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_runner.Current != null && !_runner.IsFinished) ShowStep(_runner.Current);
        }

        private void ApplyLabels()
        {
            _view.SetLabels(LocalizationHelper.GetUiText(UiTextKey.TutorialSkip), LocalizationHelper.GetUiText(UiTextKey.TutorialNext));
        }

        private void ShowStep(TutorialStep step)
        {
            if (!step.HasText)
            {
                _view.HideStep();
                return;
            }

            string anchorId = step.AnchorId;
            Func<RectTransform> target = step.HasAnchor ? () => TutorialAnchorRegistry.Find(anchorId) : (Func<RectTransform>)null;
            _view.ShowStep(target, LocalizationHelper.GetTutorialText(step), step.BubbleSide,
                clickToAdvance: step.Advance == TutorialAdvance.Click,
                counter: $"{_runner.CountTextStepsUpTo(_runner.Index)} / {_runner.CountTextStepsUpTo(_runner.Count - 1)}");
        }

        // スキップが押された。確かめる先があれば、画面を引っ込めて尋ね、答えを待つ
        private void HandleSkipRequested()
        {
            if (_confirmSkip == null)
            {
                _runner.Skip();
                return;
            }
            if (_confirmingSkip) return;

            _confirmingSkip = true;
            _view.SetSuspended(true);
            _confirmSkip.Invoke(
                () =>
                {
                    if (_disposed) return;
                    _confirmingSkip = false;
                    _view.SetSuspended(false);
                    _runner.Skip();
                },
                () =>
                {
                    if (_disposed) return;
                    _confirmingSkip = false;
                    _view.SetSuspended(false);
                });
        }

        private void HandleFinished(bool completed)
        {
            _view.HideStep();
            _onEnded?.Invoke(completed);
        }
    }
}
