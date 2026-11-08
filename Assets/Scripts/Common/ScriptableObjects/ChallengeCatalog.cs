using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // タイトルの「チャレンジ」で選べるものと、その階層数（Assets/ScriptableObjects/ChallengeCatalog.asset）。
    // 階層数はここで調整する（0 なら終わりが無い = エンドレス）。並べた順が選択ウインドウのボタンの順。
    // 敵の候補・強さの上乗せ・報酬はどのチャレンジも EndlessBalance を共用する
    [CreateAssetMenu(fileName = "ChallengeCatalog", menuName = "Campaign/ChallengeCatalog")]
    public sealed class ChallengeCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private ChallengeType _type;
            /// <summary>階層数。0 なら終わりが無い</summary>
            [SerializeField][Min(0)] private int _layerCount;

            public ChallengeType Type => _type;
            public int LayerCount => _layerCount;

            public Entry(ChallengeType type, int layerCount)
            {
                _type = type;
                _layerCount = layerCount;
            }
        }

        [SerializeField] private Entry[] _challenges =
        {
            new Entry(ChallengeType.Normal, 30),
            new Entry(ChallengeType.Hard, 60),
            new Entry(ChallengeType.Master, 100),
            new Entry(ChallengeType.Endless, 0),
        };

        /// <summary>選べるチャレンジ（並べた順。空欄と同じ種類の2つ目は除く）</summary>
        public IReadOnlyList<Entry> Challenges
        {
            get
            {
                var list = new List<Entry>();
                foreach (var entry in _challenges ?? new Entry[0])
                {
                    if (entry == null || list.Exists(e => e.Type == entry.Type)) continue;
                    list.Add(entry);
                }
                return list;
            }
        }

        /// <summary>そのチャレンジの階層数（0 は終わり無し）。無ければ 0</summary>
        public int LayerCountOf(ChallengeType type)
        {
            foreach (var entry in Challenges)
            {
                if (entry.Type == type) return entry.LayerCount;
            }
            return 0;
        }
    }
}
