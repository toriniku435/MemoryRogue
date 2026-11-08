using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 子にある MoveBackground の速度へまとめて掛ける倍率。
    // 各レイヤーの速度の比（奥ほど遅い）はそのままに、全体の流れる速さだけを変える。
    // 毎フレーム読まれるので、Play 中に Inspector で動かしてもすぐ効く
    public sealed class MoveBackgroundGroup : MonoBehaviour
    {
        [SerializeField][Min(0f)] private float _speedMultiplier = 1f;

        public float SpeedMultiplier => _speedMultiplier;
    }
}
