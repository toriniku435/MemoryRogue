namespace Live2DCardRogue.Common.Interaction
{
    // ホバー（カーソルを合わせる）が無い端末では、ホバーに頼る操作をタップで代わりにする。その判定の唯一の入口。
    // ビルド先（#if UNITY_ANDROID）ではなく実行環境で決める。
    //
    // UnityEngine.Application ではなく UnityEngine.Device.Application を見ること: Device Simulator が端末の値に
    // 差し替えるのは Device 側だけで、UnityEngine.Application.isMobilePlatform はエディタでは常に false のまま
    // （これで Simulator の中でもマウス版の操作になっていた）。実機ではどちらも同じ値を返す
    public static class InteractionModeDetector
    {
#if UNITY_EDITOR
        private const string ForceTouchKey = "Live2DCardRogue.ForceTouchMode";

        /// <summary>エディタで、端末に関係なくタッチ版にする（Tools > Interaction > Force Touch Mode）。次の戦闘から効く</summary>
        public static bool EditorForceTouch
        {
            get => UnityEditor.EditorPrefs.GetBool(ForceTouchKey, false);
            set => UnityEditor.EditorPrefs.SetBool(ForceTouchKey, value);
        }
#endif

        public static InteractionMode Current
        {
            get
            {
#if UNITY_EDITOR
                if (EditorForceTouch) return InteractionMode.Touch;
#endif
                return UnityEngine.Device.Application.isMobilePlatform ? InteractionMode.Touch : InteractionMode.Pointer;
            }
        }
    }
}
