using Live2DCardRogue.Common.Models;
using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 開いているウインドウの重なり順を持ち、一番手前のウインドウだけをぼかしの後に描く段へ置く。
    // WindowTransition が開いたときに Bring、閉じ終わったとき（非表示になったとき）に Remove を呼ぶ。
    // 重なりが変わるたびに、奥から順に各ウインドウの PopupLayerMember へ段と順番を配り直す
    // （入れ子のウインドウでも、手前のものが後から上書きするので正しい段になる）。
    //
    // ぼかしは常に1回で、何枚重なっても「一番手前以外はマップと一緒にぼける」。
    // シーンをまたいで残るので、破棄されたウインドウは配り直すたびに掃除する
    public static class PopupStack
    {
        private static readonly PopupStackModel<WindowTransition> _model = new PopupStackModel<WindowTransition>();

        /// <summary>一番手前に置く</summary>
        public static void Bring(WindowTransition window)
        {
            _model.Bring(window);
            Apply();
        }

        /// <summary>重なりから抜く</summary>
        public static void Remove(WindowTransition window)
        {
            if (_model.Remove(window)) Apply();
        }

        /// <summary>そのウインドウのすぐ後ろ（奥）に開いているウインドウ。無ければ null</summary>
        public static WindowTransition Below(WindowTransition window)
        {
            var items = _model.Items;
            for (int i = items.Count - 1; i > 0; i--)
            {
                if (items[i] == window) return items[i - 1] != null ? items[i - 1] : null;
            }
            return null;
        }

        private static void Apply()
        {
            // Unityの破棄済みオブジェクトは == null になる
            _model.RemoveAll(window => window == null);

            var items = _model.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var member = items[i].LayerMember;
                if (member != null) member.Apply(i == items.Count - 1, i);
            }
        }

#if UNITY_EDITOR
        // ドメインリロードを切っている設定でも、Playのたびに空から始める
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _model.Clear();
        }
#endif
    }
}
