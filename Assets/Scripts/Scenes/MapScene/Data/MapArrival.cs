using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.MapScene.Data
{
    // マップに着いたときの状況（戦闘の結果を反映した後）。マップの画面と、報酬や会話の流れがこれを見て決まる
    public sealed class MapArrival
    {
        public static readonly MapArrival None = new MapArrival(null, null, null, false);

        /// <summary>勝って進んできたなら、移動の演出のための1つ前の部屋。進んでいなければ null</summary>
        public int? ArrivedFromIndex { get; }

        /// <summary>受け取る報酬（負けたときは支援）。戦闘から戻ってきたのでなければ null</summary>
        public StageReward Reward { get; }

        /// <summary>勝ってきたステージ（報酬の前に流す会話を引く）。勝っていなければ null</summary>
        public StagePlan ClearedStage { get; }

        /// <summary>最後のステージに勝った（報酬は無く、会話のあとエンディング / リザルトへ）</summary>
        public bool CompletedNow { get; }

        public MapArrival(int? arrivedFromIndex, StageReward reward, StagePlan clearedStage, bool completedNow)
        {
            ArrivedFromIndex = arrivedFromIndex;
            Reward = reward;
            ClearedStage = clearedStage;
            CompletedNow = completedNow;
        }
    }
}
