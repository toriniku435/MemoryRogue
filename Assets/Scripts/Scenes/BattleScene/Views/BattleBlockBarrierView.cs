using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // ブロックを、ユニットの前に張られるエネルギーの壁として見せる。
    //   ブロックを得た: 横一線から開いて光る
    //   重ねがけで増えた: 光って少し膨らむ
    //   ダメージを吸収して減った: 揺れて光り、破片が散る。張った時からの最大値に対して削れた割合で、ひびが増えていく
    //   0 になった: 破片を大きく散らして砕け、潰れて消える
    // 値の増減を見て演出を選ぶだけで、ブロックの仕組みそのものは知らない
    public sealed class BattleBlockBarrierView : MonoBehaviour
    {
        [SerializeField] private RectTransform _wall;
        [SerializeField] private Graphic _wallGraphic;
        // ひびは軽い順に並べる（削れた割合に応じて1枚ずつ見せる）
        [SerializeField] private Graphic[] _cracks;
        [SerializeField] private CommonText _valueText;
        [SerializeField] private RectTransform _badge;
        [SerializeField] private ParticleSystem _hitParticle;
        [SerializeField] private ParticleSystem _shatterParticle;

        [SerializeField] private Color _wallColor = Color.white;
        [SerializeField] private Color _flashColor = new Color(0.8f, 1f, 1f, 1f);
        [SerializeField][Min(0.01f)] private float _openDuration = 0.18f;
        [SerializeField][Min(0.01f)] private float _hitDuration = 0.25f;
        [SerializeField][Min(0.01f)] private float _shatterDuration = 0.3f;
        [SerializeField] private float _hitShake = 12f;

        private int _value;
        // 張ってから今までの最大値（ひびの進み具合の基準）
        private int _peak;
        private bool _isInitialized;
        private Vector2 _wallOrigin;
        private float _openTime = float.MaxValue;
        private float _hitTime = float.MaxValue;
        private float _shatterTime = float.MaxValue;
        private float _pulseTime = float.MaxValue;

        // animate: 増減に応じた演出を出すか（最初の表示では出さない）
        public void SetValue(int value, bool animate)
        {
            EnsureInitialized();
            int previous = _value;
            _value = Mathf.Max(0, value);

            if (_value > 0)
            {
                if (previous == 0)
                {
                    // 新しく張った：ひびの基準も張り直す
                    _peak = _value;
                    _shatterTime = float.MaxValue;
                    gameObject.SetActive(true);
                    _openTime = animate ? 0f : float.MaxValue;
                    if (animate) AudioUseCase.PlaySe(SeId.BlockGain);
                }
                else if (_value > previous)
                {
                    _peak = Mathf.Max(_peak, _value);
                    if (animate)
                    {
                        _pulseTime = 0f;
                        AudioUseCase.PlaySe(SeId.BlockGain);
                    }
                }
                else if (_value < previous && animate)
                {
                    _hitTime = 0f;
                    if (_hitParticle != null) _hitParticle.Play();
                    AudioUseCase.PlaySe(SeId.BlockAbsorb);
                }
            }
            else if (previous > 0)
            {
                if (animate)
                {
                    _shatterTime = 0f;
                    if (_shatterParticle != null) _shatterParticle.Play();
                    AudioUseCase.PlaySe(SeId.BlockBreak);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            else if (!animate)
            {
                gameObject.SetActive(false);
            }

            if (_valueText != null) _valueText.text = _value.ToString();
            UpdateCracks();
            Apply();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            if (_wall != null) _wallOrigin = _wall.anchoredPosition;
        }

        // 削れた割合が大きいほど、重いひびまで見せる
        private void UpdateCracks()
        {
            if (_cracks == null) return;
            float damaged = _peak > 0 ? 1f - (float)_value / _peak : 0f;
            for (int i = 0; i < _cracks.Length; i++)
            {
                float threshold = (i + 1f) / (_cracks.Length + 1f);
                if (_cracks[i] != null) _cracks[i].gameObject.SetActive(_value > 0 && damaged >= threshold);
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 15f);
            _openTime += dt;
            _hitTime += dt;
            _shatterTime += dt;
            _pulseTime += dt;
            Apply();

            if (_value == 0 && _shatterTime >= _shatterDuration && _shatterTime < float.MaxValue)
            {
                _shatterTime = float.MaxValue;
                gameObject.SetActive(false);
            }
        }

        private void Apply()
        {
            if (_wall == null) return;

            float scaleX = 1f;
            float scaleY = 1f;
            var offset = Vector2.zero;
            float flash = 0f;

            if (_openTime < _openDuration)
            {
                float p = _openTime / _openDuration;
                scaleY = Mathf.Lerp(0.05f, 1f, 1f - (1f - p) * (1f - p));
                flash = 1f - p;
            }

            if (_pulseTime < _hitDuration)
            {
                float p = _pulseTime / _hitDuration;
                float s = 1f + 0.08f * Mathf.Sin(p * Mathf.PI);
                scaleX *= s;
                scaleY *= s;
                flash = Mathf.Max(flash, 1f - p);
            }

            if (_hitTime < _hitDuration)
            {
                float p = _hitTime / _hitDuration;
                offset = Random.insideUnitCircle * _hitShake * (1f - p);
                flash = Mathf.Max(flash, 1f - p);
            }

            if (_value == 0 && _shatterTime < _shatterDuration)
            {
                // 砕ける：一瞬光り、横に広がりながら潰れて消える
                float p = _shatterTime / _shatterDuration;
                scaleX = Mathf.Lerp(1f, 1.25f, p);
                scaleY = Mathf.Lerp(1f, 0.02f, p * p);
                offset = Random.insideUnitCircle * _hitShake * (1f - p);
                flash = 1f - p;
            }

            _wall.localScale = new Vector3(scaleX, scaleY, 1f);
            _wall.anchoredPosition = _wallOrigin + offset;

            if (_wallGraphic != null)
            {
                // 削れるほど少し薄くする（壊れかけに見えるように）
                float integrity = _peak > 0 ? Mathf.Clamp01((float)_value / _peak) : 1f;
                var color = Color.Lerp(_wallColor, _flashColor, flash);
                color.a = _wallColor.a * Mathf.Lerp(0.55f, 1f, integrity);
                if (_value == 0) color.a = _wallColor.a * (1f - Mathf.Clamp01(_shatterTime / _shatterDuration));
                _wallGraphic.color = color;
            }
        }
    }
}
