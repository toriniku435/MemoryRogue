using Live2DCardRogue.BattleScene.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // タッチでの手札の操作: 1回目のタップで説明を出して浮かせ（敵が複数いて1体を選ぶカードは候補パネルも開く）、
    // 浮いている1枚をもう一度タップすると使う（優先ターゲットへ）。別の敵にしたいときは候補パネルをタップする。
    // 浮かせたのを下ろすのは何も無い所のタップ（BattleCancelAreaView）。
    //
    // 説明はタップするたびにそのカードへ移り、別のコマンドや状態効果をタップするまで出し続ける
    // （使った後も、相手の番でも、何も無い所をタップしても消さない）。
    //
    // カーソルの出入りは使わない。タッチでも EventSystem は指が触れた瞬間に enter、離した瞬間に exit を送ってくるので、
    // それに反応すると指を離しただけで説明が消え、候補パネルの「離れたら外す」も効いてしまう
    internal sealed class TouchHandInteraction : IHandInteraction
    {
        private readonly IHandOperations _hand;

        public TouchHandInteraction(IHandOperations hand)
        {
            _hand = hand;
        }

        public void OnCardPointerChanged(BattleCardView view, bool isOver) { }

        public void OnCardDwelled(BattleCardView view) { }

        public void OnPickerPointerChanged(bool isOver) { }

        public void OnLiftedChanged(BattleCardView lifted) { }

        public void OnCardClicked(BattleCardView view)
        {
            _hand.ShowPreview(view);
            if (!_hand.CanOperate(view)) return;

            if (_hand.IsChoosingPurge)
            {
                _hand.TogglePurgeMark(view);
                return;
            }

            // 2回目のタップ。使えないカードなら、ここで理由が出る（1回目は浮かせて見せるだけ）
            if (view == _hand.PendingView || view == _hand.InspectedView)
            {
                _hand.Play(view);
                return;
            }

            if (_hand.NeedsTargetChoice(view)) _hand.Select(view);
            else _hand.Inspect(view);
        }

        public void Dispose() { }
    }
}
