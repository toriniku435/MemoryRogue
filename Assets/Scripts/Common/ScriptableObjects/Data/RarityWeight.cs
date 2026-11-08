using System;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // レアリティ1つ分の当たりやすさ。重みは相対値で、合計がいくつでも構わない
    // (重み0、またはそのレアリティのカードが封入されていない場合は出ない)
    [Serializable]
    public sealed class RarityWeight
    {
        [SerializeField] private CardRarity _rarity;
        [SerializeField][Min(0)] private int _weight;

        public CardRarity Rarity => _rarity;
        public int Weight => _weight;
    }
}
