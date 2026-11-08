namespace Live2DCardRogue.Common.Models
{
    // 敵の強さの上乗せ（エンドレスで、階層が深くなるほど敵を強くするため）。
    // 戦闘はこれを受け取って当てはめるだけで、どのモードの、どの階層の値なのかは知らない
    public sealed class EnemyStrength
    {
        /// <summary>上乗せ無し（ストーリー）</summary>
        public static readonly EnemyStrength None = new EnemyStrength(100, 0);

        /// <summary>最大HPの倍率（%。100 でそのまま）</summary>
        public int HpPercent { get; }

        /// <summary>与える攻撃ダメージに足す値（階層ブーストの状態効果として付ける）</summary>
        public int AttackBonus { get; }

        public EnemyStrength(int hpPercent, int attackBonus)
        {
            HpPercent = hpPercent < 1 ? 1 : hpPercent;
            AttackBonus = attackBonus < 0 ? 0 : attackBonus;
        }

        /// <summary>上乗せした最大HP（端数は切り捨て、1 以上）</summary>
        public int ApplyToMaxHp(int maxHp)
        {
            long scaled = (long)maxHp * HpPercent / 100;
            if (scaled < 1) return 1;
            return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
        }
    }
}
