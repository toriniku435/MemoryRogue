using System.Collections.Generic;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.BattleScene.Views
{
    // ユニットのHPが変わったときの演出（揺れ・色フラッシュ・数字・パーティクル）。
    // どれも見た目だけの処理で、何もしなくても戦闘は進む。各参照は未設定でもその演出が出ないだけ
    public sealed class BattleUnitEffectView : MonoBehaviour
    {
        [Header("揺れ・フラッシュ")]
        // 状態効果が付いたときの文字の色と、数字からずらす量（ダメージと同時に付くことが多いので重ならないよう下へ）
        [SerializeField] private Color _statusTextColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color _blockedStatusColor = new Color(0.7f, 0.72f, 0.8f);
        [SerializeField] private Vector2 _statusTextOffset = new Vector2(0f, -70f);
        private const float StatusTextStackWindow = 0.4f;
        private const float StatusTextRowHeight = 38f;
        private float _lastStatusTextTime = -1f;
        private int _statusTextRow;

        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private Graphic _flashTarget;
        [SerializeField] private float _shakeStrength = 14f;
        [SerializeField] private float _shakeDuration = 0.3f;
        // Image の色は絵に掛け算されるので、色味を乗せると絵によっては濁る。被弾は透明度の点滅にしておく
        [SerializeField] private Color _damageFlashColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color _healFlashColor = new Color(0.6f, 1f, 0.85f, 1f);
        [SerializeField] private float _flashDuration = 0.25f;

        [Header("数字")]
        [SerializeField] private BattleFloatingNumberView _floatingNumberPrefab;
        [SerializeField] private RectTransform _floatingNumberParent;
        [SerializeField] private Vector2 _floatingNumberOffset = new Vector2(0f, 60f);
        [SerializeField] private Color _damageNumberColor = new Color(1f, 0.25f, 0.5f, 1f);
        [SerializeField] private Color _healNumberColor = new Color(0.2f, 0.75f, 0.6f, 1f);

        [Header("パーティクル")]
        [SerializeField] private ParticleSystem _damageParticle;
        [SerializeField] private ParticleSystem _healParticle;

        // 動く見た目があれば、フラッシュと倒れたときの色はそちらに掛ける（無ければ _flashTarget）
        private IUnitVisual _colorTarget;

        private readonly List<BattleFloatingNumberView> _floatingNumbers = new List<BattleFloatingNumberView>();
        private Vector2 _shakeOrigin;
        private float _shakeTimeLeft;
        private Color _flashBaseColor = Color.white;
        private Color _flashColor;
        private float _flashTimeLeft;

        private void Awake()
        {
            if (_shakeTarget != null) _shakeOrigin = _shakeTarget.anchoredPosition;
            if (_flashTarget != null) _flashBaseColor = _flashTarget.color;
        }

        /// <summary>HPの増減量（減ったら負）に応じて演出を出す</summary>
        public void PlayHpChanged(int delta, string cause)
        {
            if (delta == 0) return;

            bool isDamage = delta < 0;
            ShowNumber(isDamage ? delta.ToString() : $"+{delta}", cause, isDamage ? _damageNumberColor : _healNumberColor);
            StartFlash(isDamage ? _damageFlashColor : _healFlashColor);

            if (isDamage)
            {
                _shakeTimeLeft = _shakeDuration;
                if (_damageParticle != null) _damageParticle.Play();
            }
            else if (_healParticle != null)
            {
                _healParticle.Play();
            }
        }

        // 倒れたユニットの絵を薄くする。フラッシュの戻り先の色もこれに合わせる
        public void SetBaseColor(Color color)
        {
            _flashBaseColor = color;
            if (_flashTimeLeft <= 0f) ApplyColor(color);
        }

        /// <summary>色を掛ける先を、動く見た目に切り替える。今の色もそちらへ移す</summary>
        public void SetColorTarget(IUnitVisual visual)
        {
            _colorTarget = visual;
            ApplyColor(_flashTimeLeft > 0f ? _flashColor : _flashBaseColor);
        }

        private bool HasColorTarget => _colorTarget != null || _flashTarget != null;

        private void ApplyColor(Color color)
        {
            if (_colorTarget != null)
            {
                _colorTarget.SetColor(color);
            }
            else if (_flashTarget != null)
            {
                _flashTarget.color = color;
            }
        }

        /// <summary>状態効果が付いた・弾かれたことを、文字だけで浮かせる（数字の演出より少し下から）</summary>
        public void PlayStatusText(string text, bool isBlocked)
        {
            if (string.IsNullOrEmpty(text)) return;

            // 同時にいくつも付いたときに重ならないよう、続けて出た分は1段ずつ下へずらす
            float now = Time.unscaledTime;
            _statusTextRow = now - _lastStatusTextTime < StatusTextStackWindow ? _statusTextRow + 1 : 0;
            _lastStatusTextTime = now;
            var offset = _statusTextOffset + Vector2.down * (StatusTextRowHeight * _statusTextRow);

            ShowNumber(string.Empty, text, isBlocked ? _blockedStatusColor : _statusTextColor, offset);
        }

        private void ShowNumber(string text, string cause, Color color, Vector2 extraOffset = default)
        {
            if (_floatingNumberPrefab == null || _floatingNumberParent == null) return;

            var view = _floatingNumbers.Find(v => !v.IsPlaying);
            if (view == null)
            {
                view = Instantiate(_floatingNumberPrefab, _floatingNumberParent);
                _floatingNumbers.Add(view);
            }

            // 連続で出たときに重ならないよう、少し横にずらす
            var jitter = new Vector2(Random.Range(-30f, 30f), Random.Range(-10f, 10f));
            view.Play(text, cause, color, _floatingNumberOffset + extraOffset + jitter);
        }

        private void StartFlash(Color color)
        {
            if (!HasColorTarget) return;
            _flashColor = color;
            _flashTimeLeft = _flashDuration;
        }

        private void Update()
        {
            if (_shakeTarget != null && _shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                float power = Mathf.Clamp01(_shakeTimeLeft / _shakeDuration) * _shakeStrength;
                _shakeTarget.anchoredPosition = _shakeTimeLeft > 0f
                    ? _shakeOrigin + Random.insideUnitCircle * power
                    : _shakeOrigin;
            }

            if (HasColorTarget && _flashTimeLeft > 0f)
            {
                _flashTimeLeft -= Time.deltaTime;
                float t = Mathf.Clamp01(_flashTimeLeft / _flashDuration);
                ApplyColor(Color.Lerp(_flashBaseColor, _flashColor, t));
            }
        }
    }
}
