using System;
using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    // テスト用に、戦闘のコマンドまわりのユースケースを BattleSceneInitializer と同じ組み合わせで作る。
    // 引く・積む・実行する・ターンを終えるを1つのテストで続けて行うので、モデルを共有した4つを揃えて渡す
    public sealed class BattleCommandRig
    {
        public BattleCardPilesModel Piles { get; }
        public CardCommandModel Commands { get; }
        public BattleTokenModel Tokens { get; }
        public BattleDrawUseCase Draw { get; }
        public BattleCommandQueueUseCase Queue { get; }
        public BattleCommandExecuteUseCase Execute { get; }
        public BattleTurnEndUseCase TurnEnd { get; }

        public BattleCommandRig(
            UnitStatusModel player,
            CardPileModel deck,
            CardPileModel hand,
            CardPileModel discard,
            CardCommandModel commands,
            CardSelectionModel selection,
            Func<UnitStatusModel, UnitStatusModel> findAlternativeTarget,
            Func<IReadOnlyList<UnitStatusModel>> getLivingOpponents = null,
            CardPileModel purge = null,
            int tokens = 0,
            Func<UnitStatusModel> getPriorityOpponent = null)
        {
            Piles = new BattleCardPilesModel(deck, hand, discard, purge);
            Commands = commands;
            Tokens = new BattleTokenModel(tokens);
            var history = new CommandHistoryModel();
            var targets = new BattleTargetResolver(player, findAlternativeTarget, getLivingOpponents, getPriorityOpponent);

            Draw = new BattleDrawUseCase(player, Piles);
            Queue = new BattleCommandQueueUseCase(player, Piles, commands, selection, targets, Tokens);
            Execute = new BattleCommandExecuteUseCase(player, Piles, commands, targets, Tokens, history);
            TurnEnd = new BattleTurnEndUseCase(player, Piles, commands, selection, history);
        }
    }
}
