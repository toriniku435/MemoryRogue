using UnityEngine;

namespace Live2DCardRogue.BattleScene.Views
{
    // 敵の頭上に出す優先ターゲットの印（下向きの黄色いカーソルと TARGET の文字）。
    // 出るときは横一線から開いてちらつき、出ている間はドット単位で上下に揺れる
    public sealed class BattlePriorityTargetView : MonoBehaviour
    {
        // 開く・揺れる対象（このオブジェクトの子。自分自身は表示の切り替えだけに使う）
        [SerializeField] private RectTransform _body;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField][Min(0.01f)] private float _openDuration = 0.16f;
        [SerializeField] private float _bobHeight = 6f;
        [SerializeField][Min(0.01f)] private float _bobPeriod = 0.9f;
        // 揺れをこの幅のドットにそろえる（デジタルな動き）
        [SerializeField][Min(1f)] private float _step = 2f;

        private const float MaxDeltaTime = 1f / 30f;

        private float _openTime;
        private float _bobTime;

        public void SetVisible(bool isVisible)
        {
            if (isVisible == gameObject.activeSelf) return;

            gameObject.SetActive(isVisible);
            if (isVisible)
            {
                _openTime = 0f;
                _bobTime = 0f;
                Apply();
            }
        }

        private void Update()
        {
            float delta = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime);
            _openTime += delta;
            _bobTime += delta;
            Apply();
        }

        private void Apply()
        {
            if (_body == null) return;

            float open = Mathf.Clamp01(_openTime / _openDuration);
            float eased = 1f - (1f - open) * (1f - open);
            _body.localScale = new Vector3(1f, Mathf.Max(0.05f, eased), 1f);

            // 開いている間はちらつかせる
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = open < 1f ? (Mathf.Repeat(_openTime * 40f, 1f) < 0.5f ? 0.35f : 1f) : 1f;
            }

            float wave = (Mathf.Sin(_bobTime / _bobPeriod * Mathf.PI * 2f) + 1f) * 0.5f * _bobHeight;
            _body.anchoredPosition = new Vector2(0f, Mathf.Round(wave / _step) * _step);
        }
    }
}
