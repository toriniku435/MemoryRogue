using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.EnemyCardPickers
{
    /// <summary>
    /// 敵が1ターンに使うカードを、実行する順番に並べて返すロジック
    /// </summary>
    public interface IEnemyCardPicker
    {
        /// <summary>
        /// 1ターン分の行動を組み立てる
        /// </summary>
        /// <param name="cards">その敵が使えるカードすべて</param>
        /// <param name="minActionCount">1ターンの最小行動回数</param>
        /// <param name="maxActionCount">1ターンの最大行動回数</param>
        /// <param name="context">戦況（HP・味方の数・ターン数）。使わない Picker は無視してよい</param>
        /// <returns>実行順に並べたカード。行動しない場合は空(nullを返さないこと)</returns>
        IReadOnlyList<CardData> Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context);
    }
}
