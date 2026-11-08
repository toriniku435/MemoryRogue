using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Models
{
    // このターンにプレイヤーが実行したコマンドの記憶。どちらもターンの終わりで忘れる
    // （チェーンはターンをまたいで繋がらず、リドゥが繰り返すのも同じターンのコマンドだけ）
    public sealed class CommandHistoryModel
    {
        private readonly List<CardData> _chainCards = new List<CardData>();

        /// <summary>このターンに実行した、連なり（エクスプロイトチェーン）の段。成立したらまとめてパージする</summary>
        public IReadOnlyList<CardData> ChainCards => _chainCards;

        /// <summary>リドゥ以外で最後に実行したコマンド（リドゥが同じ効果を出す）。まだ無ければ null</summary>
        public CardData PreviousCard { get; private set; }
        public UnitStatusModel PreviousTarget { get; private set; }

        /// <summary>そのコマンドが実際にパージしたバッファの数（リドゥはパージを繰り返さず、この数で効かせる）</summary>
        public int PreviousPurgedCount { get; private set; }

        public void AddChainStep(CardData card) => _chainCards.Add(card);

        public void ClearChain() => _chainCards.Clear();

        public void RememberPrevious(CardData card, UnitStatusModel target, int purgedCount)
        {
            PreviousCard = card;
            PreviousTarget = target;
            PreviousPurgedCount = purgedCount;
        }

        public void ForgetTurn()
        {
            _chainCards.Clear();
            RememberPrevious(null, null, 0);
        }
    }
}
