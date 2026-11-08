using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // 山札・捨て札のように、カード1枚ずつではなく合計枚数だけを表示するView。
    // クリックされたことも伝える（戦闘のアーカイブ・パージは、クリックで中身の一覧を開く）。
    // 聞いている相手がいなければ、クリックしても何も起きない（音も鳴らさない）
    public sealed class CardPileCountView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CommonText _labelText;
        [SerializeField] private CommonText _countText;

        /// <summary>クリックされた</summary>
        public event Action OnClicked;

        // 表示する文字列はPresenterが決める(Viewはローカライズテーブルを知らない)
        public void SetLabel(string label)
        {
            _labelText.text = label;
        }

        public void SetCount(int count)
        {
            _countText.text = count.ToString();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnClicked == null) return;
            AudioUseCase.PlaySe(SeId.Click);
            OnClicked.Invoke();
        }
    }
}
