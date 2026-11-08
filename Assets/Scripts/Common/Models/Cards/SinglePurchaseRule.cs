using System;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.Cards
{
    // コマンドを1つずつ選んで買う（シングル購入）ときの決まり。値段はレアリティで決まり、1種類につき買える数に上限がある。
    // 数値は CampaignBalance が持ち、この形にして渡す（Model 層は ScriptableObject を知らない）
    public sealed class SinglePurchaseRule
    {
        private readonly int _normalPrice;
        private readonly int _rarePrice;
        private readonly int _epicPrice;

        /// <summary>1種類のコマンドを買える数（キャンペーンを通して）</summary>
        public int LimitPerCommand { get; }

        public SinglePurchaseRule(int normalPrice, int rarePrice, int epicPrice, int limitPerCommand)
        {
            _normalPrice = Math.Max(0, normalPrice);
            _rarePrice = Math.Max(0, rarePrice);
            _epicPrice = Math.Max(0, epicPrice);
            LimitPerCommand = Math.Max(0, limitPerCommand);
        }

        /// <summary>そのレアリティの1つの値段（係数をかける前）</summary>
        public int BasePriceOf(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare: return _rarePrice;
                case CardRarity.Epic: return _epicPrice;
                default: return _normalPrice;
            }
        }

        /// <summary>単体で開いたとき（呼び出し元から決まりが渡されなかったとき）の既定値</summary>
        public static SinglePurchaseRule Default => new SinglePurchaseRule(40, 90, 180, 3);
    }
}
