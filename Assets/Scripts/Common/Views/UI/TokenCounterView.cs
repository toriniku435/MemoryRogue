using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // トークンの数字。増えるときは数字が刻みながら上がり、上がりきったらアイコンが弾む。
    // 何を表示するか（いくつに増えるか）は呼ぶ側が決め、ここは見せ方だけを持つ。
    // 時間は実時間で進め、1フレームの上限を付けている（処理落ちで動きが飛ばないように）
    public sealed class TokenCounterView : MonoBehaviour
    {
        [SerializeField] private CommonText _valueText;
        // 数字の書式。{0} に値が入る（"+{0}" なら獲得量の表示になる）
        [SerializeField] private string _format = "{0}";
        // 上がりきったときに弾ませるもの（アイコンなど）。未設定でもよい
        [SerializeField] private RectTransform _pulseTarget;
        [SerializeField][Min(0.01f)] private float _countDuration = 0.6f;
        [SerializeField][Min(0.01f)] private float _pulseDuration = 0.25f;
        [SerializeField][Min(1f)] private float _pulseScale = 1.35f;
        // 刻むたびに鳴らす音（同じ音の連打は SoundLibrary 側で間引かれる）
        [SerializeField] private SeId _tickSe = SeId.TokenTick;

        private int _from;
        private int _to;
        private float _time;
        private float _pulseTime = -1f;
        private bool _isCounting;
        private int _shown = int.MinValue;
        private Action _onFinished;

        public bool IsCounting => _isCounting;

        /// <summary>動きなしでその値にする</summary>
        public void SetValue(int value)
        {
            _isCounting = false;
            _onFinished = null;
            _to = value;
            Show(value, playTick: false);
        }

        /// <summary>今の表示から value まで数字を上げていき、上がりきったら onFinished を呼ぶ</summary>
        public void CountTo(int value, Action onFinished = null)
        {
            _from = _shown == int.MinValue ? 0 : _shown;
            _to = value;
            _time = 0f;
            _onFinished = onFinished;
            _isCounting = _from != _to;
            if (!_isCounting) Finish();
        }

        /// <summary>数え上げを飛ばして、最後の値にする</summary>
        public void Finish()
        {
            if (!_isCounting && _onFinished == null) return;
            _isCounting = false;
            Show(_to, playTick: false);
            _pulseTime = 0f;

            var onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_isCounting)
            {
                _time += dt;
                float t = Mathf.Clamp01(_time / _countDuration);
                // 最初は速く、最後はゆっくり（いくつになったか読めるように）
                float eased = 1f - (1f - t) * (1f - t);
                Show(Mathf.RoundToInt(Mathf.Lerp(_from, _to, eased)), playTick: true);
                if (t >= 1f) Finish();
            }

            if (_pulseTime >= 0f && _pulseTarget != null)
            {
                _pulseTime += dt;
                float t = Mathf.Clamp01(_pulseTime / _pulseDuration);
                float scale = Mathf.Lerp(_pulseScale, 1f, 1f - (1f - t) * (1f - t));
                _pulseTarget.localScale = new Vector3(scale, scale, 1f);
                if (t >= 1f) _pulseTime = -1f;
            }
        }

        private void Show(int value, bool playTick)
        {
            if (value == _shown) return;
            _shown = value;
            _valueText.text = string.Format(_format, value);
            if (playTick) AudioUseCase.PlaySe(_tickSe);
        }
    }
}
