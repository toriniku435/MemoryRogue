using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Live2DCardRogue.BattleScene.Views
{
    // 対象選択中に、選んだカードのすぐ上に出す対象の候補一覧。
    // カードが浮き上がったり並び直したりしても付いていくよう、位置は毎フレーム目印（カード）から取り直す。
    // 候補はスクリプトで横一列に並べる（手札や実行リストと同じく Layout Group は使わない）
    public sealed class BattleTargetPickerView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>カーソルがパネル（候補を含む）に乗った / 外れた。カードから候補へ移る間に選択を外さないために使う</summary>
        public event Action<bool> OnPointerOverChanged;

        public void OnPointerEnter(PointerEventData eventData) => OnPointerOverChanged?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData) => OnPointerOverChanged?.Invoke(false);

        private void OnDisable() => OnPointerOverChanged?.Invoke(false);

        // 背景パネル。ピボットは下中央（カードの上端から上へ伸びる）
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _optionParent;
        [SerializeField] private BattleTargetOptionView _optionPrefab;
        [SerializeField] private float _optionWidth = 104f;
        [SerializeField] private float _optionSpacing = 12f;
        [SerializeField] private float _padding = 16f;
        // 見出しが収まる最小の幅。候補が1件のときでも見出しがはみ出さないように
        [SerializeField] private float _minPanelWidth = 250f;
        // カードの上端からどれだけ離すか
        [SerializeField] private float _offsetY = 12f;
        [SerializeField][Min(0.01f)] private float _openDuration = 0.12f;

        private readonly List<BattleTargetOptionView> _options = new List<BattleTargetOptionView>();
        private Func<RectTransform> _anchorProvider;
        private RectTransform _anchor;
        private float _openTime;

        public void SetOptionCount(int count)
        {
            for (int i = _options.Count; i < count; i++)
            {
                _options.Add(Instantiate(_optionPrefab, _optionParent));
            }

            float total = count * _optionWidth + Mathf.Max(0, count - 1) * _optionSpacing;
            for (int i = 0; i < _options.Count; i++)
            {
                bool active = i < count;
                _options[i].SetActive(active);
                if (active)
                {
                    _options[i].RectTransform.anchoredPosition = new Vector2(-total / 2f + _optionWidth / 2f + i * (_optionWidth + _optionSpacing), 0f);
                }
            }

            _panel.sizeDelta = new Vector2(Mathf.Max(total + _padding * 2f, _minPanelWidth), _panel.sizeDelta.y);
        }

        public BattleTargetOptionView GetOption(int index)
        {
            return _options[index];
        }

        // 目印（選んでいるカード）は毎フレーム聞き直す。別のカードに変わったら、そちらへ移って開き直す
        public void Show(Func<RectTransform> anchorProvider)
        {
            _anchorProvider = anchorProvider;
            _anchor = anchorProvider?.Invoke();
            _openTime = 0f;
            gameObject.SetActive(true);
            Follow();
        }

        public void Hide()
        {
            _anchorProvider = null;
            _anchor = null;
            gameObject.SetActive(false);
        }

        // 手札のカードが動いた後に合わせたいので LateUpdate で追う
        private void LateUpdate()
        {
            _openTime += Time.unscaledDeltaTime;
            var anchor = _anchorProvider?.Invoke();
            if (anchor != null && anchor != _anchor)
            {
                _anchor = anchor;
                _openTime = 0f;
            }
            Follow();
        }

        private void Follow()
        {
            if (_anchor == null) return;

            // カードの上端（カード自身の縮小込み）の画面上の位置へ
            var top = _anchor.TransformPoint(new Vector3(0f, _anchor.rect.yMax, 0f));
            _panel.position = top;
            _panel.anchoredPosition += Vector2.up * _offsetY;

            // 横一線から開く（他のサイバー演出とそろえる）
            float p = Mathf.Clamp01(_openTime / _openDuration);
            _panel.localScale = new Vector3(1f, Mathf.Lerp(0.1f, 1f, 1f - (1f - p) * (1f - p)), 1f);
        }
    }
}
