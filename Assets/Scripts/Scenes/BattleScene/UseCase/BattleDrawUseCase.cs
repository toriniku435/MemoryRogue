using System;
using Live2DCardRogue.BattleScene.Constants;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // プレイヤーのターンの初めに手札（バッファ）を引く
    public sealed class BattleDrawUseCase
    {
        private readonly UnitStatusModel _player;
        private readonly BattleCardPilesModel _piles;

        public BattleDrawUseCase(UnitStatusModel player, BattleCardPilesModel piles)
        {
            _player = player;
            _piles = piles;
        }

        public void Draw()
        {
            Debug.Log("--- プレイヤーがカードを引く ---");

            // 引く枚数は、手札の増減の状態効果込み。減らす効果が重なっても最低1枚は引く。
            // ただしバッファロック（DrawCap）はその後に掛かり、0 枚にもできる
            int handSize = Math.Max(1, _player.CalculateLimit(StatLimitType.HandSize, BattleConstants.MaxHandSize));
            handSize = Math.Min(handSize, _player.CalculateLimit(StatLimitType.DrawCap, int.MaxValue));

            // ループバックで戻るコマンドが先に手札へ入り、そのうえで規定の枚数まで引く（戻った分も手札の枚数に数える）
            _piles.Returning.MoveAllTo(_piles.Hand);

            while (_piles.Hand.Count < handSize)
            {
                if (_piles.Deck.IsEmpty)
                {
                    if (_piles.Discard.IsEmpty) break;
                    _piles.Discard.MoveAllTo(_piles.Deck);
                    _piles.Deck.Shuffle();
                }

                var drawnCard = _piles.Deck.GetCardAt(0);
                _piles.Deck.RemoveCard(drawnCard);
                _piles.Hand.AddCard(drawnCard);
            }
        }
    }
}
