using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.SceneTransition
{
    // シーン遷移の演出。画面を横に割った帯が左右からグリッチ的に滑り込んで覆い、
    // 覆っている間に次のシーンを読み込み、初期化が済んでから帯が抜けて新しい画面を見せる。
    // 読み込み・初期化で画面が固まる瞬間はずっと覆われているので、プレイヤーには見えない。
    // 待ちが長引いたときだけ、覆った画面に「// LOADING」と進み具合を出す。
    //
    // トーストと同じく、初回に Resources から生成して DontDestroyOnLoad で持ち続ける。
    // 遷移の途中でカメラが入れ替わるので、この Canvas だけは Screen Space - Overlay で描く（カメラに依存させない）。
    // 遷移中は画面全体で入力を受け止めて、二度押しで遷移が重ならないようにする
    [RequireComponent(typeof(Canvas))]
    public sealed class SceneTransitionEffect : MonoBehaviour
    {
        private const string PrefabResourcePath = "SceneTransitionEffect";

        private static SceneTransitionEffect _instance;

        // 画面を覆う帯。上から順に、それぞれ画面の高さを等分した段に置く（アンカーで割るので画面の比率に依らない）
        [SerializeField] private RectTransform[] _slices;
        // 動いている間だけチラつかせるノイズの帯
        [SerializeField] private Graphic[] _glitchBars;
        [SerializeField] private Color[] _glitchColors = { Color.cyan, Color.magenta, Color.yellow };
        // 遷移中だけ有効にする、画面全体の入力の受け止め役
        [SerializeField] private Graphic _inputBlocker;

        [Header("ローディング表示")]
        [SerializeField] private CanvasGroup _loadingGroup;
        [SerializeField] private CommonText _loadingText;
        [SerializeField] private RectTransform _progressFill;
        // 覆ってからこれ以上待つときだけ出す（すぐ終わる遷移で一瞬ちらつかないように）
        [SerializeField][Min(0f)] private float _loadingDelay = 0.2f;

        [Header("時間")]
        [SerializeField][Min(0.01f)] private float _sliceDuration = 0.22f;
        // 帯ごとの出だしのずれの最大（グリッチっぽくバラバラに入る）
        [SerializeField][Min(0f)] private float _sliceStagger = 0.12f;
        // 新しいシーンの Start・最初のレイアウトが済むまで待つフレーム数
        [SerializeField][Min(0)] private int _settleFrames = 3;
        [SerializeField][Min(0.01f)] private float _glitchInterval = 0.04f;

        private float[] _delays;
        private float[] _directions;
        private bool _isRunning;
        private float _glitchTimer;

        public static bool IsTransitioning => _instance != null && _instance._isRunning;

        // 演出付きで遷移を始める。演出のプレハブが無ければ false（呼び出し側が普通に読み込む）
        public static bool TryLoad(string sceneName)
        {
            var instance = GetOrCreateInstance();
            if (instance == null) return false;
            instance.StartCoroutine(instance.Run(sceneName));
            return true;
        }

        private static SceneTransitionEffect GetOrCreateInstance()
        {
            if (_instance != null) return _instance;

            var prefab = Resources.Load<SceneTransitionEffect>(PrefabResourcePath);
            if (prefab == null) return null;
            _instance = Instantiate(prefab);
            _instance.name = prefab.name;
            DontDestroyOnLoad(_instance.gameObject);
            _instance.SetVisible(false);
            return _instance;
        }

        private IEnumerator Run(string sceneName)
        {
            _isRunning = true;
            AudioUseCase.PlaySe(SeId.Transition);
            SetVisible(true);
            Randomize();

            // 覆う
            float t = 0f;
            float coverTime = _sliceDuration + _sliceStagger;
            while (t < coverTime)
            {
                t += DeltaTime();
                ApplySlices(t, covering: true);
                TickGlitch(true);
                yield return null;
            }
            ApplySlices(float.MaxValue, covering: true);
            TickGlitch(false);

            // 覆ったまま読み込む。固まる瞬間はここに収まる
            float waited = 0f;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                waited += DeltaTime();
                UpdateLoading(waited, operation.progress / 0.9f);
                yield return null;
            }

            // 新しいシーンの初期化（Start）と最初の描画が済むまで待つ
            for (int i = 0; i < _settleFrames; i++)
            {
                waited += DeltaTime();
                UpdateLoading(waited, 1f);
                yield return null;
            }
            _loadingGroup.alpha = 0f;

            // 明ける
            Randomize();
            t = 0f;
            while (t < coverTime)
            {
                t += DeltaTime();
                ApplySlices(t, covering: false);
                TickGlitch(true);
                yield return null;
            }

            SetVisible(false);
            _isRunning = false;
        }

        // 実時間で進める。読み込み直後の長い1フレームで演出が飛ばないよう、1フレームの上限を付ける
        private static float DeltaTime()
        {
            return Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        }

        // 帯ごとに、入ってくる向き（左右）と出だしのずれをばらけさせる
        private void Randomize()
        {
            _delays ??= new float[_slices.Length];
            _directions ??= new float[_slices.Length];
            for (int i = 0; i < _slices.Length; i++)
            {
                _delays[i] = Random.Range(0f, _sliceStagger);
                _directions[i] = i % 2 == 0 ? -1f : 1f;
            }
        }

        // covering: 画面の外 → 定位置（覆う）/ 定位置 → 反対側の外（明ける）
        private void ApplySlices(float time, bool covering)
        {
            float width = ((RectTransform)transform).rect.width;
            for (int i = 0; i < _slices.Length; i++)
            {
                float p = Mathf.Clamp01((time - _delays[i]) / _sliceDuration);
                float x = covering
                    ? _directions[i] * width * (1f - p) * (1f - p) * (1f - p)
                    : -_directions[i] * width * p * p;
                _slices[i].anchoredPosition = new Vector2(x, 0f);
            }
        }

        private void UpdateLoading(float waited, float progress)
        {
            if (waited < _loadingDelay)
            {
                _loadingGroup.alpha = 0f;
                return;
            }

            // 出てきた直後は少しチラつかせる（ほかのサイバー演出とそろえる）
            float shown = waited - _loadingDelay;
            _loadingGroup.alpha = shown < 0.12f && Random.value < 0.4f ? 0.3f : 1f;
            bool cursor = (int)(Time.unscaledTime * 3f) % 2 == 0;
            _loadingText.text = cursor ? "// LOADING_" : "// LOADING";
            _progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), _progressFill.anchorMax.y);
        }

        private void TickGlitch(bool isActive)
        {
            if (_glitchBars == null) return;
            if (!isActive)
            {
                foreach (var bar in _glitchBars) bar.gameObject.SetActive(false);
                return;
            }

            _glitchTimer -= DeltaTime();
            if (_glitchTimer > 0f) return;
            _glitchTimer = _glitchInterval;

            foreach (var bar in _glitchBars)
            {
                bool show = Random.value < 0.6f;
                bar.gameObject.SetActive(show);
                if (!show) continue;
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(Random.value, Random.value);
                rt.sizeDelta = new Vector2(Random.Range(80f, 480f), Random.Range(3f, 10f));
                var color = _glitchColors[Random.Range(0, _glitchColors.Length)];
                color.a = Random.Range(0.5f, 0.9f);
                bar.color = color;
            }
        }

        private void SetVisible(bool isVisible)
        {
            GetComponent<Canvas>().enabled = isVisible;
            if (_inputBlocker != null) _inputBlocker.raycastTarget = isVisible;
            if (!isVisible)
            {
                _loadingGroup.alpha = 0f;
                TickGlitch(false);
            }
        }
    }
}
