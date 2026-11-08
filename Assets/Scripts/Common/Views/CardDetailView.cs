using UnityEngine;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // カーソルを合わせたカードの詳細（名前・コスト・効果・イラスト）を、画面の空いている所に大きく出すパネル。
    // 中身の埋め方は CardView と同じ（文字の欄の置き方だけが違う）。入力は一切受け取らない。
    // 戦闘の手札・実行リストと、カード購入の結果ウインドウで使っている。
    //
    // 見せ方:
    //   何も出ていない所から出るとき: 枠ごと横一線から開く
    //   出ている間に別のカードへ切り替わるとき: 枠はそのまま、中身だけをその場で入れ替える
    //   閉じるとき: 少しだけ待ってから閉じる。カードからカードへカーソルを移すと「外れる → 乗る」の順に通知が来るので、
    //     すぐ閉じると一瞬消えてまた開き、ガタついて見えるため
    public sealed class CardDetailView : CardView
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField][Min(0.01f)] private float _openDuration = 0.12f;
        [SerializeField][Min(0f)] private float _hideDelay = 0.12f;

        private float _openTime = float.MaxValue;
        // 閉じるまでの残り時間（負なら閉じる予定なし）
        private float _hideTimer = -1f;

        public void Show(CardData cardData)
        {
            _hideTimer = -1f;

            if (!gameObject.activeSelf)
            {
                SetCardData(cardData);
                gameObject.SetActive(true);
                _openTime = 0f;
            }
            else if (cardData != CardData)
            {
                SetCardData(cardData);
            }

            Apply();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf || _hideTimer >= 0f) return;
            _hideTimer = _hideDelay;
        }

        private void Update()
        {
            // 処理落ちの直後に大きな値が来ても、開く途中が飛ばないよう1フレームの上限を付ける
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_hideTimer >= 0f)
            {
                _hideTimer -= dt;
                if (_hideTimer < 0f)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

            _openTime += dt;
            Apply();
        }

        // 枠ごと開く（出てくるときだけ）
        private void Apply()
        {
            if (_panel == null) return;
            float p = Mathf.Clamp01(_openTime / _openDuration);
            _panel.localScale = new Vector3(1f, Mathf.Lerp(0.05f, 1f, 1f - (1f - p) * (1f - p)), 1f);
        }
    }
}
