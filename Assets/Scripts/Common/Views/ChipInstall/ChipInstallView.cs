using System;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // 開封演出の前半：パック（チップ）を基板のソケットに差し込み、読み込みが終わると光になって弾ける。
    // 光の色は「中身の一番高いレアリティ」に向かって段階的に上がっていく（期待演出）:
    //   ノーマルだけ: シアンのまま
    //   レアあり    : 読み込みの途中でマゼンタに昇格（輪と閃光）
    //   エピックあり: さらに金に昇格し、虹色に揺らぎ、画面が揺れ、99% で一瞬止まって溜める
    // 何が出たかは知っているだけで、決めるのは抽選（CardPackOpener）。ここは見せ方だけ。
    // 時間は実時間で進め、1フレームの上限を付けている
    public sealed class ChipInstallView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _chip;
        [SerializeField] private Image _chipCore;
        [SerializeField] private Image _glow;
        [SerializeField] private Image _socketGlow;
        [SerializeField] private RectTransform[] _rings;
        [SerializeField] private Image _flash;
        [SerializeField] private RectTransform _progressFill;
        [SerializeField] private CommonText _progressText;
        [SerializeField] private CommonText _statusText;
        [SerializeField] private ParticleSystem _burst;

        [Header("レアリティごとの光の色（ノーマル・レア・エピック）")]
        [SerializeField] private Color[] _tierColors =
        {
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
        };

        [Header("時間")]
        [SerializeField][Min(0.01f)] private float _insertDuration = 0.45f;
        [SerializeField][Min(0.01f)] private float _loadDuration = 1.3f;
        // エピックのときだけ、99% で止めて溜める
        [SerializeField][Min(0f)] private float _epicHoldDuration = 0.45f;
        [SerializeField][Min(0.01f)] private float _releaseDuration = 0.35f;
        [SerializeField][Min(0.01f)] private float _ringDuration = 0.5f;
        [SerializeField][Min(1f)] private float _insertDistance = 300f;
        [SerializeField][Min(1f)] private float _step = 12f;

        // 昇格する読み込みの進み具合（レア・エピック）
        private const float RarePromoteAt = 0.4f;
        private const float EpicPromoteAt = 0.75f;

        private enum Phase { Idle, Insert, Load, Hold, Release }

        private Phase _phase;
        private float _time;
        private int _maxTier;
        private int _tier;
        private float _shake;
        private float _flashAlpha;
        private float[] _ringAges;
        private Action _onFinished;

        /// <summary>演出を始める。maxTier は中身の一番高いレアリティ（0 ノーマル, 1 レア, 2 エピック）</summary>
        public void Play(int maxTier, Action onFinished)
        {
            _maxTier = Mathf.Clamp(maxTier, 0, _tierColors.Length - 1);
            _tier = 0;
            _onFinished = onFinished;
            _shake = 0f;
            _flashAlpha = 0f;
            EnsureRings();
            for (int i = 0; i < _ringAges.Length; i++) _ringAges[i] = float.MaxValue;
            _root.gameObject.SetActive(true);
            _chip.gameObject.SetActive(true);
            _chip.localScale = Vector3.one;
            SetAlpha(_chip, 1f);
            Next(Phase.Insert);
            Apply();
        }

        /// <summary>残りを飛ばして終える（onFinished はここでは呼ばない。呼び出し側が続きを飛ばすため）</summary>
        public void Skip()
        {
            _onFinished = null;
            Hide();
        }

        public void Hide()
        {
            // 溜めている音が残らないように
            if (_phase != Phase.Idle) AudioUseCase.StopSe(SeId.InstallCharge);
            _phase = Phase.Idle;
            _root.gameObject.SetActive(false);
            if (_burst != null) _burst.Clear();
        }

        private void Update()
        {
            if (_phase == Phase.Idle) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            _time += dt;
            _shake = Mathf.Max(0f, _shake - dt * 3f);
            _flashAlpha = Mathf.Max(0f, _flashAlpha - dt * 3.5f);
            for (int i = 0; i < _ringAges.Length; i++)
            {
                if (_ringAges[i] != float.MaxValue) _ringAges[i] += dt;
            }
            Apply();
        }

        private void Apply()
        {
            switch (_phase)
            {
                case Phase.Insert:
                {
                    // 上からカクカクと降りてきて、ソケットに収まる
                    float p = Mathf.Clamp01(_time / _insertDuration);
                    float eased = p * p;
                    float y = Mathf.Round(Mathf.Lerp(_insertDistance, 0f, eased) / _step) * _step;
                    _chip.anchoredPosition = new Vector2(0f, y);
                    SetProgress(0f);
                    SetStatus("// INSERTING");
                    if (p >= 1f)
                    {
                        Pulse(0.6f, 0.35f);
                        Next(Phase.Load);
                        // ソケットに収まったら、読み込みながら「ぐぐーっ」と溜める
                        AudioUseCase.PlaySe(SeId.InstallCharge);
                    }
                    break;
                }
                case Phase.Load:
                {
                    // 最後の方ほどゆっくり進む（期待が膨らむ間）
                    float p = Mathf.Clamp01(_time / _loadDuration);
                    float progress = 1f - (1f - p) * (1f - p) * (1f - p);
                    progress = Mathf.Lerp(progress, p, 0.35f);
                    if (_maxTier >= 1 && _tier < 1 && progress >= RarePromoteAt) Promote(1);
                    if (_maxTier >= 2 && _tier < 2 && progress >= EpicPromoteAt) Promote(2);
                    _chip.anchoredPosition = Tremble(0.5f + _tier * 1.5f);

                    if (p >= 1f)
                    {
                        if (_maxTier >= 2)
                        {
                            SetProgress(0.99f);
                            Next(Phase.Hold);
                        }
                        else
                        {
                            SetProgress(1f);
                            Release();
                        }
                    }
                    else
                    {
                        SetProgress(progress);
                    }
                    SetStatus("// INSTALLING");
                    break;
                }
                case Phase.Hold:
                {
                    // 99% で止まり、チップが震えて光を溜める
                    SetProgress(0.99f);
                    SetStatus("// OVERFLOW");
                    _chip.anchoredPosition = Tremble(6f);
                    _shake = Mathf.Max(_shake, 0.6f);
                    if (_time >= _epicHoldDuration)
                    {
                        SetProgress(1f);
                        Release();
                    }
                    break;
                }
                case Phase.Release:
                {
                    // チップが光になって弾ける
                    float p = Mathf.Clamp01(_time / _releaseDuration);
                    _chip.localScale = Vector3.one * Mathf.Lerp(1f, 1.8f, p);
                    SetAlpha(_chip, 1f - p);
                    SetStatus("// COMPLETE");
                    if (p >= 1f)
                    {
                        _phase = Phase.Idle;
                        _chip.gameObject.SetActive(false);
                        var callback = _onFinished;
                        _onFinished = null;
                        callback?.Invoke();
                    }
                    break;
                }
            }

            ApplyGlow();
            ApplyRings();
            _root.anchoredPosition = _shake > 0f ? UnityEngine.Random.insideUnitCircle * (_shake * 14f) : Vector2.zero;
            if (_flash != null) _flash.color = WithAlpha(TierColor(), _flashAlpha);
        }

        private void Release()
        {
            AudioUseCase.StopSe(SeId.InstallCharge);
            Pulse(1f, 0.6f + _tier * 0.3f);
            if (_burst != null)
            {
                var emit = new ParticleSystem.EmitParams { startColor = TierColor() };
                _burst.Emit(emit, 40 + _tier * 50);
            }
            Next(Phase.Release);
        }

        // 光の色を1段上げる。上がるたびに輪と閃光、エピックでは揺れと粒も
        private void Promote(int tier)
        {
            _tier = tier;
            AudioUseCase.PlaySe(tier >= 2 ? SeId.InstallPromoteEpic : SeId.InstallPromote);
            Pulse(0.8f, 0.4f + tier * 0.25f);
            if (tier >= 2) _shake = 1f;
            if (_burst != null)
            {
                var emit = new ParticleSystem.EmitParams { startColor = TierColor() };
                _burst.Emit(emit, 20 + tier * 25);
            }
        }

        private void Pulse(float flash, float shake)
        {
            _flashAlpha = Mathf.Max(_flashAlpha, flash * 0.6f);
            _shake = Mathf.Max(_shake, shake);
            for (int i = 0; i < _ringAges.Length; i++)
            {
                if (_ringAges[i] == float.MaxValue || _ringAges[i] >= _ringDuration)
                {
                    _ringAges[i] = 0f;
                    return;
                }
            }
        }

        private void ApplyGlow()
        {
            var color = TierColor();
            // 段が上がるほど速く、強く脈打つ
            float speed = 4f + _tier * 5f;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * speed);
            float strength = _phase == Phase.Insert ? 0.3f : 0.55f + _tier * 0.2f;
            if (_glow != null)
            {
                _glow.color = WithAlpha(color, Mathf.Clamp01(strength * pulse + (_phase == Phase.Release ? 0.5f : 0f)));
                _glow.rectTransform.localScale = Vector3.one * (1f + _tier * 0.25f + pulse * 0.1f);
            }
            if (_socketGlow != null) _socketGlow.color = WithAlpha(color, 0.35f + 0.3f * pulse);
            if (_chipCore != null) _chipCore.color = WithAlpha(color, 0.6f + 0.4f * pulse);
        }

        private void ApplyRings()
        {
            if (_rings == null) return;
            for (int i = 0; i < _rings.Length; i++)
            {
                var ring = _rings[i];
                if (ring == null) continue;
                float age = _ringAges[i];
                bool alive = age < _ringDuration;
                ring.gameObject.SetActive(alive);
                if (!alive) continue;
                float p = age / _ringDuration;
                ring.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.6f + _tier * 0.6f, 1f - (1f - p) * (1f - p));
                var image = ring.GetComponent<Graphic>();
                if (image != null) image.color = WithAlpha(TierColor(), 1f - p);
            }
        }

        // エピックは金を基調に、虹色へ揺らぐ
        private Color TierColor()
        {
            var baseColor = _tierColors[Mathf.Clamp(_tier, 0, _tierColors.Length - 1)];
            if (_tier < 2) return baseColor;
            float hue = Mathf.Repeat(Time.unscaledTime * 0.6f, 1f);
            return Color.Lerp(baseColor, Color.HSVToRGB(hue, 0.7f, 1f), 0.35f);
        }

        private void SetProgress(float progress)
        {
            if (_progressFill != null) _progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            if (_progressText != null) _progressText.text = $"{Mathf.FloorToInt(progress * 100f):00}%";
        }

        private void SetStatus(string status)
        {
            if (_statusText != null) _statusText.text = status;
        }

        private Vector2 Tremble(float amount)
        {
            return new Vector2(UnityEngine.Random.Range(-amount, amount), UnityEngine.Random.Range(-amount, amount));
        }

        private void Next(Phase phase)
        {
            _phase = phase;
            _time = 0f;
        }

        private void EnsureRings()
        {
            int count = _rings != null ? _rings.Length : 0;
            if (_ringAges == null || _ringAges.Length != count) _ringAges = new float[count];
        }

        private static void SetAlpha(RectTransform target, float alpha)
        {
            foreach (var graphic in target.GetComponentsInChildren<Graphic>())
            {
                graphic.color = WithAlpha(graphic.color, alpha);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
