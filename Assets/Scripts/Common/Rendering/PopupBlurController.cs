using UnityEngine;

namespace Live2DCardRogue.Common.Rendering
{
    // ポップアップ背後のブラーを出すかどうかの窓口。
    // ポップアップを開いたUIが Push、閉じたら Pop を呼ぶ。入れ子で開いても数を数えるので、
    // 一番外側が閉じるまでブラーは消えない。
    //
    // 強さ(Strength)は、開くとき・閉じるときにぼかしを徐々にかけたり抜いたりするためのもの。
    // ポップアップが開閉の動きに合わせて書き換える。最後の1つが閉じたら 1 に戻すので、
    // 次に開くポップアップが強さ 0 のまま始まることはない。
    //
    // RendererFeatureはシーンのオブジェクトを参照できないため、staticにしている
    // (MessageToastやSceneTransitionDataStoreと同じ理由)
    public static class PopupBlurController
    {
        private static int _openPopupCount;
        private static float _strength = 1f;

        /// <summary>矩形の中だけ別の強さにしているか（重ねて開閉する間だけ）</summary>
        public static bool HasRegion { get; private set; }

        /// <summary>別の強さにする矩形。書き込み先の UV で (最小x, 最小y, 最大x, 最大y)</summary>
        public static Vector4 Region { get; private set; }

        /// <summary>矩形の中の強さ(0..1)</summary>
        public static float RegionStrength { get; private set; }

        /// <summary>矩形の中だけ、ぼかしの強さを変える（すぐ後ろのウインドウだけを徐々にぼかす・戻す）</summary>
        public static void SetRegion(Vector4 uvRect, float strength)
        {
            HasRegion = true;
            Region = uvRect;
            RegionStrength = strength < 0f ? 0f : (strength > 1f ? 1f : strength);
        }

        public static void ClearRegion()
        {
            HasRegion = false;
        }

        /// <summary>開いているポップアップの数。重ねて開くときに、ぼかしの強さをかけ直さない判断に使う</summary>
        public static int OpenCount => _openPopupCount;

        /// <summary>ブラーをかけるか。RendererFeatureがこれを見てパスを積むか決める</summary>
        public static bool IsActive => _openPopupCount > 0 && _strength > 0f;

        /// <summary>ぼかしの強さ(0..1)。0 で元の絵のまま、1 で完全にぼかす</summary>
        public static float Strength
        {
            get => _strength;
            set => _strength = value < 0f ? 0f : (value > 1f ? 1f : value);
        }

        public static void Push()
        {
            _openPopupCount++;
        }

        public static void Pop()
        {
            if (_openPopupCount > 0) _openPopupCount--;
            if (_openPopupCount == 0) _strength = 1f;
        }

        /// <summary>シーンを抜けるときなど、数え漏れをまとめて戻す</summary>
        public static void Clear()
        {
            HasRegion = false;
            _openPopupCount = 0;
            _strength = 1f;
        }
    }
}
