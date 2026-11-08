using System;

namespace Live2DCardRogue.Common.Tutorial
{
    // チュートリアルの手を先へ進める出来事の名前。手順（TutorialStep._eventName）はこの名前で待つ
    public static class TutorialEventNames
    {
        /// <summary>コマンドを実行リストに積んだ</summary>
        public const string CardQueued = "CardQueued";
        /// <summary>実行ボタンを押した</summary>
        public const string ExecuteRequested = "ExecuteRequested";
        /// <summary>プレイヤーのターンが始まった（引き終わったあと）</summary>
        public const string PlayerTurnStarted = "PlayerTurnStarted";
        /// <summary>敵のターンが始まった（プレイヤーのコマンドを実行し終えた）</summary>
        public const string EnemyTurnStarted = "EnemyTurnStarted";
    }

    // 出来事の通知口。ゲームの側はチュートリアルがあるかを知らず、ここへ名前を投げるだけ
    // （投げる役は各シーンの小さな橋渡しのクラスが持ち、ゲームの本体には入れない）
    public static class TutorialEvents
    {
        public static event Action<string> OnRaised;

        public static void Raise(string eventName)
        {
            OnRaised?.Invoke(eventName);
        }
    }
}
