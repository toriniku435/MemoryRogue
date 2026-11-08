using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Editor
{
    // SoundLibrary の Inspector。SeId / BgmId の全値（None を除く）を enum の順に1行ずつ固定で並べ、
    // 用途（SoundUsageAttribute）、音、音量、試聴ボタンを出す。行の無い値は音を入れた時点で行を足す。
    // 音の無い値は赤く、enum に無い名前の行や重複した行は警告と「整理する」ボタンで知らせる。
    // 「選んだら試聴」がオンなら、アセット一覧で音を選び替えるたびにその音だけを鳴らす（音 2 も同じ）。
    // 最後に選んだ・試聴した行は、ショートカット（既定は A、Shortcuts で変えられる。Inspector かアセット一覧にフォーカスがあるときだけ効く）で重ねた音ごと鳴らし直せる
    [CustomEditor(typeof(SoundLibrary))]
    public sealed class SoundLibraryEditor : UnityEditor.Editor
    {
        private static readonly Color MissingColor = new Color(1f, 0.55f, 0.55f);
        private const string AutoPreviewPrefKey = "Live2DCardRogue.SoundLibrary.AutoPreview";

        // 最後に選んだ・試聴した行（ショートカットで重ねて鳴らし直す対象）
        private static SoundLibrary _lastLibrary;
        private static string _lastId;
        private static bool _lastIsSe;

        private SerializedProperty _seVolume;
        private SerializedProperty _bgmVolume;
        private SerializedProperty _bgmCrossfade;
        private SerializedProperty _seMinInterval;
        private SerializedProperty _se;
        private SerializedProperty _bgm;

        private bool _seFoldout = true;
        private bool _bgmFoldout = true;
        private GUIStyle _usageStyle;

        private void OnEnable()
        {
            _seVolume = serializedObject.FindProperty("_seVolume");
            _bgmVolume = serializedObject.FindProperty("_bgmVolume");
            _bgmCrossfade = serializedObject.FindProperty("_bgmCrossfade");
            _seMinInterval = serializedObject.FindProperty("_seMinInterval");
            _se = serializedObject.FindProperty("_se");
            _bgm = serializedObject.FindProperty("_bgm");
        }

        private void OnDisable()
        {
            SoundPreview.Stop();
        }

        public override void OnInspectorGUI()
        {
            _usageStyle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            serializedObject.Update();

            EditorGUILayout.LabelField("全体", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_seVolume, new GUIContent("SE 音量"));
            EditorGUILayout.PropertyField(_bgmVolume, new GUIContent("BGM 音量"));
            EditorGUILayout.PropertyField(_bgmCrossfade, new GUIContent("BGM クロスフェード (秒)"));
            EditorGUILayout.PropertyField(_seMinInterval, new GUIContent("同じ SE の最短間隔 (秒)"));
            EditorGUILayout.HelpBox("ここの音量にオプションの音量が掛かります。試聴はこのアセットの音量（全体 × 各行）で鳴ります。", MessageType.None);

            EditorGUI.BeginChangeCheck();
            bool autoPreview = EditorGUILayout.Toggle(new GUIContent("選んだら試聴", "音を選び替えたら、その音だけを鳴らす"), AutoPreview);
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetBool(AutoPreviewPrefKey, autoPreview);
            EditorGUILayout.HelpBox($"最後に選んだ・試聴した行を、{ShortcutLabel()} で重ねた音ごと鳴らし直せます（Edit > Shortcuts で変更可）。", MessageType.None);

            EditorGUILayout.Space();
            _seFoldout = EditorGUILayout.Foldout(_seFoldout, $"効果音 (SE)  {CountAssigned<SeId>(_se)}", true, EditorStyles.foldoutHeader);
            if (_seFoldout) DrawSection<SeId>(_se, _seVolume, hasPitch: true);

            EditorGUILayout.Space();
            _bgmFoldout = EditorGUILayout.Foldout(_bgmFoldout, $"BGM  {CountAssigned<BgmId>(_bgm)}", true, EditorStyles.foldoutHeader);
            if (_bgmFoldout) DrawSection<BgmId>(_bgm, _bgmVolume, hasPitch: false);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection<TId>(SerializedProperty array, SerializedProperty master, bool hasPitch)
            where TId : struct, Enum
        {
            DrawProblems<TId>(array);
            foreach (var name in Names<TId>())
            {
                DrawRow(array, name, Usage<TId>(name), master.floatValue, hasPitch);
            }
        }

        private static bool AutoPreview => EditorPrefs.GetBool(AutoPreviewPrefKey, true);

        private void Remember(string name, bool isSe)
        {
            _lastLibrary = (SoundLibrary)target;
            _lastId = name;
            _lastIsSe = isSe;
        }

        // 選び替えた音だけを、その行の音量で鳴らす（「なし」にしたら止める）
        private static void PreviewSelected(AudioClip clip, float volume)
        {
            if (clip == null) SoundPreview.Stop();
            else SoundPreview.Play(clip, volume, null, 0f, 0f);
        }

        // 最後に選んだ・試聴した行を、重ねる音（音 2 と遅延）ごと鳴らし直す
        // A は Timeline や Animation などでも使われているが、どれもその画面の中だけの割り当てなので、そちらが優先される。
        // ほかの画面で A を押して鳴らないよう、Inspector とアセット一覧（ObjectSelector）にフォーカスがあるときだけ鳴らす
        [Shortcut("Live2DCardRogue/SoundLibrary 重ねて試聴", KeyCode.A)]
        private static void PlayLastLayered()
        {
            string focused = EditorWindow.focusedWindow != null ? EditorWindow.focusedWindow.GetType().Name : "";
            if (focused != "InspectorWindow" && focused != "ObjectSelector") return;
            if (_lastLibrary == null || string.IsNullOrEmpty(_lastId)) return;
            var so = new SerializedObject(_lastLibrary);
            var array = so.FindProperty(_lastIsSe ? "_se" : "_bgm");
            int index = FindIndex(array, _lastId);
            if (index < 0) return;
            var entry = array.GetArrayElementAtIndex(index);
            var clip = entry.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
            if (clip == null) return;
            float master = so.FindProperty(_lastIsSe ? "_seVolume" : "_bgmVolume").floatValue;
            PlayPreview(entry, clip, master, _lastIsSe);
        }

        private static string ShortcutLabel()
        {
            var binding = ShortcutManager.instance.GetShortcutBinding("Live2DCardRogue/SoundLibrary 重ねて試聴");
            var text = binding.ToString();
            return string.IsNullOrEmpty(text) ? "ショートカット（未割り当て）" : text;
        }

        private void DrawRow(SerializedProperty array, string name, string usage, float master, bool hasPitch)
        {
            int index = FindIndex(array, name);
            var entry = index >= 0 ? array.GetArrayElementAtIndex(index) : null;
            var clipProp = entry?.FindPropertyRelative("Clip");
            var clip = clipProp?.objectReferenceValue as AudioClip;

            var previousColor = GUI.backgroundColor;
            if (clip == null) GUI.backgroundColor = MissingColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = previousColor;

            // 1行目: 試聴ボタン、名前、用途
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(clip == null))
            {
                bool playing = clip != null && SoundPreview.IsPlaying(clip);
                if (GUILayout.Button(playing ? "■" : "▶", GUILayout.Width(26)))
                {
                    if (playing) SoundPreview.Stop();
                    else PlayPreview(entry, clip, master, hasPitch);
                    Remember(name, hasPitch);
                }
            }
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(usage, _usageStyle);
            EditorGUILayout.EndHorizontal();

            // 2行目: 音、音量、ピッチの揺らぎ
            EditorGUI.BeginChangeCheck();
            var newClip = (AudioClip)EditorGUILayout.ObjectField("音", clip, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (entry == null)
                {
                    entry = AddEntry(array, name, hasPitch);
                }
                entry.FindPropertyRelative("Clip").objectReferenceValue = newClip;
                Remember(name, hasPitch);
                if (AutoPreview) PreviewSelected(newClip, entry.FindPropertyRelative("Volume").floatValue * master);
            }

            if (entry != null)
            {
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Volume"), new GUIContent("音量"));
                if (hasPitch)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("PitchJitter"), new GUIContent("ピッチの揺らぎ"));
                    if (DrawLayer(entry, master))
                    {
                        Remember(name, hasPitch);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // 重ねて鳴らす2つ目の音。音を入れたときだけ音量と遅延を出す。音 2 を選び替えたら true（その音だけを試聴する）
        private static bool DrawLayer(SerializedProperty entry, float master)
        {
            var clip2 = entry.FindPropertyRelative("Clip2");
            bool hadClip = clip2.objectReferenceValue != null;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(clip2, new GUIContent("音 2（重ね）"));
            bool changed = EditorGUI.EndChangeCheck();
            var volume2 = entry.FindPropertyRelative("Volume2");

            // 欄を足す前に作られた行は音量 2 が 0 のことがあるので、初めて音を入れたときは 1 にする
            if (clip2.objectReferenceValue != null && !hadClip && volume2.floatValue <= 0f) volume2.floatValue = 1f;
            if (changed && AutoPreview) PreviewSelected(clip2.objectReferenceValue as AudioClip, volume2.floatValue * master);
            if (clip2.objectReferenceValue == null) return changed;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(volume2, new GUIContent("音量 2"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("Delay2"), new GUIContent("遅延 2 (秒)"));
            EditorGUI.indentLevel--;
            return changed;
        }

        private static void PlayPreview(SerializedProperty entry, AudioClip clip, float master, bool hasPitch)
        {
            float volume = entry.FindPropertyRelative("Volume").floatValue * master;
            var clip2 = hasPitch ? entry.FindPropertyRelative("Clip2").objectReferenceValue as AudioClip : null;
            if (clip2 == null)
            {
                SoundPreview.Play(clip, volume, null, 0f, 0f);
                return;
            }
            SoundPreview.Play(clip, volume,
                clip2, entry.FindPropertyRelative("Volume2").floatValue * master,
                entry.FindPropertyRelative("Delay2").floatValue);
        }

        // enum に無い名前の行と、同じ名前の2つ目以降の行（実行時には使われない）を知らせる
        private static void DrawProblems<TId>(SerializedProperty array) where TId : struct, Enum
        {
            var problems = FindProblemIndices<TId>(array);
            if (problems.Count == 0) return;

            var labels = problems.Select(i => array.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue);
            EditorGUILayout.HelpBox(
                $"使われていない行があります（enum に無い名前か、重複）: {string.Join(", ", labels)}",
                MessageType.Warning);
            if (GUILayout.Button("整理する（使われていない行を削除）"))
            {
                for (int i = problems.Count - 1; i >= 0; i--) array.DeleteArrayElementAtIndex(problems[i]);
            }
        }

        private static List<int> FindProblemIndices<TId>(SerializedProperty array) where TId : struct, Enum
        {
            var valid = new HashSet<string>(Names<TId>());
            var seen = new HashSet<string>();
            var problems = new List<int>();
            for (int i = 0; i < array.arraySize; i++)
            {
                string id = array.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue;
                if (!valid.Contains(id) || !seen.Add(id)) problems.Add(i);
            }
            return problems;
        }

        private static SerializedProperty AddEntry(SerializedProperty array, string name, bool hasPitch)
        {
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            var entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Id").stringValue = name;
            entry.FindPropertyRelative("Volume").floatValue = 1f;
            if (hasPitch) entry.FindPropertyRelative("PitchJitter").floatValue = 0f;
            return entry;
        }

        private static int FindIndex(SerializedProperty array, string name)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue == name) return i;
            }
            return -1;
        }

        private static string CountAssigned<TId>(SerializedProperty array) where TId : struct, Enum
        {
            var names = Names<TId>();
            int assigned = names.Count(name =>
            {
                int index = FindIndex(array, name);
                return index >= 0 && array.GetArrayElementAtIndex(index).FindPropertyRelative("Clip").objectReferenceValue != null;
            });
            return $"{assigned}/{names.Count}";
        }

        private static List<string> Names<TId>() where TId : struct, Enum
        {
            return Enum.GetNames(typeof(TId)).Where(n => n != "None").ToList();
        }

        private static string Usage<TId>(string name) where TId : struct, Enum
        {
            var field = typeof(TId).GetField(name, BindingFlags.Public | BindingFlags.Static);
            return field?.GetCustomAttribute<SoundUsageAttribute>()?.Usage ?? "（用途未記入）";
        }

        // Edit モードでも鳴らせるよう、隠しオブジェクトの AudioSource で試聴する（重ねる音用に2本）。
        // 遅延で鳴る前の音は isPlaying では分からないので、鳴り終わる時刻で「試聴中」を判断する
        private static class SoundPreview
        {
            private static AudioSource[] _sources;
            private static AudioClip _clip;
            private static double _endTime;

            public static bool IsActive => _sources != null && EditorApplication.timeSinceStartup < _endTime;

            public static bool IsPlaying(AudioClip clip) => IsActive && _clip == clip;

            public static void Play(AudioClip clip, float volume, AudioClip clip2, float volume2, float delay2)
            {
                if (_sources == null)
                {
                    var go = EditorUtility.CreateGameObjectWithHideFlags(
                        "SoundLibraryPreview", HideFlags.HideAndDontSave, typeof(AudioSource), typeof(AudioSource));
                    _sources = go.GetComponents<AudioSource>();
                    foreach (var source in _sources)
                    {
                        source.playOnAwake = false;
                        source.spatialBlend = 0f;
                    }
                }

                foreach (var source in _sources) source.Stop();
                _clip = clip;
                _sources[0].clip = clip;
                _sources[0].volume = volume;
                _sources[0].Play();
                double length = clip.length;

                if (clip2 != null)
                {
                    _sources[1].clip = clip2;
                    _sources[1].volume = volume2;
                    if (delay2 > 0f) _sources[1].PlayDelayed(delay2);
                    else _sources[1].Play();
                    length = Math.Max(length, delay2 + clip2.length);
                }
                _endTime = EditorApplication.timeSinceStartup + length;
            }

            public static void Stop()
            {
                if (_sources == null) return;
                UnityEngine.Object.DestroyImmediate(_sources[0].gameObject);
                _sources = null;
                _clip = null;
                _endTime = 0;
            }
        }

        // 試聴中は ▶/■ の表示を更新し続ける
        public override bool RequiresConstantRepaint() => SoundPreview.IsActive;
    }
}
