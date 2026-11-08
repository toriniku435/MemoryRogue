using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // バッファ（手札）が空のとき、手札が並ぶ場所に出す説明（// BUFFER EMPTY、理由、どうすればよいか）。
    // 何も無い手札の場所が「表示が壊れた」ように見えないためのもの。出すか・何と書くかは手札の Presenter が決める。
    // 出るときは CRT のように横線から開き、少しちらつく（時間は実時間で、1フレームの上限付き）
    public sealed class BattleBufferEmptyView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CommonText _messageText;
        [SerializeField] private CommonText _hintText;
        [SerializeField][Min(0.01f)] private float _openDuration = 0.18f;

        private float _time;
        private bool _shown;

        public void Show(string message, string hint)
        {
            _messageText.text = message;
            _hintText.text = hint;
            if (_shown) return;

            _shown = true;
            _time = 0f;
            gameObject.SetActive(true);
            Apply();
        }

        public void Hide()
        {
            _shown = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _time += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            Apply();
        }

        private void Apply()
        {
            float t = Mathf.Clamp01(_time / _openDuration);
            float eased = 1f - (1f - t) * (1f - t);
            _panel.localScale = new Vector3(1f, Mathf.Max(0.02f, eased), 1f);
            // 開く間だけちらつき、開いたら案内だけをゆっくり明滅させる
            _canvasGroup.alpha = t < 1f ? (Mathf.Repeat(_time * 40f, 1f) < 0.5f ? 0.55f : 1f) : 1f;
            var color = _hintText.color;
            color.a = t < 1f ? 1f : 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_time * 2.5f));
            _hintText.color = color;
        }
    }
}
