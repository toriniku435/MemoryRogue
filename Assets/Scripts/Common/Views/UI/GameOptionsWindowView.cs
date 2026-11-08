using System;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // オプションのウインドウ（Assets/Prefabs/GameOptionsWindow.prefab。タイトルとバトルで同じものを使う）。
    // BGM・効果音・戦闘スピードのスライダー、言語・画面モードの切り替え、ユーザーデータの削除（タイトルだけ。バトルでは行ごと隠す）と、
    // 下に「デフォルトに戻す / キャンセル / 反映」。開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition に任せる。
    // 操作を伝えるだけで、値を覚えるのも閉じるのも Presenter（キャンセルと削除の確認もそちら）
    public sealed class GameOptionsWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;

        [Header("スライダー（整数の目盛り。1 目盛り = Step）")]
        [SerializeField] private SliderRow _bgmRow;
        [SerializeField] private SliderRow _seRow;
        [SerializeField] private SliderRow _speedRow;
        [SerializeField][Min(0.01f)] private float _volumeStep = 0.05f;
        [SerializeField][Min(0.01f)] private float _speedStep = 0.25f;

        [Header("切り替え")]
        [SerializeField] private OptionSelectorView _languageSelector;
        [SerializeField] private OptionSelectorView _displaySelector;

        [Header("ユーザーデータ（隠すと、その行の分だけウインドウを縮める）")]
        [SerializeField] private RectTransform _window;
        [SerializeField] private GameObject _userDataRow;
        [SerializeField][Min(0f)] private float _userDataRowHeight = 80f;
        [SerializeField] private CommonText _userDataLabel;
        [SerializeField] private CommonButton _deleteButton;
        [SerializeField] private CommonText _deleteLabel;

        [Header("ボタン")]
        [SerializeField] private CommonButton _resetButton;
        [SerializeField] private CommonText _resetLabel;
        [SerializeField] private CommonButton _cancelButton;
        [SerializeField] private CommonText _cancelLabel;
        [SerializeField] private CommonButton _applyButton;
        [SerializeField] private CommonText _applyLabel;

        [Serializable]
        private sealed class SliderRow
        {
            public CommonText Label;
            public CommonText Value;
            public Slider Slider;
        }

        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSeVolumeChanged;
        public event Action<float> OnSpeedChanged;
        /// <summary>言語の矢印（-1 / +1）</summary>
        public event Action<int> OnLanguageStep;
        /// <summary>画面モードの矢印（-1 / +1）</summary>
        public event Action<int> OnDisplayStep;
        public event Action OnDeleteClicked;
        public event Action OnResetClicked;
        /// <summary>キャンセルボタンが押された、または背面がクリックされた</summary>
        public event Action OnCancelRequested;
        public event Action OnApplyClicked;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private float _fullHeight = -1f;

        private void Awake()
        {
            SetRange(_bgmRow.Slider, 0f, 1f, _volumeStep);
            SetRange(_seRow.Slider, 0f, 1f, _volumeStep);
            _bgmRow.Slider.onValueChanged.AddListener(HandleBgmChanged);
            _seRow.Slider.onValueChanged.AddListener(HandleSeChanged);
            _speedRow.Slider.onValueChanged.AddListener(HandleSpeedChanged);
            _languageSelector.OnStep += HandleLanguageStep;
            _displaySelector.OnStep += HandleDisplayStep;
            _deleteButton.onClick.AddListener(HandleDelete);
            _resetButton.onClick.AddListener(HandleReset);
            _cancelButton.onClick.AddListener(HandleCancel);
            _applyButton.onClick.AddListener(HandleApply);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _bgmRow.Slider.onValueChanged.RemoveListener(HandleBgmChanged);
            _seRow.Slider.onValueChanged.RemoveListener(HandleSeChanged);
            _speedRow.Slider.onValueChanged.RemoveListener(HandleSpeedChanged);
            _languageSelector.OnStep -= HandleLanguageStep;
            _displaySelector.OnStep -= HandleDisplayStep;
            _deleteButton.onClick.RemoveListener(HandleDelete);
            _resetButton.onClick.RemoveListener(HandleReset);
            _cancelButton.onClick.RemoveListener(HandleCancel);
            _applyButton.onClick.RemoveListener(HandleApply);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(OptionsTexts texts)
        {
            _titleText.text = texts.Title;
            _bgmRow.Label.text = texts.Bgm;
            _seRow.Label.text = texts.Se;
            _speedRow.Label.text = texts.Speed;
            _languageSelector.SetLabel(texts.Language);
            _displaySelector.SetLabel(texts.Display);
            _userDataLabel.text = texts.UserData;
            _deleteLabel.text = texts.Delete;
            _resetLabel.text = texts.Reset;
            _cancelLabel.text = texts.Cancel;
            _applyLabel.text = texts.Apply;
        }

        /// <summary>
        /// ユーザーデータの削除の行を見せるか（バトルでは隠す）。隠すと、その行の分だけウインドウの高さを縮める
        /// （行は上から、ボタンは下から並べてあるので、縮めれば間が詰まる）
        /// </summary>
        public void SetUserDataVisible(bool visible)
        {
            if (_userDataRow != null) _userDataRow.SetActive(visible);
            if (_window == null) return;
            if (_fullHeight < 0f) _fullHeight = _window.sizeDelta.y;
            _window.sizeDelta = new Vector2(_window.sizeDelta.x, visible ? _fullHeight : _fullHeight - _userDataRowHeight);
        }

        /// <summary>戦闘スピードのスライダーの範囲（倍率）</summary>
        public void SetSpeedRange(float min, float max)
        {
            SetRange(_speedRow.Slider, min, max, _speedStep);
        }

        /// <summary>今の値を見せる（スライダーを動かしたことにはしない）</summary>
        public void SetValues(float bgm, float se, float speed, string language, string display, bool canChangeLanguage, bool canChangeDisplay)
        {
            _bgmRow.Slider.SetValueWithoutNotify(Mathf.Round(bgm / _volumeStep));
            _bgmRow.Value.text = $"{Mathf.RoundToInt(bgm * 100)}%";
            _seRow.Slider.SetValueWithoutNotify(Mathf.Round(se / _volumeStep));
            _seRow.Value.text = $"{Mathf.RoundToInt(se * 100)}%";
            _speedRow.Slider.SetValueWithoutNotify(Mathf.Round(speed / _speedStep));
            _speedRow.Value.text = $"x{speed:0.00}";
            _languageSelector.SetValue(language);
            _languageSelector.SetInteractable(canChangeLanguage);
            _displaySelector.SetValue(display);
            _displaySelector.SetInteractable(canChangeDisplay);
        }

        /// <summary>既定値のときは「デフォルトに戻す」を押せなくする。変更が無ければ「反映」も押せない</summary>
        public void SetButtonStates(bool canReset, bool canApply)
        {
            _resetButton.interactable = canReset;
            _applyButton.interactable = canApply;
        }

        public void Open()
        {
            _dialog.PlayOpen();
        }

        public void Close()
        {
            _dialog.PlayClose(null);
        }

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void HideImmediate()
        {
            _dialog.HideImmediate();
        }

        private static void SetRange(Slider slider, float min, float max, float step)
        {
            slider.wholeNumbers = true;
            slider.minValue = Mathf.Round(min / step);
            slider.maxValue = Mathf.Round(max / step);
        }

        private void HandleBgmChanged(float value)
        {
            if (IsOpen) OnBgmVolumeChanged?.Invoke(value * _volumeStep);
        }

        // 効果音は、動かした大きさがその場で分かるように試しに鳴らす
        private void HandleSeChanged(float value)
        {
            if (!IsOpen) return;
            OnSeVolumeChanged?.Invoke(value * _volumeStep);
            AudioUseCase.PlaySe(SeId.Click);
        }

        private void HandleSpeedChanged(float value)
        {
            if (IsOpen) OnSpeedChanged?.Invoke(value * _speedStep);
        }

        private void HandleLanguageStep(int step)
        {
            if (IsOpen) OnLanguageStep?.Invoke(step);
        }

        private void HandleDisplayStep(int step)
        {
            if (IsOpen) OnDisplayStep?.Invoke(step);
        }

        private void HandleDelete()
        {
            if (IsOpen) OnDeleteClicked?.Invoke();
        }

        private void HandleReset()
        {
            if (IsOpen) OnResetClicked?.Invoke();
        }

        private void HandleCancel()
        {
            if (IsOpen) OnCancelRequested?.Invoke();
        }

        private void HandleApply()
        {
            if (IsOpen) OnApplyClicked?.Invoke();
        }

        // 背面のクリックはボタンと違って自分では鳴らないので、取り消しの音をここで鳴らす
        private void HandleBackgroundClicked()
        {
            if (!IsOpen) return;
            AudioUseCase.PlaySe(SeId.Cancel);
            OnCancelRequested?.Invoke();
        }

        /// <summary>ウインドウの文言（Presenter がローカライズして渡す）</summary>
        public struct OptionsTexts
        {
            public string Title;
            public string Bgm;
            public string Se;
            public string Speed;
            public string Language;
            public string Display;
            public string UserData;
            public string Delete;
            public string Reset;
            public string Cancel;
            public string Apply;
        }
    }
}
