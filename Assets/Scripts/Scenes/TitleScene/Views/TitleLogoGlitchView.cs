using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.TitleScene.Views
{
    // タイトルロゴに、数秒おきに一瞬だけグリッチ（横ぶれ・シアン/マゼンタの残像・明滅）を乗せる。
    // ずっと揺れていると読みにくいので、たまに起きるだけにしてある。見た目だけで、入力は受けない
    public sealed class TitleLogoGlitchView : MonoBehaviour
    {
        [SerializeField] private RectTransform _logo;
        [SerializeField] private CommonText _text;
        // 色ずれの残像（ロゴの子）。左右に逆向きにずらす
        [SerializeField] private CommonText[] _ghosts;

        [SerializeField] private Vector2 _intervalRange = new Vector2(2.5f, 4.5f);
        [SerializeField][Min(0.01f)] private float _glitchDuration = 0.22f;
        [SerializeField][Min(0.01f)] private float _tickInterval = 0.04f;
        [SerializeField] private float _jitter = 14f;
        [SerializeField] private float _ghostOffset = 10f;

        private Vector2 _origin;
        private float _nextGlitch;
        private float _glitchLeft;
        private float _tickTimer;

        private void Awake()
        {
            _origin = _logo.anchoredPosition;
            foreach (var ghost in _ghosts) ghost.text = _text.text;
            ScheduleNext();
            SetGlitch(false);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_glitchLeft > 0f)
            {
                _glitchLeft -= dt;
                _tickTimer -= dt;
                if (_glitchLeft <= 0f)
                {
                    SetGlitch(false);
                    ScheduleNext();
                }
                else if (_tickTimer <= 0f)
                {
                    _tickTimer = _tickInterval;
                    SetGlitch(true);
                }
                return;
            }

            _nextGlitch -= dt;
            if (_nextGlitch <= 0f)
            {
                _glitchLeft = _glitchDuration;
                _tickTimer = 0f;
            }
        }

        private void ScheduleNext()
        {
            _nextGlitch = Random.Range(_intervalRange.x, _intervalRange.y);
        }

        private void SetGlitch(bool isOn)
        {
            _logo.anchoredPosition = _origin + (isOn ? new Vector2(Random.Range(-_jitter, _jitter), 0f) : Vector2.zero);
            _text.alpha = isOn && Random.value < 0.3f ? 0.4f : 1f;
            for (int i = 0; i < _ghosts.Length; i++)
            {
                _ghosts[i].gameObject.SetActive(isOn);
                if (!isOn) continue;
                float side = i % 2 == 0 ? -1f : 1f;
                _ghosts[i].rectTransform.anchoredPosition = new Vector2(side * _ghostOffset * Random.Range(0.5f, 1.5f), 0f);
            }
        }
    }
}
