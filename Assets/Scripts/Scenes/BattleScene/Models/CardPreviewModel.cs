using System;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Models
{
    // カーソルを合わせているカード（詳細を大きく見せる対象）。手札と実行リストの両方から書かれ、詳細パネルが読む。
    // 誰が見せているか（owner）も覚える。同じカードが複数あると CardData では区別できず、
    // 隣へ移ったときに「新しい方の Enter → 古い方の Exit」の順で来ると、古い方の Exit で新しい方まで消えてしまうため。
    //
    // タッチでは「今タップして説明を出している1つ」でもある（次に別のものをタップするまで出し続ける）。
    // 状態効果のように詳細パネルに出さないものは、card を null にして owner だけ持つ（詳細パネルは閉じ、その説明が開く）
    public sealed class CardPreviewModel
    {
        public object Owner { get; private set; }

        public CardData Card { get; private set; }

        public event Action<CardData> OnChanged;

        /// <summary>見せている持ち主が変わった（タッチで、どれの説明を開いておくかを決める）</summary>
        public event Action OnOwnerChanged;

        public void Show(object owner, CardData card)
        {
            SetOwner(owner);
            SetCard(card);
        }

        // 見せているのがその owner のときだけ消す
        public void Hide(object owner)
        {
            if (Owner != owner || owner == null) return;
            SetOwner(null);
            SetCard(null);
        }

        /// <summary>誰が見せていても消す（タッチで、実行が始まったら開いていた説明を閉じる）</summary>
        public void Clear()
        {
            SetOwner(null);
            SetCard(null);
        }

        private void SetOwner(object owner)
        {
            if (Owner == owner) return;
            Owner = owner;
            OnOwnerChanged?.Invoke();
        }

        private void SetCard(CardData card)
        {
            if (Card == card) return;
            Card = card;
            OnChanged?.Invoke(Card);
        }
    }
}
