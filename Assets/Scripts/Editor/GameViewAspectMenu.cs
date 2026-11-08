using System;
using System.Reflection;
using UnityEditor;

namespace Live2DCardRogue.Editor
{
    // Game ビューの解像度を、対応する画面比率に切り替えるメニュー（表示崩れの確認用）。
    // 16:9 が基準、16:10（Steam Deck など）と 21:9（ウルトラワイド）でも崩れないことを確かめる。
    // Game ビューの解像度を切り替える公開 API が無いので、Unity 内部の型を反射で呼んでいる。
    // Unity の更新で内部の名前が変わると動かなくなるが、その場合はメニューが例外を出すだけで、ゲーム本体には影響しない
    public static class GameViewAspectMenu
    {
        [MenuItem("Tools/Game View/16:9 (1920x1080)")]
        public static void Set169() => Set(1920, 1080);

        [MenuItem("Tools/Game View/16:10 (1920x1200)")]
        public static void Set1610() => Set(1920, 1200);

        [MenuItem("Tools/Game View/21:9 (2560x1080)")]
        public static void Set219() => Set(2560, 1080);

        public static void Set(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
            var groupType = group.GetType();

            int index = FindFixedSize(group, groupType, width, height);
            if (index < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                var newSize = ctor.Invoke(new object[] { Enum.Parse(sizeTypeEnum, "FixedResolution"), width, height, $"{width}x{height}" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            var select = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            select.Invoke(window, new object[] { index, null });
        }

        private static int FindFixedSize(object group, Type groupType, int width, int height)
        {
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == width
                    && (int)st.GetProperty("height").GetValue(size) == height
                    && st.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution")
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
