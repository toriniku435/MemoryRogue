using System;
using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    // チュートリアルの入り口。手順を渡すと画面（Resources/TutorialOverlay.prefab）を出して流し、
    // 終わったら onEnded（true: 最後まで進んだ、false: スキップした）を呼ぶ。
    // 返した IDisposable を Dispose すると途中でも片付く（シーンの Initializer の OnDestroy で呼ぶ）
    public static class TutorialUseCase
    {
        private const string PrefabResourcePath = "TutorialOverlay";

        /// <param name="confirmSkip">
        /// スキップが押されたときに確かめる（シーンの確認ウインドウで尋ね、スキップなら第1引数、やめたら第2引数を呼ぶ）。
        /// null ならすぐスキップする。尋ねている間、チュートリアルの画面は引っ込める（確認ウインドウを押せるように）
        /// </param>
        public static IDisposable Play(TutorialSequence sequence, Action<bool> onEnded, Action<Action, Action> confirmSkip = null)
        {
            if (sequence == null)
            {
                onEnded?.Invoke(true);
                return null;
            }

            var prefab = Resources.Load<TutorialOverlayView>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError("Resources/" + PrefabResourcePath + " が見つからないため、チュートリアルを飛ばします");
                onEnded?.Invoke(false);
                return null;
            }

            var view = UnityEngine.Object.Instantiate(prefab);
            var session = new TutorialSession(sequence, view, onEnded, confirmSkip);
            session.Initialize();
            return session;
        }
    }
}
