using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Live2DCardRogue.Common.Views
{
    // 状態効果1件の見出しを表示するView。説明はカーソルを合わせている間だけ開く
    // （常に出しておくと、効果が重なったときに一覧が縦に伸びすぎるため）。
    // タッチではカーソルの出入りが無いので、ExpandsOnHover を切り、タップ（OnClicked）を受けた側が SetExpanded で開け閉めする
    public sealed class StatusEffectView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _descriptionText;
        // 説明を載せる吹き出し。行の真上に最前面で出す（下にあるバッファなどに隠れないように）。未設定なら説明の文字だけを出し入れする
        [SerializeField] private GameObject _tooltip;

        // 同じ Canvas の手札などより手前に出す
        private const int TooltipSortingOrder = 50;

        public bool ExpandsOnHover { get; set; } = true;

        public event Action OnClicked;

        public void SetStatusEffect(string name, string description)
        {
            _nameText.text = name;
            _descriptionText.text = description;
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ExpandsOnHover) SetExpanded(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ExpandsOnHover) SetExpanded(false);
        }

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke();

        private void Awake() => SetExpanded(false);

        // 隠れて使い回されるときは閉じた状態に戻す
        private void OnDisable() => SetExpanded(false);

        public void SetExpanded(bool isExpanded)
        {
            var target = _tooltip != null ? _tooltip : (_descriptionText != null ? _descriptionText.gameObject : null);
            if (target == null) return;
            target.SetActive(isExpanded);

            // 入れ子の Canvas は有効になった時点で描画順の上書きが外れることがあるので、出すたびに掛け直す
            if (isExpanded && target.TryGetComponent<Canvas>(out var canvas))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = TooltipSortingOrder;
            }
        }
    }
}
