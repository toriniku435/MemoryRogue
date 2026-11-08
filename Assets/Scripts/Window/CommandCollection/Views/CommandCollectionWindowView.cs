using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.CommandCollection.Views
{
    // コマンドのコレクションのウインドウ（Assets/Prefabs/CommandCollectionWindow.prefab）。
    // コマンドの一覧（_list）、カーソルを合わせたコマンドの拡大表示（_detail）、絞り込みのタブ（すべて / チップごと）。
    // タブの数はチップの数で変わるので、_filterTemplate を複製して横に並べる（最初の1つは _filterTemplate そのもの）。
    // 押されたこと・閉じたいことを伝えるだけで、何を並べるかは Presenter が決める。
    // 見るだけのウインドウなので、背面のクリックでも閉じる。開閉の動きと背後のぼかしは WindowTransition（_dialog）に任せる
    public sealed class CommandCollectionWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonText _headerText;
        // 手に入れた数（12 / 31）
        [SerializeField] private CommonText _progressText;
        [SerializeField] private CommonScrollList _list;
        [SerializeField] private CardListDetailView _detail;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        [Header("絞り込みのタブ")]
        [SerializeField] private CommonButton _filterTemplate;
        // 隣のタブまでの距離（タブは localScale で縮めてあるので、見た目の幅で）
        [SerializeField] private float _filterSpacing = 150f;
        [SerializeField] private Color _activeFilterColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _inactiveFilterColor = new Color(0.55f, 0.62f, 0.72f, 1f);

        private readonly List<CommonButton> _filterButtons = new List<CommonButton>();
        private readonly List<CommonText> _filterLabels = new List<CommonText>();

        /// <summary>絞り込みのタブが押された（0 が「すべて」）</summary>
        public event Action<int> OnFilterClicked;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public CommonScrollList List => _list;
        public CardListDetailView Detail => _detail;

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
            foreach (var button in _filterButtons) button.onClick.RemoveAllListeners();
        }

        public void SetTexts(string title, string header, string close)
        {
            _titleText.text = title;
            _headerText.text = header;
            _closeLabel.text = close;
        }

        public void SetProgress(string progress)
        {
            _progressText.text = progress;
        }

        /// <summary>絞り込みのタブの名前（左から）。足りなければタブを増やし、余ったタブは隠す</summary>
        public void SetFilters(IReadOnlyList<string> labels)
        {
            while (_filterButtons.Count < labels.Count) AddFilterButton();
            for (int i = 0; i < _filterButtons.Count; i++)
            {
                bool used = i < labels.Count;
                _filterButtons[i].gameObject.SetActive(used);
                if (used) _filterLabels[i].text = labels[i];
            }
        }

        /// <summary>選んでいるタブの文字を黄色に、ほかを灰色にする</summary>
        public void SetSelectedFilter(int index)
        {
            for (int i = 0; i < _filterLabels.Count; i++) _filterLabels[i].color = i == index ? _activeFilterColor : _inactiveFilterColor;
        }

        public void Open()
        {
            _dialog.PlayOpen();
            _list.FitColumnsToWidth();
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

        // 最初のタブはテンプレートそのもの。2つ目からは複製して右へ並べる
        private void AddFilterButton()
        {
            int index = _filterButtons.Count;
            var button = index == 0
                ? _filterTemplate
                : Instantiate(_filterTemplate, _filterTemplate.transform.parent);
            if (index > 0)
            {
                var rt = (RectTransform)button.transform;
                rt.anchoredPosition = ((RectTransform)_filterTemplate.transform).anchoredPosition + new Vector2(_filterSpacing * index, 0f);
                button.name = $"{_filterTemplate.name}{index}";
            }
            button.onClick.AddListener(() => HandleFilter(index));
            _filterButtons.Add(button);
            _filterLabels.Add(button.GetComponentInChildren<CommonText>(true));
        }

        // 閉じる途中の押下は伝えない
        private void HandleFilter(int index)
        {
            if (IsOpen) OnFilterClicked?.Invoke(index);
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
