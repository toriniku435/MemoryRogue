using UnityEngine;

namespace Live2DCardRogue.Common.Rendering
{
    // 各シーンに1つ置くカメラ一式（Prefab: Assets/Prefabs/UICameraRig.prefab）。
    // シーンに属さないUI（DontDestroyOnLoadのトーストなど）が描画先のカメラを
    // 見つけられるよう、現在のシーンのリグを公開する
    public sealed class UICameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _baseCamera;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private Camera _popupCamera;

        public static UICameraRig Current { get; private set; }

        public Camera BaseCamera => _baseCamera;
        public Camera UiCamera => _uiCamera;
        public Camera PopupCamera => _popupCamera;

        private void Awake()
        {
            Current = this;
        }

        // シーン遷移では新しいリグのAwakeが先に走るので、自分が現役のときだけ消す
        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
