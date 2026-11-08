using Cysharp.Threading.Tasks;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using System;
using System.Threading;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.Directors
{
    public sealed class BattlePhaseDirector : IDisposable
    {
        private readonly UnitStatusModel _playerModel;

        // 手前から順に並んだ敵。この並びがそのまま行動順になる
        private readonly EnemyPartyModel _enemyParty;

        private readonly BattlePhaseModel _phaseModel;
        private readonly CardCommandModel _cardCommandModel;
        private readonly BattleDrawUseCase _drawUseCase;
        private readonly BattleCommandExecuteUseCase _executeUseCase;
        private readonly BattleTurnEndUseCase _turnEndUseCase;

        // 待ち時間はすべて標準の速さでのもの。実際には戦闘速度の設定（_getBattleSpeed）で割って使う。
        // 行動と行動の間に間を置いて、ログと画面で何が起きたかを追えるようにしてある

        // 敵の1行動ごとの待機時間（演出の尺）
        private const int EnemyActionDelayMs = 1100;

        // 実行リストのカード1枚ごとの待機時間。自分の操作の結果なので敵より少し短くしている
        private const int PlayerCommandDelayMs = 900;

        // 実行し切ってから手札を片付けるまでの待機時間。最後の1枚の結果が見えないまま手札が消えるのを防ぐ
        private const int PlayerTurnEndDelayMs = 700;

        // 敵から次の敵へ移るときの間
        private const int EnemySwitchDelayMs = 400;

        // プレイヤーの番から敵の番へ移るときの間
        private const int TurnSwitchDelayMs = 500;

        // 敵の最後の行動から、その敵のターン終了時の処理（ウイルスなどの継続効果）までの間。
        // 行動の結果と継続効果が同じ瞬間に起きて、どちらが何をしたのか分からなくならないように
        private const int EnemyTurnEndDelayMs = 700;

        // 決着したあと、結果を見せてから呼び出し元が次の画面へ進むまでの待機時間
        private const int BattleEndDelayMs = 1500;

        /// <summary>戦闘の決着。引数は勝利したか。次に何をするか（遷移など）は購読側が決める</summary>
        public event Action<bool> OnBattleFinished;

        // 以下はバトルログ用の通知。進行は何も変えない

        /// <summary>プレイヤーの番が始まった（引数は何ターン目か。1始まり）</summary>
        public event Action<int> OnPlayerTurnStarted;

        /// <summary>敵の番が始まった</summary>
        public event Action OnEnemyTurnStarted;

        /// <summary>そのユニットのターン終了時の処理（継続ダメージ・継続回復など）が始まる / 終わった</summary>
        public event Action<UnitStatusModel> OnUnitTurnEnding;
        public event Action<UnitStatusModel> OnUnitTurnEnded;

        /// <summary>停止していて、この敵は行動しない</summary>
        public event Action<UnitStatusModel> OnEnemyStunned;

        /// <summary>デッキのコマンドがすべてパージされた（この戦闘は敗北になる）</summary>
        public event Action OnDeckExhausted;

        // デッキのコマンドがすべてパージされて負けが決まった
        private bool _isDeckExhausted;

        private readonly Func<float> _getBattleSpeed;
        private int _turnNumber;

        private UniTaskCompletionSource _executeRequestedSource;

        // グリッチ（GlitchChance）で敵の行動に割り込ませるカード。null なら割り込まない
        private readonly CardData _glitchCard;
        private readonly System.Random _random;

        public BattlePhaseDirector(
            UnitStatusModel playerModel,
            EnemyPartyModel enemyParty,
            BattlePhaseModel phaseModel,
            CardCommandModel cardCommandModel,
            BattleDrawUseCase drawUseCase,
            BattleCommandExecuteUseCase executeUseCase,
            BattleTurnEndUseCase turnEndUseCase,
            Func<float> getBattleSpeed = null,
            CardData glitchCard = null,
            System.Random random = null)
        {
            _getBattleSpeed = getBattleSpeed;
            _glitchCard = glitchCard;
            _random = random ?? new System.Random();
            _playerModel = playerModel;
            _enemyParty = enemyParty;
            _phaseModel = phaseModel;
            _cardCommandModel = cardCommandModel;
            _drawUseCase = drawUseCase;
            _executeUseCase = executeUseCase;
            _turnEndUseCase = turnEndUseCase;

            _cardCommandModel.OnExecuteRequested += HandleExecuteRequested;
        }

        public void Dispose()
        {
            _cardCommandModel.OnExecuteRequested -= HandleExecuteRequested;

            _executeRequestedSource?.TrySetCanceled();
            _executeRequestedSource = null;
        }

        public async UniTaskVoid StartBattleLoop(CancellationToken ct)
        {
            Debug.Log("--- 戦闘開始 ---");

            await RunTurnsAbortableAsync(ct);

            // 戦闘終了
            bool isVictory = _playerModel.CurrentHp > 0 && !_isDeckExhausted && !_isSurrendered;
            if (isVictory)
            {
                Debug.Log("勝利！");
                MessageUseCase.ShowMessage("勝利！");
            }
            else if (_isSurrendered)
            {
                Debug.Log("敗北...（あきらめた）");
                MessageUseCase.ShowMessage("敗北... 接続を切断した");
            }
            else if (_isDeckExhausted)
            {
                Debug.Log("敗北...（デッキのコマンドがすべてパージされた）");
                MessageUseCase.ShowMessage("敗北... コマンドがすべてパージされた");
            }
            else
            {
                Debug.Log("敗北...");
                MessageUseCase.ShowMessage("敗北...");
            }

            // 勝敗の表示を見せてから次へ進む
            await DelayAsync(BattleEndDelayMs, ct);

            OnBattleFinished?.Invoke(isVictory);
        }

        // どちらかが倒れるまで、プレイヤーと敵のターンを交互に回す
        private async UniTask RunTurnsAsync(CancellationToken ct)
        {
            while (_playerModel.CurrentHp > 0 && !_enemyParty.AreAllDefeated && !_isDeckExhausted)
            {
                // 1. プレイヤーのターン処理
                await ExecutePlayerTurnAsync(ct);
                if (_enemyParty.AreAllDefeated || _isDeckExhausted) break;

                // 2. 敵のターン処理（番が変わったことが分かるよう、少し間を置いてから）
                await DelayAsync(TurnSwitchDelayMs, ct);
                await ExecuteEnemyTurnAsync(ct);
            }
        }

        // あきらめた（メニューの「あきらめる」）。決着は敗北になる
        private bool _isSurrendered;

        // ターンの途中でも打ち切れるようにしたもの（あきらめる・デバッグの強制決着）
        private CancellationTokenSource _abortSource;
        private bool _isDecided;

        /// <summary>勝敗が決まった（以降あきらめることはできない）</summary>
        public bool IsDecided => _isDecided;

        private async UniTask RunTurnsAbortableAsync(CancellationToken ct)
        {
            _abortSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                await RunTurnsAsync(_abortSource.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // あきらめた・デバッグ機能で打ち切った。シーンを抜けたことによる取り消し（ct）はそのまま上へ流す。
                // 手札を触れないよう、プレイヤーの操作を受け付けない段階にしておく
                _phaseModel.SetPhase(BattlePhase.PlayerExecution);
            }
            finally
            {
                _isDecided = true;
                _abortSource.Dispose();
                _abortSource = null;
            }
        }

        /// <summary>
        /// あきらめる: 今すぐ敗北で戦闘を終える。HP は減らさず（ログに「倒れた」と出さない）、ターンの進行を打ち切る。
        /// 以降は通常の決着と同じ流れ（敗北の表示 → OnBattleFinished(false)）になるので、マップ側では普通の敗北として扱われる。
        /// 決着済みなら何もしない
        /// </summary>
        public void Surrender()
        {
            if (_isDecided || _abortSource == null) return;

            _isSurrendered = true;
            Debug.Log("--- あきらめたので戦闘を打ち切り ---");
            _abortSource.Cancel();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// デバッグ機能: 今すぐ勝ち（負け）で戦闘を終える。負ける側のHPを0にしてからターンの進行を打ち切るので、
        /// 以降は通常の決着と同じ流れ（勝敗の表示 → OnBattleFinished）になる。決着済みなら何もしない
        /// </summary>
        public void DebugForceFinish(bool isVictory)
        {
            if (_isDecided || _abortSource == null) return;

            if (isVictory)
            {
                foreach (var enemy in _enemyParty.Enemies)
                {
                    if (!enemy.IsDefeated) enemy.Status.ChangeHp(HpChangeType.TakePiercingDamage, -enemy.Status.CurrentHp);
                }
            }
            else
            {
                // 即死はオーバーチャージに肩代わりされない（プレイヤーは即死無効を持たない）
                _playerModel.ChangeHp(HpChangeType.InstantKill, -_playerModel.CurrentHp);
            }

            Debug.Log($"--- [DEBUG] {(isVictory ? "勝利" : "敗北")}で戦闘を打ち切り ---");
            _abortSource.Cancel();
        }
#endif

        private async UniTask ExecutePlayerTurnAsync(CancellationToken ct)
        {
            Debug.Log("--- プレイヤーターン開始 ---");
            _turnNumber++;
            OnPlayerTurnStarted?.Invoke(_turnNumber);
            _phaseModel.SetPhase(BattlePhase.Draw);
            _playerModel.StartTurn();

            // ドロー演出、エネルギー回復処理などを挟む
            _drawUseCase.Draw();

            // 敵の次の行動を先に決めて予告する（プレイヤーはそれを見て実行リストを組む）
            PlanEnemyActions();

            _phaseModel.SetPhase(BattlePhase.Player);

            // 実行ボタンが押されるまで待機（カードを積む・並べ替えるのはこの間）
            _executeRequestedSource = new UniTaskCompletionSource();
            using (ct.Register(() => _executeRequestedSource.TrySetCanceled(ct)))
            {
                await _executeRequestedSource.Task;
            }

            await ExecutePlayerCommandsAsync(ct);

            // 実行し切ったので手札を捨ててスタミナを戻す
            _turnEndUseCase.EndPlayerTurn();

            // 自身のターンを終えたので、ターン終了時の状態効果処理と期限切れの掃除を行う
            EndUnitTurn(_playerModel);

            Debug.Log("--- プレイヤーターン終了 ---");
        }

        // 積まれたカードを先頭から1枚ずつ実行する。何枚積めるかは CardCommandModel の上限が決めているので、
        // ここは「リストが尽きるまで」でよく、回数の管理は持たない
        private async UniTask ExecutePlayerCommandsAsync(CancellationToken ct)
        {
            _phaseModel.SetPhase(BattlePhase.PlayerExecution);

            // 1枚も積んでいない場合は、この後の締めの待機も含めて待たせない
            bool hadCommands = _cardCommandModel.Count > 0;

            while (_cardCommandModel.Count > 0)
            {
                // カードの発動演出の実行待機
                await DelayAsync(PlayerCommandDelayMs, ct);

                _executeUseCase.ExecuteNext();

                // 全部倒したあとも撃ち続けないように（1体だけ倒れた場合は残りの敵へ対象が移る。BattleCommandExecuteUseCase側）
                if (_enemyParty.AreAllDefeated) break;

                // デッキのコマンドがすべてパージされたら負け（最後の1枚で全滅させていれば、上で勝ちとして抜けている）
                if (CheckDeckExhausted()) break;
            }

            // 最後の1枚の結果を見せてから、呼び出し元が手札を片付ける
            if (hadCommands)
            {
                await DelayAsync(PlayerTurnEndDelayMs, ct);
            }
        }

        private async UniTask ExecuteEnemyTurnAsync(CancellationToken ct)
        {
            Debug.Log("--- エネミーターン開始 ---");
            _phaseModel.SetPhase(BattlePhase.Enemy);
            OnEnemyTurnStarted?.Invoke();

            // 手前の敵から順に、1体ずつ行動を終えてから次の敵に移る
            bool isFirst = true;
            foreach (var enemy in _enemyParty.Enemies)
            {
                if (enemy.IsDefeated) continue;

                // 誰が動いているのか追えるよう、次の敵に移る前に少し間を置く
                if (!isFirst) await DelayAsync(EnemySwitchDelayMs, ct);
                isFirst = false;

                enemy.Status.StartTurn();

                // このターンに使うカードと、その順番は EnemyData 側の IEnemyCardPicker が決める（敵ごとに差し替え可能）。
                // 停止などの状態効果で行動回数が減っていれば、予定の先頭からその回数だけ行う
                var plannedCards = enemy.PlannedCards;
                int actionCount = Mathf.Clamp(enemy.Status.CalculateLimit(StatLimitType.ActionCount, plannedCards.Count), 0, plannedCards.Count);
                if (actionCount == 0 && plannedCards.Count > 0)
                {
                    Debug.Log("--- 敵は停止中のため行動しない ---");
                    OnEnemyStunned?.Invoke(enemy.Status);
                }

                for (int i = 0; i < actionCount; i++)
                {
                    var card = plannedCards[i];

                    // グリッチが付いていれば、確率で自滅の行動が予定の前に割り込む
                    await TryGlitchAsync(enemy.Status, ct);
                    if (enemy.IsDefeated) break;

                    // 敵の行動・アニメーション演出の実行待機
                    await DelayAsync(EnemyActionDelayMs, ct);

                    // 味方を回復するカードは一番弱っている味方へ、それ以外はプレイヤーへ
                    var target = card.ChoosesAlly ? (_enemyParty.FindWeakestLiving() ?? enemy.Status) : _playerModel;
                    _executeUseCase.ExecuteEnemyCard(card, enemy.Status, target);

                    // 倒したあとも殴り続けないように
                    if (_playerModel.CurrentHp == 0) break;
                }

                // 行動と継続効果が重ならないよう、少し間を置いてからターン終了時の処理へ。
                // 状態効果が何も付いていなければ起きることが無いので待たない
                if (_playerModel.CurrentHp > 0 && enemy.Status.StatusEffects.Count > 0)
                {
                    await DelayAsync(EnemyTurnEndDelayMs, ct);
                }

                // 自身のターンを終えたので、ターン終了時の状態効果処理と期限切れの掃除を行う
                EndUnitTurn(enemy.Status);
                enemy.SetPlan(null);
                // ボスの生成は「独りのまま何ターン終えたか」で決まる
                enemy.CountTurnEnd(_enemyParty.CountLivingAllies(enemy) > 0);

                if (_playerModel.CurrentHp == 0) break;
            }

            Debug.Log("--- エネミーターン終了 ---");
        }

        // 生きている敵それぞれの次の行動を決める。倒れた敵の予定は消す
        private void PlanEnemyActions()
        {
            foreach (var enemy in _enemyParty.Enemies)
            {
                if (enemy.IsDefeated)
                {
                    enemy.SetPlan(null);
                    continue;
                }
                var context = new EnemyPickContext(enemy.Status.CurrentHp, enemy.Status.MaxHp,
                    _enemyParty.CountLivingAllies(enemy), _turnNumber, enemy.TurnsAlone);
                enemy.SetPlan(enemy.Data.PickCards(context));
            }
        }

        private async UniTask TryGlitchAsync(UnitStatusModel enemy, CancellationToken ct)
        {
            if (_glitchCard == null) return;

            int chance = enemy.CalculateLimit(StatLimitType.GlitchChance, 0);
            if (chance <= 0 || _random.Next(100) >= chance) return;

            await DelayAsync(EnemyActionDelayMs, ct);
            Debug.Log("--- グリッチが割り込んだ ---");
            _executeUseCase.ExecuteEnemyCard(_glitchCard, enemy, enemy);
        }

        private bool CheckDeckExhausted()
        {
            if (_isDeckExhausted || !_executeUseCase.IsDeckExhausted) return _isDeckExhausted;

            _isDeckExhausted = true;
            Debug.Log("--- デッキのコマンドがすべてパージされた ---");
            OnDeckExhausted?.Invoke();
            return true;
        }

        // ターン終了時の処理を、ログが継続効果だと分かるように前後の通知で挟む
        private void EndUnitTurn(UnitStatusModel unit)
        {
            OnUnitTurnEnding?.Invoke(unit);
            unit.EndTurn();
            OnUnitTurnEnded?.Invoke(unit);
        }

        // 標準の速さでの待ち時間を、戦闘速度の設定で割って待つ
        private UniTask DelayAsync(int baseMs, CancellationToken ct)
        {
            float speed = _getBattleSpeed?.Invoke() ?? 1f;
            if (speed <= 0f) speed = 1f;
            return UniTask.Delay(Mathf.RoundToInt(baseMs / speed), cancellationToken: ct);
        }

        private void HandleExecuteRequested()
        {
            _executeRequestedSource?.TrySetResult();
        }
    }
}
