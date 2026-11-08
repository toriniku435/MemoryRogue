using System;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.BattleScene.Views
{
    // カードがどこへ去るか。去り方（見た目）と行き先の意味を兼ねる
    public enum CardExitStyle
    {
        // 捨て札へ
        Discard,
        // 手札から実行リスト（メモリ）へ積まれる
        Upload,
        // 実行リストのカードが実行されて消費される
        Execute,
        // 実行リストから手札へ戻る
        Return,
    }

    // カード1枚の動き（移動・登場・退場・選択中の見た目）を受け持つ。
    // 位置は Presenter が決めた目標へ「カクカク」と格子に沿って寄せる（デジタルっぽい動き）。
    // 登場・退場はブラウン管のように横一線に潰れた状態を経由し、明滅と横ぶれ（グリッチ）を乗せる。
    // 見た目だけの処理で、終わったことは退場のコールバックでしか伝えない
    public sealed class BattleCardMotionView : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Moving,
            Entering,
            Exiting,
        }

        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        // 選択中に脈打つ縁（未設定でもよい）
        [SerializeField] private Graphic _selectGlow;
        // 実行されたときに一瞬カード全体を光らせる板（未設定でもよい）
        [SerializeField] private Graphic _flash;
        [SerializeField] private Color _selectColor = new Color(0f, 0.94f, 1f, 1f);

        [Header("移動")]
        [SerializeField][Min(0.01f)] private float _moveDuration = 0.16f;
        // 移動中の位置をこの間隔の格子に吸着させる（0 ならなめらかに動く）
        [SerializeField][Min(0f)] private float _step = 8f;

        [Header("登場・退場")]
        [SerializeField][Min(0.01f)] private float _enterDuration = 0.3f;
        [SerializeField][Min(0.01f)] private float _executeFlashDuration = 0.12f;
        [SerializeField][Min(0.01f)] private float _collapseDuration = 0.14f;
        [SerializeField][Min(0.01f)] private float _travelDuration = 0.2f;

        [Header("グリッチ")]
        [SerializeField][Min(0.01f)] private float _glitchInterval = 0.04f;
        [SerializeField] private float _jitter = 10f;
        [SerializeField] private float _selectGlitchDuration = 0.12f;

        private bool _isInitialized;
        private Vector3 _baseScale;
        private State _state;
        private Vector2 _from;
        private Vector2 _to;
        private float _time;
        private float _delay;
        private CardExitStyle _exitStyle;
        private Action _onExitComplete;

        private bool _isSelected;
        private float _selectGlitchLeft;
        private float _glitchTimer;
        private float _jitterX;
        private float _flicker = 1f;

        public bool IsExiting => _state == State.Exiting;

        // 使い回す前に、前回の退場で潰れた見た目などを元に戻す
        public void ResetForReuse()
        {
            EnsureInitialized();
            _state = State.Idle;
            _isSelected = false;
            _selectGlitchLeft = 0f;
            _jitterX = 0f;
            _flicker = 1f;
            _root.localScale = _baseScale;
            SetAlpha(1f);
            SetRaycast(true);
            SetGraphicAlpha(_selectGlow, 0f);
            SetGraphicAlpha(_flash, 0f);
        }

        public void SnapTo(Vector2 position)
        {
            EnsureInitialized();
            _to = position;
            if (_state == State.Moving) _state = State.Idle;
            if (_state == State.Idle) _root.anchoredPosition = position;
        }

        // 登場中なら行き先だけ差し替え、退場中なら無視する
        public void MoveTo(Vector2 position)
        {
            EnsureInitialized();
            if (_state == State.Exiting) return;
            if (_state == State.Entering)
            {
                _to = position;
                return;
            }
            if (_state == State.Idle && (position - _root.anchoredPosition).sqrMagnitude < 0.01f)
            {
                _to = position;
                return;
            }

            _from = _root.anchoredPosition;
            _to = position;
            _time = 0f;
            _state = State.Moving;
        }

        public void PlayEnter(Vector2 from, Vector2 to, float delay)
        {
            EnsureInitialized();
            _from = from;
            _to = to;
            _delay = delay;
            _time = 0f;
            _state = State.Entering;
            // 飛んでいる途中のカードは押せない
            SetRaycast(false);
            Apply();
        }

        public void PlayExit(Vector2 to, CardExitStyle style, float delay, Action onComplete)
        {
            EnsureInitialized();
            _from = _root.anchoredPosition;
            _to = to;
            _exitStyle = style;
            _delay = delay;
            _time = 0f;
            _onExitComplete = onComplete;
            _state = State.Exiting;
            _isSelected = false;
            SetRaycast(false);
            Apply();
        }

        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            if (_isSelected == isSelected) return;
            _isSelected = isSelected;
            _selectGlitchLeft = _selectGlitchDuration;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            if (_root == null) _root = (RectTransform)transform;
            _baseScale = _root.localScale;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            float dt = Time.deltaTime;
            _time += dt;
            _selectGlitchLeft -= dt;
            TickGlitch(dt);

            Apply();
        }

        private void Apply()
        {
            switch (_state)
            {
                case State.Idle:
                    _root.anchoredPosition = _to + new Vector2(SelectJitter(), 0f);
                    break;
                case State.Moving:
                    ApplyMove();
                    break;
                case State.Entering:
                    ApplyEnter();
                    break;
                case State.Exiting:
                    ApplyExit();
                    break;
            }

            ApplySelectGlow();
        }

        private void ApplyMove()
        {
            float p = Mathf.Clamp01(_time / _moveDuration);
            _root.anchoredPosition = Stepped(_from, _to, EaseOut(p)) + new Vector2(SelectJitter(), 0f);
            if (p >= 1f) _state = State.Idle;
        }

        // 横一線に潰れた状態で飛んできて、着いたところで開く
        private void ApplyEnter()
        {
            float t = _time - _delay;
            if (t < 0f)
            {
                SetAlpha(0f);
                return;
            }

            float p = Mathf.Clamp01(t / _enterDuration);
            _root.anchoredPosition = Stepped(_from, _to, EaseOut(Mathf.Clamp01(p / 0.7f))) + new Vector2(p < 0.8f ? _jitterX : 0f, 0f);

            float open = p < 0.6f ? 0f : EaseOut((p - 0.6f) / 0.4f);
            _root.localScale = new Vector3(_baseScale.x * Mathf.Lerp(0.5f, 1f, open), _baseScale.y * Mathf.Lerp(0.06f, 1f, open), _baseScale.z);
            SetAlpha(p < 0.7f ? _flicker : 1f);

            if (p >= 1f)
            {
                _root.localScale = _baseScale;
                SetAlpha(1f);
                SetRaycast(true);
                _state = State.Idle;
            }
        }

        // （実行なら一瞬光って膨らみ）→ グリッチしながら横一線に潰れる → 行き先へ飛んで消える
        private void ApplyExit()
        {
            float t = _time - _delay;
            if (t < 0f) return;

            float flashTime = _exitStyle == CardExitStyle.Execute ? _executeFlashDuration : 0f;
            float collapseEnd = flashTime + _collapseDuration;
            float end = collapseEnd + _travelDuration;

            if (t < flashTime)
            {
                float f = t / flashTime;
                SetGraphicAlpha(_flash, 1f - f * 0.6f);
                _root.localScale = _baseScale * Mathf.Lerp(1f, 1.12f, EaseOut(f));
                _root.anchoredPosition = _from;
                return;
            }

            float swell = _exitStyle == CardExitStyle.Execute ? 1.12f : 1f;
            if (t < collapseEnd)
            {
                float c = (t - flashTime) / _collapseDuration;
                SetGraphicAlpha(_flash, _exitStyle == CardExitStyle.Execute ? 0.4f * (1f - c) : 0f);
                _root.localScale = new Vector3(_baseScale.x * swell * Mathf.Lerp(1f, 1.3f, c), _baseScale.y * swell * Mathf.Lerp(1f, 0.05f, EaseIn(c)), _baseScale.z);
                _root.anchoredPosition = _from + new Vector2(_jitterX, 0f);
                SetAlpha(_flicker);
                return;
            }

            float m = Mathf.Clamp01((t - collapseEnd) / _travelDuration);
            SetGraphicAlpha(_flash, 0f);
            _root.anchoredPosition = Stepped(_from, _to, EaseIn(m));
            _root.localScale = new Vector3(_baseScale.x * swell * Mathf.Lerp(1.3f, 0.2f, m), _baseScale.y * swell * 0.05f, _baseScale.z);
            SetAlpha(1f - m);

            if (t >= end)
            {
                _state = State.Idle;
                var callback = _onExitComplete;
                _onExitComplete = null;
                callback?.Invoke();
            }
        }

        private void ApplySelectGlow()
        {
            if (_selectGlow == null) return;
            if (!_isSelected || _state == State.Exiting)
            {
                SetGraphicAlpha(_selectGlow, 0f);
                return;
            }

            // 選択した瞬間はチラつかせ、その後はゆっくり脈打つ
            float alpha = _selectGlitchLeft > 0f ? _flicker : 0.6f + 0.4f * Mathf.Sin(Time.time * 8f);
            var color = _selectColor;
            color.a = alpha;
            _selectGlow.color = color;
        }

        // ノイズは一定間隔でだけ振り直す（毎フレーム変えると細かすぎて見えない）
        private void TickGlitch(float dt)
        {
            _glitchTimer -= dt;
            if (_glitchTimer > 0f) return;
            _glitchTimer = _glitchInterval;
            _jitterX = UnityEngine.Random.Range(-_jitter, _jitter);
            _flicker = UnityEngine.Random.value < 0.35f ? 0.25f : 1f;
        }

        private float SelectJitter()
        {
            return _selectGlitchLeft > 0f ? _jitterX * 0.6f : 0f;
        }

        // 行き先に向けて格子に吸着させた位置。最後は必ず行き先ぴったりに着く
        private Vector2 Stepped(Vector2 from, Vector2 to, float t)
        {
            var p = Vector2.LerpUnclamped(from, to, t);
            if (_step <= 0f || t >= 1f) return p;
            var d = p - to;
            return to + new Vector2(Mathf.Round(d.x / _step) * _step, Mathf.Round(d.y / _step) * _step);
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = alpha;
        }

        private void SetRaycast(bool blocks)
        {
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = blocks;
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;
    }
}
