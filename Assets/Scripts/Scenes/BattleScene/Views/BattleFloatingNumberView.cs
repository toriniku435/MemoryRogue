using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // ダメージ・回復量を浮かび上がらせて消す数字。再生が終わると自分で非アクティブになり、呼び出し元が使い回す
    public sealed class BattleFloatingNumberView : MonoBehaviour
    {
        [SerializeField] private CommonText _text;
        // 何によるダメージ・回復か（カード名・状態効果名）。未設定なら数字だけ
        [SerializeField] private CommonText _causeText;
        // 原因の文字を載せる暗いタグ（どんな背景の上でも読めるように）。数字と一緒に薄れる
        [SerializeField] private CanvasGroup _causeTag;
        [SerializeField][Min(0.01f)] private float _duration = 0.9f;
        [SerializeField] private float _riseDistance = 90f;

        // 出た直後に一瞬大きくしてから戻す（ぽんっと出る感じ）
        [SerializeField] private AnimationCurve _scaleCurve = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.15f, 1.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1f));

        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        private Color _color;
        private float _elapsed;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(string text, string cause, Color color, Vector2 startPosition)
        {
            _rectTransform ??= (RectTransform)transform;
            _text.text = text;
            if (_causeText != null)
            {
                bool hasCause = !string.IsNullOrEmpty(cause);
                var causeObject = _causeTag != null ? _causeTag.gameObject : _causeText.gameObject;
                causeObject.SetActive(hasCause);
                if (hasCause) _causeText.text = cause;
            }
            _color = color;
            _startPosition = startPosition;
            _elapsed = 0f;
            gameObject.SetActive(true);
            Apply(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Apply(t);

            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Apply(float t)
        {
            // 上昇は最初に速く、後半はゆっくり
            float rise = 1f - (1f - t) * (1f - t);
            _rectTransform.anchoredPosition = _startPosition + Vector2.up * (_riseDistance * rise);
            _rectTransform.localScale = Vector3.one * _scaleCurve.Evaluate(t);

            // 後半 40% で消える
            var color = _color;
            color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            _text.color = color;
            if (_causeText != null)
            {
                var causeColor = _color;
                causeColor.a = 1f;
                _causeText.color = causeColor;
            }
            if (_causeTag != null) _causeTag.alpha = color.a;
        }
    }
}
