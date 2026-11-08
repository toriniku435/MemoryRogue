using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.Conversation
{
    // 会話パートのウインドウ（Assets/Prefabs/ConversationWindow.prefab。シーンの PopupCanvas に置く）。
    // 画面の下にテキストボックス（話し手と台詞）を出し、後ろはほかのウインドウと同じくぼかす（WindowTransition）。
    // 台詞は1文字ずつ出し、クリック（テキストボックスでも、その外でも）で、出している途中なら全文を出し、
    // 出し終えていれば次へ進みたいことを伝える。進めるのは Presenter
    public sealed class ConversationWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        // テキストボックスの外（暗幕）と、テキストボックスそのもののクリック
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private ClickArea _boxArea;
        [SerializeField] private CommonText _speakerText;
        [SerializeField] private CommonText _bodyText;
        // 読み終えたら出す「クリックで次へ」（点滅させる）
        [SerializeField] private CommonText _nextHint;
        [SerializeField][Min(1f)] private float _charactersPerSecond = 40f;

        private float _visibleCharacters;
        private int _totalCharacters;
        private float _hintTime;

        /// <summary>出し終えた台詞の上でクリックされた（次へ進みたい）</summary>
        public event Action OnAdvanceRequested;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        /// <summary>台詞を1文字ずつ出している途中か</summary>
        public bool IsTyping => _visibleCharacters < _totalCharacters;

        private void Awake()
        {
            _backgroundArea.OnClicked += HandleClicked;
            _boxArea.OnClicked += HandleClicked;
        }

        private void OnDestroy()
        {
            _backgroundArea.OnClicked -= HandleClicked;
            _boxArea.OnClicked -= HandleClicked;
        }

        public void SetHint(string hint)
        {
            _nextHint.text = hint;
        }

        public void Open()
        {
            _dialog.PlayOpen();
        }

        public void Close()
        {
            _dialog.PlayClose(null);
        }

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void HideImmediate()
        {
            _dialog.HideImmediate();
        }

        /// <summary>1行出す（1文字ずつ出し始める）</summary>
        public void ShowLine(string speaker, string text)
        {
            _speakerText.text = speaker ?? "";
            _speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            _bodyText.text = text ?? "";
            _bodyText.ForceMeshUpdate();
            _totalCharacters = _bodyText.textInfo.characterCount;
            _visibleCharacters = 0f;
            _bodyText.maxVisibleCharacters = 0;
            _nextHint.gameObject.SetActive(false);
        }

        /// <summary>出している途中の台詞を全部出す</summary>
        public void CompleteTyping()
        {
            _visibleCharacters = _totalCharacters;
            _bodyText.maxVisibleCharacters = _totalCharacters;
        }

        // 時間は実時間で進め、1フレームの上限を付ける（処理落ちで飛ばないように）
        private void Update()
        {
            if (!_dialog.gameObject.activeSelf) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (IsTyping)
            {
                _visibleCharacters = Mathf.Min(_totalCharacters, _visibleCharacters + _charactersPerSecond * dt);
                _bodyText.maxVisibleCharacters = Mathf.FloorToInt(_visibleCharacters);
                return;
            }

            // 出し終えたら、次へ進めることを点滅で知らせる
            if (!_nextHint.gameObject.activeSelf)
            {
                _nextHint.gameObject.SetActive(true);
                _hintTime = 0f;
            }
            _hintTime += dt;
            var color = _nextHint.color;
            color.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_hintTime * 3f));
            _nextHint.color = color;
        }

        private void HandleClicked()
        {
            if (!IsOpen) return;
            if (IsTyping)
            {
                CompleteTyping();
                return;
            }
            AudioUseCase.PlaySe(SeId.Click);
            OnAdvanceRequested?.Invoke();
        }
    }
}
