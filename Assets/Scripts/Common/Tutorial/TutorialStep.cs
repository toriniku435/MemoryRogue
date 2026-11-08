using System;
using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    public enum TutorialAdvance
    {
        // どこかをクリックすると次へ（画面は操作できない）
        Click,
        // 決まった出来事（TutorialEvents）が起きたら次へ。強調した部分だけ操作できる
        Event,
    }

    public enum TutorialBubbleSide
    {
        // 空いている側を自動で選ぶ（下 → 上 → 右 → 左）
        Auto,
        Above,
        Below,
        Left,
        Right,
    }

    // チュートリアルの1手。何を強調し（目印の ID）、何を言い（文言のキー）、何で次へ進むか。
    // UI の位置は持たない（目印の ID から、その時点の実際の位置を引く）。文言も持たない（TutorialText テーブルから引く）。
    // 文言のキーが空の手は「待つだけ」: 画面に何も出さず、出来事が起きるのを待つ（行動の実行中など）
    [Serializable]
    public sealed class TutorialStep
    {
        /// <summary>強調する UI の目印（TutorialAnchor の ID、または「HandCard:Attack」のような動的な目印）。空なら強調しない</summary>
        [SerializeField] private string _anchorId;

        /// <summary>TutorialText テーブルのキー。空なら何も出さずに待つだけの手</summary>
        [SerializeField] private string _textKey;

        [SerializeField] private TutorialBubbleSide _bubbleSide = TutorialBubbleSide.Auto;
        [SerializeField] private TutorialAdvance _advance = TutorialAdvance.Click;

        /// <summary>_advance が Event のとき、この名前の出来事（TutorialEventNames）で次へ進む</summary>
        [SerializeField] private string _eventName;

        public string AnchorId => _anchorId;
        public string TextKey => _textKey;
        public TutorialBubbleSide BubbleSide => _bubbleSide;
        public TutorialAdvance Advance => _advance;
        public string EventName => _eventName;

        public bool HasText => !string.IsNullOrEmpty(_textKey);
        public bool HasAnchor => !string.IsNullOrEmpty(_anchorId);

        public TutorialStep() { }

        public TutorialStep(string anchorId, string textKey, TutorialAdvance advance = TutorialAdvance.Click, string eventName = null,
            TutorialBubbleSide bubbleSide = TutorialBubbleSide.Auto)
        {
            _anchorId = anchorId;
            _textKey = textKey;
            _advance = advance;
            _eventName = eventName;
            _bubbleSide = bubbleSide;
        }
    }
}
