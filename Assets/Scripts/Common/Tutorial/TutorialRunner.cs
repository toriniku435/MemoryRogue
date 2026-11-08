using System;
using System.Collections.Generic;

namespace Live2DCardRogue.Common.Tutorial
{
    // 手順を1手ずつ進めるだけの状態。表示も UnityEngine も知らない（EditMode でテストできる）
    public sealed class TutorialRunner
    {
        private readonly IReadOnlyList<TutorialStep> _steps;

        public int Index { get; private set; } = -1;
        public int Count => _steps.Count;
        public bool IsFinished { get; private set; }
        public TutorialStep Current => Index >= 0 && Index < _steps.Count ? _steps[Index] : null;

        /// <summary>手が変わった（その手）</summary>
        public event Action<TutorialStep> OnStepChanged;

        /// <summary>終わった（true: 最後まで進んだ、false: スキップした）</summary>
        public event Action<bool> OnFinished;

        public TutorialRunner(IReadOnlyList<TutorialStep> steps)
        {
            _steps = steps ?? new TutorialStep[0];
        }

        /// <summary>
        /// 0..index の手のうち、何かを言う手（文言のある手）の数。「3 / 12」のような表示に使う
        /// （待つだけの手は画面に出ないので数えない）
        /// </summary>
        public int CountTextStepsUpTo(int index)
        {
            int count = 0;
            for (int i = 0; i <= index && i < _steps.Count; i++)
            {
                if (_steps[i] != null && _steps[i].HasText) count++;
            }
            return count;
        }

        public void Start()
        {
            if (Index >= 0 || IsFinished) return;
            MoveTo(0);
        }

        /// <summary>クリックで進む手なら次へ（出来事で進む手は、クリックでは進まない）</summary>
        public void Next()
        {
            if (Current == null || Current.Advance != TutorialAdvance.Click) return;
            MoveTo(Index + 1);
        }

        /// <summary>出来事が起きた。今の手がそれを待っていれば次へ</summary>
        public void HandleEvent(string eventName)
        {
            var step = Current;
            if (step == null || step.Advance != TutorialAdvance.Event || step.EventName != eventName) return;
            MoveTo(Index + 1);
        }

        public void Skip()
        {
            if (IsFinished || Index < 0) return;
            IsFinished = true;
            OnFinished?.Invoke(false);
        }

        private void MoveTo(int index)
        {
            if (IsFinished) return;
            if (index >= _steps.Count)
            {
                IsFinished = true;
                OnFinished?.Invoke(true);
                return;
            }

            Index = index;
            OnStepChanged?.Invoke(_steps[index]);
        }
    }
}
