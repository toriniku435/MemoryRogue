using System;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Rendering;

namespace Live2DCardRogue.Common.Views
{
    // ウインドウの出入りの動き。ゲーム内のウインドウはすべてこれで開閉し、出方・消え方をそろえる。
    //   開く: 枠が横一線から開き、少しチラつく。背後のぼかしと暗さが徐々にかかる
    //   閉じる: 押した瞬間から横一線に潰れる → 線が電子の粒になって散り、線も縮んで消える。
    //          ぼかしと暗さは閉じる間に徐々に抜ける。
    //          潰れる前に横ずれとノイズの帯のグリッチを挟むこともできる（_glitchDuration）が、
    //          閉じるまでが長く感じられたので既定では 0（挟まない）にしている
    //
    // このコンポーネントを付けたオブジェクトの表示/非表示がウインドウの開閉そのもの。
    // ぼかしの Push / Pop は OnEnable / OnDisable で対にしてある（シーンを抜けても数え漏れが残らない）。
    // 閉じたときに散る粒（_dissolveBurst）は、このオブジェクトの外に置くこと。中に置くと、閉じ終わって
    // 非表示にした瞬間に粒も消えてしまう。
    // 時間は実時間で進め、1フレームの上限を付けている（処理落ちで動きが飛ばないように）
    //
    // 重ねて開いたとき: 開いたウインドウを PopupStack で一番手前にし、ぼかしの後に描く段へ置く（下のウインドウは
    // ぼかしの前の段へ移り、マップと一緒にぼける）。ぼかしの強さを開閉に合わせて上げ下げするのは、
    // 最初の1つが開くときと最後の1つが閉じるときだけ（重ねて開くたびにマップのぼけ方が揺れないように）。
    // 段の切り替え先はプレハブのルートの PopupLayerMember。
    // 重ねて開閉するときは、すぐ後ろのウインドウの枠の範囲だけぼかしの強さを開閉に合わせて動かす
    // （PopupBlurController.SetRegion）。開くときは 0→1 でぼけていき、閉じるときは 1→0 でくっきり戻り、
    // 閉じ終わって一番手前の段へ戻った瞬間と見た目がつながる
    public sealed class WindowTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform _window;
        [SerializeField] private CanvasGroup _windowGroup;
        // 背後を暗くする板（入力も止める）。未設定でもよい
        [SerializeField] private Graphic _scrim;
        // 閉じるときに枠の上を走るノイズの帯。未設定でもよい
        [SerializeField] private RectTransform[] _noiseBars;
        // 潰れるときに枠全体を光らせる板（枠の一番手前に置く）。これで潰れた線が光の線に見える。未設定でもよい
        [SerializeField] private Graphic _collapseFlash;
        [SerializeField] private ParticleSystem _dissolveBurst;
        [SerializeField] private bool _blurBackground = true;
        [SerializeField] private SeId _openSe = SeId.WindowOpen;
        [SerializeField] private SeId _closeSe = SeId.WindowClose;

        [SerializeField][Min(0.01f)] private float _openDuration = 0.18f;
        [SerializeField][Min(0f)] private float _glitchDuration = 0f;
        [SerializeField][Min(0.01f)] private float _collapseDuration = 0.12f;
        [SerializeField][Min(0.01f)] private float _vanishDuration = 0.14f;
        // グリッチで横にずれる幅
        [SerializeField][Min(0f)] private float _jitter = 16f;

        private enum Phase { Idle, Opening, Closing }

        private Phase _phase;
        private float _time;
        private float _scrimAlpha = -1f;
        private Vector2 _windowPosition;
        private Action _onClosed;
        private bool _burstPlayed;
        // この開閉でぼかしの強さを動かすか（ほかにぼかしているウインドウが無いときだけ）
        private bool _fadesBlur;
        private PopupLayerMember _layerMember;
        private bool _layerMemberResolved;
        // 重ねて開閉している間、ぼかしの強さを動かす範囲（すぐ後ろのウインドウの枠）。動かしていなければ false
        private bool _fadesRegion;
        private Vector4 _region;

        /// <summary>枠（ぼかしの範囲を求めるのに使う）</summary>
        public RectTransform WindowRect => _window;

        /// <summary>このウインドウの段の切り替え口（プレハブのルート）。無ければ null</summary>
        public PopupLayerMember LayerMember
        {
            get
            {
                if (!_layerMemberResolved)
                {
                    _layerMemberResolved = true;
                    _layerMember = GetComponentInParent<PopupLayerMember>(true);
                    if (_layerMember == null) Debug.LogWarning($"{name}: PopupLayerMember が親にありません。重ねて開いたときの段の切り替えができません", this);
                }
                return _layerMember;
            }
        }

        public bool IsClosing => _phase == Phase.Closing;

        private float CloseDuration => _glitchDuration + _collapseDuration + _vanishDuration;

        public void PlayOpen()
        {
            CaptureDefaults();
            int othersBlurring = PopupBlurController.OpenCount - (_blurBackground && gameObject.activeSelf ? 1 : 0);
            _fadesBlur = othersBlurring <= 0;
            gameObject.SetActive(true);
            PopupStack.Bring(this);
            BeginRegionFade();
            AudioUseCase.PlaySe(_openSe);
            _phase = Phase.Opening;
            _time = 0f;
            ApplyOpen();
        }

        /// <summary>閉じる動きを流し、終わったら非表示にして onClosed を呼ぶ。閉じている途中なら何もしない</summary>
        public void PlayClose(Action onClosed)
        {
            if (!gameObject.activeSelf || _phase == Phase.Closing) return;
            CaptureDefaults();
            _onClosed = onClosed;
            // 最後の1つが閉じるときだけ、ぼかしを抜く
            _fadesBlur = PopupBlurController.OpenCount <= (_blurBackground ? 1 : 0);
            BeginRegionFade();
            AudioUseCase.PlaySe(_closeSe);
            _phase = Phase.Closing;
            _time = 0f;
            _burstPlayed = false;
            ApplyClose();
        }

        /// <summary>動きなしで即座に閉じる（画面の初期化など）</summary>
        public void HideImmediate()
        {
            CaptureDefaults();
            _phase = Phase.Idle;
            gameObject.SetActive(false);
            ResetVisuals();
        }

        private void OnEnable()
        {
            if (_blurBackground) PopupBlurController.Push();
        }

        // 閉じ終わったとき・シーンを抜けるときは、重なりからも抜く
        private void OnDisable()
        {
            EndRegionFade();
            if (_blurBackground) PopupBlurController.Pop();
            PopupStack.Remove(this);
        }

        private void Update()
        {
            if (_phase == Phase.Idle) return;
            _time += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_phase == Phase.Opening) ApplyOpen();
            else ApplyClose();
        }

        // 開いたときの位置・暗さを覚えておく（動きはここからずらし、ここへ戻す）
        private void CaptureDefaults()
        {
            if (_scrimAlpha >= 0f) return;
            _windowPosition = _window.anchoredPosition;
            _scrimAlpha = _scrim != null ? _scrim.color.a : 0f;
        }

        private void ApplyOpen()
        {
            float p = Mathf.Clamp01(_time / _openDuration);
            float eased = 1f - (1f - p) * (1f - p);

            _window.anchoredPosition = _windowPosition;
            _window.localScale = new Vector3(1f, Mathf.Lerp(0.03f, 1f, eased), 1f);
            if (_windowGroup != null) _windowGroup.alpha = p < 0.6f && UnityEngine.Random.value < 0.3f ? 0.4f : 1f;
            SetNoiseBarsActive(false);
            SetFade(eased);

            if (p >= 1f)
            {
                ResetVisuals();
                EndRegionFade();
                _phase = Phase.Idle;
            }
        }

        private void ApplyClose()
        {
            float t = _time;
            SetFade(1f - Smooth(Mathf.Clamp01(t / CloseDuration)));

            if (t < _glitchDuration)
            {
                // 横にずれ、チラつき、ノイズの帯が走る
                bool shift = UnityEngine.Random.value < 0.5f;
                _window.anchoredPosition = _windowPosition + new Vector2(shift ? UnityEngine.Random.Range(-_jitter, _jitter) : 0f, 0f);
                _window.localScale = Vector3.one;
                if (_windowGroup != null) _windowGroup.alpha = UnityEngine.Random.value < 0.35f ? 0.35f : 1f;
                ScatterNoiseBars();
            }
            else if (t < _glitchDuration + _collapseDuration)
            {
                // 横一線に潰れる
                float p = (t - _glitchDuration) / _collapseDuration;
                _window.anchoredPosition = _windowPosition;
                _window.localScale = new Vector3(Mathf.Lerp(1f, 1.04f, p), Mathf.Lerp(1f, 0.02f, p * p), 1f);
                if (_windowGroup != null) _windowGroup.alpha = 1f;
                SetNoiseBarsActive(false);
                SetFlash(p);
            }
            else if (t < CloseDuration)
            {
                // 線が粒になって散り、線そのものも縮んで消える
                if (!_burstPlayed)
                {
                    _burstPlayed = true;
                    if (_dissolveBurst != null) _dissolveBurst.Play();
                }
                float p = (t - _glitchDuration - _collapseDuration) / _vanishDuration;
                _window.localScale = new Vector3(Mathf.Lerp(1.04f, 0f, p * p), 0.02f, 1f);
                SetFlash(1f);
                if (_windowGroup != null) _windowGroup.alpha = 1f - p;
            }
            else
            {
                _phase = Phase.Idle;
                gameObject.SetActive(false);
                ResetVisuals();
                var callback = _onClosed;
                _onClosed = null;
                callback?.Invoke();
            }
        }

        // ぼかしと暗さを 0..1 でかける
        private void SetFade(float amount)
        {
            if (_blurBackground && _fadesBlur) PopupBlurController.Strength = amount;
            if (_fadesRegion) PopupBlurController.SetRegion(_region, amount);
            if (_scrim != null)
            {
                var c = _scrim.color;
                c.a = _scrimAlpha * amount;
                _scrim.color = c;
            }
        }

        private void SetFlash(float alpha)
        {
            if (_collapseFlash == null) return;
            _collapseFlash.gameObject.SetActive(alpha > 0f);
            var c = _collapseFlash.color;
            c.a = alpha;
            _collapseFlash.color = c;
        }

        private void ScatterNoiseBars()
        {
            if (_noiseBars == null) return;
            Vector2 size = _window.rect.size;
            foreach (var bar in _noiseBars)
            {
                if (bar == null) continue;
                bool visible = UnityEngine.Random.value < 0.6f;
                bar.gameObject.SetActive(visible);
                if (!visible) continue;
                bar.anchoredPosition = new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f) * size.x, UnityEngine.Random.Range(-0.5f, 0.5f) * size.y);
                bar.sizeDelta = new Vector2(UnityEngine.Random.Range(0.2f, 0.7f) * size.x, UnityEngine.Random.Range(4f, 14f));
            }
        }

        private void SetNoiseBarsActive(bool isActive)
        {
            if (_noiseBars == null) return;
            foreach (var bar in _noiseBars)
            {
                if (bar != null) bar.gameObject.SetActive(isActive);
            }
        }

        private void ResetVisuals()
        {
            _window.anchoredPosition = _windowPosition;
            _window.localScale = Vector3.one;
            if (_windowGroup != null) _windowGroup.alpha = 1f;
            SetNoiseBarsActive(false);
            SetFlash(0f);
            SetFade(1f);
        }

        // ほかのウインドウの上に重ねて開閉するときだけ、すぐ後ろのウインドウの枠の範囲で強さを動かし始める
        // （最初の1つ・最後の1つのときは画面全体の強さを動かすので要らない）
        private void BeginRegionFade()
        {
            EndRegionFade();
            if (!_blurBackground || _fadesBlur) return;
            var below = PopupStack.Below(this);
            if (below == null || below.WindowRect == null) return;
            if (!TryGetViewportRect(below.WindowRect, out _region)) return;
            _fadesRegion = true;
        }

        private void EndRegionFade()
        {
            if (!_fadesRegion) return;
            _fadesRegion = false;
            PopupBlurController.ClearRegion();
        }

        // 枠の四隅を、描くカメラから見た画面上の位置（0..1、左下が原点）にする
        private static bool TryGetViewportRect(RectTransform rect, out Vector4 viewportRect)
        {
            viewportRect = default;
            var canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            var camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                Vector2 point = camera != null
                    ? (Vector2)camera.WorldToViewportPoint(corner)
                    : new Vector2(corner.x / Screen.width, corner.y / Screen.height);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            viewportRect = new Vector4(min.x, min.y, max.x, max.y);
            return true;
        }

        private static float Smooth(float p) => p * p * (3f - 2f * p);
    }
}
