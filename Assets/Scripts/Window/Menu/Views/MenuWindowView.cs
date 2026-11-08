using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.Menu.Views
{
    // ボタンを縦に並べるだけのウインドウ（Assets/Prefabs/MenuWindow.prefab）。マップ・バトルのメニュー、タイトルのコレクションが使う。
    // 項目は _itemTemplate（非アクティブ）を数だけ複製し、上から _itemSpacing ずつ並べる。ウインドウの高さも数に合わせて伸ばす
    // （タイトルは上端、閉じるボタンは下端に付いているので一緒に動く）。
    // 押されたことを伝えるだけで、何をするかは Presenter が決める。閉じるボタンか背面のクリックで閉じたいことを伝える。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog）に任せる
    public sealed class MenuWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;
        // 高さを変えるウインドウの枠
        [SerializeField] private RectTransform _windowRect;
        [SerializeField] private MenuItemView _itemTemplate;
        // 項目どうしの間隔（中心から中心）
        [SerializeField] private float _itemSpacing = 120f;
        // 項目の並びの中心（ウインドウの中心から上へ）
        [SerializeField] private float _itemCenterY = 10f;
        // 項目以外（見出し・タイトル・閉じるボタン）に要る高さ。ウインドウの高さ = これ + 項目数 × 間隔
        [SerializeField] private float _framePadding = 320f;

        private readonly List<MenuItemView> _items = new List<MenuItemView>();

        /// <summary>上から index 番目の項目が押された</summary>
        public event Action<int> OnItemClicked;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _itemTemplate.gameObject.SetActive(false);
            _closeButton.onClick.AddListener(HandleClose);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(HandleClose);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        /// <summary>項目を count 個にして並べ直す</summary>
        public void SetItemCount(int count)
        {
            while (_items.Count < count)
            {
                var item = Instantiate(_itemTemplate, _itemTemplate.transform.parent);
                item.transform.SetSiblingIndex(_itemTemplate.transform.GetSiblingIndex() + 1 + _items.Count);
                int index = _items.Count;
                item.OnClicked += () => HandleItem(index);
                _items.Add(item);
            }
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].gameObject.SetActive(i < count);
                _items[i].RectTransform.anchoredPosition = new Vector2(0f, _itemCenterY + ((count - 1) * 0.5f - i) * _itemSpacing);
            }
            _windowRect.sizeDelta = new Vector2(_windowRect.sizeDelta.x, _framePadding + count * _itemSpacing);
        }

        public MenuItemView GetItem(int index)
        {
            return _items[index];
        }

        public void SetTexts(string title, string close)
        {
            _titleText.text = title;
            _closeLabel.text = close;
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

        // 閉じる途中の押下は伝えない
        private void HandleItem(int index)
        {
            if (IsOpen) OnItemClicked?.Invoke(index);
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
