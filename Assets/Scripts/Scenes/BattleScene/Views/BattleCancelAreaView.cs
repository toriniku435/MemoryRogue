using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Live2DCardRogue.BattleScene.Views
{
    // Canvas のいちばん奥に敷く、画面いっぱいの透明な板。
    // ほかの UI に当たらなかったクリック（何も無い所のクリック）だけがここに届くので、それを流す
    public sealed class BattleCancelAreaView : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}
