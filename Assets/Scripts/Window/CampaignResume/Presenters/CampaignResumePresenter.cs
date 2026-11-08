using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Window.CampaignResume.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.CampaignResume.Presenters
{
    // 中断したキャンペーン（ストーリー・チャレンジ）の「再開 / 破棄 / キャンセル」ウインドウを受け持つPresenter。
    // 中断したところを見せ、選ばれたものを報告する。キャンセルなら閉じるだけ。
    // 破棄は取り消せないので、共通の確認ウインドウで「本当に破棄しますか？」と一度確かめる。
    // そこでキャンセルしたら、3択のウインドウに戻る
    public sealed class CampaignResumePresenter : PresenterBase, IWindowPresenter<CampaignResumeShowData>
    {
        private readonly CampaignResumeDialogView _view;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action<CampaignSaveData> _onResume;
        private readonly Action<CampaignSaveData> _onDiscard;
        private CampaignSaveData _data;

        public CampaignResumePresenter(CampaignResumeDialogView view, WindowHandler<ConfirmShowData> confirm, Action<CampaignSaveData> onResume, Action<CampaignSaveData> onDiscard)
        {
            _view = view;
            _confirm = confirm;
            _onResume = onResume;
            _onDiscard = onDiscard;
        }

        protected override void OnInitialize()
        {
            _view.OnResumeClicked += HandleResume;
            _view.OnDiscardClicked += HandleDiscard;
            _view.OnCancelRequested += Close;
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _view.OnResumeClicked -= HandleResume;
            _view.OnDiscardClicked -= HandleDiscard;
            _view.OnCancelRequested -= Close;
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        public bool IsOpen => _view.IsOpen;

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        public void Open(CampaignResumeShowData showData)
        {
            var data = showData.SaveData;
            _data = data;
            ApplyLabels();
            _view.Open(Summary(data));
        }

        // 中断したところを1行で見せる。チャレンジは名前を添え、終わりの無いもの（エンドレス）は今の階層だけ
        private static string Summary(CampaignSaveData data)
        {
            // 1始まりにする +1 は long で（セーブの値が int の上限でもあふれないように）
            long next = (long)data.stageIndex + 1;
            string layer = !data.isEndless || data.stageCount > 0
                ? $"LAYER {next:00} / {data.stageCount:00}"
                : $"LAYER {next:00}";
            string challenge = data.isEndless ? LocalizationHelper.GetChallengeName(CampaignSaveSnapshot.ChallengeTypeOf(data)) + "   " : "";
            return $"{challenge}{layer}   TKN {data.tokens}   HP {data.maxHp}   MEM {data.maxMemory}";
        }

        private void ApplyLabels()
        {
            bool endless = _data != null && _data.isEndless;
            _view.SetTexts(
                LocalizationHelper.GetUiText(endless ? UiTextKey.ChallengeResumeMessage : UiTextKey.CampaignResumeMessage),
                LocalizationHelper.GetUiText(UiTextKey.Resume),
                LocalizationHelper.GetUiText(UiTextKey.Discard),
                LocalizationHelper.GetUiText(UiTextKey.Cancel));
        }

        private void HandleResume()
        {
            _view.Close();
            _onResume.Invoke(_data);
        }

        // すぐには破棄せず、確かめる
        private void HandleDiscard()
        {
            _view.Close();
            // 破棄をやめたら、3択に戻る
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(_data.isEndless ? UiTextKey.ChallengeDiscardConfirmMessage : UiTextKey.DiscardConfirmMessage),
                LocalizationHelper.GetUiText(_data.isEndless ? UiTextKey.ChallengeDiscardConfirmDetail : UiTextKey.DiscardConfirmDetail),
                LocalizationHelper.GetUiText(UiTextKey.DiscardConfirmOk),
                LocalizationHelper.GetUiText(UiTextKey.Cancel),
                () => _onDiscard.Invoke(_data),
                () => _view.Open(Summary(_data))));
        }

        public void Close()
        {
            _view.Close();
        }
    }
}
