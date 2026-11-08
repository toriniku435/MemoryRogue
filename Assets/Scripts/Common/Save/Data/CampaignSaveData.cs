using System;

namespace Live2DCardRogue.Common.Save
{
    // 中断したキャンペーンの進捗（campaign.json）。JsonUtility で読み書きするので、公開フィールドだけで持つ。
    // コマンドはアセットの参照ではなく CardData.Id で持ち、読み込むときに CardCatalog から引き直す
    [Serializable]
    public sealed class CampaignSaveData
    {
        /// <summary>中身の形の版。形を変えたら上げ、古い版の読み方を CampaignSaveService に足す</summary>
        // 2: purchasedSingles（シングル購入の記録）を足した。1 の中身はそのまま読める（無いものは空）
        // 3: decks / activeDeckIndex（デッキを複数持てるようにした）を足した。2 以前は deck を1番目のデッキとして読む
        // 4: episodeIndex（遊んでいるエピソード）を足した。3 以前には無い（0 = エピソード1 として読める）
        // 5: isEndless / endlessSeed（エンドレスの進捗）を足した。4 以前はストーリーしか無かったので false のままでよい
        // 6: challengeType / challengeLayerCount（エンドレスがチャレンジの1つになった）を足した。5 のエンドレスはエンドレス（終わり無し）として読む
        public const int CurrentVersion = 6;

        public int version = CurrentVersion;

        /// <summary>保存した日時（ISO 8601）。再開の確認に出すだけ</summary>
        public string savedAt;

        /// <summary>遊んでいるエピソード（0始まり）。版 4 から。それより前はエピソード1しか無かったので 0 のままでよい</summary>
        public int episodeIndex;

        /// <summary>チャレンジの進捗か（チャレンジは endless.sav に書く）。版 5 から。名前はエンドレスしか無かったころのまま</summary>
        public bool isEndless;

        /// <summary>チャレンジの敵の編成を組むシード。版 5 から</summary>
        public int endlessSeed;

        /// <summary>どのチャレンジか（ChallengeType の名前）。版 6 から。空ならエンドレス</summary>
        public string challengeType = "";

        /// <summary>チャレンジの階層数（0 は終わり無し）。版 6 から</summary>
        public int challengeLayerCount;

        public int stageIndex;

        /// <summary>保存した時点のステージ数。タイトルで「LAYER 04 / 10」と出すため（タイトルはキャンペーンのアセットを持たない）。エンドレスは 0</summary>
        public int stageCount;

        public int backupBoostCount;
        public int tokens;
        public int maxHp;
        public int maxMemory;
        /// <summary>アクティブな（戦闘で使う）デッキ。版 2 以前はこれだけを持っていた（版 3 でも同じものを入れておく）</summary>
        public string[] deck = new string[0];

        /// <summary>デッキの一覧（空のデッキも並ぶ）。版 3 から</summary>
        public SavedDeck[] decks = new SavedDeck[0];

        /// <summary>アクティブなデッキの番号（decks の添字）。版 3 から</summary>
        public int activeDeckIndex;

        public string[] ownedCards = new string[0];

        /// <summary>マーケットのシングル購入で買ったコマンド（1種類あたりの上限を数えるため）。版 1 には無い</summary>
        public string[] purchasedSingles = new string[0];
    }

    /// <summary>デッキ1つ（JsonUtility は配列の配列を書けないので、クラスに包む）</summary>
    [Serializable]
    public sealed class SavedDeck
    {
        public string[] cards = new string[0];
    }
}
