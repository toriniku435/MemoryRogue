using System;
using UnityEngine;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 生成するウインドウのプレハブの一覧（Assets/Resources/WindowCatalog.asset）。
    // シーンにウインドウを置かない代わりに、WindowUseCase がここから WindowId で引いて生成する
    [CreateAssetMenu(fileName = "WindowCatalog", menuName = "Live2DCardRogue/WindowCatalog")]
    public sealed class WindowCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public WindowId Id;
            public GameObject Prefab;
        }

        [SerializeField] private Entry[] _entries = new Entry[0];

        public GameObject Find(WindowId id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id) return entry.Prefab;
            }
            return null;
        }
    }
}
