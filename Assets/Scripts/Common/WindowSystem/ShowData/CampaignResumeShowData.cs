using Live2DCardRogue.Common.Save;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 中断したデータの「再開 / 破棄 / キャンセル」を開くときに渡すもの
    public sealed class CampaignResumeShowData : WindowShowData
    {
        public CampaignSaveData SaveData { get; }

        public CampaignResumeShowData(CampaignSaveData saveData)
        {
            SaveData = saveData;
        }
    }
}
