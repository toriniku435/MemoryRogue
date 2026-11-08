using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Audio;
using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 敵詳細のウインドウ（マップの Assets/Prefabs/EnemyDetailWindow.prefab と、タイトルの EnemyCollectionWindow.prefab）。
    // 左の列に敵を並べ、選んだ敵の詳しい情報（HP・行動回数・無効化・特徴・使うコマンドとその効果）を右に出す。
    // 左の列がスクロールする場合（エネミーコレクション）は _slotScroll を設定し、並べ直すたびに先頭へ戻す。
    // 開閉の動きと背後のぼかしは共通の WindowTransition に任せる。どの敵を選ぶか・閉じるかは Presenter が決める
    public sealed class EnemyDetailWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        [Header("敵の一覧（左）")]
        [SerializeField] private Transform _slotParent;
        [SerializeField] private EnemyDetailSlotView _slotPrefab;
        // 左の列のスクロール（並べる数が多いときだけ設定する。未設定でもよい）
        [SerializeField] private UnityEngine.UI.ScrollRect _slotScroll;

        [Header("選んだ敵（右）")]
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _statsText;
        [SerializeField] private CommonText _immunityText;
        [SerializeField] private CommonText _traitText;
        [SerializeField] private CommonText _commandsHeader;
        [SerializeField] private Transform _commandParent;
        [SerializeField] private EnemyCommandRowView _commandRowPrefab;

        // 一度作った枠・行は破棄せず、アクティブ切り替えで使い回す
        private readonly List<EnemyDetailSlotView> _slots = new List<EnemyDetailSlotView>();
        private readonly List<EnemyCommandRowView> _commandRows = new List<EnemyCommandRowView>();

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        /// <summary>左の列の何番目の敵がクリックされたか</summary>
        public event Action<int> OnEnemyClicked;

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

        public void SetTexts(string title, string commandsHeader, string closeLabel)
        {
            _titleText.text = title;
            _commandsHeader.text = commandsHeader;
            _closeLabel.text = closeLabel;
        }

        public void SetEnemies(IReadOnlyList<EnemyDetailInfo> enemies)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (i >= _slots.Count)
                {
                    var slot = Instantiate(_slotPrefab, _slotParent);
                    slot.OnClicked += HandleSlotClicked;
                    _slots.Add(slot);
                }
                _slots[i].Bind(enemies[i]);
                _slots[i].SetActive(true);
            }
            for (int i = enemies.Count; i < _slots.Count; i++)
            {
                _slots[i].SetActive(false);
            }
            if (_slotScroll != null) _slotScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>index 番目の敵を選んだ状態にし、その詳細を右に出す</summary>
        public void ShowSelected(int index, EnemyDetailInfo info)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetSelected(i == index);
            }

            _nameText.text = info.Count > 1 ? $"{info.Name}  x{info.Count}" : info.Name;
            _statsText.text = info.Stats;
            _immunityText.gameObject.SetActive(!string.IsNullOrEmpty(info.Immunities));
            _immunityText.text = info.Immunities;
            _traitText.text = info.Trait;

            for (int i = 0; i < info.Commands.Count; i++)
            {
                if (i >= _commandRows.Count) _commandRows.Add(Instantiate(_commandRowPrefab, _commandParent));
                _commandRows[i].Bind(info.Commands[i]);
                _commandRows[i].SetActive(true);
            }
            for (int i = info.Commands.Count; i < _commandRows.Count; i++)
            {
                _commandRows[i].SetActive(false);
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

        private void HandleSlotClicked(EnemyDetailSlotView slot)
        {
            if (!IsOpen) return;
            OnEnemyClicked?.Invoke(_slots.IndexOf(slot));
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
