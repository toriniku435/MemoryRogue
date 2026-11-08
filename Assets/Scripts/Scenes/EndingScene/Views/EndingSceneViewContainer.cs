using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.EndingScene.Views
{
    public sealed class EndingSceneViewContainer : MonoBehaviour
    {
        // 画面全体。どこをクリック（タップ）しても次へ進む
        [SerializeField] private ClickArea _tapArea;

        public ClickArea TapArea => _tapArea;
    }
}
