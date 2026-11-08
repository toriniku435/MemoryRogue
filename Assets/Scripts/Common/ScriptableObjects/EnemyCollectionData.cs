using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // コレクション（エネミー）に並べる敵（並べた順に上から）。チュートリアルの練習用の敵のような、遊ぶ中で出会わない敵は入れない
    [CreateAssetMenu(fileName = "EnemyCollection", menuName = "Collection/EnemyCollectionData")]
    public sealed class EnemyCollectionData : ScriptableObject
    {
        [SerializeField] private EnemyData[] _enemies = new EnemyData[0];

        /// <summary>並べる敵（空欄と重複は除く。並べた順）</summary>
        public IReadOnlyList<EnemyData> Enemies
        {
            get
            {
                var enemies = new List<EnemyData>();
                foreach (var enemy in _enemies ?? new EnemyData[0])
                {
                    if (enemy != null && !enemies.Contains(enemy)) enemies.Add(enemy);
                }
                return enemies;
            }
        }
    }
}
