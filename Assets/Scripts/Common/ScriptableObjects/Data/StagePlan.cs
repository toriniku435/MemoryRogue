using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Conversation;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // マップの1マス分の中身: 誰と戦い、どれだけ強く、勝敗で何を受け取り、勝った後に何を話すか。
    // ストーリーは StageData（アセット）から、エンドレスは EndlessBalance がシードと階層から組む。
    // マップはこの形だけを見るので、ステージがアセットか乱数かを知らない
    public sealed class StagePlan
    {
        /// <summary>手前から順の敵の編成（null は含まない）</summary>
        public EnemyData[] Enemies { get; }

        public EnemyStrength Strength { get; }

        /// <summary>勝ってマップに戻った直後に流す会話（無ければ null）</summary>
        public ConversationData ClearConversation { get; }

        /// <summary>戦闘中に流す BGM</summary>
        public BgmId Bgm { get; }

        private readonly StageReward _victoryReward;
        private readonly StageReward _defeatReward;

        public StagePlan(EnemyData[] enemies, EnemyStrength strength, StageReward victoryReward, StageReward defeatReward, ConversationData clearConversation = null, BgmId bgm = BgmId.Battle)
        {
            Enemies = enemies ?? new EnemyData[0];
            Strength = strength ?? EnemyStrength.None;
            _victoryReward = victoryReward ?? new StageReward(true, 0, null);
            _defeatReward = defeatReward ?? new StageReward(false, 0, null);
            ClearConversation = clearConversation;
            Bgm = bgm;
        }

        public StageReward CreateReward(bool isVictory)
        {
            return isVictory ? _victoryReward : _defeatReward;
        }
    }
}
