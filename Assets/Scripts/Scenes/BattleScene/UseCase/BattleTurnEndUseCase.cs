using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // プレイヤーのターンの片付け: 手札と、実行されずに残ったコマンドを捨て札へ送り、スタミナを戻す。
    // 状態効果のターンの処理（UnitStatusModel.EndTurn）は別物で、Director がこの後に呼ぶ
    public sealed class BattleTurnEndUseCase
    {
        private readonly UnitStatusModel _player;
        private readonly BattleCardPilesModel _piles;
        private readonly CardCommandModel _commandModel;
        private readonly CardSelectionModel _selectionModel;
        private readonly CommandHistoryModel _history;

        public BattleTurnEndUseCase(UnitStatusModel player, BattleCardPilesModel piles, CardCommandModel commandModel,
            CardSelectionModel selectionModel, CommandHistoryModel history)
        {
            _player = player;
            _piles = piles;
            _commandModel = commandModel;
            _selectionModel = selectionModel;
            _history = history;
        }

        public void EndPlayerTurn()
        {
            _selectionModel.Clear();
            _history.ForgetTurn();

            // 通常は実行し切ってから呼ばれるので空
            while (_commandModel.Count > 0)
            {
                var leftover = _commandModel.RemoveAt(0);
                _piles.Discard.AddCard(leftover.Card);
                foreach (var card in leftover.PurgeCards) _piles.Discard.AddCard(card);
            }

            _piles.Hand.MoveAllTo(_piles.Discard);
            _player.RestoreStamina();
        }
    }
}
