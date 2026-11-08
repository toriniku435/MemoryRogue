using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // 最大値ぶんのコマを横に並べ、残っている分だけ点灯させるゲージ（メモリーチップが並んでいるような見た目）。
    // 値が変わったコマは短く明滅させて、どこが増減したかを見せる。コマはスクリプトで並べる（Layout Group は使わない）。
    // 最大値が増えて並べ切れないときは、_segmentParent の幅に収まるまでコマと間隔を同じ割合で細くする
    public sealed class SegmentGaugeView : MonoBehaviour
    {
        [SerializeField] private RectTransform _segmentParent;
        [SerializeField] private Image _segmentPrefab;
        [SerializeField] private float _segmentWidth = 40f;
        [SerializeField] private float _spacing = 6f;
        [SerializeField] private Color _filledColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _emptyColor = new Color(0f, 0.94f, 1f, 0.15f);
        [SerializeField][Min(0.01f)] private float _changeFlashDuration = 0.3f;
        [SerializeField][Min(0.01f)] private float _flickerInterval = 0.04f;

        private readonly List<Image> _segments = new List<Image>();
        private readonly List<float> _flashTimes = new List<float>();
        private int _value = -1;
        private float _flickerTimer;
        private bool _flickerOn;

        public void SetValue(int value, int max)
        {
            max = Mathf.Max(0, max);
            value = Mathf.Clamp(value, 0, max);

            while (_segments.Count < max)
            {
                _segments.Add(Instantiate(_segmentPrefab, _segmentParent));
                _flashTimes.Add(0f);
            }

            float segmentWidth = _segmentWidth;
            float spacing = _spacing;
            float total = max * segmentWidth + Mathf.Max(0, max - 1) * spacing;
            float available = _segmentParent.rect.width;
            if (available > 0f && total > available)
            {
                float shrink = available / total;
                segmentWidth *= shrink;
                spacing *= shrink;
                total = available;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                bool active = i < max;
                _segments[i].gameObject.SetActive(active);
                if (!active) continue;

                var rect = _segments[i].rectTransform;
                rect.sizeDelta = new Vector2(segmentWidth, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(-total / 2f + segmentWidth / 2f + i * (segmentWidth + spacing), 0f);

                // 初回表示では明滅させない。以降は点灯状態が変わったコマだけ
                bool wasFilled = i < _value;
                bool isFilled = i < value;
                if (_value >= 0 && wasFilled != isFilled) _flashTimes[i] = _changeFlashDuration;
            }

            _value = value;
            ApplyColors();
        }

        private void Update()
        {
            if (_value < 0) return;

            float dt = Time.unscaledDeltaTime;
            _flickerTimer -= dt;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = _flickerInterval;
                _flickerOn = !_flickerOn;
            }
            for (int i = 0; i < _flashTimes.Count; i++) _flashTimes[i] -= dt;

            ApplyColors();
        }

        private void ApplyColors()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (!_segments[i].gameObject.activeSelf) continue;
                bool isFilled = i < _value;
                if (_flashTimes[i] > 0f)
                {
                    // 変わったコマは白と本来の色を行き来させる（グリッチっぽく）
                    _segments[i].color = _flickerOn ? Color.white : (isFilled ? _filledColor : _emptyColor);
                }
                else
                {
                    _segments[i].color = isFilled ? _filledColor : _emptyColor;
                }
            }
        }
    }
}
