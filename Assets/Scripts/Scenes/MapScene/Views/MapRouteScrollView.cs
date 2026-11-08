using System;
using UnityEngine;
using Live2DCardRogue.Common.SceneTransition;

namespace Live2DCardRogue.MapScene.Views
{
    // マップの経路（配線・部屋・プレイヤーの3つの親）を、そろって横へずらす。
    // 部屋の位置は経路の座標（ページが進むほど右へ伸びる）で置くので、今のページが画面の真ん中に来るようにここでずらす。
    // 10階層ごとのページ送りでは、格子に吸着しながら（カクカクと）1ページ分を流す。見た目だけで、どこへずらすかは Presenter が決める
    public sealed class MapRouteScrollView : MonoBehaviour
    {
        // 3つの親は位置もアンカーも同じにしてある（部屋の位置からプレイヤーの位置を出せるように）。そろってずらす
        [SerializeField] private RectTransform[] _layers = new RectTransform[0];
        // 流している間の位置をこの間隔の格子に吸着させる
        [SerializeField][Min(0f)] private float _step = 12f;

        private Vector2[] _basePositions;
        private float _offset;
        private float _from;
        private float _to;
        private float _duration;
        private float _time;
        private bool _isPlaying;
        private Action _onFinished;

        public bool IsPlaying => _isPlaying;

        /// <summary>経路の座標の x = offset が、画面の真ん中に来るようにする（すぐに）</summary>
        public void SetOffset(float offset)
        {
            _isPlaying = false;
            _onFinished = null;
            Apply(offset);
        }

        /// <summary>from から to へ、duration 秒で流す。終わったら onFinished</summary>
        public void PlayScroll(float from, float to, float duration, Action onFinished = null)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(0.01f, duration);
            _time = 0f;
            _isPlaying = true;
            _onFinished = onFinished;
            Apply(from);
        }

        private void Update()
        {
            if (!_isPlaying) return;
            // 画面の切り替えの覆いの下では進めない（流れる途中が見えるように）
            if (SceneTransitionEffect.IsTransitioning) return;

            _time += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            float p = Mathf.Clamp01(_time / _duration);
            float eased = p * p * (3f - 2f * p);
            float offset = Mathf.LerpUnclamped(_from, _to, eased);
            if (_step > 0f && p < 1f) offset = _to + Mathf.Round((offset - _to) / _step) * _step;
            Apply(offset);

            if (p < 1f) return;
            _isPlaying = false;
            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke();
        }

        private void Apply(float offset)
        {
            CaptureBasePositions();
            _offset = offset;
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i] != null) _layers[i].anchoredPosition = _basePositions[i] - new Vector2(_offset, 0f);
            }
        }

        // シーンに置いたままの位置を、ずらす前の基準として最初に一度だけ覚える
        private void CaptureBasePositions()
        {
            if (_basePositions != null) return;
            _basePositions = new Vector2[_layers.Length];
            for (int i = 0; i < _layers.Length; i++)
            {
                _basePositions[i] = _layers[i] != null ? _layers[i].anchoredPosition : Vector2.zero;
            }
        }
    }
}
