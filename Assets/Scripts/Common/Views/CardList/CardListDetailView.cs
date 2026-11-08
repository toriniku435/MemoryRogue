using UnityEngine;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // コマンドの一覧（コマンドコレクション、戦闘のアーカイブ・パージ）で、カーソルを合わせたコマンドを大きく見せる（// COMMAND DATA）。
    // 伏せているコマンド（コレクションで手に入れたことが無いもの）は、伏せたカード（「？」）のまま大きく見せる。
    // まだ何も合わせていないときは案内（_emptyHint）を出す
    public sealed class CardListDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CardView _cardView;
        // 伏せたカード（「？」。_root の中で _cardView と同じ場所に置く）
        [SerializeField] private GameObject _lockedCard;
        // 何も合わせていないときに出す案内（_root の外に置く）。未設定でもよい
        [SerializeField] private GameObject _emptyHint;

        public void Hide()
        {
            _root.SetActive(false);
            if (_emptyHint != null) _emptyHint.SetActive(true);
        }

        public void Show(CardData cardData, bool isDiscovered)
        {
            _root.SetActive(true);
            if (_emptyHint != null) _emptyHint.SetActive(false);
            _cardView.gameObject.SetActive(isDiscovered);
            if (_lockedCard != null) _lockedCard.SetActive(!isDiscovered);
            // 伏せているときは中身を描かない（名前や説明が見えないように）
            if (isDiscovered) _cardView.SetCardData(cardData);
        }
    }
}
