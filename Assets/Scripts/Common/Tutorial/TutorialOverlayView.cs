using System;
using Live2DCardRogue.Common.Rendering;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Tutorial
{
    // チュートリアルの画面（Resources/TutorialOverlay.prefab）。どの画面の上にも重ねる。
    //   ・強調する UI の周りを暗くし（4枚の板で囲んで真ん中に穴を空ける）、穴に光る枠を出す
    //   ・吹き出しは穴の空いている側（下 → 上 → 右 → 左）に出し、画面からはみ出さないよう寄せる
    //   ・クリックで進む手は、画面全体でクリックを受けて「次へ」を知らせる（下の画面は触れない）
    //   ・出来事で進む手は、穴の中だけ下の画面を触れる（暗い板がほかを塞ぐ）
    // 目印の位置は毎フレーム引き直すので、UI が動いても（持ち上がるカードなど）枠が付いてくる。
    // 何を出すか・いつ進むかは知らない（TutorialSession が決める）
    public sealed class TutorialOverlayView : MonoBehaviour
    {
        private const float PopupPlaneDistance = 90f;
        private const float HolePadding = 10f;
        private const float BubbleMargin = 26f;
        private const float ScreenMargin = 24f;
        private const float OpenDuration = 0.1f;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _root;
        // 見せるもの全部の親（待つだけの手では隠す）
        [SerializeField] private GameObject _visual;
        [SerializeField] private RectTransform _dimTop;
        [SerializeField] private RectTransform _dimBottom;
        [SerializeField] private RectTransform _dimLeft;
        [SerializeField] private RectTransform _dimRight;
        [SerializeField] private RectTransform _frame;
        [SerializeField] private Graphic _frameGraphic;
        [SerializeField] private ClickArea _clickCatcher;
        // 吹き出しそのもののクリック（クリックで進む手では、吹き出しを押しても次へ進む）
        [SerializeField] private ClickArea _bubbleClickArea;
        [SerializeField] private RectTransform _bubble;
        [SerializeField] private CommonText _bubbleText;
        [SerializeField] private CommonText _nextHint;
        [SerializeField] private CommonText _counterText;
        [SerializeField] private CommonButton _skipButton;
        [SerializeField] private CommonText _skipLabel;

        private Func<RectTransform> _target;
        private TutorialBubbleSide _side;
        private bool _hasStep;
        private bool _clickToAdvance;
        // 引っ込めているか（スキップの確認ウインドウを出している間。下の画面を触れるようにする）
        private bool _suspended;
        private float _openTime;
        private readonly Vector3[] _corners = new Vector3[4];

        /// <summary>クリックで進む手で、画面がクリックされた</summary>
        public event Action OnNextRequested;

        public event Action OnSkipRequested;

        private void Awake()
        {
            _clickCatcher.OnClicked += HandleClicked;
            if (_bubbleClickArea != null) _bubbleClickArea.OnClicked += HandleBubbleClicked;
            _skipButton.onClick.AddListener(HandleSkip);
            _visual.SetActive(false);
        }

        private void OnDestroy()
        {
            _clickCatcher.OnClicked -= HandleClicked;
            if (_bubbleClickArea != null) _bubbleClickArea.OnClicked -= HandleBubbleClicked;
            _skipButton.onClick.RemoveListener(HandleSkip);
        }

        public void SetLabels(string skip, string next)
        {
            _skipLabel.text = skip;
            _nextHint.text = next;
        }

        /// <param name="target">強調する UI（毎フレーム引き直す。null を返すなら強調しない）</param>
        /// <param name="clickToAdvance">true ならどこをクリックしても次へ。false なら穴の中だけ操作できる</param>
        public void ShowStep(Func<RectTransform> target, string text, TutorialBubbleSide side, bool clickToAdvance, string counter)
        {
            BindToPopupCamera();
            _target = target;
            _side = side;
            _hasStep = true;
            _clickToAdvance = clickToAdvance;
            _bubbleText.text = text;
            _counterText.text = counter;
            _nextHint.gameObject.SetActive(clickToAdvance);
            _clickCatcher.gameObject.SetActive(clickToAdvance);
            _openTime = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
            Layout();
        }

        /// <summary>待つだけの手: 何も出さず、下の画面をそのまま触れるようにする</summary>
        public void HideStep()
        {
            _hasStep = false;
            _target = null;
            _visual.SetActive(false);
        }

        /// <summary>一時的に引っ込める / 戻す（確認ウインドウを出している間など）。手はそのまま覚えている</summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
            if (suspended) _visual.SetActive(false);
        }

        private void HandleClicked()
        {
            if (_hasStep) OnNextRequested?.Invoke();
        }

        // 吹き出しは画面の手前にあって奥のクリックを受け止めてしまうので、クリックで進む手ならここでも進める
        private void HandleBubbleClicked()
        {
            if (_hasStep && _clickToAdvance) OnNextRequested?.Invoke();
        }

        private void HandleSkip()
        {
            OnSkipRequested?.Invoke();
        }

        // ほかのポップアップより上に描く（トーストと同じくポップアップのカメラに繋ぐ）
        private void BindToPopupCamera()
        {
            // ポップアップの層のいちばん上（ウインドウより手前）に描く。
            // レイヤーはぼかしの後に描かれる UI_PopupTop にする（開いているウインドウのぼかしの下にならないように）
            _canvas.sortingLayerName = "UI_Popup";
            _canvas.sortingOrder = 100;
            int layer = LayerMask.NameToLayer(PopupLayerMember.FrontLayerName);
            if (layer >= 0) SetLayerRecursively(transform, layer);
            var popupCamera = UICameraRig.Current != null ? UICameraRig.Current.PopupCamera : null;
            if (popupCamera == null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = popupCamera;
            _canvas.planeDistance = PopupPlaneDistance;
        }

        private static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target) SetLayerRecursively(child, layer);
        }

        private void LateUpdate()
        {
            if (!_hasStep) return;
            // 場面の切り替えの幕が上がるまでは出さない（幕の下で説明しない）
            bool show = !SceneTransitionEffect.IsTransitioning && !_suspended;
            if (_visual.activeSelf != show) _visual.SetActive(show);
            if (!show) return;

            Layout();

            _openTime += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Mathf.Clamp01(_openTime / OpenDuration);
            _bubble.localScale = new Vector3(1f, Mathf.Max(0.05f, 1f - (1f - t) * (1f - t)), 1f);
            if (_frameGraphic != null)
            {
                var c = _frameGraphic.color;
                c.a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
                _frameGraphic.color = c;
            }
        }

        private void Layout()
        {
            var size = _root.rect.size;
            float halfW = size.x / 2f, halfH = size.y / 2f;
            var targetRect = _target?.Invoke();
            Rect hole = default;
            bool hasHole = targetRect != null && targetRect.gameObject.activeInHierarchy && TryGetLocalRect(targetRect, out hole);

            if (!hasHole)
            {
                // 強調するものが無い（まだ出ていない）: 全体を暗くして吹き出しを真ん中に。
                // 出来事を待つ手なら、操作できないと進めないので暗くしない
                bool dim = _clickCatcher.gameObject.activeSelf;
                SetDim(_dimTop, -halfW, halfW, -halfH, halfH, dim);
                SetDim(_dimBottom, 0, 0, 0, 0, false);
                SetDim(_dimLeft, 0, 0, 0, 0, false);
                SetDim(_dimRight, 0, 0, 0, 0, false);
                _frame.gameObject.SetActive(false);
                PlaceBubble(new Rect(0, 0, 0, 0), centered: true, halfW, halfH);
                return;
            }

            hole = new Rect(hole.xMin - HolePadding, hole.yMin - HolePadding, hole.width + HolePadding * 2, hole.height + HolePadding * 2);
            SetDim(_dimTop, -halfW, halfW, hole.yMax, halfH, true);
            SetDim(_dimBottom, -halfW, halfW, -halfH, hole.yMin, true);
            SetDim(_dimLeft, -halfW, hole.xMin, hole.yMin, hole.yMax, true);
            SetDim(_dimRight, hole.xMax, halfW, hole.yMin, hole.yMax, true);
            _frame.gameObject.SetActive(true);
            _frame.anchoredPosition = hole.center;
            _frame.sizeDelta = hole.size + new Vector2(12f, 12f);
            PlaceBubble(hole, centered: false, halfW, halfH);
        }

        private static void SetDim(RectTransform dim, float xMin, float xMax, float yMin, float yMax, bool active)
        {
            bool show = active && xMax > xMin && yMax > yMin;
            if (dim.gameObject.activeSelf != show) dim.gameObject.SetActive(show);
            if (!show) return;
            dim.anchoredPosition = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);
            dim.sizeDelta = new Vector2(xMax - xMin, yMax - yMin);
        }

        private void PlaceBubble(Rect hole, bool centered, float halfW, float halfH)
        {
            var bubbleSize = _bubble.rect.size;
            Vector2 pos;
            if (centered)
            {
                pos = Vector2.zero;
            }
            else
            {
                var side = _side;
                if (side == TutorialBubbleSide.Auto)
                {
                    float below = hole.yMin - (-halfH);
                    float above = halfH - hole.yMax;
                    float right = halfW - hole.xMax;
                    float left = hole.xMin - (-halfW);
                    float needV = bubbleSize.y + BubbleMargin + ScreenMargin;
                    float needH = bubbleSize.x + BubbleMargin + ScreenMargin;
                    side = below >= needV ? TutorialBubbleSide.Below
                        : above >= needV ? TutorialBubbleSide.Above
                        : right >= needH ? TutorialBubbleSide.Right
                        : TutorialBubbleSide.Left;
                }

                switch (side)
                {
                    case TutorialBubbleSide.Above:
                        pos = new Vector2(hole.center.x, hole.yMax + BubbleMargin + bubbleSize.y / 2f);
                        break;
                    case TutorialBubbleSide.Left:
                        pos = new Vector2(hole.xMin - BubbleMargin - bubbleSize.x / 2f, hole.center.y);
                        break;
                    case TutorialBubbleSide.Right:
                        pos = new Vector2(hole.xMax + BubbleMargin + bubbleSize.x / 2f, hole.center.y);
                        break;
                    default:
                        pos = new Vector2(hole.center.x, hole.yMin - BubbleMargin - bubbleSize.y / 2f);
                        break;
                }
            }

            // 画面からはみ出さないよう寄せる
            pos.x = Mathf.Clamp(pos.x, -halfW + ScreenMargin + bubbleSize.x / 2f, halfW - ScreenMargin - bubbleSize.x / 2f);
            pos.y = Mathf.Clamp(pos.y, -halfH + ScreenMargin + bubbleSize.y / 2f, halfH - ScreenMargin - bubbleSize.y / 2f);
            _bubble.anchoredPosition = pos;
        }

        // 目印の四隅を画面座標にし、このキャンバスの座標へ直す（キャンバスが違っても、カメラが違ってもよい）
        private bool TryGetLocalRect(RectTransform target, out Rect rect)
        {
            rect = default;
            var targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas == null) return false;
            targetCanvas = targetCanvas.rootCanvas;
            var targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            var myCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            target.GetWorldCorners(_corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in _corners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(targetCamera, corner);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, myCamera, out var local)) return false;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.width > 1f && rect.height > 1f;
        }
    }
}
