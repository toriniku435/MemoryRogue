namespace Live2DCardRogue.BattleScene.Models
{
    // 戦闘中に使えるトークン（クラウドバーストのように、トークンを払うコマンド用）と、この戦闘で払った分。
    // 積むときに払い、手札に戻したら返す。リドゥで繰り返すときはもう一度払う。払った分は負けても戻らない
    public sealed class BattleTokenModel
    {
        public BattleTokenModel(int tokens)
        {
            Tokens = tokens < 0 ? 0 : tokens;
        }

        public int Tokens { get; private set; }

        public int Spent { get; private set; }

        public bool CanPay(int cost) => cost <= Tokens;

        public void Pay(int cost)
        {
            if (cost <= 0) return;
            Tokens -= cost;
            Spent += cost;
        }

        public void Refund(int cost)
        {
            if (cost <= 0) return;
            Tokens += cost;
            Spent -= cost;
        }
    }
}
