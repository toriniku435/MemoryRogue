using UnityEngine;
using UnityEngine.Serialization;

namespace Live2DCardRogue.Common.Views
{
    // 背景の1枚を横に流し続ける。x が _wrapX 以下になったら _resetX に戻す（2枚を並べて途切れなく流す）。
    // 親に MoveBackgroundGroup があれば、その倍率を速度に掛ける（重なった背景の速度をまとめて調整するため）。
    // もとは外部の背景パックに入っていたスクリプトで、フィールドの旧名は FormerlySerializedAs で引き継いでいる
    public sealed class MoveBackground : MonoBehaviour
    {
        // 1秒あたりに動く量（ワールド座標。負なら左へ流れる）
        [FormerlySerializedAs("speed")]
        [SerializeField] private float _speed;
        // ここまで来たら戻す x
        [FormerlySerializedAs("PontoDeDestino")]
        [SerializeField] private float _wrapX;
        // 戻す先の x
        [FormerlySerializedAs("PontoOriginal")]
        [SerializeField] private float _resetX;

        private MoveBackgroundGroup _group;

        private void Awake()
        {
            _group = GetComponentInParent<MoveBackgroundGroup>(true);
        }

        private void Update()
        {
            float multiplier = _group != null ? _group.SpeedMultiplier : 1f;
            var position = transform.position;
            position.x += _speed * multiplier * Time.deltaTime;
            if (position.x <= _wrapX) position.x = _resetX;
            transform.position = position;
        }
    }
}
