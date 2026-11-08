namespace Live2DCardRogue.Common.Models.EnemyCardPickers
{
    /// <summary>
    /// 敵が1ターン分の行動を決めるときの戦況（決めるのはプレイヤーの番の頭）。
    /// 使わない Picker は無視してよい。default は「HP満タン・味方なし・1ターン目」として扱う
    /// </summary>
    public readonly struct EnemyPickContext
    {
        public int CurrentHp { get; }
        public int MaxHp { get; }

        /// <summary>自分以外の生きている敵の数</summary>
        public int LivingAllyCount { get; }

        /// <summary>何ターン目か（1始まり）。0 は不明</summary>
        public int TurnNumber { get; }

        /// <summary>生きている味方がいないまま終えた、自分のターンの数（味方がいるターンを終えると 0 に戻る）</summary>
        public int TurnsAlone { get; }

        public EnemyPickContext(int currentHp, int maxHp, int livingAllyCount, int turnNumber, int turnsAlone)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
            LivingAllyCount = livingAllyCount;
            TurnNumber = turnNumber;
            TurnsAlone = turnsAlone;
        }

        /// <summary>残りHPの割合（%）。最大HPが分からなければ 100</summary>
        public int HpPercent => MaxHp <= 0 ? 100 : CurrentHp * 100 / MaxHp;
    }
}
