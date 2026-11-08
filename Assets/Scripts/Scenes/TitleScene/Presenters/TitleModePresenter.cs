using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.TitleScene.Views;
using UnityEngine.Events;

namespace Live2DCardRogue.TitleScene.Presenters
{
    // モード選択ボタン一式を受け持つPresenter。
    // ラベルを流し込み、押されたモードを報告するだけで、モードごとに何が起きるかは知らない
    public sealed class TitleModePresenter : PresenterBase
    {
        private readonly TitleSceneViewContainer _viewContainer;
        private readonly Action<GameMode> _onModeSelected;

        // 解除するために、購読時に作ったデリゲートをボタンと同じ並びで覚えておく
        private readonly UnityAction[] _clickHandlers;

        public TitleModePresenter(TitleSceneViewContainer viewContainer, Action<GameMode> onModeSelected)
        {
            _viewContainer = viewContainer;
            _onModeSelected = onModeSelected;
            _clickHandlers = new UnityAction[viewContainer.ModeButtons.Count];
        }

        protected override void OnInitialize()
        {
            for (int i = 0; i < _viewContainer.ModeButtons.Count; i++)
            {
                var modeButton = _viewContainer.ModeButtons[i];
                _clickHandlers[i] = () => _onModeSelected.Invoke(modeButton.Mode);
                modeButton.Button.onClick.AddListener(_clickHandlers[i]);
            }

            ApplyLabels();
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _viewContainer.ModeButtons.Count; i++)
            {
                _viewContainer.ModeButtons[i].Button.onClick.RemoveListener(_clickHandlers[i]);
            }
        }

        // ラベルはローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            foreach (var modeButton in _viewContainer.ModeButtons)
            {
                modeButton.SetLabel(LocalizationHelper.GetUiText(ToUiTextKey(modeButton.Mode)));
            }
        }

        // モードを増やしたらここも足す。分岐を1箇所に閉じ込めてあるので、足し忘れれば例外で気付ける
        private static UiTextKey ToUiTextKey(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Campaign: return UiTextKey.Campaign;
                case GameMode.Challenge: return UiTextKey.Challenge;
                case GameMode.Collection: return UiTextKey.Collection;
                case GameMode.Options: return UiTextKey.Options;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, "対応するUIテキストのキーがありません");
            }
        }
    }
}
