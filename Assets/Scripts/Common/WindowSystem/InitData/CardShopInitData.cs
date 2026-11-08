using System;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.WindowSystem
{
    // マーケット。開くたびにその時点のトークンと持ち物で買い物を始め、買うたびにその場で伝える
    public sealed class CardShopInitData : WindowInitData
    {
        public override WindowId Id => WindowId.CardShop;

        /// <summary>マップのトークン表示。買って減った分を数え下げる</summary>
        public TokenCounterView MapTokenCounter { get; }

        /// <summary>開いたときの状態でモデルを作る</summary>
        public Func<CardShopModel> CreateModel { get; }

        /// <summary>買った（手に入れたコマンド, そのうちシングルで買ったもの, 残りトークン）</summary>
        public Action<CardData[], CardData[], int> OnPurchased { get; }

        public CardShopInitData(TokenCounterView mapTokenCounter, Func<CardShopModel> createModel, Action<CardData[], CardData[], int> onPurchased)
        {
            MapTokenCounter = mapTokenCounter;
            CreateModel = createModel;
            OnPurchased = onPurchased;
        }
    }
}
