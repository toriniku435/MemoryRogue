using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // 開封結果に並ぶカード1枚分。カーソルが乗った・外れたを伝えるだけで、詳細の出し方は知らない。
    //   出てくる: 横一線からチラつきながら開く（データを受信したように見せる）。裏向きで出すこともできる
    //   めくる  : 光を溜めて震え（溜める長さと色は呼び出し側がレアリティで決める）、裏から表へ横に返る
    //   消える  : 横に伸びながら一線に潰れて消える
    // 表になるまではカーソルに反応しない（中身が見える前に詳細が出てしまわないように）
    public sealed class ChipInstallCardView : CardView, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _body;
        [SerializeField] private CanvasGroup _canvasGroup;
        // 裏面（カードの上に重ねる）と、カードの後ろで光る縁
        [SerializeField] private GameObject _backFace;
        [SerializeField] private Image _rarityGlow;
        [SerializeField][Min(0.01f)] private float _appearDuration = 0.18f;
        [SerializeField][Min(0.01f)] private float _disappearDuration = 0.12f;
        [SerializeField][Min(0.01f)] private float _flipDuration = 0.22f;
        // 説明文。読めない大きさまで縮めて並べるときは隠す（詳細は右側に大きく出る）
        [SerializeField] private GameObject _descriptionObject;

        // 出てくる動きは、めくる動きと重なって進む（出切る前にめくる予約が入る）ので、タイマーを別に持つ
        private enum Mode { Idle, Reveal, Disappear }

        private Action _onEntered;
        private Action _onExited;
        private Mode _mode;
        private float _delay;
        private float _time;
        private float _appearDelay;
        private float _appearTime = float.MaxValue;

        private float _chargeDuration;
        private Color _glowColor;
        private bool _keepGlow;
        private bool _isFaceUp;
        private Action _onFlipped;

        public void Initialize(CardData cardData, Action onEntered, Action onExited)
        {
            _onEntered = onEntered;
            _onExited = onExited;
            SetCardData(cardData);
        }

        public void SetDescriptionVisible(bool isVisible)
        {
            if (_descriptionObject != null) _descriptionObject.SetActive(isVisible);
        }

        /// <summary>delay 秒待ってから出てくる。faceDown なら裏向きのまま出る（めくるのは PlayReveal）</summary>
        public void PlayAppear(float delay, bool faceDown = false)
        {
            SetFaceUp(!faceDown);
            SetGlow(Color.clear);
            _mode = Mode.Idle;
            _appearDelay = delay;
            _appearTime = 0f;
            ApplyAppear();
        }

        /// <summary>
        /// delay 秒待ち、charge 秒のあいだ glow の色で光を溜めてから表に返す。表になった瞬間に onFlipped を呼ぶ。
        /// keepGlow なら返った後も縁を薄く光らせ続ける（レア以上の目印）
        /// </summary>
        public void PlayReveal(float delay, float charge, Color glow, bool keepGlow, Action onFlipped)
        {
            _chargeDuration = charge;
            _glowColor = glow;
            _keepGlow = keepGlow;
            _onFlipped = onFlipped;
            BeginMode(Mode.Reveal, delay);
        }

        /// <summary>動きなしで表にする（スキップ用）</summary>
        public void RevealImmediately(Color glow, bool keepGlow)
        {
            _mode = Mode.Idle;
            _appearTime = float.MaxValue;
            _onFlipped = null;
            SetFaceUp(true);
            if (_body != null) _body.localScale = Vector3.one;
            if (_body != null) _body.anchoredPosition = Vector2.zero;
            SetGlow(keepGlow ? WithAlpha(glow, 0.5f) : Color.clear);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            SetInteractable(true);
        }

        /// <summary>delay 秒待ってから消える</summary>
        public void PlayDisappear(float delay)
        {
            _appearTime = float.MaxValue;
            BeginMode(Mode.Disappear, delay);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onEntered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onExited?.Invoke();
        }

        private void OnDisable()
        {
            // 閉じたカードが「乗ったまま」を残さないように
            _onExited?.Invoke();
        }

        private void BeginMode(Mode mode, float delay)
        {
            _mode = mode;
            _delay = delay;
            _time = 0f;
            Apply();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            if (_appearTime != float.MaxValue)
            {
                _appearTime += dt;
                ApplyAppear();
            }

            if (_mode == Mode.Idle) return;
            _time += dt;
            Apply();
        }

        private void Apply()
        {
            switch (_mode)
            {
                case Mode.Reveal: ApplyReveal(); break;
                case Mode.Disappear: ApplyDisappear(); break;
            }
        }

        private void ApplyAppear()
        {
            float p = Mathf.Clamp01((_appearTime - _appearDelay) / _appearDuration);
            float open = 1f - (1f - p) * (1f - p);
            if (_body != null) _body.localScale = new Vector3(Mathf.Lerp(1.2f, 1f, open), Mathf.Lerp(0.04f, 1f, open), 1f);
            if (_canvasGroup != null)
            {
                bool flicker = p > 0f && p < 1f && UnityEngine.Random.value < 0.3f;
                _canvasGroup.alpha = p <= 0f ? 0f : (flicker ? 0.35f : 1f);
            }
            SetInteractable(p >= 1f && _isFaceUp && _mode == Mode.Idle);
            if (p >= 1f) _appearTime = float.MaxValue;
        }

        private void ApplyReveal()
        {
            float t = _time - _delay;
            if (t < 0f) return;

            if (t < _chargeDuration)
            {
                // 光を溜める：だんだん強く脈打ち、震えも大きくなる
                float p = t / _chargeDuration;
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.Lerp(10f, 40f, p));
                SetGlow(WithAlpha(_glowColor, Mathf.Lerp(0.2f, 1f, p) * (0.6f + 0.4f * pulse)));
                float shake = p * 6f;
                if (_body != null) _body.anchoredPosition = new Vector2(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake));
                return;
            }

            float flip = Mathf.Clamp01((t - _chargeDuration) / _flipDuration);
            if (_body != null) _body.anchoredPosition = Vector2.zero;

            // 前半で横に畳み、半分で裏から表へ替え、後半で開く
            if (flip < 0.5f)
            {
                if (_body != null) _body.localScale = new Vector3(Mathf.Lerp(1f, 0f, flip * 2f), 1f + flip * 0.2f, 1f);
            }
            else
            {
                if (!_isFaceUp)
                {
                    SetFaceUp(true);
                    var callback = _onFlipped;
                    _onFlipped = null;
                    callback?.Invoke();
                }
                float q = (flip - 0.5f) * 2f;
                if (_body != null) _body.localScale = new Vector3(Mathf.Lerp(0f, 1f, 1f - (1f - q) * (1f - q)), Mathf.Lerp(1.1f, 1f, q), 1f);
            }

            // 返った直後に一番強く光り、以後は薄く残す（ノーマルは消える）
            float after = Mathf.Clamp01((flip - 0.5f) * 2f);
            float rest = _keepGlow ? 0.5f : 0f;
            SetGlow(WithAlpha(_glowColor, flip < 0.5f ? 1f : Mathf.Lerp(1f, rest, after)));

            if (flip >= 1f)
            {
                _mode = Mode.Idle;
                SetInteractable(true);
            }
        }

        private void ApplyDisappear()
        {
            float p = Mathf.Clamp01((_time - _delay) / _disappearDuration);
            if (_body != null) _body.localScale = new Vector3(Mathf.Lerp(1f, 1.2f, p), Mathf.Lerp(1f, 0.04f, p * p), 1f);
            if (_canvasGroup != null)
            {
                bool flicker = p > 0f && p < 1f && UnityEngine.Random.value < 0.35f;
                _canvasGroup.alpha = p >= 1f ? 0f : (flicker ? 0.35f : 1f);
            }
            SetInteractable(false);
            if (p >= 1f) _mode = Mode.Idle;
        }

        private void SetFaceUp(bool isFaceUp)
        {
            _isFaceUp = isFaceUp;
            if (_backFace != null) _backFace.SetActive(!isFaceUp);
        }

        private void SetGlow(Color color)
        {
            if (_rarityGlow == null) return;
            _rarityGlow.gameObject.SetActive(color.a > 0.001f);
            _rarityGlow.color = color;
        }

        private void SetInteractable(bool isInteractable)
        {
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = isInteractable;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
