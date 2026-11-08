using System;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Models
{
    // 今まさに効果を出しているカードと、その使い手（プレイヤー・敵とも）。
    // ダメージの数字に「何によるものか」を添えるのと、使い手の行動アニメを始めるのに使う。
    // ターン終了時の継続効果（ウイルスなど）の間は空にしておき、そちらは状態効果の名前で出す
    public sealed class CurrentActionModel
    {
        public CardData Card { get; private set; }
        public UnitStatusModel User { get; private set; }

        /// <summary>誰かがカードを使い始めた（使い手, カード）</summary>
        public event Action<UnitStatusModel, CardData> OnActionStarted;

        public void Set(UnitStatusModel user, CardData card)
        {
            User = user;
            Card = card;
            OnActionStarted?.Invoke(user, card);
        }

        public void Clear()
        {
            User = null;
            Card = null;
        }
    }
}
