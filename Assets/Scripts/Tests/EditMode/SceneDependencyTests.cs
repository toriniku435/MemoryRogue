using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // 依存の向き: シーン同士は互いを参照しない。Window はシーンを参照しない（シーンが Window を使う）。Common はどちらも参照しない。
    // シーンをまたいで渡すもの（パラメータ・結果）は Common/SceneTransition/Parameters に置く。
    // using を1行足すだけで破れ、コンパイルは通ってしまうので、ソースの名前空間の参照をここで見張る
    public sealed class SceneDependencyTests
    {
        private const string WindowFolder = "Window";

        private static string ScriptsRoot => Path.Combine(Application.dataPath, "Scripts");
        private static string ScenesRoot => Path.Combine(ScriptsRoot, "Scenes");
        private static string WindowRoot => Path.Combine(ScriptsRoot, WindowFolder);

        // Scenes/ 直下のシーンのフォルダ名 = 名前空間の2段目
        private static IEnumerable<string> SceneNames()
        {
            return Directory.GetDirectories(ScenesRoot).Select(Path.GetFileName);
        }

        [Test]
        public void Scene_DoesNotReferenceAnotherScene()
        {
            var scenes = SceneNames().ToList();
            var violations = new List<string>();
            foreach (var scene in scenes)
            {
                var others = scenes.Where(s => s != scene).ToList();
                violations.AddRange(FindReferences(Path.Combine(ScenesRoot, scene), others));
            }
            Assert.IsEmpty(violations, "シーンが別のシーンを参照しています（渡すものは Common/SceneTransition/Parameters へ）:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Window_DoesNotReferenceScene()
        {
            var violations = FindReferences(WindowRoot, SceneNames().ToList());
            Assert.IsEmpty(violations, "Window がシーンを参照しています:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Common_DoesNotReferenceSceneOrWindow()
        {
            var targets = SceneNames().Append(WindowFolder).ToList();
            var violations = FindReferences(Path.Combine(ScriptsRoot, "Common"), targets);
            Assert.IsEmpty(violations, "Common がシーンか Window を参照しています:\n" + string.Join("\n", violations));
        }

        // folder 以下の .cs から、Live2DCardRogue.<target> への参照（using・完全修飾名）を探す。コメントと文字列は除く
        private static List<string> FindReferences(string folder, IReadOnlyCollection<string> targets)
        {
            var result = new List<string>();
            if (targets.Count == 0) return result;
            var pattern = new Regex(@"\bLive2DCardRogue\.(" + string.Join("|", targets.Select(Regex.Escape)) + @")\b");
            foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                string code = Strip(File.ReadAllText(file));
                foreach (Match match in pattern.Matches(code))
                {
                    result.Add(file.Substring(ScriptsRoot.Length + 1).Replace('\\', '/') + " -> " + match.Value);
                }
            }
            return result.Distinct().ToList();
        }

        private static string Strip(string code)
        {
            code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);
            code = Regex.Replace(code, @"//[^\n]*", "");
            return Regex.Replace(code, "\"(?:[^\"\\\\\\n]|\\\\.)*\"", "\"\"");
        }
    }
}
