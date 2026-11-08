using System;
using Live2DCardRogue.BattleScene.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 手札への入力（カーソル・クリック・タップ）をどう解釈するか。マウスとタッチで差し替える（InteractionMode）。
    // 選ぶ・浮かせる・使うといった操作そのものは IHandOperations（手札の Presenter）が持ち、ここは「どの入力でどれを呼ぶか」だけを決める
    internal interface IHandInteraction : IDisposable
    {
        void OnCardPointerChanged(BattleCardView view, bool isOver);

        /// <summary>カーソルを合わせたまま少し止まった</summary>
        void OnCardDwelled(BattleCardView view);

        void OnCardClicked(BattleCardView view);

        /// <summary>カーソルが対象の候補パネルに乗った / 外れた</summary>
        void OnPickerPointerChanged(bool isOver);

        /// <summary>浮いている1枚が変わったかもしれない（並べ直すたびに呼ばれる。null なら何も浮いていない）</summary>
        void OnLiftedChanged(BattleCardView lifted);
    }

    // 入力の解釈（IHandInteraction）から呼ぶ、手札の操作
    internal interface IHandOperations
    {
        /// <summary>プレイヤーの番で、手札にあるカードか</summary>
        bool CanOperate(BattleCardView view);

        bool IsChoosingPurge { get; }

        /// <summary>対象を選んでいる1枚（候補パネルを開いている）</summary>
        BattleCardView PendingView { get; }

        /// <summary>浮かせて見せているだけの1枚</summary>
        BattleCardView InspectedView { get; }

        /// <summary>使えるカードで、敵が複数いて1体を選ぶもの（候補パネルを開く意味がある）</summary>
        bool NeedsTargetChoice(BattleCardView view);

        /// <summary>そのまま実行リストへ積む。使えなければ理由を出して、浮かせて見せる</summary>
        void Play(BattleCardView view);

        /// <summary>対象選択に入る（候補パネルを開く）</summary>
        void Select(BattleCardView view);

        /// <summary>浮かせて見せるだけ（前の選択は外す）</summary>
        void Inspect(BattleCardView view);

        /// <summary>浮かせて見せているだけの1枚なら下ろす</summary>
        void Lower(BattleCardView view);

        void TogglePurgeMark(BattleCardView view);

        void ShowPreview(BattleCardView view);

        void HidePreview(BattleCardView view);

        /// <summary>対象選択中なら取り消す</summary>
        void CancelTargetSelection();
    }
}
