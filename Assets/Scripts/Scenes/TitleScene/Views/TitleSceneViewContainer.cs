using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.TitleScene.Views
{
    public sealed class TitleSceneViewContainer : MonoBehaviour
    {
        // 画面に並べた順に入れる。どのボタンがどのモードかは各View側が持つ
        [SerializeField] private TitleModeButtonView[] _modeButtons;

        // ウインドウを生成する親（WindowUseCase）
        [SerializeField] private RectTransform _popupCanvas;

        public IReadOnlyList<TitleModeButtonView> ModeButtons => _modeButtons;
        public RectTransform PopupCanvas => _popupCanvas;
    }
}
