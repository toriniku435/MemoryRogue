using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // 手札のCardView。クリックをPresenterに流すだけで、今そのカードを選べるかどうかの判断は持たない。
    // 「どのカードが押されたか」はPresenterが作るコールバック側が知っているので、ここでは何も渡さない
    // (同じCardDataのカードが複数枚あっても、押された1枚を区別できるようにするため)。
    // 動き（移動・登場・退場・選択中の見た目）は _motion に任せる。未設定なら瞬時に置き換わる。
    // カーソルを合わせて少し止まったら _onHovered を流す（合わせるだけで選ぶ操作用）。
    // すぐに流さないのは、選んだカードの上の対象一覧へカーソルを動かす途中で、隣のカードをかすめただけで選択が移らないようにするため
    public sealed class BattleCardView : CardView, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BattleCardMotionView _motion;
        [SerializeField][Min(0f)] private float _hoverDwell = 0.1f;

        // 今は使えないカードに重ねて暗くする板（グレーアウト）。カードの形の画像にしてある
        [SerializeField] private Graphic _disabledOverlay;
        [SerializeField] private Color _costNormalColor = Color.white;
        // コストが払えないときのコストの数字の色
        [SerializeField] private Color _costShortColor = new Color(1f, 0.18f, 0.77f, 1f);

        // パージするバッファに選ばれた印（マゼンタの板と PURGE の文字）
        [SerializeField] private GameObject _purgeMark;

        private Action _onClicked;
        private Action _onHovered;
        private Action<bool> _onHoverChanged;
        private bool _isHovered;
        private float _hoverTime;
        private bool _hasReportedHover;

        public bool IsExiting => _motion != null && _motion.IsExiting;

        public void Initialize(CardData cardData, Action onClicked, Action onHovered, Action<bool> onHoverChanged)
        {
            _onClicked = onClicked;
            _onHovered = onHovered;
            _onHoverChanged = onHoverChanged;
            SetCardData(cardData);
        }

        // 使い回しで取り出したときに、前回の退場で潰れた見た目を戻す
        public void ResetForReuse()
        {
            if (_motion != null) _motion.ResetForReuse();
        }

        public void MoveTo(Vector2 position)
        {
            if (_motion != null) _motion.MoveTo(position);
            else RectTransform.anchoredPosition = position;
        }

        public void PlayEnter(Vector2 from, Vector2 to, float delay)
        {
            if (_motion != null) _motion.PlayEnter(from, to, delay);
            else RectTransform.anchoredPosition = to;
        }

        public void PlayExit(Vector2 destination, CardExitStyle style, float delay, Action onComplete)
        {
            if (_motion == null)
            {
                onComplete?.Invoke();
                return;
            }
            SetHovered(false);
            _motion.PlayExit(destination, style, delay, onComplete);
        }

        public void SetSelected(bool isSelected)
        {
            if (_motion != null) _motion.SetSelected(isSelected);
        }

        // isPlayable: 今選べるか（選べなければ暗くする） / isCostShort: 選べない理由がメモリー不足か（コストの数字を目立たせる）
        public void SetPlayable(bool isPlayable, bool isCostShort)
        {
            if (_disabledOverlay != null) _disabledOverlay.gameObject.SetActive(!isPlayable);
            CostText.color = isCostShort ? _costShortColor : _costNormalColor;
        }

        public void SetPurgeMarked(bool isMarked)
        {
            if (_purgeMark != null) _purgeMark.SetActive(isMarked);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsExiting) return;
            _onClicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hoverTime = 0f;
            _hasReportedHover = false;
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
        }

        // 使い回しで非表示になると OnPointerExit が来ないので、ここで落とす
        private void OnDisable()
        {
            SetHovered(false);
        }

        private void SetHovered(bool isHovered)
        {
            if (_isHovered == isHovered) return;
            _isHovered = isHovered;
            _onHoverChanged?.Invoke(isHovered);
        }

        private void Update()
        {
            if (!_isHovered || _hasReportedHover || IsExiting) return;

            _hoverTime += Time.unscaledDeltaTime;
            if (_hoverTime < _hoverDwell) return;

            _hasReportedHover = true;
            _onHovered?.Invoke();
        }
    }
}
