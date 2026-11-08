namespace Live2DCardRogue.Common.Repositories
{
    // キャンペーンの進行状況。シーンをまたいで残り続ける唯一の状態で、
    // マップ→戦闘→マップと往復しても渡し直さなくてよいようにここが持つ。
    //
    // staticなのは「誰が持つか」を決めずに済ませるため。シーンのオブジェクトに持たせると
    // 戦闘シーンへ移った時点で消えるので、持ち主を増やさないこの形にしている。
    // ファイルへの保存と再開は CampaignSaveService が、ほかのキャンペーンのリポジトリとまとめて行う
    public static class CampaignProgressRepository
    {
        /// <summary>遊んでいるエピソード（0始まり。0 がエピソード1）。クリアの記録に、どのエピソードかを残すため</summary>
        public static int EpisodeIndex { get; private set; }

        /// <summary>今いるマスの位置（0始まり）。全ステージを踏破すると ステージ数 と同じ値になる</summary>
        public static int CurrentStageIndex { get; private set; }

        /// <summary>バックアップブースト（敗北時の支援）を受けた回数。リザルトのスコアで減点になる</summary>
        public static int BackupBoostCount { get; private set; }

        /// <summary>そのエピソード（0始まり）の最初のマスから始め直す</summary>
        public static void Reset(int episodeIndex = 0)
        {
            EpisodeIndex = episodeIndex < 0 ? 0 : episodeIndex;
            CurrentStageIndex = 0;
            BackupBoostCount = 0;
        }

        /// <summary>保存した進捗から再開する</summary>
        public static void Restore(int stageIndex, int backupBoostCount, int episodeIndex = 0)
        {
            EpisodeIndex = episodeIndex < 0 ? 0 : episodeIndex;
            CurrentStageIndex = stageIndex < 0 ? 0 : (stageIndex > MaxStageIndex ? MaxStageIndex : stageIndex);
            BackupBoostCount = backupBoostCount < 0 ? 0 : backupBoostCount;
        }

        /// <summary>負けて支援を受けたときに呼ぶ</summary>
        public static void RecordBackupBoost()
        {
            BackupBoostCount++;
        }

        /// <summary>進める一番先のマス。表示の「マス + 1」（1始まりの階層）が int に収まるよう、上限の1つ手前で止める</summary>
        public const int MaxStageIndex = int.MaxValue - 1;

        /// <summary>1マス進む。勝利したときだけ呼ぶ（MaxStageIndex より先へは進まない）</summary>
        public static void AdvanceStage()
        {
            if (CurrentStageIndex < MaxStageIndex) CurrentStageIndex++;
        }

        /// <summary>全ステージを踏破したか</summary>
        public static bool IsCompleted(int stageCount)
        {
            return CurrentStageIndex >= stageCount;
        }
    }
}
