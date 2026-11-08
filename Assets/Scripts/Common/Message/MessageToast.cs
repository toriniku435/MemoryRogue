using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Live2DCardRogue.Common.Rendering;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.Message
{
    // 画面上部に「ざざっ」とノイズ混じりで出てきて、少し経つとまたノイズと共に消える通知。
    //
    // 操作の邪魔をしないことが一番の要件なので、背景はぼかさず、入力も一切受け取らない
    // （Canvas に GraphicRaycaster を付けず、どの Graphic も raycastTarget を切ってある）。
    // 出ている間も、その下の画面はそのまま操作できる。
    //
    // 出現: 上から滑り込みつつ、パネルがブラウン管のように横一線から開く。その間、
    //       明滅・横に走るノイズ帯・色ずれ（シアン/マゼンタの残像）・文字のデコード（記号から順に確定）が乗る
    // 表示中にもう一度呼ばれたら: 位置はそのまま、デコードとノイズだけやり直して文字を差し替える
    // 退場: ノイズを出しながら横一線に潰れて、上へ抜ける
    // シーンが切り替わったら: 動きなしで即座に消す（前の画面の通知を次の画面に持ち越さない）。
    //   読み込みはシーン遷移の覆いの下で起きるので、消える瞬間は見えない
    //
    // 背景はテキストの長さに合わせて伸びる（Layout Group + ContentSizeFitter）。
    // 動かすのは外側の _slider と _panel のスケールだけで、Layout Group が並べる内側の位置には触らないので、
    // レイアウト再計算とアニメーションが競合しない
    [RequireComponent(typeof(Canvas))]
    public sealed class MessageToast : MonoBehaviour
    {
        private enum Phase
        {
            Appearing,
            Holding,
            Disappearing,
        }

        private const string PrefabResourcePath = "MessageToast";
        private const float PopupPlaneDistance = 100f;

        // デコード中に未確定の文字の代わりに出す記号。全角でそろえて、幅の見た目が大きく変わらないようにする
        private const string ScrambleCharacters = "■□◆◇＃＊％＆０１アイウエオカキクケコサシスセソ";

        private static MessageToast _instance;

        [SerializeField] private CommonText _text;
        // デコード中に文字数が変わっても幅が揺れないよう、最終的な文字列の幅で固定する
        [SerializeField] private LayoutElement _textLayout;
        // 色ずれ用の残像（_text の子）。未設定でもよい
        [SerializeField] private CommonText[] _ghostTexts;
        [SerializeField] private Vector2 _ghostOffset = new Vector2(6f, 0f);

        // 上下に動かす入れ物。表示位置はこの anchoredPosition（画面上端基準）
        [SerializeField] private RectTransform _slider;
        [SerializeField] private CanvasGroup _canvasGroup;
        // 横一線から開く／潰れる対象（背景パネル）
        [SerializeField] private RectTransform _panel;
        // パネル上を走るノイズの帯。未設定でもよい
        [SerializeField] private Graphic[] _glitchBars;
        [SerializeField] private Color[] _glitchColors = { Color.cyan, Color.magenta, Color.white };

        [SerializeField] private float _shownY = -24f;
        [SerializeField] private float _hiddenY = 180f;

        [SerializeField][Min(0.01f)] private float _appearDuration = 0.35f;
        [SerializeField] private float _displayDuration = 1.6f;
        [SerializeField][Min(0.01f)] private float _disappearDuration = 0.22f;
        // ノイズの切り替わる間隔（小さいほどチラつく）
        [SerializeField][Min(0.01f)] private float _glitchInterval = 0.04f;

        private readonly StringBuilder _scrambleBuilder = new StringBuilder();
        private string _message = string.Empty;
        private Phase _phase;
        private float _phaseTime;
        private bool _isRefresh;
        private float _glitchTimer;

        public static void Show(string message)
        {
            GetOrCreateInstance().ShowInternal(message);
        }

        private static MessageToast GetOrCreateInstance()
        {
            if (_instance != null) return _instance;

            var prefab = Resources.Load<MessageToast>(PrefabResourcePath);
            _instance = Instantiate(prefab);
            _instance.name = prefab.name;
            DontDestroyOnLoad(_instance.gameObject);

            // 常駐するのはこのオブジェクトだけなので、購読もここで1度だけ行う
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            return _instance;
        }

        // ドメインリロードを切った状態で Play し直しても、前回の購読が残らないように
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_instance != null) _instance.HideImmediate();
        }

        private void HideImmediate()
        {
            _message = string.Empty;
            gameObject.SetActive(false);
        }

        private void ShowInternal(string message)
        {
            BindToPopupCamera();
            _message = message ?? string.Empty;
            FixTextWidth();

            // 出ている（消えかけも含む）最中なら、滑り込みと開く演出は省いて中身だけ書き換える
            _isRefresh = gameObject.activeSelf && _phase != Phase.Disappearing;
            _phase = Phase.Appearing;
            _phaseTime = 0f;
            _glitchTimer = 0f;
            gameObject.SetActive(true);
            Apply();
        }

        private void FixTextWidth()
        {
            if (_textLayout == null) return;
            _textLayout.preferredWidth = _text.GetPreferredValues(_message).x;
        }

        // シーンをまたいで生き残るので、表示のたびに今のシーンのポップアップカメラに繋ぎ直す。
        // 開いているウインドウのぼかしの下にならないよう、レイヤーは一番手前のウインドウと同じ UI_PopupTop にする
        // （Popup Camera の描画の最後に、ぼかしの後で描かれる。PopupBlurRendererFeature）。
        // リグが無いシーンでもメッセージだけは出せるよう、その場合はOverlayで描く
        private void BindToPopupCamera()
        {
            var canvas = GetComponent<Canvas>();
            var popupCamera = UICameraRig.Current != null ? UICameraRig.Current.PopupCamera : null;

            if (popupCamera == null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }

            int layer = LayerMask.NameToLayer(PopupLayerMember.FrontLayerName);
            if (layer >= 0 && gameObject.layer != layer) SetLayerRecursively(transform, layer);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = popupCamera;
            canvas.planeDistance = PopupPlaneDistance;
        }


        private static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target) SetLayerRecursively(child, layer);
        }

        // 1フレームで進める時間の上限。unscaledDeltaTime には deltaTime のような上限が無く、
        // 処理落ちの直後に大きな値が来ると、出現も表示時間も1フレームで飛ばされてしまう
        private const float MaxFrameTime = 1f / 15f;

        // 演出の速さが timeScale（スローや一時停止）に引きずられないよう、実時間で進める
        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);
            _phaseTime += dt;
            _glitchTimer -= dt;

            switch (_phase)
            {
                case Phase.Appearing when _phaseTime >= _appearDuration:
                    _phase = Phase.Holding;
                    _phaseTime = 0f;
                    break;
                case Phase.Holding when _phaseTime >= _displayDuration:
                    _phase = Phase.Disappearing;
                    _phaseTime = 0f;
                    break;
                case Phase.Disappearing when _phaseTime >= _disappearDuration:
                    gameObject.SetActive(false);
                    return;
            }

            Apply();
        }

        private void Apply()
        {
            float p;
            switch (_phase)
            {
                case Phase.Appearing:
                    p = Mathf.Clamp01(_phaseTime / _appearDuration);
                    if (_isRefresh)
                    {
                        SetPlacement(1f, 1f);
                    }
                    else
                    {
                        // 前半で滑り込み、ほぼ同時に横一線から開く
                        SetPlacement(EaseOut(Mathf.Clamp01(p * 2.5f)), Mathf.Max(0.06f, EaseOut(Mathf.Clamp01(p * 3f))));
                    }
                    SetText(Decode(_message, Mathf.Clamp01(p * 1.25f)));
                    SetGlitch(p < 0.85f, flicker: !_isRefresh && p < 0.6f);
                    break;

                case Phase.Holding:
                    SetPlacement(1f, 1f);
                    SetText(_message);
                    SetGlitch(false, flicker: false);
                    break;

                case Phase.Disappearing:
                    p = Mathf.Clamp01(_phaseTime / _disappearDuration);
                    // 横一線に潰れてから、上へ抜ける
                    SetPlacement(1f - EaseIn(Mathf.Clamp01((p - 0.4f) / 0.6f)), Mathf.Max(0.04f, 1f - EaseIn(Mathf.Clamp01(p * 1.6f))));
                    SetText(_message);
                    SetGlitch(true, flicker: true);
                    break;
            }
        }

        // slide: 0 = 画面外、1 = 表示位置 / openness: パネルの縦の開き具合
        private void SetPlacement(float slide, float openness)
        {
            if (_slider != null) _slider.anchoredPosition = new Vector2(_slider.anchoredPosition.x, Mathf.Lerp(_hiddenY, _shownY, slide));
            if (_panel != null) _panel.localScale = new Vector3(1f, openness, 1f);
        }

        private void SetText(string text)
        {
            _text.text = text;
            if (_ghostTexts == null) return;
            foreach (var ghost in _ghostTexts)
            {
                if (ghost != null) ghost.text = text;
            }
        }

        // 先頭から progress の割合までを確定させ、残りは記号で埋める
        private string Decode(string message, float progress)
        {
            if (progress >= 1f) return message;

            int revealed = Mathf.FloorToInt(message.Length * progress);
            _scrambleBuilder.Clear();
            _scrambleBuilder.Append(message, 0, revealed);
            for (int i = revealed; i < message.Length; i++)
            {
                _scrambleBuilder.Append(char.IsWhiteSpace(message[i])
                    ? message[i]
                    : ScrambleCharacters[Random.Range(0, ScrambleCharacters.Length)]);
            }
            return _scrambleBuilder.ToString();
        }

        // ノイズは _glitchInterval ごとにだけ振り直す（毎フレーム変えると細かすぎて灰色に見える）
        private void SetGlitch(bool isActive, bool flicker)
        {
            if (!isActive)
            {
                if (_canvasGroup != null) _canvasGroup.alpha = 1f;
                if (_slider != null) _slider.anchoredPosition = new Vector2(0f, _slider.anchoredPosition.y);
                SetGhosts(false);
                SetBars(false);
                return;
            }

            if (_glitchTimer > 0f) return;
            _glitchTimer = _glitchInterval;

            if (_canvasGroup != null) _canvasGroup.alpha = flicker && Random.value < 0.35f ? 0.25f : 1f;
            if (_slider != null) _slider.anchoredPosition = new Vector2(Random.Range(-8f, 8f), _slider.anchoredPosition.y);
            SetGhosts(true);
            SetBars(true);
        }

        private void SetGhosts(bool isActive)
        {
            if (_ghostTexts == null) return;
            for (int i = 0; i < _ghostTexts.Length; i++)
            {
                var ghost = _ghostTexts[i];
                if (ghost == null) continue;
                ghost.gameObject.SetActive(isActive);
                if (!isActive) continue;

                // 残像は左右に逆向きにずらす（シアンとマゼンタが両側にはみ出す）
                float side = i % 2 == 0 ? -1f : 1f;
                ghost.rectTransform.anchoredPosition = side * _ghostOffset * Random.Range(0.4f, 1.6f);
            }
        }

        private void SetBars(bool isActive)
        {
            if (_glitchBars == null || _panel == null) return;
            float halfHeight = _panel.rect.height * 0.5f;
            foreach (var bar in _glitchBars)
            {
                if (bar == null) continue;
                bool show = isActive && Random.value < 0.7f;
                bar.gameObject.SetActive(show);
                if (!show) continue;

                var rt = bar.rectTransform;
                rt.anchoredPosition = new Vector2(Random.Range(-40f, 40f), Random.Range(-halfHeight, halfHeight));
                rt.sizeDelta = new Vector2(Random.Range(-120f, 40f), Random.Range(3f, 9f));
                var color = _glitchColors.Length > 0 ? _glitchColors[Random.Range(0, _glitchColors.Length)] : Color.white;
                color.a = Random.Range(0.5f, 0.9f);
                bar.color = color;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;
    }
}
