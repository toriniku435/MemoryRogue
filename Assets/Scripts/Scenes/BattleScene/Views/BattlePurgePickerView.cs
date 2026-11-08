using System;
using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // パージするバッファを選んでいる間、そのコマンドのすぐ上に出す小さなパネル（// SELECT PURGE）。
    // 何個選んだか（1/2）と、選び終えたら押す決定ボタンだけを持つ。バッファのコマンドをクリックして選ぶのは手札の View 側。
    // 対象の候補一覧（BattleTargetPickerView）と同じく、位置は毎フレーム目印（カード）から取り直す
    public sealed class BattlePurgePickerView : MonoBehaviour
    {
        // 背景パネル。ピボットは下中央（カードの上端から上へ伸びる）
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CommonText _hintText;
        [SerializeField] private CommonText _countText;
        [SerializeField] private CommonButton _confirmButton;
        [SerializeField] private CommonText _confirmLabel;
        [SerializeField] private float _offsetY = 12f;
        [SerializeField][Min(0.01f)] private float _openDuration = 0.12f;

        private Func<RectTransform> _anchorProvider;
        private float _openTime;

        public event Action OnConfirmClicked;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(HandleConfirm);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(HandleConfirm);
        }

        public void Show(Func<RectTransform> anchorProvider)
        {
            _anchorProvider = anchorProvider;
            _openTime = 0f;
            gameObject.SetActive(true);
            Follow();
        }

        public void Hide()
        {
            _anchorProvider = null;
            gameObject.SetActive(false);
        }

        public void SetTexts(string hint, string confirm)
        {
            _hintText.text = hint;
            _confirmLabel.text = confirm;
        }

        /// <summary>選んだ数と上限。1個も選んでいなければ決定は押せない</summary>
        public void SetCount(int chosen, int capacity)
        {
            _countText.text = $"{chosen}/{capacity}";
            _confirmButton.interactable = chosen > 0;
        }

        private void HandleConfirm()
        {
            OnConfirmClicked?.Invoke();
        }

        private void LateUpdate()
        {
            _openTime += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            Follow();
        }

        private void Follow()
        {
            var anchor = _anchorProvider?.Invoke();
            if (anchor == null) return;

            var top = anchor.TransformPoint(new Vector3(0f, anchor.rect.yMax, 0f));
            _panel.position = top;
            _panel.anchoredPosition += Vector2.up * _offsetY;

            // 横一線から開く（他のサイバー演出とそろえる）
            float p = Mathf.Clamp01(_openTime / _openDuration);
            _panel.localScale = new Vector3(1f, Mathf.Lerp(0.1f, 1f, 1f - (1f - p) * (1f - p)), 1f);
        }
    }
}
