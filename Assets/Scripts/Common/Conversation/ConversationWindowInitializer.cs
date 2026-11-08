using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Common.Conversation
{
    // 会話パート（ConversationWindow.prefab）のコンポジションルート
    public sealed class ConversationWindowInitializer : WindowInitializerBase<ConversationInitData, ConversationPresenter>
    {
        [SerializeField] private ConversationWindowView _view;

        protected override ConversationPresenter Initialize(ConversationInitData data)
        {
            return new ConversationPresenter(_view);
        }
    }
}
