#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Live2DCardRogue.Common.DebugMenu
{
    // デバッグ機能のボタンを並べる画面。エディタと Development Build にだけ存在し、
    // リリースビルドにはこのファイルごと入らない（#if で囲んである。使う側も必ず同じ条件で囲むこと）。
    //
    //   - 数字キーで 0707 と打つと表示し、表示中にもう一度打つと隠す（上段の数字・テンキーのどちらでもよい）。
    //     入力欄に文字を打っている間は数えない（数値を打っただけで表示が切り替わらないように）
    //   - キーボードの無い端末では、画面の隅を「左上 → 右上 → 左上 → 右上」と2秒以内にタップしても同じ（マウスのクリックでもよい）。
    //     ゲームの UI とは別に Input System のポインターから直接読むので、隅にボタンがあればそのボタンも押される
    //   - 項目は各シーンが AddCommand（ボタン）/ AddIntCommand（数値の入力欄 + ボタン）で登録し、
    //     戻り値を Dispose して外す（シーンを抜けるときに外す）
    //
    // 見た目はゲームの UI に混ぜないよう IMGUI で描く。プレハブや Resources のアセットを持たないので、
    // リリースビルドに何も残らない。
    // 自分で自分を生成する常駐オブジェクト（DontDestroyOnLoad）で、シーンに置く必要はない
    public sealed class DebugMenuOverlay : MonoBehaviour
    {
        private const string ToggleSequence = "0707";

        // 1920x1080 を基準に、画面の高さに合わせて拡大する
        private const float ReferenceHeight = 1080f;
        private const float PanelWidth = 420f;
        private const float RowHeight = 56f;
        private const float RowSpacing = 8f;
        private const float FieldWidth = 130f;

        // 隅の大きさ（画面の短い辺に対する割合）と、4回のタップにかけてよい秒数
        private const float CornerSizeRatio = 0.1f;
        private const float CornerSequenceDuration = 2f;

        private static readonly List<Entry> Entries = new List<Entry>();

        private readonly DebugCommandSequence _sequence = new DebugCommandSequence(ToggleSequence);
        private readonly DebugCommandSequence _cornerSequence = new DebugCommandSequence(ToggleSequence, CornerSequenceDuration);
        private bool _isVisible;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _fieldStyle;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        private static readonly Key[] NumpadKeys =
        {
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
        };

        /// <summary>
        /// デバッグ機能のボタンを1つ足す。戻り値を Dispose すると外れる。
        /// 呼び出し側も #if UNITY_EDITOR || DEVELOPMENT_BUILD で囲むこと
        /// </summary>
        public static IDisposable AddCommand(string label, Action action)
        {
            return Add(new ButtonEntry(label, action));
        }

        /// <summary>
        /// 数値の入力欄つきのボタンを1つ足す。押すと入力欄の値（0以上の整数）を渡して呼ぶ。戻り値を Dispose すると外れる。
        /// 呼び出し側も #if UNITY_EDITOR || DEVELOPMENT_BUILD で囲むこと
        /// </summary>
        public static IDisposable AddIntCommand(string label, int defaultValue, Action<int> action)
        {
            return Add(new IntEntry(label, defaultValue, action));
        }

        private static Entry Add(Entry entry)
        {
            Entries.Add(entry);
            return entry;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            var go = new GameObject(nameof(DebugMenuOverlay));
            DontDestroyOnLoad(go);
            go.AddComponent<DebugMenuOverlay>();
        }

        // ドメインリロードを切った状態で Play し直しても、前回の登録が残らないように
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
        }

        private void Update()
        {
            ReadCornerTap();
            ReadDigitKeys();
        }

        private void ReadCornerTap()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            var position = pointer.position.ReadValue();
            char corner = DebugScreenCorner.Of(position.x, position.y, Screen.width, Screen.height, CornerSizeRatio);
            if (_cornerSequence.Input(corner, Time.unscaledTime)) Toggle();
        }

        private void ReadDigitKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 入力欄に打っている数字は、隠しコマンドとして数えない
            if (_isVisible && GUIUtility.keyboardControl != 0) return;

            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (!keyboard[DigitKeys[i]].wasPressedThisFrame && !keyboard[NumpadKeys[i]].wasPressedThisFrame) continue;
                if (_sequence.Input((char)('0' + i))) Toggle();
            }
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            // 隠すときは入力欄から抜けておく（抜けないまま隠すと、次に開くまで 0707 が数えられない）
            if (!_isVisible) GUIUtility.keyboardControl = 0;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            EnsureStyles();

            float height = 60f + Mathf.Max(1, Entries.Count) * (RowHeight + RowSpacing);
            var panel = new Rect(24f, 140f, PanelWidth, height);

            // パネルの外をクリックしたら入力欄から抜ける（抜けないと 0707 で隠せなくなる）
            if (Event.current.type == EventType.MouseDown && !panel.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
            }

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label("DEBUG  (0707 / TL TR TL TR)", _labelStyle);

            if (Entries.Count == 0)
            {
                GUILayout.Label("no commands in this scene", _labelStyle);
            }

            // 押した処理の中で登録が外れることがある（シーン遷移など）ので、写しを回す
            foreach (var entry in Entries.ToArray())
            {
                if (entry.Draw(_buttonStyle, _fieldStyle))
                {
                    // 実行したら入力欄から抜ける
                    GUIUtility.keyboardControl = 0;
                }
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_buttonStyle != null) return;
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            _fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 26, alignment = TextAnchor.MiddleRight };
        }

        private abstract class Entry : IDisposable
        {
            /// <summary>1行ぶん描く。押されて実行したら true</summary>
            public abstract bool Draw(GUIStyle buttonStyle, GUIStyle fieldStyle);

            public void Dispose()
            {
                Entries.Remove(this);
            }
        }

        private sealed class ButtonEntry : Entry
        {
            private readonly string _label;
            private readonly Action _action;

            public ButtonEntry(string label, Action action)
            {
                _label = label;
                _action = action;
            }

            public override bool Draw(GUIStyle buttonStyle, GUIStyle fieldStyle)
            {
                if (!GUILayout.Button(_label, buttonStyle, GUILayout.Height(RowHeight))) return false;
                _action?.Invoke();
                return true;
            }
        }

        private sealed class IntEntry : Entry
        {
            private readonly string _label;
            private readonly Action<int> _action;
            private string _text;

            public IntEntry(string label, int defaultValue, Action<int> action)
            {
                _label = label;
                _action = action;
                _text = Math.Max(0, defaultValue).ToString();
            }

            public override bool Draw(GUIStyle buttonStyle, GUIStyle fieldStyle)
            {
                GUILayout.BeginHorizontal();
                _text = DigitsOnly(GUILayout.TextField(_text, 9, fieldStyle, GUILayout.Width(FieldWidth), GUILayout.Height(RowHeight)));
                bool pressed = GUILayout.Button(_label, buttonStyle, GUILayout.Height(RowHeight));
                GUILayout.EndHorizontal();

                if (!pressed) return false;
                _action?.Invoke(int.TryParse(_text, out int value) ? value : 0);
                return true;
            }

            private static string DigitsOnly(string text)
            {
                var chars = new List<char>(text.Length);
                foreach (var c in text)
                {
                    if (c >= '0' && c <= '9') chars.Add(c);
                }
                return new string(chars.ToArray());
            }
        }
    }
}
#endif
