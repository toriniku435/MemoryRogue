namespace Live2DCardRogue.Common.WindowSystem
{
    // WindowCatalog からプレハブを引くキー。View の型では引かない（敵詳細と敵コレクションのように、同じ View で別のプレハブがあるため）。
    // 値は WindowCatalog.asset に番号で保存されるので、並びを変えずに末尾へ足す
    public enum WindowId
    {
        ConfirmDialog,
        GameOptions,
        Menu,
        CampaignResume,
        EpisodeSelect,
        ChallengeSelect,
        CommandCollection,
        EnemyCollection,
        DeckEdit,
        CardShop,
        UnitUpgrade,
        EnemyDetail,
        FirstBuild,
        StageReward,
        Conversation,
        BattleLog,
        BattlePile,
    }
}
