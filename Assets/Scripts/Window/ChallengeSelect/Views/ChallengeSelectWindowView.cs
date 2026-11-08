using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.ChallengeSelect.Views
{
    // チャレンジを選ぶウインドウ（Assets/Prefabs/ChallengeSelectWindow.prefab）。
    // チャレンジのボタンを上から並べ（並びは ChallengeCatalog の順）、押されたものを何番目かで伝えるだけ。
    // 踏破したことのあるチャレンジは、ボタンの右にクリアマークを出す。
    // 終わりの無いチャレンジ（エンドレス）は、代わりに最高到達階層（LAYER 37）を出す。
    // 閉じるボタンか背面のクリックで閉じたいことを伝える。開閉の動きと背後のぼかしは WindowTransition（_dialog）に任せる
    public sealed class ChallengeSelectWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        // 上から1つ目、2つ目…（ChallengeCatalog の並び）
        [SerializeField] private CommonButton[] _challengeButtons;
        [SerializeField] private CommonText[] _challengeLabels;
        // ボタンと同じ並びの、踏破したら出す印（未設定の要素があってもよい）
        [SerializeField] private GameObject[] _clearMarks;
        // ボタンと同じ並びの、最高到達階層の印とその文字（エンドレスのボタンにだけあればよい。未設定の要素があってもよい）
        [SerializeField] private GameObject[] _bestLayerMarks;
        [SerializeField] private CommonText[] _bestLayerTexts;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        /// <summary>何番目のチャレンジが押されたか</summary>
        public event Action<int> OnChallengeClicked;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public int ButtonCount => _challengeButtons.Length;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            for (int i = 0; i < _challengeButtons.Length; i++)
            {
                int index = i;
                _challengeButtons[i].onClick.AddListener(() => HandleChallenge(index));
            }
            _closeButton.onClick.AddListener(HandleClose);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            foreach (var button in _challengeButtons) button.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveListener(HandleClose);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string title, string close)
        {
            _titleText.text = title;
            _closeLabel.text = close;
        }

        /// <summary>
        /// index 番目のボタンの名前と、踏破したことがあるか、最高到達階層（0 なら出さない）。名前が null ならそのボタンを隠す（チャレンジがボタンより少ないとき）
        /// </summary>
        public void SetChallenge(int index, string label, bool cleared, int bestLayer)
        {
            bool showBest = label != null && bestLayer > 0;
            if (_bestLayerMarks != null && index < _bestLayerMarks.Length && _bestLayerMarks[index] != null) _bestLayerMarks[index].SetActive(showBest);
            if (showBest && _bestLayerTexts != null && index < _bestLayerTexts.Length && _bestLayerTexts[index] != null)
            {
                _bestLayerTexts[index].text = $"LAYER {bestLayer:00}";
            }

            if (index < 0 || index >= _challengeButtons.Length) return;
            _challengeButtons[index].gameObject.SetActive(label != null);
            if (label != null) _challengeLabels[index].text = label;
            if (_clearMarks != null && index < _clearMarks.Length && _clearMarks[index] != null)
            {
                _clearMarks[index].SetActive(label != null && cleared);
            }
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

        // 閉じる途中の二度押しは伝えない
        private void HandleChallenge(int index)
        {
            if (IsOpen) OnChallengeClicked?.Invoke(index);
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
