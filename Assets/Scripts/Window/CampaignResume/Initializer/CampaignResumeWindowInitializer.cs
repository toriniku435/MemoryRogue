using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.CampaignResume.Presenters;
using Live2DCardRogue.Window.CampaignResume.Views;

namespace Live2DCardRogue.Window.CampaignResume.Initializer
{
    // 中断したデータの「再開 / 破棄 / キャンセル」（CampaignResumeDialog.prefab）のコンポジションルート
    public sealed class CampaignResumeWindowInitializer : WindowInitializerBase<CampaignResumeInitData, CampaignResumePresenter>
    {
        [SerializeField] private CampaignResumeDialogView _view;

        protected override CampaignResumePresenter Initialize(CampaignResumeInitData data)
        {
            return new CampaignResumePresenter(_view, data.Confirm, data.OnResume, data.OnDiscard);
        }
    }
}
