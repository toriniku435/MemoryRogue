namespace Live2DCardRogue.Common.WindowSystem
{
    // 会話パート。流す会話は開くときに ConversationShowData で渡す
    public sealed class ConversationInitData : WindowInitData<ConversationShowData>
    {
        public override WindowId Id => WindowId.Conversation;
    }
}
