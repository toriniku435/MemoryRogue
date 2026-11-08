using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.MapScene.Presenters
{
    // マップの「チュートリアル」ボタン（いつでもチュートリアルを見直せる）。押されたことを報告するだけ
    public sealed class MapTutorialButtonPresenter : PresenterBase
    {
        private readonly CommonButton _button;
        private readonly CommonText _label;
        private readonly Action _onClicked;

        public MapTutorialButtonPresenter(CommonButton button, CommonText label, Action onClicked)
        {
            _button = button;
            _label = label;
            _onClicked = onClicked;
        }

        protected override void OnInitialize()
        {
            _button.onClick.AddListener(HandleClicked);
            ApplyLabel();
        }

        protected override void OnDispose()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabel();
        }

        private void ApplyLabel()
        {
            _label.text = LocalizationHelper.GetUiText(UiTextKey.Tutorial);
        }

        private void HandleClicked()
        {
            _onClicked.Invoke();
        }
    }
}
