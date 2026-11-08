using System;
using Live2DCardRogue.Common.Save;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 中断したデータの「再開 / 破棄 / キャンセル」。開くときに、そのセーブデータを渡す
    public sealed class CampaignResumeInitData : WindowInitData<CampaignResumeShowData>
    {
        public override WindowId Id => WindowId.CampaignResume;

        /// <summary>破棄を確かめる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>再開すると決めた</summary>
        public Action<CampaignSaveData> OnResume { get; }

        /// <summary>破棄すると決めた（確かめた後）</summary>
        public Action<CampaignSaveData> OnDiscard { get; }

        public CampaignResumeInitData(WindowHandler<ConfirmShowData> confirm, Action<CampaignSaveData> onResume, Action<CampaignSaveData> onDiscard)
        {
            Confirm = confirm;
            OnResume = onResume;
            OnDiscard = onDiscard;
        }
    }
}
