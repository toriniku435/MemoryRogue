using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Message;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // プレイヤーがターン中に実行リストを組む操作: カードを選ぶ・対象を選ぶ・パージするバッファを選ぶ・積む・戻す・並べ替える、
    // そして実行を求める。選ぶことと積むことは、選べない理由の判定（スタミナ・上限・トークン）と積むときの支払いを共有するので、
    // ここで1つにまとめてある。積んだ分の実行は BattleCommandExecuteUseCase
    public sealed class BattleCommandQueueUseCase
    {
        private readonly UnitStatusModel _player;
        private readonly CardPileModel _hand;
        private readonly CardCommandModel _commandModel;
        private readonly CardSelectionModel _selectionModel;
        private readonly BattleTargetResolver _targets;
        private readonly BattleTokenModel _tokens;

        public BattleCommandQueueUseCase(UnitStatusModel player, BattleCardPilesModel piles, CardCommandModel commandModel,
            CardSelectionModel selectionModel, BattleTargetResolver targets, BattleTokenModel tokens)
        {
            _player = player;
            _hand = piles.Hand;
            _commandModel = commandModel;
            _selectionModel = selectionModel;
            _targets = targets;
            _tokens = tokens;
        }

        /// <summary>
        /// 対象選択に入る（敵が複数いて、カーソルを止めて別の敵を選びたいとき）。
        /// 選べないカードなら理由のメッセージを出して false。
        /// 同じカードが手札に複数あると CardData では押された1枚を区別できないので、どの1枚かは handIndex で受け取る
        /// </summary>
        public bool TrySelectCard(CardData cardData, int handIndex = -1)
        {
            var refusal = GetSelectRefusal(cardData);
            if (refusal != null)
            {
                MessageUseCase.ShowMessage(refusal);
                return false;
            }

            _selectionModel.SetPendingCard(cardData, handIndex);
            return true;
        }

        /// <summary>
        /// 対象を選ばずに積もうとする（手札のクリックの入口）。
        /// 選ぶ余地が無いカード（自分に効く・全体に効く・生きている敵が1体だけ）は、その対象でそのまま積む
        /// （バッファをパージするコマンドは、続けてパージの選択に入る）。
        /// 敵が複数いて1体を選ぶカードなら何もせず NeedsTarget を返すので、呼び出し側が TrySelectCard で対象選択に入る。
        /// preferFrontTarget なら、選ぶ余地があっても優先ターゲット（無ければ一番手前の敵）に向けて積む（クリックで使ったとき）
        /// </summary>
        /// <param name="handIndex">押されたのが手札の何番目か（同じカードが複数あるとき、その1枚を抜く）。分からなければ -1</param>
        public QuickPlayResult TryPlayWithoutChoosing(CardData cardData, bool preferFrontTarget = false, int handIndex = -1)
        {
            var refusal = GetSelectRefusal(cardData);
            if (refusal != null)
            {
                MessageUseCase.ShowMessage(refusal);
                return QuickPlayResult.Refused;
            }

            var target = _targets.FindAutoTarget(cardData) ?? (preferFrontTarget ? _targets.FindFrontOpponent() : null);
            if (target == null) return QuickPlayResult.NeedsTarget;

            _selectionModel.Clear();
            if (cardData.HandPurgeCount > 0)
            {
                _selectionModel.BeginPurgeChoice(cardData, target, cardData.HandPurgeCount, handIndex);
                return QuickPlayResult.Played;
            }

            return AddCommand(cardData, target, null, handIndex, null) ? QuickPlayResult.Played : QuickPlayResult.Refused;
        }

        /// <summary>選ぶ余地が無いときの対象（選ぶ必要があれば null）</summary>
        public UnitStatusModel FindAutoTarget(CardData cardData) => _targets.FindAutoTarget(cardData);

        /// <summary>
        /// そのカードを今選べるか。TrySelectCard と同じ判定だが、断る理由のメッセージは出さない。
        /// カーソルを合わせただけで選ぶ操作では、合わせるたびにメッセージが出ると邪魔になるため
        /// </summary>
        public bool CanSelectCard(CardData cardData)
        {
            return GetSelectRefusal(cardData) == null;
        }

        // 選べない理由（選べるなら null）。判定の入口を1つにして、黙って聞く場合と理由を出す場合で食い違わないようにする
        private string GetSelectRefusal(CardData cardData)
        {
            if (_commandModel.IsFull) return "これ以上コマンドを積めません";
            if (!CanAfford(cardData)) return "メモリーが不足しています";
            if (!_tokens.CanPay(cardData.TokenCost)) return "トークンが足りません";
            // バッファをパージするコマンドは、ほかにパージできるバッファが無ければ積めない
            if (cardData.HandPurgeCount > 0 && _hand.Count <= 1) return "パージできるバッファがありません";
            return null;
        }

        /// <summary>
        /// そのカードのコストを今払えるか（選べない理由がメモリー不足かどうかを見分けたいとき用）。
        /// 積んだ時点でスタミナを払うので、「今払えるか」を見るだけで積み上がった分も込みの判定になる
        /// </summary>
        public bool CanAfford(CardData cardData)
        {
            return _player.Stamina >= cardData.Cost;
        }

        /// <summary>
        /// 選択中のカードの対象として、そのユニットを選べるかどうか。
        /// 表示（選択可能なユニットのハイライト）と実際の受け付けで判定がずれないよう、入口を1つにしてある
        /// </summary>
        public bool CanSelectTarget(UnitStatusModel target)
        {
            var cardData = _selectionModel.PendingCard;
            return cardData != null && _targets.CanTarget(cardData, target);
        }

        /// <summary>対象ユニットが選ばれた。対象選択中でない、またはそのカードでは選べないユニットなら何もしない</summary>
        /// <returns>実行リストに積んだ（パージの選択に入った）か</returns>
        public bool SelectTarget(UnitStatusModel target)
        {
            if (!CanSelectTarget(target)) return false;

            var cardData = _selectionModel.PendingCard;
            int handIndex = _selectionModel.PendingHandIndex;

            // バッファをパージするコマンドは、続けてどのバッファをパージするかを選ぶ
            if (cardData.HandPurgeCount > 0)
            {
                _selectionModel.BeginPurgeChoice(cardData, target, cardData.HandPurgeCount, handIndex);
                return true;
            }

            _selectionModel.Clear();
            return AddCommand(cardData, target, null, handIndex, null);
        }

        /// <summary>
        /// パージするバッファを選び終えた。選んだものを預かってコマンドを積む。
        /// 1個以上、上限以下で、どれもバッファにある（積むコマンド自身の分は除く）ときだけ受け付ける
        /// </summary>
        /// <param name="purgeHandIndices">選んだバッファがそれぞれ手札の何番目か（purgeCards と同じ順）。無ければ先頭から抜く</param>
        public bool ConfirmPurgeChoice(IReadOnlyList<CardData> purgeCards, IReadOnlyList<int> purgeHandIndices = null)
        {
            var cardData = _selectionModel.PurgeCard;
            if (cardData == null || purgeCards == null) return false;
            if (purgeCards.Count < 1 || purgeCards.Count > _selectionModel.PurgeCapacity) return false;

            // 同じコマンドが複数あり得るので、数で確かめる（積むコマンド自身の1枚は先に取っておく）
            var available = new List<CardData>(_hand.Cards);
            if (!available.Remove(cardData)) return false;
            foreach (var card in purgeCards)
            {
                if (!available.Remove(card)) return false;
            }

            var target = _selectionModel.PurgeTarget;
            int handIndex = _selectionModel.PendingHandIndex;
            _selectionModel.Clear();
            return AddCommand(cardData, target, purgeCards, handIndex, purgeHandIndices);
        }

        public void CancelSelection()
        {
            _selectionModel.Clear();
        }

        /// <summary>実行リストのカードを手札に戻す。積むときに払ったスタミナとトークン、預かっていたバッファも返す</summary>
        public void ReturnCommandToHand(int index)
        {
            var command = _commandModel.RemoveAt(index);
            if (command == null) return;

            // 消費と対になる返却なので Gain 扱い（消費側に割り込む効果が出てきたら、返却の扱いもそこで考えること）
            _player.ChangeStamina(StaminaChangeType.Gain, command.Card.Cost);
            _tokens.Refund(command.Card.TokenCost);
            _hand.AddCard(command.Card);
            foreach (var card in command.PurgeCards) _hand.AddCard(card);
        }

        public void SwapCommands(int index, int otherIndex)
        {
            _commandModel.Swap(index, otherIndex);
        }

        /// <summary>実行ボタンが押された。選択を片付けて実行を求めるだけで、進行（1枚ずつの間合い）は Director に任せる</summary>
        public void RequestExecuteCommands()
        {
            _selectionModel.Clear();
            _commandModel.RequestExecute();
        }

        // 実行リストへ積む。積めたときだけコストを払い、手札から取り除く。
        // パージ予定のバッファがあれば、それもバッファから抜いて預かる
        private bool AddCommand(CardData cardData, UnitStatusModel target, IReadOnlyList<CardData> purgeCards,
            int handIndex, IReadOnlyList<int> purgeHandIndices)
        {
            var copy = purgeCards != null ? new List<CardData>(purgeCards) : null;
            if (!_commandModel.TryAdd(new CardCommand(cardData, target, copy))) return false;

            _player.ChangeStamina(StaminaChangeType.Consume, -cardData.Cost);
            // トークンを払うコマンドは、積んだ時点で払う（メモリーと同じ。手札に戻したら返す）
            _tokens.Pay(cardData.TokenCost);

            // 同じカードが複数あると、先頭から抜くと押したのとは別の1枚が抜けて見える。
            // 位置が分かっているものは後ろから順に位置で抜き（前の位置がずれないように）、分からないものは先頭から抜く
            var removals = new List<(CardData Card, int Index)> { (cardData, handIndex) };
            if (copy != null)
            {
                for (int i = 0; i < copy.Count; i++)
                {
                    int index = purgeHandIndices != null && i < purgeHandIndices.Count ? purgeHandIndices[i] : -1;
                    removals.Add((copy[i], index));
                }
            }
            removals.Sort((a, b) => b.Index.CompareTo(a.Index));
            foreach (var (card, index) in removals)
            {
                if (!_hand.RemoveCardAt(index, card)) _hand.RemoveCard(card);
            }
            return true;
        }
    }
}
