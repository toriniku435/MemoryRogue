namespace Live2DCardRogue.Common.Conversation
{
    // 会話をどこまで読んだか。行の数だけを知っていて、次へ進めると読み終わったかを返す（UnityEngine に依存しない）
    public sealed class ConversationRunner
    {
        private readonly int _count;

        public ConversationRunner(int count)
        {
            _count = count < 0 ? 0 : count;
            Index = 0;
        }

        /// <summary>今出している行（0始まり）</summary>
        public int Index { get; private set; }

        /// <summary>最後まで読んだか（行が無ければ最初から読み終わり）</summary>
        public bool IsFinished => Index >= _count;

        /// <summary>次の行へ進む。まだ行があれば true、読み終わったら false</summary>
        public bool Next()
        {
            if (IsFinished) return false;
            Index++;
            return !IsFinished;
        }
    }
}
