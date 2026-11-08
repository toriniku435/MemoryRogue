using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.EpisodeSelect.Views
{
    // ストーリーのエピソードを選ぶウインドウ（Assets/Prefabs/EpisodeSelectWindow.prefab）。
    // エピソードのボタンを上から並べ、遊べないエピソードは押せない見た目にする（押されたら、それも伝える）。
    // クリアしたエピソードは、ボタンの右に CLEAR の印と、そのエピソードで取った一番良いランクを出す。
    // 閉じるボタンか背面のクリックで閉じたいことを伝えるだけで、何をするかは Presenter が決める。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる
    public sealed class EpisodeSelectWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        // 上から エピソード1, 2, 3 …
        [SerializeField] private CommonButton[] _episodeButtons;
        [SerializeField] private CommonText[] _episodeLabels;
        // ボタンと同じ並びの、クリアしたら出す印と、一番良いランクの文字（未設定の要素があってもよい）
        [SerializeField] private GameObject[] _clearMarks;
        [SerializeField] private CommonText[] _rankTexts;
        // ランクの色（E, D, C, B, A, S, SS, SSS）。リザルトのランクと同じ
        [SerializeField] private Color[] _rankColors =
        {
            new Color(0.54f, 0.58f, 0.66f, 1f),
            new Color(0.61f, 0.71f, 0.79f, 1f),
            new Color(0f, 0.94f, 1f, 1f),
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
            new Color(1f, 1f, 1f, 1f),
        };
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        /// <summary>遊べるエピソードが押された（0始まりの番号）</summary>
        public event Action<int> OnEpisodeClicked;

        /// <summary>遊べないエピソードが押された（0始まりの番号）</summary>
        public event Action<int> OnUnavailableEpisodeClicked;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public int EpisodeCount => _episodeButtons.Length;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            for (int i = 0; i < _episodeButtons.Length; i++)
            {
                int index = i;
                _episodeButtons[i].onClick.AddListener(() => HandleEpisode(index));
                _episodeButtons[i].OnDisabledClicked += () => HandleUnavailableEpisode(index);
            }
            _closeButton.onClick.AddListener(HandleClose);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            foreach (var button in _episodeButtons) button.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveListener(HandleClose);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string title, string close)
        {
            _titleText.text = title;
            _closeLabel.text = close;
        }

        /// <summary>エピソードの名前と、遊べるかどうか。クリアしていれば、その一番良いランク（クリアしていなければ null）</summary>
        public void ShowEpisode(int index, string label, bool available, CampaignRank? bestRank)
        {
            _episodeLabels[index].text = label;
            _episodeButtons[index].interactable = available;

            bool cleared = bestRank.HasValue;
            if (_clearMarks != null && index < _clearMarks.Length && _clearMarks[index] != null) _clearMarks[index].SetActive(cleared);
            if (_rankTexts == null || index >= _rankTexts.Length || _rankTexts[index] == null) return;
            _rankTexts[index].gameObject.SetActive(cleared);
            if (!cleared) return;
            _rankTexts[index].text = bestRank.Value.ToString();
            _rankTexts[index].color = _rankColors[Mathf.Clamp((int)bestRank.Value, 0, _rankColors.Length - 1)];
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
        private void HandleEpisode(int index)
        {
            if (IsOpen) OnEpisodeClicked?.Invoke(index);
        }

        private void HandleUnavailableEpisode(int index)
        {
            if (IsOpen) OnUnavailableEpisodeClicked?.Invoke(index);
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
