using System;
using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Directors;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.Initializer
{
    // バトルログを用意して、戦闘の出来事につなぐ。ユニットの変化はレコーダーが自分で見張り、
    // ターンの区切り・カードの使用・停止・パージは、進行（Director と UseCase）の通知をここでつなぐ。
    // レコーダーが Director を知らずに済むよう、つなぐのは組み立て側（ここ）の役目
    public sealed class BattleLogWiring : IDisposable
    {
        private readonly BattlePhaseDirector _director;
        private readonly BattleCommandExecuteUseCase _executeUseCase;
        private readonly BattleLogRecorder _recorder;

        public BattleLogModel Log { get; }

        public BattleLogFormatter Formatter { get; }

        public BattleLogWiring(BattlePhaseDirector director, BattleCommandExecuteUseCase executeUseCase, UnitStatusModel player, Sprite playerIcon,
            IReadOnlyList<BattleEnemy> enemies)
        {
            _director = director;
            _executeUseCase = executeUseCase;

            var units = new List<UnitStatusModel> { player };
            var enemyEntries = new List<(UnitStatusModel Status, EnemyData Data, Sprite Icon)>();
            foreach (var enemy in enemies)
            {
                units.Add(enemy.Status);
                enemyEntries.Add((enemy.Status, enemy.Data, enemy.Data.Portrait));
            }

            Log = new BattleLogModel();
            Formatter = new BattleLogFormatter(player, playerIcon, enemyEntries);
            _recorder = new BattleLogRecorder(Log, units);
            _recorder.Initialize();

            _director.OnPlayerTurnStarted += _recorder.RecordPlayerTurn;
            _director.OnEnemyTurnStarted += _recorder.RecordEnemyTurn;
            _director.OnUnitTurnEnding += _recorder.BeginTurnEnd;
            _director.OnUnitTurnEnded += _recorder.EndTurnEnd;
            _director.OnEnemyStunned += _recorder.RecordStunned;
            _director.OnDeckExhausted += _recorder.RecordDeckExhausted;
            _executeUseCase.OnCardExecuting += _recorder.RecordCardUsed;
            _executeUseCase.OnCardDisposed += _recorder.RecordCardDisposed;
        }

        public void Dispose()
        {
            _director.OnPlayerTurnStarted -= _recorder.RecordPlayerTurn;
            _director.OnEnemyTurnStarted -= _recorder.RecordEnemyTurn;
            _director.OnUnitTurnEnding -= _recorder.BeginTurnEnd;
            _director.OnUnitTurnEnded -= _recorder.EndTurnEnd;
            _director.OnEnemyStunned -= _recorder.RecordStunned;
            _director.OnDeckExhausted -= _recorder.RecordDeckExhausted;
            _executeUseCase.OnCardExecuting -= _recorder.RecordCardUsed;
            _executeUseCase.OnCardDisposed -= _recorder.RecordCardDisposed;
            _recorder.Dispose();
        }
    }
}
