using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // レアリティごとのカード枠の組。CardView がこれを参照して枠を切り替える。
    // 見た目を丸ごと差し替えたいときは、別の CardFrameSet を作って CardView の参照先を替えればよい
    [CreateAssetMenu(fileName = "NewCardFrameSet", menuName = "Cards/CardFrameSet")]
    public sealed class CardFrameSet : ScriptableObject
    {
        [SerializeField] private Sprite _normal;
        [SerializeField] private Sprite _rare;
        [SerializeField] private Sprite _epic;

        /// <summary>そのレアリティの枠。未設定なら Normal の枠で代用する（枠が消えるよりは目立たない）</summary>
        public Sprite GetFrame(CardRarity rarity)
        {
            var frame = rarity switch
            {
                CardRarity.Rare => _rare,
                CardRarity.Epic => _epic,
                _ => _normal,
            };

            return frame != null ? frame : _normal;
        }
    }
}
