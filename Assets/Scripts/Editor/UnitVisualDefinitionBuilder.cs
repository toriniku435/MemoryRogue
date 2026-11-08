using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Editor
{
    // ArtSource/assets/characters.py が書き出したコマから、SpriteSheetUnitVisualDefinition を作り直す。
    //   Assets/Art/Characters/<Name>/<Name>_<idle|action|hit>_<n>.png → Assets/ScriptableObjects/UnitVisuals/<Name>.asset
    // コマ数を変えて画像を書き出し直したら、これを実行すればアセットの並びが追いつく。
    // Id が <Name> の EnemyData で見た目が未設定のものには、ついでに割り当てる（設定済みのものは触らない）
    public static class UnitVisualDefinitionBuilder
    {
        private const string CharactersFolder = "Assets/Art/Characters";
        private const string OutputFolder = "Assets/ScriptableObjects/UnitVisuals";
        private static readonly Regex FrameName = new Regex(@"^(?<name>.+)_(?<clip>idle|action|hit)_(?<index>\d+)$");

        [MenuItem("Tools/Art/Rebuild Unit Visuals")]
        public static void RebuildAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(OutputFolder).Replace('\\', '/'), Path.GetFileName(OutputFolder));
            }

            foreach (var folder in AssetDatabase.GetSubFolders(CharactersFolder))
            {
                Rebuild(Path.GetFileName(folder), folder);
            }
            AssetDatabase.SaveAssets();
        }

        public static SpriteSheetUnitVisualDefinition Rebuild(string name, string folder)
        {
            var clips = new Dictionary<string, SortedDictionary<int, Sprite>>
            {
                { "idle", new SortedDictionary<int, Sprite>() },
                { "action", new SortedDictionary<int, Sprite>() },
                { "hit", new SortedDictionary<int, Sprite>() },
            };
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var match = FrameName.Match(Path.GetFileNameWithoutExtension(path));
                if (!match.Success || match.Groups["name"].Value != name) continue;
                clips[match.Groups["clip"].Value][int.Parse(match.Groups["index"].Value)] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            string assetPath = $"{OutputFolder}/{name}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<SpriteSheetUnitVisualDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SpriteSheetUnitVisualDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            var so = new SerializedObject(definition);
            so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{name}.png");
            SetFrames(so.FindProperty("_idleFrames"), clips["idle"].Values);
            SetFrames(so.FindProperty("_actionFrames"), clips["action"].Values);
            SetFrames(so.FindProperty("_hitFrames"), clips["hit"].Values);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            AssignToEnemies(name, definition);
            Debug.Log($"UnitVisual {name}: idle {clips["idle"].Count}, action {clips["action"].Count}, hit {clips["hit"].Count}");
            return definition;
        }

        private static void SetFrames(SerializedProperty array, IEnumerable<Sprite> sprites)
        {
            var list = sprites.ToList();
            array.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }

        private static void AssignToEnemies(string name, UnitVisualDefinition definition)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy == null || enemy.Id != name || enemy.Visual != null) continue;

                var so = new SerializedObject(enemy);
                so.FindProperty("_visual").objectReferenceValue = definition;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }
        }
    }
}
