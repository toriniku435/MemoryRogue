namespace Live2DCardRogue.Common.Models
{
    // ユニット強化の決まり（1段の上がり幅・上限・値段）。
    // 数値は CampaignBalance（ScriptableObject）が持ち、CreateUnitUpgradeRule でこの形にして渡す。
    // 次の1段の値段は「基礎 + 増分 × 今の段」
    public sealed class UnitUpgradeRule
    {
        public int HpStep { get; }
        public int HpLimit { get; }
        public int HpBaseCost { get; }
        public int HpCostPerLevel { get; }

        public int MemoryStep { get; }
        public int MemoryLimit { get; }
        public int MemoryBaseCost { get; }
        public int MemoryCostPerLevel { get; }

        public UnitUpgradeRule(
            int hpStep, int hpLimit, int hpBaseCost, int hpCostPerLevel,
            int memoryStep, int memoryLimit, int memoryBaseCost, int memoryCostPerLevel)
        {
            HpStep = hpStep < 1 ? 1 : hpStep;
            HpLimit = hpLimit;
            HpBaseCost = hpBaseCost;
            HpCostPerLevel = hpCostPerLevel;
            MemoryStep = memoryStep < 1 ? 1 : memoryStep;
            MemoryLimit = memoryLimit;
            MemoryBaseCost = memoryBaseCost;
            MemoryCostPerLevel = memoryCostPerLevel;
        }
    }
}
