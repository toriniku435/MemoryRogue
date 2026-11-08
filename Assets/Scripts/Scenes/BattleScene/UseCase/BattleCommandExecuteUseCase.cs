using System;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using UnityEngine;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // 積んだコマンドを実行する: 預かったバッファのパージ、エクスプロイトチェーン、マルチスレッドの繰り返し、リドゥ、実行後の行き先。
    // 敵のカードの実行もここ（敵はカードもコストも消費しない）。
    // 1枚ごとの間合いは Director が持ち、ここは「1枚分の結果」だけを出す
    public sealed class BattleCommandExecuteUseCase
    {
        private readonly UnitStatusModel _player;
        private readonly BattleCardPilesModel _piles;
        private readonly CardCommandModel _commandModel;
        private readonly BattleTargetResolver _targets;
        private readonly BattleTokenModel _tokens;
        private readonly CommandHistoryModel _history;
        // 敵の生成（EnemyPartyModel.Summon）。カードの効果は2体のユニットしか知らないので、生成はここから頼む。無ければ生成は不発
        private readonly Func<EnemyData, bool> _summon;
        // この戦闘で倒した敵の数（敵の並びを知っている EnemyPartyModel が数える）。無ければ 0
        private readonly Func<int> _getDefeatedEnemyCount;

        /// <summary>カードの効果を実行する直前（使用者, カード, 対象）。プレイヤーと敵の両方で飛ぶ。バトルログが購読する</summary>
        public event Action<UnitStatusModel, CardData, UnitStatusModel> OnCardExecuting;

        /// <summary>
        /// プレイヤーのコマンドが捨て札以外（次の手札・パージ）へ行った（カード, 行き先）。
        /// 実行したコマンドのほか、預かっていたバッファのパージや、成立したチェーンの段でも飛ぶ。バトルログが購読する
        /// </summary>
        public event Action<CardData, CommandDestination> OnCardDisposed;

        public BattleCommandExecuteUseCase(UnitStatusModel player, BattleCardPilesModel piles, CardCommandModel commandModel,
            BattleTargetResolver targets, BattleTokenModel tokens, CommandHistoryModel history, Func<EnemyData, bool> summon = null,
            Func<int> getDefeatedEnemyCount = null)
        {
            _player = player;
            _piles = piles;
            _commandModel = commandModel;
            _targets = targets;
            _tokens = tokens;
            _history = history;
            _summon = summon;
            _getDefeatedEnemyCount = getDefeatedEnemyCount;
        }

        /// <summary>デッキのコマンドがすべてパージされたか（どの山にも実行リストにも残っていない）。そうなったら敗北（Director が見る）</summary>
        public bool IsDeckExhausted => _piles.HasOnlyPurged && _commandModel.Count == 0;

        /// <summary>実行リストの先頭を1枚だけ実行する。積んだ時点の対象が倒れていたら、次の敵へ振り直す</summary>
        /// <returns>実行したか（リストが空なら false）</returns>
        public bool ExecuteNext()
        {
            var command = _commandModel.RemoveAt(0);
            if (command == null) return false;

            Debug.Log("--- プレイヤーがカードを使用 ---");

            // 「次に実行するコマンド」に効く効果（ループバック・マルチスレッド）を、このコマンドに向けて準備させる
            _player.BeginCommand();

            var target = _targets.Resolve(command.Target);
            OnCardExecuting?.Invoke(_player, command.Card, target);

            // 積むときに選んで預かっていたバッファを、ここでパージする（マルチスレッドで繰り返しても1回だけ）
            foreach (var card in command.PurgeCards)
            {
                _piles.Purge.AddCard(card);
                OnCardDisposed?.Invoke(card, CommandDestination.Purge);
            }

            // 連なりの段なら、実行する前に「これで成立するか」を聞いておく（実行すると段の記録が消えるので）
            bool isChainStep = false;
            bool completesChain = false;
            foreach (var effect in command.Card.Effects)
            {
                if (!(effect is IChainCardEffect chain)) continue;
                isChainStep = true;
                if (chain.StartsChain) _history.ClearChain();
                if (chain.CompletesChain(_player)) completesChain = true;
            }

            // マルチスレッドなら、同じコマンドを続けて何度も実行する。途中で対象が倒れたら次の敵へ振り直す
            int repeat = Math.Max(1, _player.CalculateLimit(StatLimitType.CommandRepeat, 1));
            for (int i = 0; i < repeat; i++)
            {
                if (i > 0) target = _targets.Resolve(target);
                // リドゥは、自分の代わりに一つ前のコマンドの効果を出す
                if (command.Card.RepeatsPrevious) ExecutePrevious();
                else ExecuteEffects(command.Card, target, command.PurgeCards.Count, skipChain: false);
            }

            // リドゥが繰り返すのは、リドゥ以外で最後に実行したコマンド
            if (!command.Card.RepeatsPrevious)
            {
                _history.RememberPrevious(command.Card, target, command.PurgeCards.Count);
            }

            var destination = _player.CalculateCommandDestination(command.Card.PurgeOnUse ? CommandDestination.Purge : CommandDestination.Discard);
            // 連なりが成立したら、このコマンドも、それまでの段（アーカイブか次のバッファ行きにある）もパージする
            if (completesChain)
            {
                destination = CommandDestination.Purge;
                PurgeChainCards();
            }
            else if (isChainStep)
            {
                _history.AddChainStep(command.Card);
            }
            _piles.PileOf(destination).AddCard(command.Card);
            if (destination != CommandDestination.Discard) OnCardDisposed?.Invoke(command.Card, destination);

            _player.EndCommand();
            return true;
        }

        /// <summary>
        /// 敵がカードを使う。敵は手札・山札もスタミナも持たないので、カードもコストも消費されない。
        /// 1ターンに何回行動するかは EnemyData の行動回数と IEnemyCardPicker が決める
        /// </summary>
        public void ExecuteEnemyCard(CardData cardData, UnitStatusModel user, UnitStatusModel target)
        {
            Debug.Log("--- 敵がカードを使用 ---");
            OnCardExecuting?.Invoke(user, cardData, target);

            foreach (var effect in cardData.Effects)
            {
                if (effect is SummonEnemyCardEffect summon)
                {
                    if (summon.Enemy != null && _summon?.Invoke(summon.Enemy) != true) Debug.Log("--- 生成できる枠が無いため不発 ---");
                    continue;
                }
                effect?.Execute(user, target);
            }
        }

        // これまでの段を、今いる山（アーカイブ・次のバッファ行き）から抜いてパージする
        private void PurgeChainCards()
        {
            foreach (var card in _history.ChainCards)
            {
                CardPileModel from = Holds(_piles.Discard, card) ? _piles.Discard : Holds(_piles.Returning, card) ? _piles.Returning : null;
                if (from == null) continue;
                from.RemoveCard(card);
                _piles.Purge.AddCard(card);
                OnCardDisposed?.Invoke(card, CommandDestination.Purge);
            }
            _history.ClearChain();
        }

        private static bool Holds(CardPileModel pile, CardData card)
        {
            foreach (var c in pile.Cards)
            {
                if (c == card) return true;
            }
            return false;
        }

        // 一つ前のコマンドと同じ効果をもう一度出す（リドゥ）。対象が倒れていれば振り直す。
        // パージは繰り返さず、そのとき実際にパージした数のまま効かせる。連なりの段は繰り返さない（成立の数え方が崩れるため）
        private void ExecutePrevious()
        {
            var previous = _history.PreviousCard;
            if (previous == null)
            {
                Debug.Log("--- リドゥ: このターンに繰り返すコマンドがまだ無い ---");
                return;
            }

            // トークンを払うコマンドは、繰り返すときにもう一度払う。足りなければ不発
            int tokenCost = previous.TokenCost;
            if (!_tokens.CanPay(tokenCost))
            {
                Debug.Log($"--- リドゥ: トークンが足りないため不発（必要{tokenCost} / 所持{_tokens.Tokens}）---");
                return;
            }
            _tokens.Pay(tokenCost);

            ExecuteEffects(previous, _targets.Resolve(_history.PreviousTarget), _history.PreviousPurgedCount, skipChain: true);
        }

        private void ExecuteEffects(CardData card, UnitStatusModel target, int purgedCount, bool skipChain)
        {
            // バッファの数・敵味方すべてを見て効く効果に渡す事情（実行する時点のもの）
            var context = new CardExecutionContext(_piles.Hand.Count, _targets.AllLivingUnits(target),
                _getDefeatedEnemyCount?.Invoke() ?? 0);

            foreach (var effect in card.Effects)
            {
                if (effect == null) continue;
                if (skipChain && effect is IChainCardEffect) continue;

                // 全体攻撃は、選んだ1体ではなく生きている相手全員に1回ずつ当てる（筋力などは1体ごとに効く）。
                // 先の効果で倒れた相手には当てないよう、効果ごとに聞き直す
                var opponents = effect.TargetsAllOpponents ? _targets.LivingOpponents : null;
                if (opponents != null)
                {
                    foreach (var opponent in opponents) effect.Execute(_player, opponent);
                    continue;
                }

                if (effect is IHandPurgeCardEffect handPurge)
                {
                    handPurge.ExecuteWithPurged(_player, target, purgedCount);
                    continue;
                }

                if (effect is ICommandContextCardEffect contextual)
                {
                    contextual.ExecuteWithContext(_player, target, context);
                    continue;
                }

                effect.Execute(_player, target);
            }
        }
    }
}
