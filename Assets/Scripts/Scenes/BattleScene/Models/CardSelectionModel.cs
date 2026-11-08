using System;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Models
{
    // 対象選択待ちのカード。相手に作用するカードをクリックしてから、対象ユニットを選ぶまでの一時状態だけを持つ。
    // 手札のハイライトと、対象ユニットの選択可能表示が同じ状態を見る必要があるのでモデルに置いている。
    //
    // バッファ（手札）をパージするコマンド（バッファオーバーフロー）は、対象を選んだあとにもう1段、
    // 「どのバッファをパージするか」を選ぶ（PurgeCard / PurgeTarget / PurgeCapacity）。その間 PendingCard は空になる。
    // 選んだ数（PurgeChosenCount）と「決定したい」（RequestPurgeConfirm）もここを通すので、
    // 決定はパネルの決定ボタンからでも、実行ボタン（その間は決定ボタンになる）からでも同じ流れになる
    public sealed class CardSelectionModel
    {
        public CardData PendingCard { get; private set; }

        /// <summary>
        /// 選んでいるカードが手札の何番目か（同じカードが複数あるとき、押された1枚を抜くため）。分からなければ -1。
        /// パージの選択に移っても引き継ぐ
        /// </summary>
        public int PendingHandIndex { get; private set; } = -1;

        public bool IsSelectingTarget => PendingCard != null;

        /// <summary>パージするバッファを選んでいる最中のコマンド。選んでいなければ null</summary>
        public CardData PurgeCard { get; private set; }

        /// <summary>パージを選んでいるコマンドの対象（もう選んである）</summary>
        public UnitStatusModel PurgeTarget { get; private set; }

        /// <summary>何個までパージできるか</summary>
        public int PurgeCapacity { get; private set; }

        public bool IsChoosingPurge => PurgeCard != null;

        /// <summary>パージに選んだバッファの数（選んでいなければ 0）。1個以上で決定できる</summary>
        public int PurgeChosenCount { get; private set; }

        public bool CanConfirmPurge => IsChoosingPurge && PurgeChosenCount > 0;

        public event Action<CardData> OnPendingCardChanged;

        /// <summary>パージの選択が始まった / 終わった</summary>
        public event Action OnPurgeChoiceChanged;

        /// <summary>パージに選んだ数が変わった</summary>
        public event Action OnPurgeChosenCountChanged;

        /// <summary>パージの選択を決定したい（選んでいるバッファを知っている手札の Presenter が受けて積む）</summary>
        public event Action OnPurgeConfirmRequested;

        public void SetPendingCard(CardData cardData, int handIndex = -1)
        {
            if (cardData != null) PendingHandIndex = handIndex;
            if (PendingCard == cardData) return;

            PendingCard = cardData;
            OnPendingCardChanged?.Invoke(PendingCard);
        }

        /// <summary>対象選択を終えて、パージするバッファの選択に移る</summary>
        public void BeginPurgeChoice(CardData card, UnitStatusModel target, int capacity, int handIndex = -1)
        {
            SetPendingCard(null);
            PendingHandIndex = handIndex;
            PurgeCard = card;
            PurgeTarget = target;
            PurgeCapacity = capacity;
            PurgeChosenCount = 0;
            OnPurgeChoiceChanged?.Invoke();
        }

        /// <summary>パージに選んだ数を伝える（選んでいる最中だけ）</summary>
        public void SetPurgeChosenCount(int count)
        {
            if (!IsChoosingPurge) return;
            count = Math.Max(0, count);
            if (count == PurgeChosenCount) return;

            PurgeChosenCount = count;
            OnPurgeChosenCountChanged?.Invoke();
        }

        /// <summary>決定を求める。決定できないとき（選んでいない・1個も選んでいない）は何もせず false</summary>
        public bool RequestPurgeConfirm()
        {
            if (!CanConfirmPurge) return false;
            OnPurgeConfirmRequested?.Invoke();
            return true;
        }

        public void Clear()
        {
            SetPendingCard(null);
            if (PurgeCard == null) return;

            PurgeCard = null;
            PurgeTarget = null;
            PurgeCapacity = 0;
            PurgeChosenCount = 0;
            OnPurgeChoiceChanged?.Invoke();
        }
    }
}
