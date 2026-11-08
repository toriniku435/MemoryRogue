using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // ユニットの変化を見張って、バトルログに書き込む。
    //
    // カードの効果クラスはログのことを知らない。代わりにここが各ユニットの HP と状態効果の変化を見て、
    // 「何が起きたか」を差分から書き起こす（効果が増えても、ここを直さずに記録される）。
    //   - HP が減った / 増えた → ダメージ / 回復（ターン終了時の処理中なら継続ダメージ / 継続回復。
    //     継続のほうには、それを起こした状態効果（ウイルス・リジェネなど）を添える）
    //   - 状態効果が増えた / 値が変わった / 消えた → 付与 / 増減 / 消滅
    //     ブロックが攻撃を吸収したときは値が減るだけで通知が来ないので、HP の変化のたびにも差分を取る
    //   - HP が 0 になった → 倒れた
    // ターンの区切り・カードの使用・停止は、進行側（BattlePhaseDirector / BattleCommandExecuteUseCase）の通知を
    // Initializer がここの Record* につなぐ。
    //
    // どの記録にも「誰の行動の結果か（Actor）」を付ける。カードを使ったらその使用者、
    // ターン終了時の処理ならそのターンの主。表示側はこれで敵味方を色分けする
    public sealed class BattleLogRecorder : IDisposable
    {
        private readonly BattleLogModel _log;
        private readonly IReadOnlyList<UnitStatusModel> _units;

        private readonly Dictionary<UnitStatusModel, int> _lastHp = new Dictionary<UnitStatusModel, int>();
        private readonly Dictionary<UnitStatusModel, Dictionary<string, (IStatusEffect Effect, int Value)>> _lastEffects =
            new Dictionary<UnitStatusModel, Dictionary<string, (IStatusEffect Effect, int Value)>>();
        private readonly List<(UnitStatusModel Unit, Action<int, int> OnHp, Action OnEffects, Action OnRevived)> _subscriptions =
            new List<(UnitStatusModel Unit, Action<int, int> OnHp, Action OnEffects, Action OnRevived)>();

        // 今起きている変化が、誰の行動によるものか
        private UnitStatusModel _actor;

        // ターン終了時の処理中のユニット（この間の HP の変化は継続効果によるもの）
        private UnitStatusModel _turnEndingUnit;

        public BattleLogRecorder(BattleLogModel log, IReadOnlyList<UnitStatusModel> units)
        {
            _log = log;
            _units = units;
        }

        public void Initialize()
        {
            foreach (var unit in _units)
            {
                _lastHp[unit] = unit.CurrentHp;
                _lastEffects[unit] = Snapshot(unit);

                var captured = unit;
                Action<int, int> onHp = (current, max) => HandleHpChanged(captured, current);
                Action onEffects = () => DiffEffects(captured);
                unit.OnHpChanged += onHp;
                unit.OnStatusEffectsChanged += onEffects;
                unit.OnStatusEffectValuesChanged += onEffects;
                // 生成で戦場に戻ったときは回復でも状態効果の消滅でもないので、記録だけ今の状態に合わせる（使ったカードの行が残る）
                Action onRevived = () =>
                {
                    _lastHp[captured] = captured.CurrentHp;
                    _lastEffects[captured] = Snapshot(captured);
                };
                unit.OnRevived += onRevived;
                _subscriptions.Add((unit, onHp, onEffects, onRevived));
            }
        }

        public void Dispose()
        {
            foreach (var (unit, onHp, onEffects, onRevived) in _subscriptions)
            {
                unit.OnRevived -= onRevived;
                unit.OnHpChanged -= onHp;
                unit.OnStatusEffectsChanged -= onEffects;
                unit.OnStatusEffectValuesChanged -= onEffects;
            }
            _subscriptions.Clear();
        }

        public void RecordPlayerTurn(int turnNumber)
        {
            _actor = null;
            _log.Add(new BattleLogEntry(BattleLogKind.PlayerTurn, amount: turnNumber));
        }

        public void RecordEnemyTurn()
        {
            _actor = null;
            _log.Add(new BattleLogEntry(BattleLogKind.EnemyTurn));
        }

        public void RecordCardUsed(UnitStatusModel user, CardData card, UnitStatusModel target)
        {
            _actor = user;
            _log.Add(new BattleLogEntry(BattleLogKind.CardUsed, user, user, target == user ? null : target, card));
        }

        public void RecordStunned(UnitStatusModel unit)
        {
            _log.Add(new BattleLogEntry(BattleLogKind.Stunned, unit, unit));
        }

        /// <summary>プレイヤーのコマンドが捨て札以外へ行った（パージ・次の手札）</summary>
        public void RecordCardDisposed(CardData card, CommandDestination destination)
        {
            var kind = destination == CommandDestination.Purge ? BattleLogKind.CardPurged : BattleLogKind.CardReturned;
            _log.Add(new BattleLogEntry(kind, _actor, _actor, card: card));
        }

        public void RecordDeckExhausted()
        {
            _log.Add(new BattleLogEntry(BattleLogKind.DeckExhausted));
        }

        public void BeginTurnEnd(UnitStatusModel unit)
        {
            _turnEndingUnit = unit;
            _actor = unit;
        }

        public void EndTurnEnd(UnitStatusModel unit)
        {
            if (_turnEndingUnit == unit) _turnEndingUnit = null;
        }

        private void HandleHpChanged(UnitStatusModel unit, int current)
        {
            // 先に状態効果の差分（ブロックの吸収など）を書き、そのあとで HP の増減を書く
            DiffEffects(unit);

            int delta = current - _lastHp[unit];
            _lastHp[unit] = current;
            if (delta == 0) return;

            bool overTime = _turnEndingUnit == unit;
            // 「継続効果で」ではなく「ウイルスで」と書けるよう、HP を動かした状態効果を持たせる（分からなければ null）
            var source = overTime ? unit.LastHpChangeSource : null;
            if (delta < 0)
            {
                Add(overTime ? BattleLogKind.DamageOverTime : BattleLogKind.Damage, unit, source, -delta);
                if (current == 0) Add(BattleLogKind.Defeated, unit);
            }
            else
            {
                Add(overTime ? BattleLogKind.HealOverTime : BattleLogKind.Heal, unit, source, delta);
            }
        }

        private void DiffEffects(UnitStatusModel unit)
        {
            var before = _lastEffects[unit];
            var after = Snapshot(unit);

            foreach (var pair in after)
            {
                var (effect, value) = pair.Value;
                if (!before.TryGetValue(pair.Key, out var old))
                {
                    if (value > 0)
                    {
                        Add(BattleLogKind.StatusGained, unit, effect, value);
                        AddDetail(unit, effect, value);
                    }
                    continue;
                }

                // 時間制の効果は、残りターンが減っていくだけなので書かない（付いたときと消えたときだけ書く）
                if (value != old.Value && !(effect is TimedStatusEffectBase))
                {
                    Add(BattleLogKind.StatusChanged, unit, effect, value - old.Value);
                    // 使い切ったブロックのように 0 になったものは、説明を添えても意味が無い
                    if (value > 0) AddDetail(unit, effect, value);
                }
            }

            foreach (var pair in before)
            {
                // 値が 0 まで減ってから消えた（使い切ったブロックなど）ものは、減った時点で書いてあるので繰り返さない
                if (!after.ContainsKey(pair.Key) && pair.Value.Value > 0)
                {
                    Add(BattleLogKind.StatusRemoved, unit, pair.Value.Effect);
                }
            }

            _lastEffects[unit] = after;
        }

        private void Add(BattleLogKind kind, UnitStatusModel subject, IStatusEffect effect = null, int amount = 0)
        {
            _log.Add(new BattleLogEntry(kind, _actor, subject, effect: effect, amount: amount));
        }

        // その時点の値で、効果が何をするかを添える（「2ターンの間、回復がダメージになる」など）
        private void AddDetail(UnitStatusModel subject, IStatusEffect effect, int value)
        {
            int effectAmount = effect is TimedStatusEffectBase timed ? timed.Amount : 0;
            _log.Add(new BattleLogEntry(BattleLogKind.StatusDetail, _actor, subject, effect: effect, amount: value, effectAmount: effectAmount));
        }

        private static Dictionary<string, (IStatusEffect Effect, int Value)> Snapshot(UnitStatusModel unit)
        {
            var map = new Dictionary<string, (IStatusEffect Effect, int Value)>();
            foreach (var effect in unit.StatusEffects)
            {
                map[effect.Id] = (effect, effect.Value);
            }
            return map;
        }
    }
}
