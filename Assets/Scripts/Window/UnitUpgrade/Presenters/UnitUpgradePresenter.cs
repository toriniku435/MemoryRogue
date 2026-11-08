using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.UnitUpgrade.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.UnitUpgrade.Presenters
{
    // マップの「ユニット強化」ボタンと、それで開くウインドウを受け持つPresenter。
    // 上げられるか・いくらかはすべてモデルに聞く。強化ボタンで上げた分は仮のもので、反映で確定する。
    // 確定したら onUpgraded で結果（残りトークン・最大HP・メモリー上限）を伝え、
    // 保存先への書き戻しは Initializer が行う（この Presenter はリポジトリを知らない）。
    // 確定していない強化があるのにキャンセル（背面のクリックも）したら、捨ててよいかを確認ウインドウで確かめる
    public sealed class UnitUpgradePresenter : PresenterBase, IWindowPresenter
    {
        private readonly UnitUpgradeModel _model;
        private readonly UnitUpgradeWindowView _window;
        private readonly TokenCounterView _mapTokenCounter;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action<int, int, int> _onUpgraded;

        /// <param name="mapTokenCounter">マップのトークン表示。確定して減った分を数え下げる</param>
        /// <param name="confirm">変更を捨ててよいかの確認（マップで共用）</param>
        /// <param name="onUpgraded">確定した（残りトークン, 最大HP, メモリー上限）</param>
        public UnitUpgradePresenter(UnitUpgradeModel model, UnitUpgradeWindowView window,
            TokenCounterView mapTokenCounter, WindowHandler<ConfirmShowData> confirm, Action<int, int, int> onUpgraded)
        {
            _model = model;
            _window = window;
            _mapTokenCounter = mapTokenCounter;
            _confirm = confirm;
            _onUpgraded = onUpgraded;
        }

        public bool IsOpen => _window.IsOpen;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _window.OnCloseRequested += HandleCloseRequested;
            _window.OnApplyClicked += HandleApply;
            _window.HpRow.OnUpgradeClicked += HandleUpgradeHp;
            _window.MemoryRow.OnUpgradeClicked += HandleUpgradeMemory;
            _model.OnChanged += HandleModelChanged;
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _window.OnCloseRequested -= HandleCloseRequested;
            _window.OnApplyClicked -= HandleApply;
            _window.HpRow.OnUpgradeClicked -= HandleUpgradeHp;
            _window.MemoryRow.OnUpgradeClicked -= HandleUpgradeMemory;
            _model.OnChanged -= HandleModelChanged;
        }

        // ラベルはローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            string title = LocalizationHelper.GetUiText(UiTextKey.UnitUpgrade);
            string upgrade = LocalizationHelper.GetUiText(UiTextKey.UpgradeButton);
            _window.SetTexts(title, LocalizationHelper.GetUiText(UiTextKey.Tokens),
                LocalizationHelper.GetUiText(UiTextKey.Cancel), LocalizationHelper.GetUiText(UiTextKey.Apply));
            _window.HpRow.SetTexts(LocalizationHelper.GetUiText(UiTextKey.UpgradeHp), upgrade);
            _window.MemoryRow.SetTexts(LocalizationHelper.GetUiText(UiTextKey.UpgradeMemory), upgrade);
        }

        // 開くたびに確定した値から始める（前回キャンセルした強化は残らない）
        public void Open()
        {
            if (_window.IsOpen) return;
            _model.Discard();
            _window.TokenCounter.SetValue(_model.Tokens);
            Sync();
            _window.Open();
        }

        public void Close()
        {
            _window.Close();
        }

        private void HandleUpgradeHp()
        {
            if (_model.TryUpgradeHp()) _window.HpRow.PlayUpgraded();
        }

        private void HandleUpgradeMemory()
        {
            if (_model.TryUpgradeMemory()) _window.MemoryRow.PlayUpgraded();
        }

        // 確定して閉じる。マップのトークン表示はここで数え下げる
        private void HandleApply()
        {
            if (!_model.Apply()) return;

            _mapTokenCounter.CountTo(_model.AppliedTokens);
            _onUpgraded.Invoke(_model.AppliedTokens, _model.AppliedHp, _model.AppliedMemory);
            _window.Close();
        }

        // 確定していない強化があれば、捨ててよいかを確かめてから閉じる
        private void HandleCloseRequested()
        {
            if (!_model.IsDirty)
            {
                _window.Close();
                return;
            }

            // 確認をやめたら、強化のウインドウに戻る（仮に上げた分はそのまま）
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesMessage),
                null,
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesOk),
                LocalizationHelper.GetUiText(UiTextKey.Back),
                HandleDiscardConfirmed));
        }

        private void HandleDiscardConfirmed()
        {
            _model.Discard();
            _window.Close();
        }

        // 上げたら（戻したら）、両方の項目（片方で払ったぶん、もう片方も払えなくなることがある）とウインドウのトークン表示を塗り直す
        private void HandleModelChanged()
        {
            Sync();
            _window.TokenCounter.CountTo(_model.Tokens);
        }

        private void Sync()
        {
            _window.HpRow.Show(_model.Hp, _model.NextHp, _model.IsHpMaxed,
                _model.HpLevel, _model.HpMaxLevel, _model.HpCost, _model.CanUpgradeHp);
            _window.MemoryRow.Show(_model.Memory, _model.NextMemory, _model.IsMemoryMaxed,
                _model.MemoryLevel, _model.MemoryMaxLevel, _model.MemoryCost, _model.CanUpgradeMemory);
            _window.SetApplyInteractable(_model.IsDirty);
        }
    }
}
