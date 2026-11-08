namespace Live2DCardRogue.Common.Repositories
{
    // キャンペーン中に持っているトークン（カード購入に使う通貨）。寿命はキャンペーン1回分で、
    // タイトルからキャンペーンを始めるたびに Clear され、マップが初期値を配り直す。
    //
    // ステージの報酬で増え、カード購入画面で減る。購入画面へは「渡して、残りを返してもらう」形にしてあるので、
    // あの画面はここを知らない（CampaignDeckRepository と同じ考え方）。
    // ファイルへの保存と再開は CampaignSaveService が、ほかのキャンペーンのリポジトリとまとめて行う
    public static class CampaignTokenRepository
    {
        public static int Tokens { get; private set; }

        /// <summary>このキャンペーンで初期値を配り終えたか</summary>
        public static bool IsInitialized { get; private set; }

        public static void Initialize(int tokens)
        {
            Tokens = tokens < 0 ? 0 : tokens;
            IsInitialized = true;
        }

        /// <summary>報酬などで増やす。0以下は無視する</summary>
        public static void Add(int amount)
        {
            if (amount <= 0) return;
            // 桁あふれしないように上限で止める
            Tokens = Tokens > int.MaxValue - amount ? int.MaxValue : Tokens + amount;
        }

        /// <summary>購入画面から戻ってきた残りで置き換える</summary>
        public static void Set(int tokens)
        {
            Tokens = tokens < 0 ? 0 : tokens;
        }

        /// <summary>キャンペーンを抜ける（次に始めたときに初期値を配り直す）</summary>
        public static void Clear()
        {
            Tokens = 0;
            IsInitialized = false;
        }
    }
}
