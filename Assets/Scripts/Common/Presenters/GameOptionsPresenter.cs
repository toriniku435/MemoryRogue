using System;
using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.Settings;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Common.Presenters
{
    // オプション（タイトル・マップ・バトルで共通。GameOptionsWindowHandler が生成して持つ）。編集は GameOptionsModel に溜め、
    //   反映     : 設定（GameSettingsRepository）へ書き込み、settings.json に保存し、画面モードと言語を当てて閉じる
    //   キャンセル: 変更が無ければそのまま閉じる。変更があれば「破棄しますか？」を確認してから閉じる
    //   デフォルト: 編集中の値を既定値に戻す（言語はそのまま。反映するまで設定は変わらない）
    //   削除     : 確認してから onDeleteUserData を呼ぶ（何を消すかは Initializer が決める）。編集中の値には触らない。
    //              onDeleteUserData を渡さなければ（バトル）、削除の行ごと隠す
    // バトルでは開いている間も戦闘は止めない。反映した戦闘スピードは次の待ち時間から効く
    // 音量だけは、動かした大きさが分かるよう編集中もその音量で鳴らす（AudioUseCase のプレビュー。閉じたら設定の値に戻す）
    public sealed class GameOptionsPresenter : PresenterBase, IWindowPresenter
    {
        private readonly GameOptionsWindowView _window;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action _onDeleteUserData;

        private GameOptionsModel _model;
        private IReadOnlyList<LocaleOption> _locales = new LocaleOption[0];

        /// <param name="confirm">シーンで共用の確認ウインドウ</param>
        /// <param name="onDeleteUserData">ユーザーデータを消す。null なら削除の行を出さない</param>
        public GameOptionsPresenter(GameOptionsWindowView window, WindowHandler<ConfirmShowData> confirm, Action onDeleteUserData = null)
        {
            _window = window;
            _confirm = confirm;
            _onDeleteUserData = onDeleteUserData;
        }

        public bool IsOpen => _window.IsOpen;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _window.OnBgmVolumeChanged += HandleBgmChanged;
            _window.OnSeVolumeChanged += HandleSeChanged;
            _window.OnSpeedChanged += HandleSpeedChanged;
            _window.OnLanguageStep += HandleLanguageStep;
            _window.OnDisplayStep += HandleDisplayStep;
            _window.OnDeleteClicked += HandleDelete;
            _window.OnResetClicked += HandleReset;
            _window.OnCancelRequested += HandleCancel;
            _window.OnApplyClicked += HandleApply;

            _window.SetUserDataVisible(_onDeleteUserData != null);
            _window.SetSpeedRange(GameSettingsRepository.MinBattleSpeed, GameSettingsRepository.MaxBattleSpeed);
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _window.OnBgmVolumeChanged -= HandleBgmChanged;
            _window.OnSeVolumeChanged -= HandleSeChanged;
            _window.OnSpeedChanged -= HandleSpeedChanged;
            _window.OnLanguageStep -= HandleLanguageStep;
            _window.OnDisplayStep -= HandleDisplayStep;
            _window.OnDeleteClicked -= HandleDelete;
            _window.OnResetClicked -= HandleReset;
            _window.OnCancelRequested -= HandleCancel;
            _window.OnApplyClicked -= HandleApply;
            if (_model != null) _model.OnChanged -= Sync;
            AudioUseCase.EndVolumePreview();
        }

        // 反映で言語が変わったときも、ここで文言を読み直す
        protected override void OnLocaleChanged()
        {
            ApplyTexts();
            if (_model != null) Sync();
        }

        private void ApplyTexts()
        {
            _window.SetTexts(new GameOptionsWindowView.OptionsTexts
            {
                Title = LocalizationHelper.GetUiText(UiTextKey.Options),
                Bgm = LocalizationHelper.GetUiText(UiTextKey.BgmVolume),
                Se = LocalizationHelper.GetUiText(UiTextKey.SeVolume),
                Speed = LocalizationHelper.GetUiText(UiTextKey.BattleSpeed),
                Language = LocalizationHelper.GetUiText(UiTextKey.Language),
                Display = LocalizationHelper.GetUiText(UiTextKey.DisplayMode),
                UserData = LocalizationHelper.GetUiText(UiTextKey.UserData),
                Delete = LocalizationHelper.GetUiText(UiTextKey.DeleteUserData),
                Reset = LocalizationHelper.GetUiText(UiTextKey.ResetToDefault),
                Cancel = LocalizationHelper.GetUiText(UiTextKey.Cancel),
                Apply = LocalizationHelper.GetUiText(UiTextKey.Apply),
            });
        }

        /// <summary>開く。開くたびに、今の設定から編集をやり直す</summary>
        public void Open()
        {
            if (_window.IsOpen) return;

            _locales = LocalizationHelper.GetAvailableLocales();
            if (_model != null) _model.OnChanged -= Sync;
            _model = new GameOptionsModel(
                GameSettingsRepository.BgmVolume, GameSettingsRepository.SeVolume, GameSettingsRepository.BattleSpeed,
                _locales.Select(locale => locale.Code).ToList(), LocalizationHelper.GetSelectedLocaleCode(), GameSettingsRepository.Fullscreen);
            _model.OnChanged += Sync;
            Sync();
            _window.Open();
        }

        private void Sync()
        {
            AudioUseCase.PreviewVolumes(_model.BgmVolume, _model.SeVolume);
            _window.SetValues(
                _model.BgmVolume, _model.SeVolume, _model.BattleSpeed,
                LocaleName(_model.LocaleCode),
                LocalizationHelper.GetUiText(_model.Fullscreen ? UiTextKey.Fullscreen : UiTextKey.Windowed),
                canChangeLanguage: _locales.Count > 1);
            _window.SetButtonStates(canReset: !_model.IsDefault, canApply: _model.IsDirty);
        }

        private string LocaleName(string code)
        {
            foreach (var locale in _locales)
                if (locale.Code == code) return locale.Name;
            return code;
        }

        private void HandleBgmChanged(float volume) => _model?.SetBgmVolume(volume);
        private void HandleSeChanged(float volume) => _model?.SetSeVolume(volume);
        private void HandleSpeedChanged(float speed) => _model?.SetBattleSpeed(speed);
        private void HandleLanguageStep(int step) => _model?.StepLocale(step);

        // 画面モードは2つしかないので、どちらの矢印でも入れ替える
        private void HandleDisplayStep(int step)
        {
            if (_model != null) _model.SetFullscreen(!_model.Fullscreen);
        }

        private void HandleReset() => _model?.ResetToDefault();

        private void HandleApply()
        {
            if (_model == null) return;

            GameSettingsRepository.SetBgmVolume(_model.BgmVolume);
            GameSettingsRepository.SetSeVolume(_model.SeVolume);
            GameSettingsRepository.SetBattleSpeed(_model.BattleSpeed);
            GameSettingsRepository.SetLocaleCode(_model.LocaleCode);
            GameSettingsRepository.SetFullscreen(_model.Fullscreen);
            GameSettingsSaveService.Default.Save();
            Close();
            GameSettingsApplier.ApplyDisplay();
            GameSettingsApplier.ApplyLocale();
        }

        // 変更があれば、捨ててよいかを確かめてから閉じる
        private void HandleCancel()
        {
            if (_model == null || !_model.IsDirty)
            {
                Close();
                return;
            }

            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesMessage), null,
                LocalizationHelper.GetUiText(UiTextKey.DiscardChangesOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                Close));
        }

        // 消すものの説明を添えて確かめてから消す。オプションのウインドウは開いたまま
        private void HandleDelete()
        {
            if (_onDeleteUserData == null) return;
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.DeleteUserDataConfirm),
                LocalizationHelper.GetUiText(UiTextKey.DeleteUserDataDetail),
                LocalizationHelper.GetUiText(UiTextKey.DeleteUserDataOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                () =>
                {
                    _onDeleteUserData.Invoke();
                    MessageUseCase.ShowMessage(LocalizationHelper.GetUiText(UiTextKey.UserDataDeleted));
                }));
        }

        public void Close()
        {
            AudioUseCase.EndVolumePreview();
            _window.Close();
        }
    }
}
