using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.DeckEdit.Presenters;
using Live2DCardRogue.Window.DeckEdit.Views;

namespace Live2DCardRogue.Window.DeckEdit.Initializer
{
    // デッキ編集（DeckEditWindow.prefab）のコンポジションルート
    public sealed class DeckEditWindowInitializer : WindowInitializerBase<DeckEditInitData, DeckEditPresenter>
    {
        [SerializeField] private DeckEditWindowView _view;

        protected override DeckEditPresenter Initialize(DeckEditInitData data)
        {
            return new DeckEditPresenter(_view, data.GetOwnedCards, data.GetDecks, data.GetActiveIndex, data.AutoDeckRule, data.Confirm, data.OnSaved,
                data.OnEnemyDetailClicked, data.CanOpenEnemyDetail);
        }
    }
}
