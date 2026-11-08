using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Conversation
{
    // 会話パート1回分（Assets/ScriptableObjects/Conversations/ に置く）。上から順に1行ずつ出し、クリックで次へ進む。
    // どの会話をいつ流すかは、エピソード（CampaignData の開始時の会話）とステージ（StageData のクリア後の会話）が持つ。
    // 中身はこのアセットを書き換えるだけで差し替えられる（コードには会話の文は無い）
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Campaign/Conversation")]
    public sealed class ConversationData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private ConversationLine[] _lines = new ConversationLine[0];

        public string Id => _id;

        public IReadOnlyList<ConversationLine> Lines => _lines ?? new ConversationLine[0];

        /// <summary>流すものがあるか（行が1つも無ければ流さない）</summary>
        public bool HasLines => _lines != null && _lines.Length > 0;
    }

    // 会話の1行（話し手と、その台詞）
    [Serializable]
    public sealed class ConversationLine
    {
        [SerializeField] private string _speaker;
        [SerializeField][TextArea(2, 5)] private string _text;

        public string Speaker => _speaker;
        public string Text => _text;
    }
}
