using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.BattleLog.Views
{
    // ログボタンで開く、戦闘開始からの全ログのウインドウ（Assets/Prefabs/BattleLogWindow.prefab）。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // 行は CommonScrollList で見えている分だけ作る（戦闘が長引いてもGameObjectは増えない）。
    // 閉じるボタンか背面のクリックで閉じたいことを伝えるだけで、閉じるのは Presenter
    public sealed class BattleLogWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonScrollList _list;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public CommonScrollList List => _list;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleClose);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(HandleClose);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string title, string closeLabel)
        {
            _titleText.text = title;
            _closeLabel.text = closeLabel;
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

        private void HandleClose()
        {
            if (IsOpen) OnCloseRequested?.Invoke();
        }

        // 背面のクリックはボタンと違って自分では鳴らないので、取り消しの音をここで鳴らす
        private void HandleBackgroundClicked()
        {
            if (!IsOpen) return;
            AudioUseCase.PlaySe(SeId.Cancel);
            OnCloseRequested?.Invoke();
        }
    }
}
