using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Live2DCardRogue.Editor
{
    // 再生ボタンの隣に置く「どのシーンから再生するか」のプルダウン。
    // 選んだシーンを EditorSceneManager.playModeStartScene にするだけなので、編集中のシーンは開いたまま、
    // 再生ボタンを押すと選んだシーンから始まる（止めれば編集中のシーンに戻る）。
    // 「開いているシーン」を選べば、いつも通り開いているシーンから再生する。
    // 候補は Build Settings に入っている（有効な）シーン。選んだものはプロジェクトごとに覚えておき、Editor を開き直しても保つ
    [InitializeOnLoad]
    public static class PlayModeStartSceneToolbar
    {
        private const string ElementPath = "Live2DCardRogue/Play Start Scene";

        // EditorPrefs はマシン全体で共有なので、プロジェクトの場所を混ぜてプロジェクトごとに分ける
        private static string PrefsKey => "Live2DCardRogue.PlayModeStartScene." + Application.dataPath;

        static PlayModeStartSceneToolbar()
        {
            // 起動直後やスクリプトの再読み込み後にも、覚えておいた選択を当て直す
            EditorApplication.delayCall += ApplySavedScene;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateElement()
        {
            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            var content = new MainToolbarContent(CurrentLabel(), icon, "再生ボタンで開始するシーン");
            return new MainToolbarDropdown(content, ShowMenu);
        }

        private static void ShowMenu(Rect rect)
        {
            string current = SavedScenePath();
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("開いているシーン"), string.IsNullOrEmpty(current), () => Select(null));
            menu.AddSeparator("");

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || string.IsNullOrEmpty(scene.path)) continue;
                string path = scene.path;
                menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(path)), path == current, () => Select(path));
            }

            menu.DropDown(rect);
        }

        private static void Select(string scenePath)
        {
            EditorPrefs.SetString(PrefsKey, scenePath ?? string.Empty);
            ApplySavedScene();
        }

        private static void ApplySavedScene()
        {
            string path = SavedScenePath();
            var scene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            // 覚えていたシーンが消えていたら、開いているシーンからの再生に戻す
            if (scene == null && !string.IsNullOrEmpty(path)) EditorPrefs.SetString(PrefsKey, string.Empty);

            EditorSceneManager.playModeStartScene = scene;
            MainToolbar.Refresh(ElementPath);
        }

        private static string SavedScenePath()
        {
            return EditorPrefs.GetString(PrefsKey, string.Empty);
        }

        private static string CurrentLabel()
        {
            var scene = EditorSceneManager.playModeStartScene;
            return scene != null ? scene.name : "開いているシーン";
        }
    }
}
