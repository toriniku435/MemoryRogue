using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.Presenters
{
    // シーンに置いた「ウインドウを開くボタン」。押されたら onClick（Initializer が Handler の Show を渡す）を呼び、ラベルを言語に合わせる。
    // ウインドウの Presenter は自分を開くボタンを知らない（ウインドウを生成するようになって、開くのは呼び出し元の役目になった）
    public sealed class WindowOpenButtonPresenter : PresenterBase
    {
        private readonly CommonButton _button;
        private readonly CommonText _label;
        private readonly UiTextKey _labelKey;
        private readonly Action _onClick;
        private readonly Func<bool> _canPress;

        /// <param name="label">ボタンのラベル（無ければ null）</param>
        /// <param name="canPress">押せるか（初期化のときに見る）。null なら常に押せる</param>
        public WindowOpenButtonPresenter(CommonButton button, CommonText label, UiTextKey labelKey, Action onClick, Func<bool> canPress = null)
        {
            _button = button;
            _label = label;
            _labelKey = labelKey;
            _onClick = onClick;
            _canPress = canPress;
        }

        protected override void OnInitialize()
        {
            _button.onClick.AddListener(HandleClick);
            if (_canPress != null) _button.interactable = _canPress.Invoke();
            ApplyLabel();
        }

        protected override void OnDispose()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabel();
        }

        private void ApplyLabel()
        {
            if (_label != null) _label.text = LocalizationHelper.GetUiText(_labelKey);
        }

        private void HandleClick()
        {
            _onClick.Invoke();
        }
    }
}
