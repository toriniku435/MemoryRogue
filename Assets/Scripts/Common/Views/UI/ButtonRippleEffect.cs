using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // ボタンに付ける波紋の演出。どの CommonButton にも後付けできる（見た目だけで、押したときの処理には関わらない）
    //   カーソルを合わせている間: ボタンの縁と同じ形の輪が、一定間隔で外へ広がって消える
    //   押したとき: 押した位置から円の波紋がボタンの中に広がり（ボタンの形で切り抜く）、面が一瞬光り、外へも強めの輪が出る
    //   押せない間: ボタン全体を暗くし、波紋も出さない
    // 時間は実時間で進める（スローや一時停止中でも UI の反応は変わらないように）
    public sealed class ButtonRippleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private Selectable _selectable;
        // 押せない間に暗くする対象（ボタン全体）
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField][Range(0f, 1f)] private float _disabledAlpha = 0.4f;
        [SerializeField] private Color _color = new Color(0f, 0.94f, 1f, 1f);

        [Header("カーソルを合わせたときの輪")]
        // 縁と同じ形の輪（使い回す）。アンカーはボタンいっぱい、ピボットは中央に置くこと
        [SerializeField] private RectTransform[] _echoes;
        [SerializeField][Min(0.05f)] private float _echoInterval = 0.55f;
        [SerializeField][Min(0.05f)] private float _echoDuration = 0.6f;
        [SerializeField] private float _echoScale = 1.25f;

        [Header("押したときの波紋")]
        // 波紋を置く範囲（ボタンの形のマスクの子にしておく）
        [SerializeField] private RectTransform _rippleArea;
        [SerializeField] private RectTransform[] _ripples;
        [SerializeField] private Graphic _flash;
        [SerializeField][Min(0.05f)] private float _rippleDuration = 0.55f;
        [SerializeField][Min(0.05f)] private float _flashDuration = 0.2f;

        private float[] _echoAges;
        private float[] _echoScales;
        private float[] _rippleAges;
        // 押した位置から一番遠い角まで届く直径（押した位置ごとに違う）
        private float[] _rippleMaxSizes;
        private bool _isHovered;
        private float _echoTimer;
        private float _flashAge = float.MaxValue;

        private bool IsInteractable => _selectable == null || _selectable.IsInteractable();

        private void Awake()
        {
            _echoAges = NewAges(_echoes);
            _echoScales = new float[_echoAges.Length];
            _rippleAges = NewAges(_ripples);
            _rippleMaxSizes = new float[_rippleAges.Length];
            HideAll();
        }

        private void OnDisable()
        {
            _isHovered = false;
            HideAll();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _echoTimer = 0f; // 合わせた瞬間に1つ目を出す
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable) return;

            if (_rippleArea != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rippleArea, eventData.position, eventData.pressEventCamera, out var local))
            {
                int i = OldestIndex(_rippleAges);
                if (i >= 0)
                {
                    var rect = _rippleArea.rect;
                    _ripples[i].anchoredPosition = local - rect.center;
                    _rippleAges[i] = 0f;
                    _rippleMaxSizes[i] = FarthestCornerDistance(rect, local) * 2f;
                }
            }

            _flashAge = 0f;
            SpawnEcho(1.5f);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            bool interactable = IsInteractable;
            if (_canvasGroup != null) _canvasGroup.alpha = interactable ? 1f : _disabledAlpha;

            if (_isHovered && interactable)
            {
                _echoTimer -= dt;
                if (_echoTimer <= 0f)
                {
                    _echoTimer = _echoInterval;
                    SpawnEcho(1f);
                }
            }

            UpdateEchoes(dt);
            UpdateRipples(dt);
            UpdateFlash(dt);
        }

        // strength: 広がる大きさの倍率（押したときは大きめ）
        private void SpawnEcho(float strength)
        {
            int i = OldestIndex(_echoAges);
            if (i < 0) return;
            _echoAges[i] = 0f;
            _echoScales[i] = 1f + (_echoScale - 1f) * strength;
        }

        // 縁の形のまま全体を拡大するので、斜めの辺の角度は崩れない
        private void UpdateEchoes(float dt)
        {
            for (int i = 0; i < _echoAges.Length; i++)
            {
                _echoAges[i] += dt;
                float p = _echoAges[i] / _echoDuration;
                bool visible = p < 1f;
                _echoes[i].gameObject.SetActive(visible);
                if (!visible) continue;

                float eased = 1f - (1f - p) * (1f - p);
                _echoes[i].localScale = Vector3.one * Mathf.Lerp(1f, _echoScales[i], eased);
                SetAlpha(_echoes[i], 0.9f * (1f - p));
            }
        }

        private void UpdateRipples(float dt)
        {
            if (_rippleArea == null) return;
            for (int i = 0; i < _rippleAges.Length; i++)
            {
                _rippleAges[i] += dt;
                float p = _rippleAges[i] / _rippleDuration;
                bool visible = p < 1f;
                _ripples[i].gameObject.SetActive(visible);
                if (!visible) continue;

                // ボタンは背が低く、急に広げると一瞬で上下にはみ出して見えなくなるので、ゆるやかに広げる
                float eased = 1f - (1f - p) * (1f - p);
                float size = Mathf.Lerp(8f, _rippleMaxSizes[i], eased);
                _ripples[i].sizeDelta = new Vector2(size, size);
                SetAlpha(_ripples[i], 0.8f * (1f - p));
            }
        }

        private void UpdateFlash(float dt)
        {
            if (_flash == null) return;
            _flashAge += dt;
            var color = _color;
            color.a = _flashAge < _flashDuration ? 0.45f * (1f - _flashAge / _flashDuration) : 0f;
            _flash.color = color;
        }

        private void HideAll()
        {
            if (_echoAges != null)
            {
                for (int i = 0; i < _echoAges.Length; i++) _echoAges[i] = float.MaxValue;
                foreach (var e in _echoes) if (e != null) e.gameObject.SetActive(false);
            }
            if (_rippleAges != null)
            {
                for (int i = 0; i < _rippleAges.Length; i++) _rippleAges[i] = float.MaxValue;
                foreach (var r in _ripples) if (r != null) r.gameObject.SetActive(false);
            }
            _flashAge = float.MaxValue;
            if (_flash != null) _flash.color = new Color(_color.r, _color.g, _color.b, 0f);
        }

        private void SetAlpha(RectTransform target, float alpha)
        {
            var graphic = target.GetComponent<Graphic>();
            if (graphic == null) return;
            var color = _color;
            color.a = alpha;
            graphic.color = color;
        }

        private static float FarthestCornerDistance(Rect rect, Vector2 point)
        {
            float dx = Mathf.Max(Mathf.Abs(point.x - rect.xMin), Mathf.Abs(point.x - rect.xMax));
            float dy = Mathf.Max(Mathf.Abs(point.y - rect.yMin), Mathf.Abs(point.y - rect.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float[] NewAges(RectTransform[] items)
        {
            var ages = new float[items != null ? items.Length : 0];
            for (int i = 0; i < ages.Length; i++) ages[i] = float.MaxValue;
            return ages;
        }

        // 空いているもの（一番古いもの）を使い回す
        private static int OldestIndex(float[] ages)
        {
            int best = -1;
            float bestAge = -1f;
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i] > bestAge)
                {
                    bestAge = ages[i];
                    best = i;
                }
            }
            return best;
        }
    }
}
