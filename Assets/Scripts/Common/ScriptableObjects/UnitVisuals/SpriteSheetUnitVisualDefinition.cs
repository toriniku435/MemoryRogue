using Live2DCardRogue.Common.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // コマ送りのドット絵。画像は ArtSource/assets/characters.py が Assets/Art/Characters/<Name>/ に書き出し、
    // Tools > Art > Rebuild Unit Visuals がそのフォルダからこのアセットのコマを並べ直す
    [CreateAssetMenu(fileName = "NewSpriteSheetUnitVisual", menuName = "Units/Sprite Sheet Unit Visual")]
    public sealed class SpriteSheetUnitVisualDefinition : UnitVisualDefinition
    {
        [SerializeField] private Sprite _icon;

        [Header("アイドル（ループ）")]
        [SerializeField] private Sprite[] _idleFrames;
        [SerializeField][Min(0.1f)] private float _idleFps = 5f;

        [Header("行動（1回再生してアイドルへ）")]
        [SerializeField] private Sprite[] _actionFrames;
        [SerializeField][Min(0.1f)] private float _actionFps = 10f;

        [Header("被弾（1回再生してアイドルへ。倒れたら最後のコマで止まる）")]
        [SerializeField] private Sprite[] _hitFrames;
        [SerializeField][Min(0.1f)] private float _hitFps = 5f;

        public override Sprite Icon => _icon != null ? _icon : First(_idleFrames);

        public override IUnitVisual Create(RectTransform parent)
        {
            var go = new GameObject("UnitVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SpriteSheetUnitVisual));
            // カメラの振り分けは GameObject のレイヤーで決まるので、親に合わせる
            go.layer = parent.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var visual = go.GetComponent<SpriteSheetUnitVisual>();
            visual.Setup(_idleFrames ?? new Sprite[0], _actionFrames ?? new Sprite[0], _hitFrames ?? new Sprite[0], _idleFps, _actionFps, _hitFps);
            return visual;
        }

        private static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;
    }
}
