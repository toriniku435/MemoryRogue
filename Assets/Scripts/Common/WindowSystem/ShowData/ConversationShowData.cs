using System;
using Live2DCardRogue.Common.Conversation;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 会話のウインドウを開くときに渡すもの: 流す会話と、読み終えたら呼ぶもの
    public sealed class ConversationShowData : WindowShowData
    {
        public ConversationData Data { get; }
        public Action OnFinished { get; }

        public ConversationShowData(ConversationData data, Action onFinished)
        {
            Data = data;
            OnFinished = onFinished;
        }
    }
}
