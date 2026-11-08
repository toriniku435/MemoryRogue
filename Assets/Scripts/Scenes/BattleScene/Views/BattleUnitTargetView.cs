using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Live2DCardRogue.BattleScene.Views
{
    // カードの対象として選べるユニットの当たり判定領域。
    // クリックを流すことと、選択できる間だけ色を変えることだけを持つ
    public sealed class BattleUnitTargetView : MonoBehaviour, IPointerClickHandler
    {
        // クリック判定に使うので、透明にする場合も Graphic 自体は消さないこと（消すとレイキャストに当たらなくなる）
        [SerializeField] private Graphic _graphic;
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color _selectableColor = new Color(1f, 0.35f, 0.35f, 0.6f);

        // 選べる間は透明度を脈打たせて、押せることに気づかせる。0 なら点滅しない
        [SerializeField][Min(0f)] private float _pulseSpeed = 4f;
        [SerializeField][Range(0f, 1f)] private float _pulseMinAlphaRate = 0.45f;

        // 対象の候補一覧でカーソルを合わせている間の色（選べる色より目立たせる）
        [SerializeField] private Color _focusedColor = new Color(1f, 0.9f, 0f, 1f);

        private bool _isSelectable;
        private bool _isFocused;

        public event Action OnClicked;

        public void SetSelectable(bool isSelectable)
        {
            _isSelectable = isSelectable;
            _graphic.color = CurrentColor;
        }

        // 指されている間は、選べるかどうかに関係なく強調色にする（実行リストのカードから指す場合は対象選択中ではない）
        public void SetFocused(bool isFocused)
        {
            _isFocused = isFocused;
            _graphic.color = CurrentColor;
        }

        private Color CurrentColor => _isFocused ? _focusedColor : _isSelectable ? _selectableColor : _normalColor;

        private void Update()
        {
            if ((!_isSelectable && !_isFocused) || _pulseSpeed <= 0f) return;

            // 指している間は速く・強く脈打たせる
            float speed = _isFocused ? _pulseSpeed * 2.5f : _pulseSpeed;
            float minRate = _isFocused ? 0.75f : _pulseMinAlphaRate;
            float wave = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            var color = CurrentColor;
            color.a *= Mathf.Lerp(minRate, 1f, wave);
            _graphic.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}
