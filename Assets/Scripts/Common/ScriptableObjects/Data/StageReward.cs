namespace Live2DCardRogue.Common.ScriptableObjects
{
    // 1戦の結果として受け取るもの。StageData.CreateReward が勝敗に応じて組み立てる。
    // 負けたときも「報酬」ではなく「支援」として同じ形で返すので、受け取る側は勝敗で型を分けなくてよい
    public sealed class StageReward
    {
        public bool IsVictory { get; }

        /// <summary>受け取るトークン</summary>
        public int Tokens { get; }

        /// <summary>受け取るカード。負けたとき（と、ステージにカードが設定されていないとき）は null</summary>
        public CardData Card { get; }

        /// <summary>トークンに上乗せした割合（%。マイニング）。Tokens はもう上乗せした後の額</summary>
        public int BonusPercent { get; }

        public StageReward(bool isVictory, int tokens, CardData card, int bonusPercent = 0)
        {
            IsVictory = isVictory;
            Tokens = tokens < 0 ? 0 : tokens;
            Card = card;
            BonusPercent = bonusPercent < 0 ? 0 : bonusPercent;
        }

        /// <summary>トークンを percent % 増やした報酬を返す（端数は切り捨て）。0 以下ならそのまま</summary>
        public StageReward WithTokenBonus(int percent)
        {
            if (percent <= 0) return this;

            // 大きな額に大きな割合を掛けてもあふれないよう、long で計算して int の上限で止める
            long tokens = Tokens + (long)Tokens * percent / 100;
            long bonus = (long)BonusPercent + percent;
            return new StageReward(IsVictory, tokens > int.MaxValue ? int.MaxValue : (int)tokens, Card, bonus > int.MaxValue ? int.MaxValue : (int)bonus);
        }
    }
}
