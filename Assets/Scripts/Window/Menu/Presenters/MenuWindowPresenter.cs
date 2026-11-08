using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.Menu.Views;

namespace Live2DCardRogue.Window.Menu.Presenters
{
    // ボタンを縦に並べるウインドウ（MenuWindowView）を、渡された MenuWindowInitData のとおりに動かす。
    //   開く   : 呼ぶ側が Handler の Show で（シーンの開くボタンは WindowOpenButtonPresenter）
    //   項目   : 押せなければ何もしない。確認があれば共用の確認ウインドウで尋ね、OK ならメニューを閉じてから実行。
    //            確認が無ければ、メニューを開いたまま実行する（上にウインドウを重ねるもの向け）
    //   閉じる : 閉じるボタンか背面のクリック
    public sealed class MenuWindowPresenter : PresenterBase, IWindowPresenter
    {
        private readonly MenuWindowView _window;
        private readonly MenuWindowInitData _data;
        private readonly WindowHandler<ConfirmShowData> _confirm;

        public MenuWindowPresenter(MenuWindowView window, MenuWindowInitData data)
        {
            _window = window;
            _data = data;
            _confirm = data.Confirm;
        }

        protected override void OnInitialize()
        {
            _window.SetItemCount(_data.Items.Count);
            for (int i = 0; i < _data.Items.Count; i++)
            {
                _window.GetItem(i).SetDanger(_data.Items[i].Style == MenuItemStyle.Danger);
            }
            _window.OnItemClicked += HandleItemClicked;
            _window.OnCloseRequested += Close;
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _window.OnItemClicked -= HandleItemClicked;
            _window.OnCloseRequested -= Close;
        }

        protected override void OnLocaleChanged()
        {
            ApplyTexts();
        }

        public bool IsOpen => _window.IsOpen;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        public void Open()
        {
            if (_window.IsOpen) return;
            RefreshInteractable();
            _window.Open();
        }

        public void Close()
        {
            _window.Close();
        }

        private void ApplyTexts()
        {
            string title = LocalizationHelper.GetUiText(_data.Title);
            _window.SetTexts(title, LocalizationHelper.GetUiText(UiTextKey.Close));
            for (int i = 0; i < _data.Items.Count; i++)
            {
                _window.GetItem(i).SetLabel(LocalizationHelper.GetUiText(_data.Items[i].Label));
            }
        }

        private void RefreshInteractable()
        {
            for (int i = 0; i < _data.Items.Count; i++)
            {
                _window.GetItem(i).SetInteractable(_data.Items[i].CanPress());
            }
        }

        private void HandleItemClicked(int index)
        {
            var item = _data.Items[index];
            // 開いている間に押せなくなっていた（バトルの決着など）
            if (!item.CanPress())
            {
                RefreshInteractable();
                return;
            }

            if (item.Confirm == null || _confirm == null)
            {
                item.OnClicked.Invoke();
                return;
            }

            var confirm = item.Confirm;
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(confirm.Message),
                LocalizationHelper.GetUiText(confirm.Detail),
                LocalizationHelper.GetUiText(confirm.Ok),
                LocalizationHelper.GetUiText(confirm.Cancel),
                () =>
                {
                    Close();
                    // 確かめている間に押せなくなっていたら、何もしない（勝っていたのに負けにはしない）
                    if (item.CanPress()) item.OnClicked.Invoke();
                }));
        }
    }
}
