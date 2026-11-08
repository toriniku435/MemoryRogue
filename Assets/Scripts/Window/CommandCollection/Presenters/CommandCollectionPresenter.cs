using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Window.CommandCollection.Models;
using Live2DCardRogue.Window.CommandCollection.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.CommandCollection.Presenters
{
    // コマンドのコレクションのウインドウを受け持つPresenter。
    // 開くたびに、手に入れたことがあるコマンドを聞き直してモデルを作り直す（ストーリーやエンドレスで増えた分が反映されるように）。
    // 絞り込みのタブ（すべて / チップごと）でリストを並べ直し、カーソルを合わせたコマンドを拡大表示に出す。
    // 手に入れたことが無いコマンドは、リストでも拡大表示でも「？」のまま（View が伏せる）
    public sealed class CommandCollectionPresenter : PresenterBase, IWindowPresenter
    {
        private readonly CommandCollectionWindowView _view;
        private readonly CommandCollectionData _data;
        private readonly Func<ISet<string>> _getDiscoveredIds;

        private CommandCollectionModel _model;
        // 拡大表示に出しているコマンド（まだ無ければ null）
        private CardData _selected;

        /// <param name="getDiscoveredIds">一度でも手に入れたことがあるコマンドの ID（開くたびに聞き直す）</param>
        public CommandCollectionPresenter(CommandCollectionWindowView view, CommandCollectionData data, Func<ISet<string>> getDiscoveredIds)
        {
            _view = view;
            _data = data;
            _getDiscoveredIds = getDiscoveredIds;
        }

        protected override void OnInitialize()
        {
            _view.OnFilterClicked += HandleFilterClicked;
            _view.OnCloseRequested += Close;
            _view.List.Initialize(BindItem);
            // タブは最初に作っておく（ウインドウを開く前に並べておけば、開いたときのレイヤーの切り替えにそのまま乗る）
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _view.OnFilterClicked -= HandleFilterClicked;
            _view.OnCloseRequested -= Close;
            _view.List.Dispose();
            if (_model != null) _model.OnChanged -= HandleModelChanged;
        }

        protected override void OnLocaleChanged()
        {
            ApplyTexts();
            if (_model == null) return;
            _view.List.Refresh();
            if (_selected != null) _view.Detail.Show(_selected, _model.IsDiscovered(_selected));
        }

        public bool IsOpen => _view.IsOpen;

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        public void Open()
        {
            if (_model != null) _model.OnChanged -= HandleModelChanged;
            _model = CreateModel();
            _model.OnChanged += HandleModelChanged;
            _selected = null;

            ApplyTexts();
            _view.Detail.Hide();
            _view.Open();
            HandleModelChanged();
        }

        private CommandCollectionModel CreateModel()
        {
            if (_data == null)
            {
                Debug.LogError("TitleSceneInitializer に CommandCollectionData が設定されていません");
                return new CommandCollectionModel(new CardData[0], null, null);
            }

            var packFilters = new List<IReadOnlyList<CardData>>();
            foreach (var pack in _data.Packs) packFilters.Add(CommandCollectionData.CardsOf(pack));
            return new CommandCollectionModel(_data.CreateAllCards(), packFilters, _getDiscoveredIds.Invoke());
        }

        // 見出し・閉じる・タブの名前（すべて + チップの名前）と、手に入れた数
        private void ApplyTexts()
        {
            _view.SetTexts(
                LocalizationHelper.GetUiText(UiTextKey.CommandCollection),
                LocalizationHelper.GetUiText(UiTextKey.CollectionCommands),
                LocalizationHelper.GetUiText(UiTextKey.Close));

            var labels = new List<string> { LocalizationHelper.GetUiText(UiTextKey.CollectionFilterAll) };
            if (_data != null)
            {
                foreach (var pack in _data.Packs) labels.Add(LocalizationHelper.GetCardPackName(pack));
            }
            _view.SetFilters(labels);
            _view.SetSelectedFilter(_model != null ? _model.SelectedFilter : 0);

            if (_model != null)
            {
                _view.SetProgress(string.Format(LocalizationHelper.GetUiText(UiTextKey.CollectionProgress), _model.DiscoveredCount, _model.TotalCount));
            }
        }

        private void HandleFilterClicked(int index)
        {
            _model?.SelectFilter(index);
        }

        // 絞り込みが変わった（開いたときも）: タブの色とリストを並べ直し、先頭から見せる
        private void HandleModelChanged()
        {
            _view.SetSelectedFilter(_model.SelectedFilter);
            _view.SetProgress(string.Format(LocalizationHelper.GetUiText(UiTextKey.CollectionProgress), _model.DiscoveredCount, _model.TotalCount));
            _view.List.SetItemCount(_model.Entries.Count);
            _view.List.ScrollToStart();
        }

        private void BindItem(int index, RectTransform item)
        {
            if (_model == null || index < 0 || index >= _model.Entries.Count) return;
            var entry = _model.Entries[index];
            item.GetComponent<CardListItemView>().Bind(entry.Card, entry.IsDiscovered, entry.Card == _selected, HandleHovered);
        }

        // カーソルを合わせた: 拡大表示に出し（手に入れたことが無ければ伏せたまま）、リストの印も付け替える
        private void HandleHovered(CardData card)
        {
            if (card == null || card == _selected || _model == null) return;
            _selected = card;
            _view.Detail.Show(card, _model.IsDiscovered(card));
            _view.List.Refresh();
        }

        public void Close()
        {
            _view.Close();
        }
    }
}
