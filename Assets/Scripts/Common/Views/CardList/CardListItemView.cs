using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // コマンドを並べるリストの1枚分（Assets/Prefabs/CollectionCardView.prefab）。タイトルのコマンドコレクションと、戦闘のアーカイブ・パージの一覧が使う。
    // 見せてよいコマンドはそのまま見せ、まだ見せないもの（コレクションで手に入れたことが無いもの）は伏せたカード（「？」）にする。
    // カーソルを合わせたことは、伏せたカードでも伝える（拡大表示も伏せたまま出す）。
    // スクロールで使い回されるので、合わせたときに「どのコマンドか」はその時点の値を渡す
    public sealed class CardListItemView : CardView, IPointerEnterHandler
    {
        // コマンドの絵（手に入れたことがあるときだけ出す）
        [SerializeField] private GameObject _cardRoot;
        // 伏せたカード（「？」。手に入れたことが無いときだけ出す）
        [SerializeField] private GameObject _lockedRoot;
        // 拡大表示に出しているコマンドに付ける印（コマンドの絵の側と、伏せたカードの側）。未設定でもよい
        [SerializeField] private GameObject _selectedMark;
        [SerializeField] private GameObject _lockedSelectedMark;

        private Action<CardData> _onHovered;

        public void Bind(CardData cardData, bool isDiscovered, bool isSelected, Action<CardData> onHovered)
        {
            _onHovered = onHovered;
            SetCardData(cardData);
            _cardRoot.SetActive(isDiscovered);
            _lockedRoot.SetActive(!isDiscovered);
            if (_selectedMark != null) _selectedMark.SetActive(isDiscovered && isSelected);
            if (_lockedSelectedMark != null) _lockedSelectedMark.SetActive(!isDiscovered && isSelected);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHovered?.Invoke(CardData);
        }
    }
}
