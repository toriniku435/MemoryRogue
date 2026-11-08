using UnityEditor;
using Live2DCardRogue.Common.Interaction;

namespace Live2DCardRogue.Editor
{
    // エディタのゲームビューで、マウスのままタッチ版の操作（タップで説明、手札は2回タップで使う）を試すための切り替え。
    // マウスのクリックがそのままタップの代わりになる。設定はこのPCのエディタにだけ残る（EditorPrefs）
    public static class ForceTouchModeMenu
    {
        private const string MenuPath = "Tools/Interaction/Force Touch Mode";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            InteractionModeDetector.EditorForceTouch = !InteractionModeDetector.EditorForceTouch;
        }

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            Menu.SetChecked(MenuPath, InteractionModeDetector.EditorForceTouch);
            return true;
        }
    }
}
