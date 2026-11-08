using System;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // 敵詳細ウインドウの左の列の1枠: 敵の動く立ち絵と名前。クリックで選ばれ、選ばれている間はカーソルが指す。
    // まだ出会っていない敵（エネミーコレクション）は、立ち絵を黒い影にして「？」を重ねる
    public sealed class EnemyDetailSlotView : MonoBehaviour
    {
        // カーソルは横に揺れて「ここを見ている」ことを示す
        private const float CursorSwing = 6f;
        private const float CursorSpeed = 6f;

        [SerializeField] private CommonButton _button;
        // 動く見た目を作る枠。敵は戦闘と同じく左向きにするので、x スケール -1 にしてある
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private Image _iconImage;
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _countText;
        // 選択中だけ出すもの（カーソルと光る枠）
        [SerializeField] private RectTransform _cursor;
        [SerializeField] private GameObject _selectedFrame;
        // まだ出会っていない敵に重ねる「？」（未設定でもよい）
        [SerializeField] private GameObject _unknownMark;

        private UnitVisualDefinition _visualDefinition;
        private GameObject _visualObject;
        private IUnitVisual _visual;
        // カーソルの揺れの基準（最初に使うときに覚える）
        private Vector2 _cursorOrigin;
        private bool _hasCursorOrigin;
        private bool _isSelected;

        public event Action<EnemyDetailSlotView> OnClicked;

        // 枠は閉じたウインドウ（非アクティブ）の中で作られ、選択状態が付いたあとで Awake が走ることがある。
        // なので Awake では選択状態に触らない（ここで外すと、開いた瞬間にカーソルが消える）
        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        public void Bind(EnemyDetailInfo info)
        {
            _nameText.text = info.Name;
            _countText.gameObject.SetActive(info.Count > 1);
            _countText.text = $"x{info.Count}";
            BindVisual(info);

            // 出会っていない敵は影にする（同じ見た目を使い回すので、毎回色を付け直す）
            var color = info.IsDiscovered ? Color.white : Color.black;
            _visual?.SetColor(color);
            _iconImage.color = color;
            if (_unknownMark != null) _unknownMark.SetActive(!info.IsDiscovered);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void SetSelected(bool isSelected)
        {
            EnsureCursorOrigin();
            _isSelected = isSelected;
            _cursor.gameObject.SetActive(isSelected);
            _selectedFrame.SetActive(isSelected);
            _cursor.anchoredPosition = _cursorOrigin;
        }

        private void EnsureCursorOrigin()
        {
            if (_hasCursorOrigin) return;
            _cursorOrigin = _cursor.anchoredPosition;
            _hasCursorOrigin = true;
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke(this);
        }

        private void Update()
        {
            if (!_isSelected) return;
            EnsureCursorOrigin();
            // 1 画素ずつの段で動かす（ドット絵の動きに合わせる）
            float offset = Mathf.Round(Mathf.Sin(Time.unscaledTime * CursorSpeed) * CursorSwing / 2f) * 2f;
            _cursor.anchoredPosition = _cursorOrigin + new Vector2(offset, 0f);
        }

        // 同じ敵のまま出し直すときは作り直さない（アイドルが頭から始まってしまうので）
        private void BindVisual(EnemyDetailInfo info)
        {
            if (_visualObject != null && _visualDefinition == info.Visual) return;

            if (_visualObject != null) Destroy(_visualObject);
            _visualObject = null;
            _visual = null;
            _visualDefinition = info.Visual;

            if (info.Visual != null)
            {
                _visual = info.Visual.Create(_visualRoot);
                _visualObject = (_visual as Component)?.gameObject;
            }

            _iconImage.sprite = info.Icon;
            _iconImage.enabled = _visualObject == null && info.Icon != null;
        }
    }
}
