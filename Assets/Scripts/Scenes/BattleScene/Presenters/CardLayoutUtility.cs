using UnityEngine;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // カードの並び計算で共有する座標の小道具
    internal static class CardLayoutUtility
    {
        // target の画面上の位置を、space の子（アンカー・ピボットとも中央）の anchoredPosition に直す。
        // 山札や捨て札のように別の親の下にある物を、カードの飛び先・飛び元にするときに使う
        public static Vector2 ToChildAnchoredPosition(RectTransform space, Transform target)
        {
            Vector2 local = space.InverseTransformPoint(target.position);
            return local - space.rect.center;
        }
    }
}
