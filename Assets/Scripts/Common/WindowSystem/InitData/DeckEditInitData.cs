using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // デッキ編集（コマンドビルド）。開くたびに、そのときの所持コマンドとデッキから編集を始める
    public sealed class DeckEditInitData : WindowInitData
    {
        public override WindowId Id => WindowId.DeckEdit;

        /// <summary>開いたときの所持コマンド（同じものは個数分並べる）</summary>
        public Func<IReadOnlyList<CardData>> GetOwnedCards { get; }

        /// <summary>開いたときのデッキの一覧</summary>
        public Func<IReadOnlyList<IReadOnlyList<CardData>>> GetDecks { get; }

        /// <summary>開いたときのアクティブな（戦闘で使う）デッキの番号</summary>
        public Func<int> GetActiveIndex { get; }

        /// <summary>「おまかせ」で組むときの役割ごとの個数。null ならこの画面の既定の割合で組む</summary>
        public AutoDeckRule AutoDeckRule { get; }

        /// <summary>おまかせ・変更の破棄・保存を確かめる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>保存した（編集後のデッキの一覧, アクティブな番号）</summary>
        public Action<CardData[][], int> OnSaved { get; }

        /// <summary>右上の「敵詳細」が押された（同じウインドウをマップからも開くので、開くのは呼ぶ側）</summary>
        public Action OnEnemyDetailClicked { get; }

        /// <summary>敵詳細を開けるか（全部踏破した後は見せる相手がいない）。開くたびに見る</summary>
        public Func<bool> CanOpenEnemyDetail { get; }

        public DeckEditInitData(Func<IReadOnlyList<CardData>> getOwnedCards, Func<IReadOnlyList<IReadOnlyList<CardData>>> getDecks, Func<int> getActiveIndex, AutoDeckRule autoDeckRule, WindowHandler<ConfirmShowData> confirm, Action<CardData[][], int> onSaved, Action onEnemyDetailClicked, Func<bool> canOpenEnemyDetail)
        {
            GetOwnedCards = getOwnedCards;
            GetDecks = getDecks;
            GetActiveIndex = getActiveIndex;
            AutoDeckRule = autoDeckRule;
            Confirm = confirm;
            OnSaved = onSaved;
            OnEnemyDetailClicked = onEnemyDetailClicked;
            CanOpenEnemyDetail = canOpenEnemyDetail;
        }
    }
}
