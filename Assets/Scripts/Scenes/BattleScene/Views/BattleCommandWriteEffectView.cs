using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.BattleScene.Views
{
    // 実行リストにカードが入ったときの「メモリへ命令を書き込む」演出。
    //   差し込み: 少し大きい状態から枠へ押し込まれる → 書き込みの走査線が上から下へ走る → 縁が光る
    //   書き換え: 並べ替えなどで中身が変わったときは、走査線と発光だけ
    // 見た目だけの処理で、位置（anchoredPosition）には触らない（位置はPresenterが決める）
    public sealed class BattleCommandWriteEffectView : MonoBehaviour
    {
        [SerializeField] private RectTransform _scaleTarget;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _scanBar;
        [SerializeField] private Graphic _flash;

        [SerializeField][Min(0.01f)] private float _insertDuration = 0.14f;
        [SerializeField] private float _insertScale = 1.3f;
        [SerializeField][Min(0.01f)] private float _scanDuration = 0.26f;
        [SerializeField][Min(0.01f)] private float _flashDuration = 0.35f;
        [SerializeField] private Color _flashColor = new Color(0f, 0.94f, 1f, 1f);

        private bool _isInitialized;
        private Vector3 _baseScale;
        private bool _isPlaying;
        private bool _withInsert;
        private float _elapsed;

        public void PlayInsert()
        {
            Play(withInsert: true);
        }

        public void PlayRewrite()
        {
            Play(withInsert: false);
        }

        // 演出の途中で退場が始まったときなど、途中の状態を残さずに止める
        public void Stop()
        {
            EnsureInitialized();
            _isPlaying = false;
            ResetVisuals();
        }

        private void Play(bool withInsert)
        {
            EnsureInitialized();
            _withInsert = withInsert;
            _elapsed = 0f;
            _isPlaying = true;
            Apply();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _baseScale = _scaleTarget != null ? _scaleTarget.localScale : Vector3.one;
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _elapsed += Time.deltaTime;
            Apply();
        }

        // 使い回しで非アクティブにされても、途中の状態を残さない
        private void OnDisable()
        {
            if (!_isInitialized) return;
            _isPlaying = false;
            ResetVisuals();
        }

        private void Apply()
        {
            float insertTime = _withInsert ? _insertDuration : 0f;
            // 押し込みが終わり切る少し前から走査線を走らせる
            float scanStart = insertTime * 0.6f;
            float flashStart = scanStart + _scanDuration;
            float total = flashStart + _flashDuration;

            if (_withInsert)
            {
                float p = Mathf.Clamp01(_elapsed / _insertDuration);
                float eased = 1f - (1f - p) * (1f - p) * (1f - p);
                if (_scaleTarget != null) _scaleTarget.localScale = _baseScale * Mathf.Lerp(_insertScale, 1f, eased);
                if (_canvasGroup != null) _canvasGroup.alpha = p;
            }

            if (_scanBar != null)
            {
                float s = (_elapsed - scanStart) / _scanDuration;
                bool visible = s >= 0f && s <= 1f;
                _scanBar.gameObject.SetActive(visible);
                if (visible)
                {
                    float height = ((RectTransform)_scanBar.parent).rect.height;
                    _scanBar.anchoredPosition = new Vector2(_scanBar.anchoredPosition.x, -height * s);
                }
            }

            if (_flash != null)
            {
                float f = (_elapsed - flashStart) / _flashDuration;
                var color = _flashColor;
                color.a = f >= 0f && f <= 1f ? 1f - f : 0f;
                _flash.color = color;
            }

            if (_elapsed >= total)
            {
                _isPlaying = false;
                ResetVisuals();
            }
        }

        private void ResetVisuals()
        {
            if (_scaleTarget != null) _scaleTarget.localScale = _baseScale;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_scanBar != null) _scanBar.gameObject.SetActive(false);
            if (_flash != null)
            {
                var color = _flashColor;
                color.a = 0f;
                _flash.color = color;
            }
        }
    }
}
