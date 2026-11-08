namespace Live2DCardRogue.Common.Repositories
{
    // キャンペーン中のプレイヤーユニットの強さ（最大HPとメモリー上限）。ユニット強化で上がり、戦闘の開始時に使う。
    // 寿命はキャンペーン1回分で、タイトルからキャンペーンを始めるたびに Clear され、マップが初期値を配り直す。
    // 戦闘ごとに満タンから始まるので、持つのは上限だけ（今のHPは持たない）
    // ファイルへの保存と再開は CampaignSaveService が、ほかのキャンペーンのリポジトリとまとめて行う
    public static class CampaignUnitRepository
    {
        public static int MaxHp { get; private set; }
        public static int MaxMemory { get; private set; }

        /// <summary>このキャンペーンで初期値を配り終えたか</summary>
        public static bool IsInitialized { get; private set; }

        public static void Initialize(int maxHp, int maxMemory)
        {
            MaxHp = maxHp;
            MaxMemory = maxMemory;
            IsInitialized = true;
        }

        public static void Set(int maxHp, int maxMemory)
        {
            MaxHp = maxHp;
            MaxMemory = maxMemory;
        }

        public static void Clear()
        {
            MaxHp = 0;
            MaxMemory = 0;
            IsInitialized = false;
        }
    }
}
