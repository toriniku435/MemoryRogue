using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Window.BattlePile.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.BattlePile.Presenters
{
    // アーカイブとパージの山をクリックすると、その中身の一覧ウインドウを開く（ウインドウは1つを2つの山で使い回す）。
    // 一覧は山に入った順（新しいものほど後ろ）で、カーソルを合わせたコマンドを右に大きく出す。
    // 開いている間も戦闘は進むので、見ている山が変わったら一覧と数をその場で直す。山札（コマンドデッキ）は引く順が分かってしまうので開かない
    public sealed class BattlePileWindowPresenter : PresenterBase, IWindowPresenter<BattlePileShowData>
    {
        private readonly BattlePileWindowView _window;
        private readonly CardPileModel _archive;
        private readonly CardPileModel _purge;

        // 見ている山（開いていなければ使わない）と、拡大表示に出しているコマンド（何番目か。同じコマンドが何枚あっても印は1枚だけに付ける）
        private BattlePileKind _pile;
        private CardData _selected;
        private int _selectedIndex = -1;

        public BattlePileWindowPresenter(BattlePileWindowView window, CardPileModel archive, CardPileModel purge)
        {
            _window = window;
            _archive = archive;
            _purge = purge;
        }

        private CardPileModel CurrentPile => _pile == BattlePileKind.Archive ? _archive : _purge;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _window.OnCloseRequested += Close;
            _window.List.Initialize(BindItem);
            foreach (var pile in new[] { _archive, _purge })
            {
                pile.OnCardAdded += HandlePileChanged;
                pile.OnCardRemoved += HandlePileChanged;
                pile.OnCleared += HandlePileCleared;
            }
        }

        protected override void OnDispose()
        {
            _window.OnCloseRequested -= Close;
            _window.List.Dispose();
            foreach (var pile in new[] { _archive, _purge })
            {
                pile.OnCardAdded -= HandlePileChanged;
                pile.OnCardRemoved -= HandlePileChanged;
                pile.OnCleared -= HandlePileCleared;
            }
        }

        protected override void OnLocaleChanged()
        {
            if (!_window.IsOpen) return;
            ApplyTexts();
            _window.List.Refresh();
            if (_selected != null) _window.Detail.Show(_selected, true);
        }

        public bool IsOpen => _window.IsOpen;

        // もう開いているなら、見る山だけ切り替える
        public void Open(BattlePileShowData data)
        {
            _pile = data.Pile;
            _selected = null;
            _selectedIndex = -1;
            ApplyTexts();
            _window.Detail.Hide();
            if (!_window.IsOpen) _window.Open();
            _window.List.SetItemCount(CurrentPile.Count);
            _window.List.ScrollToStart();
        }

        private void ApplyTexts()
        {
            bool archive = _pile == BattlePileKind.Archive;
            string name = LocalizationHelper.GetUiText(archive ? UiTextKey.DiscardPile : UiTextKey.PurgePile);
            _window.SetTexts(archive ? "// ARCHIVE" : "// PURGED", name, LocalizationHelper.GetUiText(UiTextKey.CollectionCommands), LocalizationHelper.GetUiText(UiTextKey.Close));
            _window.SetCount(string.Format(LocalizationHelper.GetUiText(UiTextKey.PileCount), CurrentPile.Count));
        }

        // 見ている山が変わった: 数と一覧を直す（拡大表示はそのまま）
        private void HandlePileChanged(CardData _)
        {
            Resync();
        }

        private void HandlePileCleared()
        {
            Resync();
        }

        private void Resync()
        {
            if (!_window.IsOpen) return;
            _window.SetCount(string.Format(LocalizationHelper.GetUiText(UiTextKey.PileCount), CurrentPile.Count));
            _window.List.SetItemCount(CurrentPile.Count);
        }

        private void BindItem(int index, RectTransform item)
        {
            var cards = CurrentPile.Cards;
            if (index < 0 || index >= cards.Count) return;
            // 山が変わって並びがずれたら、印は付けない（拡大表示はそのまま）
            bool selected = index == _selectedIndex && cards[index] == _selected;
            item.GetComponent<CardListItemView>().Bind(cards[index], true, selected, card => HandleHovered(index, card));
        }

        // カーソルを合わせた: 拡大表示に出し、一覧の印も付け替える
        private void HandleHovered(int index, CardData card)
        {
            if (card == null || index == _selectedIndex) return;
            _selectedIndex = index;
            _selected = card;
            _window.Detail.Show(card, true);
            _window.List.Refresh();
        }

        public void Close()
        {
            _window.Close();
        }
    }
}
