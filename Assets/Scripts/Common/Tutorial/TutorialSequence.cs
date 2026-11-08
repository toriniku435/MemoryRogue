using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    // チュートリアルの手順（1つの画面ぶん）。手を差し込む・並べ替える・消すのは、このアセットの編集だけで済む。
    // 文言は TutorialText テーブル、強調する場所はシーンの TutorialAnchor が持つ
    [CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "Tutorial/TutorialSequence")]
    public sealed class TutorialSequence : ScriptableObject
    {
        /// <summary>この手順を流すシーンの名前（SceneName）。テストが目印の ID をこのシーンから探すのに使う</summary>
        [SerializeField] private string _sceneName;

        [SerializeField] private TutorialStep[] _steps = new TutorialStep[0];

        public string SceneName => _sceneName;
        public IReadOnlyList<TutorialStep> Steps => _steps ?? new TutorialStep[0];
    }
}
