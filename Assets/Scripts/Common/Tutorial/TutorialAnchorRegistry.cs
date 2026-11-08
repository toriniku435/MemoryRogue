using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    // 目印の ID から、強調する RectTransform を引く。
    //   ・シーンに置いた目印（TutorialAnchor）は、有効になったときに自分で登録する
    //   ・手札のカードのように実行時にできて使い回されるものは、シーンが「接頭辞: 解決する関数」を登録する
    //     （例: 「HandCard:Attack」→ 手札の中のアタックのカード）。チュートリアルは手札の中身を知らない
    public static class TutorialAnchorRegistry
    {
        private static readonly Dictionary<string, TutorialAnchor> Anchors = new Dictionary<string, TutorialAnchor>();
        private static readonly Dictionary<string, Func<string, RectTransform>> Resolvers = new Dictionary<string, Func<string, RectTransform>>();

        public const char PrefixSeparator = ':';

        internal static void Register(TutorialAnchor anchor)
        {
            if (!string.IsNullOrEmpty(anchor.AnchorId)) Anchors[anchor.AnchorId] = anchor;
        }

        internal static void Unregister(TutorialAnchor anchor)
        {
            if (!string.IsNullOrEmpty(anchor.AnchorId) && Anchors.TryGetValue(anchor.AnchorId, out var current) && current == anchor)
            {
                Anchors.Remove(anchor.AnchorId);
            }
        }

        /// <summary>「接頭辞:名前」の形の目印を解決する関数を登録する。返した IDisposable で外す</summary>
        public static IDisposable RegisterResolver(string prefix, Func<string, RectTransform> resolver)
        {
            Resolvers[prefix] = resolver;
            return new Registration(() =>
            {
                if (Resolvers.TryGetValue(prefix, out var current) && current == resolver) Resolvers.Remove(prefix);
            });
        }

        /// <summary>今その目印が指す RectTransform。見つからなければ null（まだ出ていない・隠れている）</summary>
        public static RectTransform Find(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId)) return null;

            int separator = anchorId.IndexOf(PrefixSeparator);
            if (separator > 0)
            {
                string prefix = anchorId.Substring(0, separator);
                return Resolvers.TryGetValue(prefix, out var resolver) ? resolver.Invoke(anchorId.Substring(separator + 1)) : null;
            }

            return Anchors.TryGetValue(anchorId, out var anchor) && anchor != null && anchor.isActiveAndEnabled
                ? (RectTransform)anchor.transform
                : null;
        }

        private sealed class Registration : IDisposable
        {
            private Action _onDispose;

            public Registration(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose?.Invoke();
                _onDispose = null;
            }
        }
    }
}
