#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Live2DCardRogue.Common.DebugMenu
{
    // タップした位置を、隠しコマンド（DebugCommandSequence）に渡す1文字にする。
    // 左上を '0'、右上を '7' にしてあるので、キーボードの 0707 と同じく「左上 → 右上 → 左上 → 右上」で開く。
    // 隅でない所のタップも 'x' として渡す（並びの途中でほかを触ったら、そこで途切れるように）
    public static class DebugScreenCorner
    {
        public const char TopLeft = '0';
        public const char TopRight = '7';
        public const char BottomLeft = 'L';
        public const char BottomRight = 'R';
        public const char None = 'x';

        /// <summary>
        /// 位置（左下が原点のスクリーン座標。Input System の Pointer.position と同じ）がどの隅か。
        /// 隅の大きさは画面の短い辺に対する割合で、縦横どちらに長い画面でも同じ大きさの正方形にする
        /// （端ぎりぎりはノッチや角の丸みで触れない端末があるので、少し内側まで含めて大きめにとる）
        /// </summary>
        public static char Of(float x, float y, float screenWidth, float screenHeight, float sizeRatio)
        {
            float size = (screenWidth < screenHeight ? screenWidth : screenHeight) * sizeRatio;
            bool left = x <= size;
            bool right = x >= screenWidth - size;
            bool bottom = y <= size;
            bool top = y >= screenHeight - size;

            if (top && left) return TopLeft;
            if (top && right) return TopRight;
            if (bottom && left) return BottomLeft;
            if (bottom && right) return BottomRight;
            return None;
        }
    }
}
#endif
