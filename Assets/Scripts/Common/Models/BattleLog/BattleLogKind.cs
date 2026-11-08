namespace Live2DCardRogue.Common.Models
{
    // バトルログの1行の種類。文章そのものは持たず、表示するときに BattleLogFormatter が組み立てる
    public enum BattleLogKind
    {
        PlayerTurn,
        EnemyTurn,
        CardUsed,
        Damage,
        Heal,
        DamageOverTime,
        HealOverTime,
        StatusGained,
        StatusChanged,
        StatusRemoved,
        // 付与・増減された状態効果の、その時点の値での詳しい説明（StatusGained / StatusChanged の直後に続く）
        StatusDetail,
        Defeated,
        Stunned,
        // プレイヤーのコマンドがパージされた / 次の手札に戻る（捨て札へ行ったときは書かない）
        CardPurged,
        CardReturned,
        // デッキのコマンドがすべてパージされた（敗北）
        DeckExhausted,
    }
}
