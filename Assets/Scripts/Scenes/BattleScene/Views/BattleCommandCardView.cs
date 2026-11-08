using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // 実行リストに並ぶCardView。カーソルを乗せている間だけ、左右の入れ替えボタンを出す。
    // ボタンはこのカードの矩形の内側に置いてある（uGUIのenter/exitは階層をさかのぼるので、
    // 子のボタンへカーソルを移してもこのカードのOnPointerExitは飛ばない）。
    // 1枚の View は1つのコマンドに付いたまま動く（並べ替えではカードごと枠を移る）
    public sealed class BattleCommandCardView : CardView, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private CommonButton _moveLeftButton;
        [SerializeField] private CommonButton _moveRightButton;

        // 命令を書き込む演出。未設定なら演出なしで並ぶだけ
        [SerializeField] private BattleCommandWriteEffectView _writeEffect;

        // 移動・退場の動き。未設定なら瞬時に置き換わる
        [SerializeField] private BattleCardMotionView _motion;

        private Action _onClicked;
        private Action<bool> _onHoverChanged;
        private Action _onMoveLeft;
        private Action _onMoveRight;
        private bool _canMoveLeft;
        private bool _canMoveRight;
        private bool _isHovered;
        // タッチではカーソルの出入りを使わない（指が触れた瞬間と離した瞬間に来るだけなので）。矢印はタップで選んだ間（_isPinned）に出す
        private bool _hoverEnabled = true;
        private bool _isPinned;

        private void Awake()
        {
            // 押された時点の並び順で動かしたいので、ボタン側はデリゲートを都度読み直す形にしてある
            _moveLeftButton.onClick.AddListener(() => _onMoveLeft?.Invoke());
            _moveRightButton.onClick.AddListener(() => _onMoveRight?.Invoke());
        }

        public bool IsExiting => _motion != null && _motion.IsExiting;

        // 並び替えで位置が変わるたびにPresenterから呼び直される
        public void Initialize(CardData cardData, Action onClicked, Action onMoveLeft, Action onMoveRight, Action<bool> onHoverChanged)
        {
            _onClicked = onClicked;
            _onHoverChanged = onHoverChanged;
            _onMoveLeft = onMoveLeft;
            _onMoveRight = onMoveRight;
            SetCardData(cardData);
        }

        // 使い回しで取り出したときに、前回の退場の見た目を戻してから置く
        public void Place(Vector2 position)
        {
            if (_motion == null)
            {
                RectTransform.anchoredPosition = position;
                return;
            }
            _motion.ResetForReuse();
            _motion.SnapTo(position);
        }

        // 別の番地へ移る（並べ替え・前詰め）。移った先で「書き換え」の走査線を走らせる
        public void MoveToSlot(Vector2 position)
        {
            if (_motion != null) _motion.MoveTo(position);
            else RectTransform.anchoredPosition = position;
            if (_writeEffect != null) _writeEffect.PlayRewrite();
        }

        // 実行リストから抜けるときの演出。終わったら onComplete（Presenterが使い回しへ戻す）
        public void PlayExit(Vector2 destination, CardExitStyle style, Action onComplete)
        {
            _isPinned = false;
            SetHovered(false);
            if (_writeEffect != null) _writeEffect.Stop();
            if (_motion == null)
            {
                onComplete?.Invoke();
                return;
            }
            _motion.PlayExit(destination, style, 0f, onComplete);
        }

        // 実行リストに新しく差し込まれた（隠れていた枠が表示された）ときの演出
        private void OnEnable()
        {
            if (_writeEffect != null) _writeEffect.PlayInsert();
        }

        // 端のカードは片側にしか動かせないので、出すボタンもその分だけ
        public void SetMovable(bool canMoveLeft, bool canMoveRight)
        {
            _canMoveLeft = canMoveLeft;
            _canMoveRight = canMoveRight;
            ApplyButtonVisibility();
        }

        public void SetHoverEnabled(bool isEnabled)
        {
            _hoverEnabled = isEnabled;
            if (!isEnabled) SetHovered(false);
        }

        /// <summary>タップで選ばれている（矢印を出しておく）</summary>
        public void SetPinned(bool isPinned)
        {
            _isPinned = isPinned;
            ApplyButtonVisibility();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsExiting || !_hoverEnabled) return;
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
        }

        // カード自体のクリックは手札に戻す操作。ボタンを押した場合はボタン側がクリックを受け取るのでここには来ない
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsExiting) return;
            _onClicked?.Invoke();
        }

        // 使い回しで非アクティブにされたときはOnPointerExitが飛ばないので、ここでホバー状態を落としておく
        private void OnDisable()
        {
            _isPinned = false;
            SetHovered(false);
        }

        private void SetHovered(bool isHovered)
        {
            bool changed = _isHovered != isHovered;
            _isHovered = isHovered;
            ApplyButtonVisibility();
            if (changed) _onHoverChanged?.Invoke(isHovered);
        }

        private void ApplyButtonVisibility()
        {
            bool showsArrows = _isHovered || _isPinned;
            _moveLeftButton.gameObject.SetActive(showsArrows && _canMoveLeft);
            _moveRightButton.gameObject.SetActive(showsArrows && _canMoveRight);
        }
    }
}
