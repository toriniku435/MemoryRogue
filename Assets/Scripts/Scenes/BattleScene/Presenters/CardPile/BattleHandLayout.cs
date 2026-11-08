using System;
using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 手札のカードの View を持ち、手札の中身に合わせて増やす・減らす・並べる。どの1枚を浮かせるかは呼ぶ側が決める。
    // 位置は目標として渡し、そこまでの動きは View（BattleCardMotionView）に任せる。
    // Layout Group を使わないのは、Layout Group が並べ直しのたびに位置を上書きして、動きと喧嘩するため
    public sealed class BattleHandLayout
    {
        private const float CardSpacing = 16f;

        // 手札の列の最大幅（基準解像度の単位）。手札が増える効果で枚数が増えても、左右のメモリー表示や実行ボタンに
        // かからないよう、これを超えるときはカードを重ねて詰める（5枚ちょうどで収まる幅）
        private const float MaxRowWidth = 1220f;

        // 何枚もまとめて引く・捨てるときに、1枚ずつずらして動かす間隔（秒）
        private const float EnterStagger = 0.07f;
        private const float ExitStagger = 0.05f;

        private readonly BattleCardView _prefab;
        private readonly RectTransform _hand;
        private readonly Action<BattleCardView, CardData> _bind;

        // 手札に並んでいる View（手札の順）。退場中の View はここにも _pool にも入っていない
        private List<BattleCardView> _views = new List<BattleCardView>();
        private readonly Stack<BattleCardView> _pool = new Stack<BattleCardView>();

        // まとめて動くときの時間差を、Sync をまたいで積み上げるための時刻
        private float _nextEnterTime;
        private float _nextExitTime;

        /// <param name="bind">貸し出す View にカードと、その View 専用のコールバックを付ける（押されたのがどの1枚かを特定するため）</param>
        public BattleHandLayout(BattleCardView prefab, RectTransform hand, Action<BattleCardView, CardData> bind)
        {
            _prefab = prefab;
            _hand = hand;
            _bind = bind;
        }

        public IReadOnlyList<BattleCardView> Views => _views;

        public int IndexOf(BattleCardView view) => _views.IndexOf(view);

        public bool Contains(BattleCardView view) => _views.Contains(view);

        /// <summary>
        /// 手札とViewを突き合わせ、増えた分（entered）と減った分（removed）を返す。
        /// インデックスで入れ直すと、途中の1枚が抜けたときに末尾のViewが消えて見えてしまうので、Viewとカードの組は保ったまま動かす。
        /// CardData は共有参照で同じカードの View を区別できないので、avoid（抜けるはずの1枚）は他に候補が無いときだけ残す
        /// </summary>
        public void Diff(IReadOnlyList<CardData> cards, HashSet<BattleCardView> avoid, List<BattleCardView> entered, List<BattleCardView> removed)
        {
            var next = new List<BattleCardView>(cards.Count);
            int cursor = 0;
            foreach (var cardData in cards)
            {
                int match = FindMatch(cardData, cursor, avoid);
                if (match < 0)
                {
                    var view = Rent(cardData);
                    next.Add(view);
                    entered.Add(view);
                    continue;
                }

                for (int k = cursor; k < match; k++) removed.Add(_views[k]);
                // 言語切り替えでも Sync が走るので、残るカードも文字を読み直す
                _views[match].SetCardData(cardData);
                next.Add(_views[match]);
                cursor = match + 1;
            }
            for (int k = cursor; k < _views.Count; k++) removed.Add(_views[k]);
            _views = next;
        }

        /// <summary>手札から抜けた View を destination へ飛ばし、終わったらプールへ戻す。捨てるときだけ1枚ずつずらす</summary>
        public void PlayExit(BattleCardView view, CardExitStyle style, Transform destination)
        {
            var to = CardLayoutUtility.ToChildAnchoredPosition(_hand, destination);
            float delay = style == CardExitStyle.Discard ? NextDelay(ref _nextExitTime, ExitStagger) : 0f;
            view.PlayExit(to, style, delay, () =>
            {
                view.SetActive(false);
                _pool.Push(view);
            });
        }

        /// <summary>
        /// 中央寄せの1列に並べる。entered は enterFrom から飛んでくる。
        /// 重なるときは左のカードが奥、右へ行くほど手前（描画順 = 並び順）で、front だけは最前面に出す
        /// </summary>
        /// <param name="liftOf">その View を浮かせる高さ（0 なら浮かせない）</param>
        /// <param name="isSelected">選んでいる光る縁を付けるか</param>
        public void Reposition(List<BattleCardView> entered, Transform enterFrom, BattleCardView front,
            Func<BattleCardView, float> liftOf, Func<BattleCardView, bool> isSelected)
        {
            int count = _views.Count;
            if (count == 0) return;

            // プレハブ自体を縮小して置いてあるので、見た目の幅はスケールを掛けたもの（実行リストと同じ）
            var prefabRect = (RectTransform)_prefab.transform;
            float cardWidth = prefabRect.rect.width * prefabRect.localScale.x;
            float step = cardWidth + CardSpacing;
            if (count > 1 && count * cardWidth + (count - 1) * CardSpacing > MaxRowWidth)
            {
                step = (MaxRowWidth - cardWidth) / (count - 1);
            }
            float startX = -(cardWidth + (count - 1) * step) / 2f + cardWidth / 2f;

            foreach (var view in _views) view.transform.SetAsLastSibling();
            if (front != null) front.transform.SetAsLastSibling();

            for (int i = 0; i < count; i++)
            {
                var view = _views[i];
                var target = new Vector2(startX + i * step, liftOf.Invoke(view));
                view.SetSelected(isSelected.Invoke(view));

                if (entered != null && entered.Contains(view))
                {
                    view.PlayEnter(CardLayoutUtility.ToChildAnchoredPosition(_hand, enterFrom), target, NextDelay(ref _nextEnterTime, EnterStagger));
                }
                else
                {
                    view.MoveTo(target);
                }
            }
        }

        // cursor 以降で同じカードの View を探す。避けたい View は、他に候補が無いときだけ使う
        private int FindMatch(CardData cardData, int cursor, HashSet<BattleCardView> avoid)
        {
            int fallback = -1;
            for (int j = cursor; j < _views.Count; j++)
            {
                if (_views[j].CardData != cardData) continue;
                if (!avoid.Contains(_views[j])) return j;
                if (fallback < 0) fallback = j;
            }
            return fallback;
        }

        private BattleCardView Rent(CardData cardData)
        {
            var view = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _hand);
            _bind.Invoke(view, cardData);
            view.ResetForReuse();
            view.SetActive(true);
            return view;
        }

        private static float NextDelay(ref float nextTime, float stagger)
        {
            float now = Time.time;
            nextTime = Mathf.Max(nextTime, now);
            float delay = nextTime - now;
            nextTime += stagger;
            return delay;
        }
    }
}
