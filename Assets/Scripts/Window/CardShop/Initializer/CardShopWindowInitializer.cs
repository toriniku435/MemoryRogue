using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.CardShop.Presenters;
using Live2DCardRogue.Window.CardShop.Views;

namespace Live2DCardRogue.Window.CardShop.Initializer
{
    // マーケット（CardShopWindow.prefab）のコンポジションルート
    public sealed class CardShopWindowInitializer : WindowInitializerBase<CardShopInitData, CardShopPresenter>
    {
        [SerializeField] private CardShopWindowView _view;

        protected override CardShopPresenter Initialize(CardShopInitData data)
        {
            return new CardShopPresenter(_view, data.MapTokenCounter, data.CreateModel, data.OnPurchased);
        }
    }
}
