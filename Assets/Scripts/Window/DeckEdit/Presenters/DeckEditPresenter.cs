using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using Live2DCardRogue.Window.DeckEdit.Constants;
using Live2DCardRogue.Window.DeckEdit.Models;
using Live2DCardRogue.Window.DeckEdit.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.DeckEdit.Presenters
{
    // デッキ編集ウインドウを受け持つPresenter（開くのは呼ぶ側が Handler の Show で）。
    // デッキは複数あり（DeckSetEditModel）、上のタブで選んだ1つを編集する。所持リスト・デッキリスト・サブビューの3つは
    // 選んでいるデッキの DeckEditModel を見ているので、増減した結果の辻褄合わせは Sync 1本で済む。
    //
    // 開くたびに、そのときの所持コマンドとデッキの一覧からモデルを作り直す。デッキの切り替え・編集・アクティブにするは
    // どれも保存でまとめて確定し、キャンセルはモデルを捨てて閉じるだけなので、「開く前の状態に戻す」巻き戻しはどこにも要らない。
    // 保存したら onSaved でデッキの一覧とアクティブな番号を伝え、保存先への書き戻しは Initializer が行う
    // （この Presenter はリポジトリを知らない）
    public sealed class DeckEditPresenter : PresenterBase, IWindowPresenter
    {
        private readonly DeckEditWindowView _window;
        private readonly Func<IReadOnlyList<CardData>> _getOwnedCards;
        private readonly Func<IReadOnlyList<IReadOnlyList<CardData>>> _getDecks;
        private readonly Func<int> _getActiveIndex;
        private readonly AutoDeckRule _autoDeckRule;
        private readonly Action<CardData[][], int> _onSaved;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        // 右上の「敵詳細」。同じウインドウをマップからも開くので、開くのは呼ぶ側（Initializer が敵詳細の Handler を渡す）
        private readonly Action _onEnemyDetailClicked;
        private readonly Func<bool> _canOpenEnemyDetail;

        // 確認ウインドウで何を確かめているか。確認ウインドウはマップで共用なので、自分が開いたときだけ答える
        private enum Confirming { AutoBuild, Clear, Discard, Save }

        // 編集中の状態。開くたびに作り直す（まだ一度も開いていなければ null）
        private DeckSetEditModel _set;

        // 選んでいるデッキ
        private DeckEditModel _model => _set?.Selected;

        private IReadOnlyList<CardData> _availableCards = new CardData[0];
        private IReadOnlyList<CardData> _deckCards = new CardData[0];

        // 表示のしかた（すべて / デッキ / 所持）。開き直しても前回のまま
        private DeckEditLayout _layout = DeckEditLayout.Both;

        // サブビューに出しているカードと、それがどちらのリストから開かれたか
        private CardData _selectedCard;
        private bool _isSelectedFromDeck;

        /// <param name="getOwnedCards">開いたときの所持コマンド（同じものは個数分並べる）</param>
        /// <param name="getDecks">開いたときのデッキの一覧</param>
        /// <param name="getActiveIndex">開いたときのアクティブな（戦闘で使う）デッキの番号</param>
        /// <param name="autoDeckRule">「おまかせ」で組むときの役割ごとの個数。null ならこの画面の既定の割合で組む</param>
        /// <param name="confirm">おまかせ・変更の破棄・保存を確かめる確認ウインドウ（マップで共用）</param>
        /// <param name="onSaved">保存した（編集後のデッキの一覧, アクティブな番号）</param>
        /// <param name="onEnemyDetailClicked">右上の「敵詳細」が押された</param>
        /// <param name="canOpenEnemyDetail">敵詳細を開けるか（全部踏破した後は見せる相手がいない）。開くたびに見る</param>
        public DeckEditPresenter(
            DeckEditWindowView window,
            Func<IReadOnlyList<CardData>> getOwnedCards,
            Func<IReadOnlyList<IReadOnlyList<CardData>>> getDecks,
            Func<int> getActiveIndex,
            AutoDeckRule autoDeckRule,
            WindowHandler<ConfirmShowData> confirm,
            Action<CardData[][], int> onSaved,
            Action onEnemyDetailClicked,
            Func<bool> canOpenEnemyDetail)
        {
            _window = window;
            _getOwnedCards = getOwnedCards;
            _getDecks = getDecks;
            _getActiveIndex = getActiveIndex;
            _autoDeckRule = autoDeckRule ?? CreateDefaultAutoDeckRule();
            _confirm = confirm;
            _onSaved = onSaved;
            _onEnemyDetailClicked = onEnemyDetailClicked;
            _canOpenEnemyDetail = canOpenEnemyDetail;
        }

        public bool IsOpen => _window.IsOpen;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _window.OnCancelClicked += HandleCancel;
            _window.EnemyDetailButton.onClick.AddListener(HandleEnemyDetail);
            _window.OnSaveClicked += HandleSave;
            _window.OnAutoClicked += HandleAuto;
            _window.OnActivateClicked += HandleActivate;
            _window.OnClearClicked += HandleClear;
            _window.OnDeckTabClicked += HandleDeckTabClicked;
            _window.OnLayoutClicked += HandleLayoutClicked;
            _window.OnCardDroppedToDeck += HandleCardDroppedToDeck;
            _window.OnCardDroppedToOwned += HandleCardDroppedToOwned;
            _window.SubView.AddButton.onClick.AddListener(HandleAdd);
            _window.SubView.RemoveButton.onClick.AddListener(HandleRemove);

            _window.OwnedList.Initialize(BindOwnedItem);
            _window.DeckList.Initialize(BindDeckItem);

            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _window.OnCancelClicked -= HandleCancel;
            _window.EnemyDetailButton.onClick.RemoveListener(HandleEnemyDetail);
            _window.OnSaveClicked -= HandleSave;
            _window.OnAutoClicked -= HandleAuto;
            _window.OnActivateClicked -= HandleActivate;
            _window.OnClearClicked -= HandleClear;
            _window.OnDeckTabClicked -= HandleDeckTabClicked;
            _window.OnLayoutClicked -= HandleLayoutClicked;
            _window.OnCardDroppedToDeck -= HandleCardDroppedToDeck;
            _window.OnCardDroppedToOwned -= HandleCardDroppedToOwned;
            _window.SubView.AddButton.onClick.RemoveListener(HandleAdd);
            _window.SubView.RemoveButton.onClick.RemoveListener(HandleRemove);

            _window.OwnedList.Dispose();
            _window.DeckList.Dispose();

            ReleaseModel();
        }

        // カード名・説明も見出しもローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_set != null) Sync();
        }

        private void ApplyLabels()
        {
            string title = LocalizationHelper.GetUiText(UiTextKey.DeckEdit);
            _window.EnemyDetailLabel.text = LocalizationHelper.GetUiText(UiTextKey.EnemyDetail);
            _window.SetTexts(
                title,
                LocalizationHelper.GetUiText(UiTextKey.OwnedCards),
                LocalizationHelper.GetUiText(UiTextKey.Cancel),
                LocalizationHelper.GetUiText(UiTextKey.Save),
                LocalizationHelper.GetUiText(UiTextKey.AutoBuild),
                LocalizationHelper.GetUiText(UiTextKey.ActiveDeck),
                LocalizationHelper.GetUiText(UiTextKey.SetActiveDeck));
            _window.SetLayoutTexts(new[]
            {
                LocalizationHelper.GetUiText(UiTextKey.DeckEditLayoutAll),
                LocalizationHelper.GetUiText(UiTextKey.DeckEditLayoutDeck),
                LocalizationHelper.GetUiText(UiTextKey.DeckEditLayoutOwned),
            });
        }

        // 開くたびに、今の所持コマンドとデッキの一覧から編集を始め直す（前回キャンセルした編集は残らない）。
        // 最初はアクティブなデッキを選んでおく
        public void Open()
        {
            if (_window.IsOpen) return;
            ReleaseModel();
            _set = new DeckSetEditModel(_getOwnedCards.Invoke(), _getDecks.Invoke(), _getActiveIndex.Invoke(),
                _window.DeckTabCount, DeckEditConstants.MaxDeckSize);
            _set.OnDeckChanged += HandleModelChanged;
            _set.OnSelectionChanged += HandleSelectionChanged;
            _set.OnActiveChanged += HandleModelChanged;

            _selectedCard = null;
            _isSelectedFromDeck = false;

            _window.EnemyDetailButton.interactable = _canOpenEnemyDetail.Invoke();
            _window.Open();
            _window.SetLayout(_layout);
            Sync();
            _window.OwnedList.ScrollToStart();
            _window.DeckList.ScrollToStart();
        }

        // 編集結果を返さずに閉じる。呼び出し元のデッキは触っていないので、そのまま
        // 変更が無ければそのまま閉じる。あれば、捨ててよいかを確かめてから閉じる
        private void HandleCancel()
        {
            if (_set == null || !_set.IsDirty)
            {
                Close();
                return;
            }

            OpenConfirm(Confirming.Discard,
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesMessage), null,
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesOk));
        }

        // 編集中は上限を超えて入れてもよい。アクティブでないデッキは過不足があっても保存できるが、
        // アクティブなデッキ（戦闘で使う）がちょうど上限の個数でなければ保存させない
        private void HandleSave()
        {
            if (!_set.IsSaveValid)
            {
                var active = _set.GetDeck(_set.ActiveIndex);
                MessageUseCase.ShowMessage($"アクティブなデッキ{_set.ActiveIndex + 1:00}のコマンドは{active.MaxDeckSize}個にしてください（現在{active.DeckCount}個）");
                return;
            }

            // 変更が無ければ、保存するものは無いので閉じるだけ
            if (!_set.IsDirty)
            {
                Close();
                return;
            }

            OpenConfirm(Confirming.Save,
                LocalizationHelper.GetUiText(UiTextKey.SaveChangesConfirm), null,
                LocalizationHelper.GetUiText(UiTextKey.SaveChangesOk));
        }

        // 選んでいるデッキをアクティブにする。個数は問わない（確かめるのは保存のとき。HandleSave）
        private void HandleActivate()
        {
            if (_set == null) return;
            _set.TryActivateSelected();
        }

        private void HandleDeckTabClicked(int index)
        {
            _set?.Select(index);
        }

        // 表示のしかたを変える。所持リストだけのときは、拡大表示の「−」も出す（デッキリストが見えないので）
        private void HandleLayoutClicked(DeckEditLayout layout)
        {
            if (_layout == layout) return;
            _layout = layout;
            _window.SetLayout(_layout);
            if (_set != null) Sync();
        }

        // デッキを切り替えたら、サブビューの選択を外し、両方の一覧を先頭から見せる
        private void HandleSelectionChanged()
        {
            _selectedCard = null;
            _isSelectedFromDeck = false;
            Sync();
            _window.OwnedList.ScrollToStart();
            _window.DeckList.ScrollToStart();
        }

        // おまかせ：所持しているコマンドから、ファーストビルドと同じ組み方（AutoDeckBuilder）で、選んでいるデッキを組み直す。
        // 今の中身が置き換わるので、どのデッキかを添えて確かめてから。
        // 保存はしない（気に入らなければそのまま編集もキャンセルもできる）
        private void HandleAuto()
        {
            if (_set == null) return;
            OpenConfirm(Confirming.AutoBuild,
                LocalizationHelper.GetUiText(UiTextKey.AutoBuildConfirm), DeckName(_set.SelectedIndex),
                LocalizationHelper.GetUiText(UiTextKey.AutoBuildOk));
        }

        // ゴミ箱：選んでいるデッキを空にする。中身が全部抜けるので、どのデッキかを添えて確かめてから。
        // 保存はしない（キャンセルすれば元に戻る）
        private void HandleClear()
        {
            if (_set == null || _model.DeckCount == 0) return;
            OpenConfirm(Confirming.Clear,
                LocalizationHelper.GetUiText(UiTextKey.ClearDeckConfirm), DeckName(_set.SelectedIndex),
                LocalizationHelper.GetUiText(UiTextKey.ClearDeckOk));
        }

        private void OpenConfirm(Confirming what, string message, string detail, string ok)
        {
            // 戻るなら何もせず、デッキ編集に戻る（編集中の中身はそのまま）
            _confirm.Show(new ConfirmShowData(message, detail, ok, LocalizationHelper.GetUiText(UiTextKey.Back), () => HandleConfirmOk(what)));
        }

        private void HandleConfirmOk(Confirming what)
        {
            switch (what)
            {
                case Confirming.AutoBuild:
                    _model.ReplaceDeck(AutoDeckBuilder.Build(_model.CreateOwnedCards(), _autoDeckRule));
                    break;
                case Confirming.Clear:
                    _model.ReplaceDeck(new CardData[0]);
                    break;
                case Confirming.Discard:
                    Debug.Log("--- デッキ編集を破棄して閉じます ---");
                    Close();
                    break;
                case Confirming.Save:
                    _onSaved.Invoke(_set.CreateDecks(), _set.ActiveIndex);
                    Close();
                    break;
            }
        }

        private static string DeckName(int index)
        {
            return $"{LocalizationHelper.GetUiText(UiTextKey.DeckCards)} {index + 1:00}";
        }

        // モデルは次に開くまで持っておく（閉じる動きの間もリストのスクロールで描き直しが走るため）。
        // 次に開いたときに作り直すので、閉じた後の中身が使われることはない
        public void Close()
        {
            _window.Close();
        }

        private void HandleEnemyDetail()
        {
            _onEnemyDetailClicked.Invoke();
        }

        private void ReleaseModel()
        {
            if (_set == null) return;
            _set.OnDeckChanged -= HandleModelChanged;
            _set.OnSelectionChanged -= HandleSelectionChanged;
            _set.OnActiveChanged -= HandleModelChanged;
            _set = null;
        }

        private void Sync()
        {
            _availableCards = _model.CreateAvailableCards();
            _deckCards = _model.CreateDeckCards();

            _window.OwnedList.SetItemCount(_availableCards.Count);
            _window.DeckList.SetItemCount(_deckCards.Count);
            _window.DeckCountView.SetCount(_model.DeckCount, _model.MaxDeckSize);
            _window.ShowDecks(DeckName(_set.SelectedIndex), _set.SelectedIndex, _set.ActiveIndex);
            _window.ShowDeckSummary(DeckName(_set.SelectedIndex), _model.DeckCount, _model.MaxDeckSize);
            _window.SetClearInteractable(_model.DeckCount > 0);

            SyncSubView();
        }

        // 増減でボタンの活性やバッジが変わるので、開いたままでも出し直す
        private void SyncSubView()
        {
            if (_selectedCard == null)
            {
                _window.SubView.Hide();
                return;
            }

            var status = $"{LocalizationHelper.GetUiText(UiTextKey.DeckCards)} {_model.GetDeckCount(_selectedCard)} / " +
                         $"{LocalizationHelper.GetUiText(UiTextKey.OwnedCards)} {_model.GetAvailableCount(_selectedCard)}";

            _window.SubView.Show(
                _selectedCard,
                status,
                canAdd: _model.CanAddToDeck(_selectedCard),
                showRemove: _isSelectedFromDeck || _layout == DeckEditLayout.OwnedOnly,
                canRemove: _model.CanRemoveFromDeck(_selectedCard));
        }

        private void BindOwnedItem(int index, RectTransform item)
        {
            var card = _availableCards[index];
            bool isSelected = !_isSelectedFromDeck && card == _selectedCard;
            item.GetComponent<DeckEditCardView>().Bind(card, _model.GetAvailableCount(card), isSelected, HandleOwnedCardClicked);
        }

        private void BindDeckItem(int index, RectTransform item)
        {
            var card = _deckCards[index];
            bool isSelected = _isSelectedFromDeck && card == _selectedCard;
            item.GetComponent<DeckEditCardView>().Bind(card, _model.GetDeckCount(card), isSelected, HandleDeckCardClicked);
        }

        private void HandleOwnedCardClicked(CardData card)
        {
            if (_model == null) return;
            _selectedCard = card;
            _isSelectedFromDeck = false;
            SyncSelection();
        }

        private void HandleDeckCardClicked(CardData card)
        {
            if (_model == null) return;
            _selectedCard = card;
            _isSelectedFromDeck = true;
            SyncSelection();
        }

        // 選択の印は両方のリストにまたがるので、両方を描き直す
        private void SyncSelection()
        {
            _window.OwnedList.Refresh();
            _window.DeckList.Refresh();
            SyncSubView();
        }

        private void HandleAdd()
        {
            _model?.TryAddToDeck(_selectedCard);
        }

        private void HandleRemove()
        {
            _model?.TryRemoveFromDeck(_selectedCard);
        }

        // ドラッグで所持→デッキへ1個移す。移したコマンドをサブビューに出す（デッキ側から選んだ扱い）。
        // 入れられない（デッキに入れられる個数の上限）ときは、理由をトーストで伝える
        private void HandleCardDroppedToDeck(CardData card)
        {
            if (_model == null) return;
            if (!_model.CanAddToDeck(card))
            {
                AudioUseCase.PlaySe(SeId.Error);
                if (card.MaxCopiesInDeck > 0 && _model.GetDeckCount(card) >= card.MaxCopiesInDeck)
                {
                    MessageUseCase.ShowMessage($"{LocalizationHelper.GetCardName(card)}はデッキに{card.MaxCopiesInDeck}個までです");
                }
                return;
            }

            AudioUseCase.PlaySe(SeId.MemoryWrite);
            _selectedCard = card;
            _isSelectedFromDeck = true;
            _model.TryAddToDeck(card);
        }

        // ドラッグでデッキ→所持へ1個戻す。戻したコマンドをサブビューに出す（所持側から選んだ扱い）
        private void HandleCardDroppedToOwned(CardData card)
        {
            if (_model == null || !_model.CanRemoveFromDeck(card)) return;

            AudioUseCase.PlaySe(SeId.CardReturn);
            _selectedCard = card;
            _isSelectedFromDeck = false;
            _model.TryRemoveFromDeck(card);
        }

        private void HandleModelChanged()
        {
            Sync();
        }

        // 呼び出し元が組み方を渡さなかったときの割合。攻撃5：防御3：補助2
        private static AutoDeckRule CreateDefaultAutoDeckRule()
        {
            int size = DeckEditConstants.MaxDeckSize;
            int attack = size / 2;
            int defense = size * 3 / 10;
            return new AutoDeckRule(attack, defense, size - attack - defense);
        }
    }
}
