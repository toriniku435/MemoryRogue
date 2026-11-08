using UnityEngine;

namespace Live2DCardRogue.Common.Tutorial
{
    // チュートリアルで強調する UI に付ける目印。手順は ID でこれを探し、その時点の位置に枠と吹き出しを出す。
    // UI を動かしても・作り直しても、同じ ID の目印を付けておけば手順は直さなくてよい
    [DisallowMultipleComponent]
    public sealed class TutorialAnchor : MonoBehaviour
    {
        [SerializeField] private string _anchorId;

        public string AnchorId => _anchorId;

        private void OnEnable()
        {
            TutorialAnchorRegistry.Register(this);
        }

        private void OnDisable()
        {
            TutorialAnchorRegistry.Unregister(this);
        }
    }
}
